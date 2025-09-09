using Ideku.Data.Repositories;
using Ideku.Data.Context;
using Ideku.Models.Entities;
using Ideku.Services.Notification;
using Ideku.Services.WorkflowManagement;
using Ideku.Services.IdeaRelation;
using Ideku.ViewModels.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ideku.Services.Workflow
{
    public class WorkflowService : IWorkflowService
    {
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly IIdeaRepository _ideaRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IWorkflowManagementService _workflowManagementService;
        private readonly IIdeaRelationService _ideaRelationService;
        private readonly ILogger<WorkflowService> _logger;

        public WorkflowService(
            INotificationService notificationService,
            IUserRepository userRepository, 
            IIdeaRepository ideaRepository, 
            IWorkflowRepository workflowRepository, 
            IWorkflowManagementService workflowManagementService,
            IIdeaRelationService ideaRelationService,
            ILogger<WorkflowService> logger)
        {
            _notificationService = notificationService;
            _userRepository = userRepository;
            _ideaRepository = ideaRepository;
            _workflowRepository = workflowRepository;
            _workflowManagementService = workflowManagementService;
            _ideaRelationService = ideaRelationService;
            _logger = logger;
        }

        public async Task InitiateWorkflowAsync(Models.Entities.Idea idea)
        {
            _logger.LogInformation("Initiating workflow for idea {IdeaId} - {IdeaName}", idea.Id, idea.IdeaName);
            
            try
            {
                var approvers = await GetApproversForNextStageAsync(idea);
                
                if (!approvers.Any())
                {
                    _logger.LogWarning("No approvers found for idea {IdeaId} at next stage", idea.Id);
                    return;
                }
                
                // Send notification to approvers using NotificationService
                await _notificationService.NotifyIdeaSubmitted(idea, approvers);
                _logger.LogInformation("Workflow initiated successfully for Idea {IdeaId} - notification sent to {ApproverCount} approvers", 
                    idea.Id, approvers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send workflow initiation notification for Idea {IdeaId}", idea.Id);
                throw; // Re-throw to maintain error handling contract
            }
        }


        public async Task<IEnumerable<Models.Entities.Idea>> GetPendingApprovalsForUserAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return new List<Models.Entities.Idea>();
            }

            // Get all ideas that this user can see based on their role and approval status
            if (user.Role.RoleName == "Superuser")
            {
                // Superuser can see all ideas regardless of stage or status
                return await _ideaRepository.GetAllIdeasForApprovalAsync();
            }
            else if (user.Role.RoleName == "Workstream Leader")
            {
                // Workstream Leader can see:
                // 1. Ideas waiting for their approval (stage 0 -> S1)
                // 2. Ideas they have processed (approved/rejected)
                // 3. Ideas that have moved beyond their stage
                
                var allIdeas = await _ideaRepository.GetAllIdeasForApprovalAsync();
                
                // Filter to show ideas relevant to Workstream Leader:
                // - Ideas at stage 0 waiting for S1 approval (their responsibility)
                // - Ideas that were processed at stage 0 (their history)
                return allIdeas.Where(idea => 
                    (idea.CurrentStage == 0 && idea.CurrentStatus == "Waiting Approval S1") || // Current responsibility
                    (idea.CurrentStage >= 1) || // Ideas that have passed their stage
                    (idea.CurrentStatus.StartsWith("Rejected S0")) // Ideas they rejected
                ).ToList();
            }

            // Return empty list if the user is not a designated approver
            return new List<Models.Entities.Idea>();
        }

        public async Task<IQueryable<Models.Entities.Idea>> GetPendingApprovalsQueryAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return Enumerable.Empty<Models.Entities.Idea>().AsQueryable();
            }

            // Get base queryable for ideas with all necessary includes
            var baseQuery = _ideaRepository.GetQueryableWithIncludes();

            // Apply role-based filtering using the same logic as GetPendingApprovalsForUserAsync
            if (user.Role.RoleName == "Superuser")
            {
                // Superuser can see all ideas regardless of stage or status
                return baseQuery.Where(idea => 
                    idea.CurrentStatus.StartsWith("Waiting Approval") ||
                    idea.CurrentStatus.StartsWith("Rejected S") ||
                    idea.CurrentStatus == "Approved")
                    .OrderByDescending(idea => idea.SubmittedDate)
                    .ThenByDescending(idea => idea.Id);
            }
            else if (user.Role.RoleName == "Workstream Leader")
            {
                // Workstream Leader can see:
                // - Ideas at stage 0 waiting for S1 approval (their responsibility)
                // - Ideas that were processed at stage 0 (their history)
                return baseQuery.Where(idea => 
                    (idea.CurrentStage == 0 && idea.CurrentStatus == "Waiting Approval S1") || // Current responsibility
                    (idea.CurrentStage >= 1) || // Ideas that have passed their stage
                    (idea.CurrentStatus.StartsWith("Rejected S0")) // Ideas they rejected
                ).OrderByDescending(idea => idea.SubmittedDate)
                .ThenByDescending(idea => idea.Id);
            }

            // Return empty queryable if the user is not a designated approver
            return Enumerable.Empty<Models.Entities.Idea>().AsQueryable();
        }

        public async Task<Models.Entities.Idea> GetIdeaForReview(int ideaId, string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                _logger.LogWarning("No user found with username {Username} when trying to review idea {IdeaId}", username, ideaId);
                return null;
            }

            var idea = await _ideaRepository.GetByIdAsync(ideaId);
            if (idea == null)
            {
                _logger.LogWarning("No idea found with ID {IdeaId}", ideaId);
                return null;
            }

            bool canView = false;
            
            // Superuser can view anything
            if (user.Role.RoleName == "Superuser")
            {
                canView = true;
            }
            else
            {
                // Check if user is an approver for the current stage or has history with this idea
                var approversForCurrentStage = await GetApproversForNextStageAsync(idea);
                
                // User can view if:
                // 1. They are an approver for the current stage that needs approval
                // 2. They have previously acted on this idea (check workflow history)
                canView = approversForCurrentStage.Any(a => a.Id == user.Id);

                if (!canView)
                {
                    // Check if user has history with this idea (previously approved/rejected)
                    var workflowHistory = await _workflowRepository.GetWorkflowHistoryForIdeaAsync(ideaId);
                    canView = workflowHistory.Any(wh => wh.ActorUserId == user.Id);
                }
            }

            if (!canView)
            {
                _logger.LogWarning("User {Username} is not authorized to view idea {IdeaId} at its current stage and status.", username, ideaId);
                return null;
            }

            return idea;
        }

        public async Task ProcessApprovalAsync(long ideaId, string username, string? comments, long? validatedSavingCost)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            var idea = await _ideaRepository.GetByIdAsync(ideaId);

            if (user == null || idea == null)
            {
                _logger.LogError("User or Idea not found when processing approval for IdeaId {IdeaId}", ideaId);
                return;
            }

            // Authorization check: User must be authorized approver for the target stage
            var authorizedApprovers = await GetApproversForNextStageAsync(idea);
            
            bool isAuthorized = user.Role.RoleName == "Superuser" || 
                               authorizedApprovers.Any(a => a.Id == user.Id);

            if (!isAuthorized)
            {
                _logger.LogWarning("User {Username} with role {RoleName} is not authorized to approve idea {IdeaId} at target stage {TargetStage}", 
                    username, user.Role.RoleName, ideaId, idea.CurrentStage + 1);
                return;
            }

            var previousStage = idea.CurrentStage;
            var nextStage = idea.CurrentStage + 1;
            
            // Check if we can advance to next stage (validate against MaxStage)
            if (nextStage > idea.MaxStage)
            {
                // Already at final stage - mark as approved/completed
                idea.CurrentStatus = "Approved";
                idea.CompletedDate = DateTime.Now;
                _logger.LogInformation("Idea {IdeaId} completed approval process - reached final stage {MaxStage}", ideaId, idea.MaxStage);
            }
            else
            {
                // Advance to next stage
                idea.CurrentStage = nextStage;
                
                // Set status based on whether this is the final stage or not
                if (nextStage == idea.MaxStage)
                {
                    idea.CurrentStatus = "Approved"; // Final approval
                    idea.CompletedDate = DateTime.Now;
                }
                else
                {
                    idea.CurrentStatus = $"Waiting Approval S{nextStage + 1}";
                }
                
                _logger.LogInformation("Idea {IdeaId} advanced from stage {FromStage} to stage {ToStage}, max stage is {MaxStage}", 
                    ideaId, previousStage, idea.CurrentStage, idea.MaxStage);
            }
            
            idea.UpdatedDate = DateTime.Now;
            
            if (validatedSavingCost.HasValue)
            {
                idea.SavingCostValidated = validatedSavingCost.Value;
            }

            // Add a record to WorkflowHistory
            var workflowHistory = new Models.Entities.WorkflowHistory
            {
                IdeaId = ideaId,
                ActorUserId = user.Id,
                FromStage = previousStage,
                ToStage = nextStage <= idea.MaxStage ? nextStage : (int?)null, // null if completed
                Action = "Approved",
                Comments = comments,
                Timestamp = DateTime.Now
            };

            // Save to WorkflowRepository and update idea
            await _workflowRepository.CreateAsync(workflowHistory);
            await _ideaRepository.UpdateAsync(idea);

            // Send approval confirmation email to initiator
            try
            {
                await _notificationService.NotifyIdeaApproved(idea, user);
                _logger.LogInformation("Approval confirmation email sent to initiator {InitiatorEmail} for Idea {IdeaId}", 
                    idea.InitiatorUser.Employee.EMAIL, ideaId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to send approval confirmation email for Idea {IdeaId}", ideaId);
            }

            // If idea moved to next stage (not completed), send emails to next stage approvers
            if (nextStage <= idea.MaxStage && idea.CurrentStatus != "Approved")
            {
                try
                {
                    var nextStageApprovers = await GetApproversForNextStageAsync(idea);
                    
                    if (nextStageApprovers.Any())
                    {
                        await _notificationService.NotifyIdeaSubmitted(idea, nextStageApprovers);
                        _logger.LogInformation("Next stage approval emails sent for Idea {IdeaId} to {ApproverCount} approvers", 
                            ideaId, nextStageApprovers.Count);
                    }
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Failed to send next stage approval emails for Idea {IdeaId}", ideaId);
                }
            }

            // Log successful approval
            _logger.LogInformation("Idea {IdeaId} successfully approved from S{FromStage} to S{ToStage} by {Username}. Status: {Status}", 
                ideaId, previousStage, nextStage, username, idea.CurrentStatus);
        }





        private async Task SendApprovalNotificationToInitiatorAsync(Models.Entities.Idea idea)
        {
            try
            {
                if (idea.InitiatorUser?.Employee?.EMAIL == null)
                {
                    _logger.LogWarning("No email found for idea {IdeaId} initiator", idea.Id);
                    return;
                }

                // Need to create a dummy user for NotifyIdeaApproved - in this case it's a general approval notification
                var systemUser = new Models.Entities.User 
                { 
                    Name = "System", 
                    Role = new Models.Entities.Role { RoleName = "System" } 
                };

                await _notificationService.NotifyIdeaApproved(idea, systemUser);
                _logger.LogInformation("Approval notification sent to initiator {Email} for idea {IdeaId}", 
                    idea.InitiatorUser.Employee.EMAIL, idea.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send approval notification to initiator for idea {IdeaId}", idea.Id);
                // Don't throw - notification failure shouldn't break the approval process
            }
        }


        public async Task<WorkflowResult> ProcessApprovalDatabaseAsync(ApprovalProcessDto approvalData)
        {
            try
            {
                _logger.LogInformation("Processing approval database operations for idea {IdeaId} by user {UserId}", 
                    approvalData.IdeaId, approvalData.ApprovedBy);

                var user = await _userRepository.GetByIdAsync(approvalData.ApprovedBy);
                if (user == null)
                {
                    _logger.LogError("User {UserId} not found during approval processing", approvalData.ApprovedBy);
                    return WorkflowResult.Failure("User not found");
                }

                var idea = await _ideaRepository.GetByIdAsync(approvalData.IdeaId);
                if (idea == null)
                {
                    _logger.LogError("Idea {IdeaId} not found", approvalData.IdeaId);
                    return WorkflowResult.Failure("Idea not found");
                }

                var previousStage = idea.CurrentStage;
                var nextStage = idea.CurrentStage + 1;
                
                if (nextStage > idea.MaxStage)
                {
                    idea.CurrentStatus = "Approved";
                    idea.CompletedDate = DateTime.Now;
                }
                else
                {
                    idea.CurrentStage = nextStage;
                    if (nextStage == idea.MaxStage)
                    {
                        idea.CurrentStatus = "Approved";
                        idea.CompletedDate = DateTime.Now;
                    }
                    else
                    {
                        idea.CurrentStatus = $"Waiting Approval S{nextStage + 1}";
                    }
                }
                
                idea.UpdatedDate = DateTime.Now;
                idea.SavingCostValidated = approvalData.ValidatedSavingCost;

                var workflowHistory = new Models.Entities.WorkflowHistory
                {
                    IdeaId = approvalData.IdeaId,
                    ActorUserId = user.Id,
                    FromStage = previousStage,
                    ToStage = nextStage <= idea.MaxStage ? nextStage : (int?)null,
                    Action = "Approved",
                    Comments = approvalData.ApprovalComments,
                    Timestamp = DateTime.Now
                };

                await _workflowRepository.CreateAsync(workflowHistory);

                // Rename files to match new stage
                if (nextStage <= idea.MaxStage && previousStage != nextStage)
                {
                    await RenameFilesToNewStageAsync(approvalData.IdeaId, previousStage, nextStage);
                }

                await _ideaRepository.UpdateAsync(idea);

                if (approvalData.RelatedDivisions?.Any() == true)
                {
                    await _ideaRelationService.UpdateIdeaRelatedDivisionsAsync(
                        approvalData.IdeaId, 
                        approvalData.RelatedDivisions);
                }

                var finalIdea = await _ideaRepository.GetByIdAsync(approvalData.IdeaId);
                
                _logger.LogInformation("Successfully processed database approval for idea {IdeaId}. Status: {Status}", 
                    approvalData.IdeaId, finalIdea?.CurrentStatus);

                return WorkflowResult.Success(
                    approvalData.IdeaId, 
                    finalIdea?.CurrentStage ?? 0, 
                    finalIdea?.CurrentStatus ?? "Unknown",
                    "Idea approved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing approval database operations for idea {IdeaId}", 
                    approvalData.IdeaId);
                return WorkflowResult.Failure($"Error processing approval: {ex.Message}");
            }
        }

        public async Task SendApprovalNotificationsAsync(ApprovalProcessDto approvalData)
        {
            try
            {
                _logger.LogInformation("Sending approval notifications for idea {IdeaId}", approvalData.IdeaId);

                var user = await _userRepository.GetByIdAsync(approvalData.ApprovedBy);
                var idea = await _ideaRepository.GetByIdAsync(approvalData.IdeaId);

                if (user == null || idea == null) return;

                // Send approval notification to initiator
                await _notificationService.NotifyIdeaApproved(idea, user);
                _logger.LogInformation("Approval confirmation email sent to initiator {InitiatorEmail} for Idea {IdeaId}", 
                    idea.InitiatorUser.Employee.EMAIL, approvalData.IdeaId);

                // Send notifications to next stage approvers if needed
                if (idea.CurrentStage < idea.MaxStage && idea.CurrentStatus != "Approved")
                {
                    var nextStageApprovers = await GetApproversForNextStageAsync(idea);
                    
                    if (nextStageApprovers.Any())
                    {
                        await _notificationService.NotifyIdeaSubmitted(idea, nextStageApprovers);
                        _logger.LogInformation("Next stage approval emails sent for Idea {IdeaId} to {ApproverCount} approvers", 
                            approvalData.IdeaId, nextStageApprovers.Count);
                    }
                }

                // Notify related divisions
                if (approvalData.RelatedDivisions?.Any() == true)
                {
                    await _ideaRelationService.NotifyRelatedDivisionsAsync(idea, approvalData.RelatedDivisions);
                }

                _logger.LogInformation("Successfully sent all approval notifications for idea {IdeaId}", approvalData.IdeaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send approval notifications for idea {IdeaId}", approvalData.IdeaId);
                throw;
            }
        }

        public async Task ProcessRejectionDatabaseAsync(long ideaId, string username, string reason)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            var idea = await _ideaRepository.GetByIdAsync(ideaId);

            if (user == null || idea == null)
            {
                _logger.LogError("User or Idea not found when processing rejection for IdeaId {IdeaId}", ideaId);
                return;
            }

            idea.IsRejected = true;
            idea.RejectedReason = reason;
            idea.CurrentStatus = $"Rejected S{idea.CurrentStage}";
            idea.UpdatedDate = DateTime.Now;
            idea.CompletedDate = DateTime.Now;

            var workflowHistory = new Models.Entities.WorkflowHistory
            {
                IdeaId = ideaId,
                ActorUserId = user.Id,
                FromStage = idea.CurrentStage,
                ToStage = null,
                Action = "Rejected",
                Comments = reason,
                Timestamp = DateTime.Now
            };

            await _workflowRepository.CreateAsync(workflowHistory);
            await _ideaRepository.UpdateAsync(idea);

            _logger.LogInformation("Successfully processed database rejection for idea {IdeaId}", ideaId);
        }

        public async Task SendRejectionNotificationAsync(long ideaId, string username, string reason)
        {
            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);
                var idea = await _ideaRepository.GetByIdAsync(ideaId);

                if (user == null || idea == null) return;

                await _notificationService.NotifyIdeaRejected(idea, user, reason);
                _logger.LogInformation("Rejection notification email sent to initiator {InitiatorEmail} for Idea {IdeaId}", 
                    idea.InitiatorUser.Employee.EMAIL, ideaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send rejection notification email for Idea {IdeaId}", ideaId);
                throw;
            }
        }

        public async Task SaveApprovalFilesAsync(long ideaId, List<IFormFile> files, int stage)
        {
            var idea = await _ideaRepository.GetByIdAsync(ideaId);
            if (idea == null) return;

            var newFilePaths = await HandleFileUploadsAsync(files, idea.IdeaCode, stage, true);
            
            if (newFilePaths.Any())
            {
                var existingFiles = string.IsNullOrEmpty(idea.AttachmentFiles) 
                    ? new List<string>() 
                    : idea.AttachmentFiles.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

                existingFiles.AddRange(newFilePaths);
                idea.AttachmentFiles = string.Join(";", existingFiles);
                
                await _ideaRepository.UpdateAsync(idea);
                
                _logger.LogInformation("Added {FileCount} approval files to idea {IdeaId} for stage {Stage}", 
                    newFilePaths.Count, ideaId, stage);
            }
        }


        private async Task<List<string>> HandleFileUploadsAsync(List<IFormFile> files, string ideaCode, int stage, bool isApprovalFile = false)
        {
            var filePaths = new List<string>();
            if (files == null || !files.Any()) return filePaths;

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "ideas");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Get existing files count for sequential numbering (ALL files from this idea)
            var ideaPattern = $"{ideaCode}_";
            var existingFiles = Directory.GetFiles(uploadsPath)
                .Where(f => Path.GetFileName(f).StartsWith(ideaPattern))
                .ToList();

            int fileCounter = existingFiles.Count + 1;

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".jpg", ".jpeg", ".png" };
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        throw new InvalidOperationException($"File type {fileExtension} is not allowed");
                    }

                    if (file.Length > 10 * 1024 * 1024)
                    {
                        throw new InvalidOperationException($"File {file.FileName} is too large. Maximum size is 10MB");
                    }

                    var fileName = $"{ideaCode}_S{stage}_{fileCounter:D3}{fileExtension}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    filePaths.Add($"uploads/ideas/{fileName}");
                    fileCounter++;

                    _logger.LogInformation("Uploaded {FileType} file {FileName} for idea {IdeaCode} at stage S{Stage}", 
                        isApprovalFile ? "approval" : "initiator", fileName, ideaCode, stage);
                }
            }

            return filePaths;
        }

        public async Task RenameFilesToNewStageAsync(long ideaId, int fromStage, int toStage)
        {
            var idea = await _ideaRepository.GetByIdAsync(ideaId);
            if (idea == null || string.IsNullOrEmpty(idea.AttachmentFiles)) return;

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "ideas");
            var filePaths = idea.AttachmentFiles.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            var updatedPaths = new List<string>();
            var renameOperations = new List<(string oldPath, string newPath)>();

            try
            {
                foreach (var filePath in filePaths)
                {
                    var fileName = Path.GetFileName(filePath);
                    
                    if (fileName.Contains($"_S{fromStage}_"))
                    {
                        var newFileName = fileName.Replace($"_S{fromStage}_", $"_S{toStage}_");
                        var oldFullPath = Path.Combine(uploadsPath, fileName);
                        var newFullPath = Path.Combine(uploadsPath, newFileName);
                        
                        if (File.Exists(oldFullPath))
                        {
                            renameOperations.Add((oldFullPath, newFullPath));
                            updatedPaths.Add($"uploads/ideas/{newFileName}");
                        }
                        else
                        {
                            updatedPaths.Add(filePath);
                        }
                    }
                    else
                    {
                        updatedPaths.Add(filePath);
                    }
                }

                foreach (var (oldPath, newPath) in renameOperations)
                {
                    File.Move(oldPath, newPath);
                    _logger.LogInformation("Renamed file from {OldPath} to {NewPath} for idea {IdeaId}", 
                        Path.GetFileName(oldPath), Path.GetFileName(newPath), ideaId);
                }

                idea.AttachmentFiles = string.Join(";", updatedPaths);
                await _ideaRepository.UpdateAsync(idea);

                _logger.LogInformation("Successfully renamed {Count} files from S{FromStage} to S{ToStage} for idea {IdeaId}", 
                    renameOperations.Count, fromStage, toStage, ideaId);
            }
            catch (Exception ex)
            {
                foreach (var (oldPath, newPath) in renameOperations.Where(op => File.Exists(op.newPath)))
                {
                    try
                    {
                        if (File.Exists(oldPath)) File.Delete(oldPath);
                        File.Move(newPath, oldPath);
                    }
                    catch
                    {
                        _logger.LogError("Failed to rollback file rename: {NewPath} -> {OldPath}", newPath, oldPath);
                    }
                }

                _logger.LogError(ex, "Failed to rename files from S{FromStage} to S{ToStage} for idea {IdeaId}", 
                    fromStage, toStage, ideaId);
                throw;
            }
        }

        private async Task<List<User>> GetApproversForNextStageAsync(Models.Entities.Idea idea)
        {
            return await GetApproversForStageAsync(idea, idea.CurrentStage + 1);
        }

        private async Task<List<User>> GetApproversForStageAsync(Models.Entities.Idea idea, int targetStage)
        {
            try
            {
                if (targetStage > idea.MaxStage)
                {
                    _logger.LogInformation("Idea {IdeaId} target stage {TargetStage} exceeds max stage {MaxStage}, no approvers needed", 
                        idea.Id, targetStage, idea.MaxStage);
                    return new List<User>();
                }
                
                var approvers = await _workflowManagementService.GetApproversForWorkflowStageAsync(
                    idea.WorkflowId, targetStage, idea.ToDivisionId, idea.ToDepartmentId);
                
                _logger.LogInformation("Found {ApproverCount} approvers for idea {IdeaId} at stage {Stage}", 
                    approvers.Count(), idea.Id, targetStage);
                
                return approvers.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get approvers for stage {TargetStage} for idea {IdeaId}", targetStage, idea.Id);
                return new List<User>();
            }
        }
    }
}
