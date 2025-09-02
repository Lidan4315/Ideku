using Ideku.Data.Repositories;
using Ideku.Services.Workflow;
using Ideku.Services.IdeaRelation;
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
    public class ApprovalController : Controller
    {
        private readonly IWorkflowService _workflowService;
        private readonly IUserRepository _userRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly ILookupRepository _lookupRepository;
        private readonly IIdeaRelationService _ideaRelationService;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<ApprovalController> _logger;

        public ApprovalController(
            IWorkflowService workflowService, 
            IUserRepository userRepository, 
            IWorkflowRepository workflowRepository, 
            ILookupRepository lookupRepository,
            IIdeaRelationService ideaRelationService,
            IWebHostEnvironment hostEnvironment,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ApprovalController> logger)
        {
            _workflowService = workflowService;
            _userRepository = userRepository;
            _workflowRepository = workflowRepository;
            _lookupRepository = lookupRepository;
            _ideaRelationService = ideaRelationService;
            _hostEnvironment = hostEnvironment;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
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
            
            var viewModel = new ApprovalListViewModel
            {
                PagedIdeas = pagedResult,
                SearchTerm = searchTerm,
                SelectedDivision = selectedDivision,
                SelectedDepartment = selectedDepartment,
                SelectedCategory = selectedCategory,
                SelectedStage = selectedStage,
                SelectedStatus = selectedStatus
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
            else if (user?.Role?.RoleName == "Workstream Leader" && 
                     ideaForReview.CurrentStage == 0 && 
                     ideaForReview.CurrentStatus == "Waiting Approval S1")
            {
                canTakeAction = true;
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
                    // Always use validated saving cost from approver input when provided
                    ValidatedSavingCost = viewModel.ValidatedSavingCost.HasValue && viewModel.ValidatedSavingCost > 0
                        ? viewModel.ValidatedSavingCost.Value
                        : ideaForApproval.SavingCost,
                    ApprovalComments = viewModel.ApprovalComments,
                    RelatedDivisions = viewModel.SelectedRelatedDivisions ?? new List<string>(),
                    ApprovedBy = user.Id
                };

                // Process approval database operations only (fast)
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

        /// <summary>
        /// Helper method to clean up ModelState for rejection validation
        /// </summary>
        private void CleanupModelStateForReject()
        {
            ModelState.Remove("Idea");
            ModelState.Remove("ValidatedSavingCost");
            ModelState.Remove("ApprovalComments");
        }

        /// <summary>
        /// Download attachment file
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(string filename, int ideaId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Challenge();
            }

            // Validate user can access this idea
            var ideaForDownload = await _workflowService.GetIdeaForReview(ideaId, username);
            if (ideaForDownload == null)
            {
                return NotFound("Idea not found or access denied");
            }

            // Sanitize filename to prevent path traversal
            var sanitizedFilename = Path.GetFileName(filename);
            if (string.IsNullOrEmpty(sanitizedFilename))
            {
                return BadRequest("Invalid filename");
            }

            // Construct file path
            var filePath = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "ideas", sanitizedFilename);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found");
            }

            try
            {
                // Read file and determine content type
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var contentType = GetContentType(sanitizedFilename);

                // Return file with download headers (forces download)
                return File(fileBytes, contentType, sanitizedFilename);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error reading file");
            }
        }

        /// <summary>
        /// View attachment file inline (for popup preview)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ViewAttachment(string filename, int ideaId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Challenge();
            }

            // Validate user can access this idea
            var ideaForView = await _workflowService.GetIdeaForReview(ideaId, username);
            if (ideaForView == null)
            {
                return NotFound("Idea not found or access denied");
            }

            // Sanitize filename to prevent path traversal
            var sanitizedFilename = Path.GetFileName(filename);
            if (string.IsNullOrEmpty(sanitizedFilename))
            {
                return BadRequest("Invalid filename");
            }

            // Construct file path
            var filePath = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "ideas", sanitizedFilename);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found");
            }

            try
            {
                // Read file and determine content type
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var contentType = GetContentType(sanitizedFilename);

                // Return file for inline viewing (no attachment header)
                return File(fileBytes, contentType);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error reading file");
            }
        }

        /// <summary>
        /// Get MIME content type based on file extension
        /// </summary>
        private string GetContentType(string filename)
        {
            var extension = Path.GetExtension(filename).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Helper method to clean up ModelState for approval validation  
        /// </summary>
        private void CleanupModelStateForApprove()
        {
            ModelState.Remove("Idea");
            ModelState.Remove("RejectionReason");
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
    }
}
