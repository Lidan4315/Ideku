using Ideku.Data.Repositories;
using Ideku.Services.Workflow;
using Ideku.Services.IdeaRelation;
using Ideku.Services.Idea;
using Ideku.Services.ApprovalToken;
using Ideku.ViewModels.Approval;
using Ideku.ViewModels.DTOs;
using Ideku.Extensions;
using Ideku.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ideku.Controllers
{
    [Authorize]
    [ModuleAuthorize("approval")]
    public class ApprovalController : Controller
    {
        private readonly IWorkflowService _workflowService;
        private readonly IUserRepository _userRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly ILookupRepository _lookupRepository;
        private readonly IIdeaRelationService _ideaRelationService;
        private readonly IIdeaService _ideaService;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<ApprovalController> _logger;
        private readonly IApprovalTokenService _approvalTokenService;
        private readonly IIdeaRepository _ideaRepository;

        public ApprovalController(
            IWorkflowService workflowService,
            IUserRepository userRepository,
            IWorkflowRepository workflowRepository,
            ILookupRepository lookupRepository,
            IIdeaRelationService ideaRelationService,
            IIdeaService ideaService,
            IWebHostEnvironment hostEnvironment,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ApprovalController> logger,
            IApprovalTokenService approvalTokenService,
            IIdeaRepository ideaRepository)
        {
            _workflowService = workflowService;
            _userRepository = userRepository;
            _workflowRepository = workflowRepository;
            _lookupRepository = lookupRepository;
            _ideaRelationService = ideaRelationService;
            _ideaService = ideaService;
            _hostEnvironment = hostEnvironment;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _approvalTokenService = approvalTokenService;
            _ideaRepository = ideaRepository;
        }

        // GET: /Approval or /Approval/Index
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? selectedDivision = null,
            string? selectedDepartment = null,
            int? selectedCategory = null,
            int? selectedStage = null,
            string? selectedStatus = null)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Challenge(); // Or redirect to login
            }

            // Validate and normalize pagination parameters
            pageSize = PaginationHelper.ValidatePageSize(pageSize);
            page = Math.Max(1, page);

            // Get user info for ViewBag
            var user = await _userRepository.GetByUsernameAsync(username);

            // Get base queryable with role-based filtering already applied
            var ideasQuery = await _workflowService.GetPendingApprovalsQueryAsync(username);

            // Apply additional filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                ideasQuery = ideasQuery.Where(i => 
                    i.IdeaCode.Contains(searchTerm) ||
                    i.IdeaName.Contains(searchTerm) ||
                    (i.InitiatorUser != null && i.InitiatorUser.Name.Contains(searchTerm))
                );
            }

            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                ideasQuery = ideasQuery.Where(i => i.ToDivisionId == selectedDivision);
            }

            if (!string.IsNullOrWhiteSpace(selectedDepartment))
            {
                ideasQuery = ideasQuery.Where(i => i.ToDepartmentId == selectedDepartment);
            }

            if (selectedCategory.HasValue)
            {
                ideasQuery = ideasQuery.Where(i => i.CategoryId == selectedCategory.Value);
            }

            if (selectedStage.HasValue)
            {
                ideasQuery = ideasQuery.Where(i => i.CurrentStage == selectedStage.Value);
            }

            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                ideasQuery = ideasQuery.Where(i => i.CurrentStatus == selectedStatus);
            }

            // Apply pagination - this executes the database queries
            var pagedResult = await ideasQuery.ToPagedResultAsync(page, pageSize);

            // Get lookup data for dropdowns
            var divisions = await _lookupRepository.GetDivisionsAsync();
            var categories = await _lookupRepository.GetCategoriesAsync();

            // Get available stages from database
            var stages = await _ideaService.GetAvailableStagesAsync();

            // Get available statuses from database
            var statuses = await _ideaService.GetAvailableStatusesAsync();

            var viewModel = new ApprovalListViewModel
            {
                PagedIdeas = pagedResult,
                SearchTerm = searchTerm,
                SelectedDivision = selectedDivision,
                SelectedDepartment = selectedDepartment,
                SelectedCategory = selectedCategory,
                SelectedStage = selectedStage,
                SelectedStatus = selectedStatus,
                AvailableStages = stages.Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = $"Stage S{s}"
                }).ToList(),
                StatusOptions = statuses
            };

            // Pass lookup data to view
            ViewBag.Divisions = divisions;
            ViewBag.Categories = categories;
            ViewBag.UserRole = user?.Role?.RoleName ?? "";

            return View(viewModel);
        }

        // AJAX endpoint for real-time filtering with pagination support
        [HttpGet]
        public async Task<IActionResult> FilterIdeas(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? selectedDivision = null,
            string? selectedDepartment = null,
            int? selectedCategory = null,
            int? selectedStage = null,
            string? selectedStatus = null)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            // Validate pagination parameters
            pageSize = PaginationHelper.ValidatePageSize(pageSize);
            page = Math.Max(1, page);

            // Get base queryable with role-based filtering
            var ideasQuery = await _workflowService.GetPendingApprovalsQueryAsync(username);

            // Apply filters (same logic as Index method)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                ideasQuery = ideasQuery.Where(i => 
                    i.IdeaCode.Contains(searchTerm) ||
                    i.IdeaName.Contains(searchTerm) ||
                    (i.InitiatorUser != null && i.InitiatorUser.Name.Contains(searchTerm))
                );
            }

            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                ideasQuery = ideasQuery.Where(i => i.ToDivisionId == selectedDivision);
            }

            if (!string.IsNullOrWhiteSpace(selectedDepartment))
            {
                ideasQuery = ideasQuery.Where(i => i.ToDepartmentId == selectedDepartment);
            }

            if (selectedCategory.HasValue)
            {
                ideasQuery = ideasQuery.Where(i => i.CategoryId == selectedCategory.Value);
            }

            if (selectedStage.HasValue)
            {
                ideasQuery = ideasQuery.Where(i => i.CurrentStage == selectedStage.Value);
            }

            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                ideasQuery = ideasQuery.Where(i => i.CurrentStatus == selectedStatus);
            }

            // Apply pagination
            var pagedResult = await ideasQuery.ToPagedResultAsync(page, pageSize);
            
            // Return JSON with paginated results
            return Json(new { 
                success = true, 
                ideas = pagedResult.Items.Select(i => new {
                    ideaCode = i.IdeaCode,
                    ideaName = i.IdeaName,
                    initiatorName = i.InitiatorUser?.Name,
                    divisionName = i.TargetDivision?.NameDivision,
                    departmentName = i.TargetDepartment?.NameDepartment,
                    categoryName = i.Category?.CategoryName,
                    eventName = i.Event?.EventName,
                    currentStage = i.CurrentStage,
                    savingCost = i.SavingCost,
                    currentStatus = i.CurrentStatus,
                    submittedDate = i.SubmittedDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    reviewUrl = Url.Action("Review", new { id = i.Id })
                }),
                pagination = new {
                    currentPage = pagedResult.Page,
                    pageSize = pagedResult.PageSize,
                    totalCount = pagedResult.TotalCount,
                    totalPages = pagedResult.TotalPages,
                    hasPrevious = pagedResult.HasPrevious,
                    hasNext = pagedResult.HasNext,
                    firstItemIndex = pagedResult.FirstItemIndex,
                    lastItemIndex = pagedResult.LastItemIndex
                }
            });
        }


        // AJAX endpoint for cascading dropdown - Get departments by division
        [HttpGet]
        public async Task<JsonResult> GetDepartmentsByDivision(string divisionId)
        {
            if (string.IsNullOrWhiteSpace(divisionId))
            {
                return Json(new { success = true, departments = new List<object>() });
            }

            try
            {
                // Use real data from LookupRepository
                var departmentSelectList = await _lookupRepository.GetDepartmentsByDivisionAsync(divisionId);
                
                // Convert SelectListItem to simple object for JSON
                var departments = departmentSelectList
                    .Where(d => !string.IsNullOrEmpty(d.Value)) // Filter out empty options
                    .Select(d => new { id = d.Value, name = d.Text })
                    .ToList();

                return Json(new { success = true, departments });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error loading departments" });
            }
        }

        // GET: /Approval/Review/{id}
        public async Task<IActionResult> Review(int id)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Challenge();
            }

            var ideaForReview = await _workflowService.GetIdeaForReview(id, username);
            if (ideaForReview == null)
            {
                return NotFound(); // Or a custom access denied page
            }

            // Determine if user can actually approve/reject this idea
            var user = await _userRepository.GetByUsernameAsync(username);
            bool canTakeAction = false;

            if (user?.Role?.RoleName == "Superuser" && ideaForReview.CurrentStatus.StartsWith("Waiting Approval"))
            {
                canTakeAction = true;
            }
            else if (ideaForReview.CurrentStatus.StartsWith("Waiting Approval"))
            {
                // For all roles (including Workstream Leader): Check if user is designated approver for the next stage
                // No more hardcoded logic - all roles use dynamic workflow configuration
                var approversForNextStage = await _workflowService.GetApproversForNextStageAsync(ideaForReview);
                canTakeAction = approversForNextStage.Any(a => a.Id == user.Id);
            }

            // Get workflow history for this idea
            var workflowHistory = await _workflowRepository.GetByIdeaIdAsync(ideaForReview.Id);

            // Get available divisions for related divisions dropdown
            var availableDivisions = await _ideaRelationService.GetAvailableDivisionsAsync(ideaForReview.Id);

            var viewModel = new ApprovalReviewViewModel
            {
                Idea = ideaForReview,
                // Stage-based auto-fill: Stage 0 = empty, Stage 1+ = from previous approver
                ValidatedSavingCost = ideaForReview.CurrentStage == 0 ? null : ideaForReview.SavingCostValidated,
                AvailableDivisions = availableDivisions.Select(d => new SelectListItem
                {
                    Value = d.Id,
                    Text = d.NameDivision
                }).ToList()
            };

            // Pass action capability and workflow history to view
            ViewBag.CanTakeAction = canTakeAction;
            ViewBag.UserRole = user?.Role?.RoleName ?? "";
            ViewBag.WorkflowHistory = workflowHistory;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, ApprovalReviewViewModel viewModel)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Challenge();
            }

            // Clean up ModelState for approval validation
            CleanupModelStateForApprove();

            // Validate file requirement
            var ideaForValidation = await _workflowService.GetIdeaForReview(id, username);
            if (ideaForValidation != null)
            {
                var hasInitiatorFiles = !string.IsNullOrEmpty(ideaForValidation.AttachmentFiles);
                var hasApprovalFiles = viewModel.ApprovalFiles?.Any() == true;

                if (!hasInitiatorFiles && !hasApprovalFiles)
                {
                    ModelState.AddModelError("ApprovalFiles", "Files are required since initiator did not upload any files");
                }
            }

            if (ModelState.IsValid)
            {
                // Get current user for approval process
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found. Please try again.";
                    return RedirectToAction(nameof(Review), new { id });
                }

                // Get idea untuk check current stage
                var ideaForApproval = await _workflowService.GetIdeaForReview(id, username);
                if (ideaForApproval == null)
                {
                    TempData["ErrorMessage"] = "Idea not found or access denied.";
                    return RedirectToAction(nameof(Index));
                }

                // Create approval data DTO
                var approvalData = new ApprovalProcessDto
                {
                    IdeaId = id,
                    ValidatedSavingCost = viewModel.ValidatedSavingCost.Value,
                    ApprovalComments = viewModel.ApprovalComments,
                    RelatedDivisions = viewModel.SelectedRelatedDivisions ?? new List<string>(),
                    ApprovedBy = user.Id
                };

                // Process approval files first
                if (viewModel.ApprovalFiles?.Any() == true)
                {
                    await _workflowService.SaveApprovalFilesAsync(id, viewModel.ApprovalFiles, ideaForApproval.CurrentStage + 1);
                }

                // Process approval database operations
                var result = await _workflowService.ProcessApprovalDatabaseAsync(approvalData);
                
                if (result.IsSuccess)
                {
                    // Send email notifications in background (non-blocking)
                    SendApprovalEmailInBackground(approvalData);
                    
                    TempData["SuccessMessage"] = result.SuccessMessage;
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction(nameof(Review), new { id });
                }
            }

            // If model state is invalid, reload the review page
            var reloadedIdea = await _workflowService.GetIdeaForReview(id, username);
            if (reloadedIdea == null)
            {
                return NotFound();
            }

            // Reload available divisions for dropdown
            var availableDivisions = await _ideaRelationService.GetAvailableDivisionsAsync(reloadedIdea.Id);
            viewModel.Idea = reloadedIdea;
            viewModel.AvailableDivisions = availableDivisions.Select(d => new SelectListItem
            {
                Value = d.Id,
                Text = d.NameDivision
            }).ToList();

            return View("Review", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, ApprovalReviewViewModel viewModel)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Challenge();
            }

            // Clean up ModelState for rejection validation
            CleanupModelStateForReject();

            if (ModelState.IsValid)
            {
                var rejectionReason = viewModel.RejectionReason ?? "No reason provided";

                // Process rejection database operations only (fast)
                await _workflowService.ProcessRejectionDatabaseAsync((long)id, username, rejectionReason);

                // Send rejection notification in background (non-blocking)
                SendRejectionEmailInBackground((long)id, username, rejectionReason);

                TempData["SuccessMessage"] = "Idea has been successfully rejected!";
                return RedirectToAction(nameof(Index));
            }

            // If model state is invalid, reload the review page
            var reloadedIdeaForReject = await _workflowService.GetIdeaForReview(id, username);
            if (reloadedIdeaForReject == null)
            {
                return NotFound();
            }
            viewModel.Idea = reloadedIdeaForReject;
            return View("Review", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendFeedback(int id, ApprovalReviewViewModel viewModel)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Challenge();
            }

            // Clean up ModelState for feedback validation
            CleanupModelStateForFeedback();

            if (ModelState.IsValid)
            {
                var feedbackComment = viewModel.FeedbackComment ?? "No feedback provided";

                // Process feedback database operations only (fast)
                await _workflowService.ProcessFeedbackAsync((long)id, username, feedbackComment);

                // Send feedback notification in background (non-blocking)
                SendFeedbackEmailInBackground((long)id, username, feedbackComment);

                TempData["SuccessMessage"] = "Feedback has been successfully sent!";
                return RedirectToAction(nameof(Index));
            }

            // If model state is invalid, reload the review page
            var reloadedIdeaForFeedback = await _workflowService.GetIdeaForReview(id, username);
            if (reloadedIdeaForFeedback == null)
            {
                return NotFound();
            }
            viewModel.Idea = reloadedIdeaForFeedback;
            return View("Review", viewModel);
        }

        /// Helper method to clean up ModelState for rejection validation
        private void CleanupModelStateForReject()
        {
            ModelState.Remove("Idea");
            ModelState.Remove("ValidatedSavingCost");
            ModelState.Remove("ApprovalComments");
            ModelState.Remove("FeedbackComment");
        }

        /// Helper method to clean up ModelState for feedback validation
        private void CleanupModelStateForFeedback()
        {
            ModelState.Remove("Idea");
            ModelState.Remove("ValidatedSavingCost");
            ModelState.Remove("ApprovalComments");
            ModelState.Remove("RejectionReason");
        }


        /// Helper method to clean up ModelState for approval validation
        private void CleanupModelStateForApprove()
        {
            ModelState.Remove("Idea");
            ModelState.Remove("RejectionReason");
            ModelState.Remove("FeedbackComment");
        }

        private void SendApprovalEmailInBackground(ApprovalProcessDto approvalData)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting background approval email process for idea {IdeaId}", approvalData.IdeaId);

                    using var scope = _serviceScopeFactory.CreateScope();
                    var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();

                    await workflowService.SendApprovalNotificationsAsync(approvalData);
                    
                    _logger.LogInformation("Background approval email sent successfully for idea {IdeaId}", approvalData.IdeaId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send background approval email for idea {IdeaId}", approvalData.IdeaId);
                }
            });
        }

        private void SendRejectionEmailInBackground(long ideaId, string username, string reason)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting background rejection email process for idea {IdeaId}", ideaId);

                    using var scope = _serviceScopeFactory.CreateScope();
                    var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();

                    await workflowService.SendRejectionNotificationAsync(ideaId, username, reason);

                    _logger.LogInformation("Background rejection email sent successfully for idea {IdeaId}", ideaId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send background rejection email for idea {IdeaId}", ideaId);
                }
            });
        }

        private void SendFeedbackEmailInBackground(long ideaId, string username, string feedbackComment)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting background feedback email process for idea {IdeaId}", ideaId);

                    using var scope = _serviceScopeFactory.CreateScope();
                    var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();

                    await workflowService.SendFeedbackNotificationsAsync(ideaId, username, feedbackComment);

                    _logger.LogInformation("Background feedback email sent successfully for idea {IdeaId}", ideaId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send background feedback email for idea {IdeaId}", ideaId);
                }
            });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ApproveViaEmail(string token)
        {
            try
            {
                var tokenData = _approvalTokenService.ValidateAndDecryptToken(token);
                if (!tokenData.IsValid)
                {
                    TempData["ErrorMessage"] = tokenData.ErrorMessage;
                    return RedirectToAction(nameof(Index));
                }

                var idea = await _ideaRepository.GetByIdAsync(tokenData.IdeaId);
                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea tidak ditemukan";
                    return RedirectToAction(nameof(Index));
                }

                if (idea.CurrentStage != tokenData.Stage)
                {
                    TempData["ErrorMessage"] = "Link approval sudah tidak berlaku. Idea telah diproses sebelumnya.";
                    return RedirectToAction(nameof(Index));
                }

                if (!idea.CurrentStatus.StartsWith("Waiting Approval"))
                {
                    TempData["ErrorMessage"] = "Idea sudah diproses sebelumnya";
                    return RedirectToAction(nameof(Index));
                }

                var approver = await _userRepository.GetByIdAsync(tokenData.ApproverId);
                if (approver == null)
                {
                    TempData["ErrorMessage"] = "User tidak ditemukan";
                    return RedirectToAction(nameof(Index));
                }

                var workflowHistory = await _workflowRepository.GetByIdeaIdAsync(idea.Id);
                var alreadyProcessed = workflowHistory.Any(wh =>
                    wh.ActorUserId == tokenData.ApproverId &&
                    wh.FromStage == idea.CurrentStage);

                if (alreadyProcessed)
                {
                    TempData["ErrorMessage"] = "Anda sudah memproses idea ini sebelumnya";
                    return RedirectToAction(nameof(Index));
                }

                var approvalData = new ApprovalProcessDto
                {
                    IdeaId = tokenData.IdeaId,
                    ValidatedSavingCost = idea.SavingCost,
                    ApprovalComments = "Approved via email",
                    RelatedDivisions = new List<string>(),
                    ApprovedBy = tokenData.ApproverId
                };

                var result = await _workflowService.ProcessApprovalDatabaseAsync(approvalData);

                if (result.IsSuccess)
                {
                    SendApprovalEmailInBackground(approvalData);
                    TempData["SuccessMessage"] = $"Idea {idea.IdeaCode} - {idea.IdeaName} has been successfully approved via email!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ApproveViaEmail");
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memproses approval";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> RejectViaEmail(string token)
        {
            try
            {
                var tokenData = _approvalTokenService.ValidateAndDecryptToken(token);
                if (!tokenData.IsValid)
                {
                    TempData["ErrorMessage"] = tokenData.ErrorMessage;
                    return RedirectToAction(nameof(Index));
                }

                var idea = await _ideaRepository.GetByIdAsync(tokenData.IdeaId);
                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea tidak ditemukan";
                    return RedirectToAction(nameof(Index));
                }

                if (idea.CurrentStage != tokenData.Stage)
                {
                    TempData["ErrorMessage"] = "Link rejection sudah tidak berlaku. Idea telah diproses sebelumnya.";
                    return RedirectToAction(nameof(Index));
                }

                if (!idea.CurrentStatus.StartsWith("Waiting Approval"))
                {
                    TempData["ErrorMessage"] = "Idea sudah diproses sebelumnya";
                    return RedirectToAction(nameof(Index));
                }

                var rejector = await _userRepository.GetByIdAsync(tokenData.ApproverId);
                if (rejector == null)
                {
                    TempData["ErrorMessage"] = "User tidak ditemukan";
                    return RedirectToAction(nameof(Index));
                }

                var workflowHistory = await _workflowRepository.GetByIdeaIdAsync(idea.Id);
                var alreadyProcessed = workflowHistory.Any(wh =>
                    wh.ActorUserId == tokenData.ApproverId &&
                    wh.FromStage == idea.CurrentStage);

                if (alreadyProcessed)
                {
                    TempData["ErrorMessage"] = "Anda sudah memproses idea ini sebelumnya";
                    return RedirectToAction(nameof(Index));
                }

                await _workflowService.ProcessRejectionDatabaseAsync(idea.Id, rejector.Username, "Rejected via email");

                SendRejectionEmailInBackground(idea.Id, rejector.Username, "Rejected via email");

                TempData["SuccessMessage"] = $"Idea {idea.IdeaCode} - {idea.IdeaName} has been successfully rejected via email!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RejectViaEmail");
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memproses rejection";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
