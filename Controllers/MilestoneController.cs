using Ideku.Services.Milestone;
using Ideku.Services.Lookup;
using Ideku.Services.Idea;
using Ideku.Services.Workflow;
using Ideku.ViewModels.Milestone;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using Ideku.Helpers;

namespace Ideku.Controllers
{
    [Authorize]
    [ModuleAuthorize("milestone")]
    public class MilestoneController : Controller
    {
        private readonly IMilestoneService _milestoneService;
        private readonly ILookupService _lookupService;
        private readonly IIdeaService _ideaService;
        private readonly IWorkflowService _workflowService;
        private readonly ILogger<MilestoneController> _logger;

        public MilestoneController(
            IMilestoneService milestoneService,
            ILookupService lookupService,
            IIdeaService ideaService,
            IWorkflowService workflowService,
            ILogger<MilestoneController> logger)
        {
            _milestoneService = milestoneService;
            _lookupService = lookupService;
            _ideaService = ideaService;
            _workflowService = workflowService;
            _logger = logger;
        }

        // GET: /Milestone or /Milestone/Index
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
            try
            {
                var pagedIdeas = await _milestoneService.GetMilestoneEligibleIdeasAsync(
                    page, pageSize, searchTerm, selectedDivision, selectedDepartment, selectedCategory, selectedStage, selectedStatus);

                // Get lookup data for filters
                var divisions = await _lookupService.GetDivisionsAsync();
                var categories = await _lookupService.GetCategoriesAsync();

                // Get available stages from database (only S2 and above for milestone-eligible ideas)
                var stages = await _ideaService.GetAvailableStagesAsync();
                var milestoneStages = stages.Where(s => s >= 2).ToList();

                var viewModel = new MilestoneListViewModel
                {
                    PagedIdeas = pagedIdeas,
                    SearchTerm = searchTerm,
                    SelectedDivision = selectedDivision,
                    SelectedDepartment = selectedDepartment,
                    SelectedCategory = selectedCategory,
                    SelectedStage = selectedStage,
                    SelectedStatus = selectedStatus,
                    AvailableStages = milestoneStages.Select(s => new SelectListItem
                    {
                        Value = s.ToString(),
                        Text = $"Stage S{s}"
                    }).ToList()
                };

                // Pass lookup data to view
                ViewBag.Divisions = divisions;
                ViewBag.Categories = categories;

                if (!string.IsNullOrWhiteSpace(selectedDivision))
                {
                    ViewBag.Departments = await _lookupService.GetDepartmentsByDivisionAsync(selectedDivision);
                }

                // Get unique statuses for filter
                ViewBag.Statuses = pagedIdeas.Items
                    .Select(i => i.CurrentStatus)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading milestone list");
                TempData["ErrorMessage"] = "An error occurred while loading the milestone list.";
                return View(new MilestoneListViewModel());
            }
        }

        // GET: /Milestone/Detail/{ideaId}
        public async Task<IActionResult> Detail(long ideaId)
        {
            try
            {
                var idea = await _milestoneService.GetMilestoneEligibleIdeaByIdAsync(ideaId);
                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found or not eligible for milestone management.";
                    return RedirectToAction(nameof(Index));
                }

                var milestones = await _milestoneService.GetMilestonesByIdeaIdAsync(ideaId);
                var availablePICUsers = await _milestoneService.GetAvailablePICUsersAsync(ideaId);

                var viewModel = new MilestoneDetailViewModel
                {
                    Idea = idea,
                    Milestones = milestones,
                    AvailablePICUsers = availablePICUsers,
                    IsEligibleForMilestones = true // Already filtered in service
                };

                _logger.LogInformation("[MilestoneDetail] IdeaId={IdeaId}, CurrentStage={Stage}, IsMilestoneCreated={IsMilestoneCreated}, CurrentStatus={Status}",
                    ideaId, idea.CurrentStage, idea.IsMilestoneCreated, idea.CurrentStatus);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading milestone detail for idea {IdeaId}", ideaId);
                TempData["ErrorMessage"] = "An error occurred while loading the milestone details.";
                return RedirectToAction(nameof(Index));
            }
        }


        // AJAX: Create milestone from modal
        [HttpPost]
        public async Task<IActionResult> CreateMilestone(
            long ideaId,
            string title,
            string? note,
            DateTime startDate,
            DateTime endDate,
            string status,
            string creatorName,
            string creatorEmployeeId,
            List<long>? selectedPICUserIds = null)
        {
            try
            {
                var result = await _milestoneService.CreateMilestoneAsync(
                    ideaId, title, note, startDate, endDate, status, creatorName, creatorEmployeeId, selectedPICUserIds);

                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating milestone via AJAX for idea {IdeaId}", ideaId);
                return Json(new { success = false, message = "An error occurred while creating the milestone." });
            }
        }

        // POST: /Milestone/UpdateMilestone (AJAX)
        [HttpPost]
        public async Task<IActionResult> UpdateMilestone([FromBody] UpdateMilestoneRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid data provided." });
                }

                // Authorization handled by ModuleAuthorize attribute
                var result = await _milestoneService.UpdateMilestoneAsync(
                    request.MilestoneId,
                    request.Title,
                    request.Note,
                    request.StartDate,
                    request.EndDate,
                    request.Status,
                    request.SelectedPICUserIds);

                if (result.Success)
                {
                    return Json(new { success = true, message = "Milestone updated successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating milestone {MilestoneId}", request.MilestoneId);
                return Json(new { success = false, message = "An error occurred while updating the milestone." });
            }
        }

        // POST: /Milestone/DeleteMilestone (AJAX)
        [HttpPost]
        public async Task<IActionResult> DeleteMilestone(long milestoneId)
        {
            try
            {
                // Authorization handled by ModuleAuthorize attribute
                var result = await _milestoneService.DeleteMilestoneAsync(milestoneId);

                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting milestone {MilestoneId}", milestoneId);
                return Json(new { success = false, message = "An error occurred while deleting the milestone." });
            }
        }

        // POST: /Milestone/SendToStage3Approval
        [HttpPost]
        [ModuleAuthorize("milestone_send_approval")]
        public async Task<IActionResult> SendToStage3Approval(long ideaId)
        {
            try
            {
                // Authorization handled by ModuleAuthorize attribute

                // Get idea
                var idea = await _milestoneService.GetMilestoneEligibleIdeaByIdAsync(ideaId);
                if (idea == null)
                {
                    return Json(new { success = false, message = "Idea not found." });
                }

                // Validate: must be Stage 2
                if (idea.CurrentStage != 2)
                {
                    return Json(new { success = false, message = "Only Stage 2 ideas can be sent to Stage 3 approval." });
                }

                // Validate: must have milestone created
                if (!idea.IsMilestoneCreated)
                {
                    return Json(new { success = false, message = "Please create at least one milestone before requesting Stage 3 approval." });
                }

                // Validate: not already approved
                if (idea.CurrentStatus == "Approved")
                {
                    return Json(new { success = false, message = "This idea has already been fully approved." });
                }

                // Submit to next stage approval (S3)
                var result = await _workflowService.SubmitForNextStageApprovalAsync(ideaId, User.Identity!.Name!);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Idea {IdeaId} sent to Stage 3 approval by {Username}", ideaId, User.Identity!.Name);
                    return Json(new { success = true, message = result.SuccessMessage ?? "Idea has been successfully sent to Stage 3 approvers for review." });
                }
                else
                {
                    return Json(new { success = false, message = result.ErrorMessage ?? "Failed to send to Stage 3 approval." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending idea {IdeaId} to Stage 3 approval", ideaId);
                return Json(new { success = false, message = "An error occurred while sending to Stage 3 approval." });
            }
        }

        // AJAX endpoint for real-time filtering
        [HttpGet]
        public async Task<IActionResult> FilterAllIdeas(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? selectedDivision = null,
            string? selectedDepartment = null,
            int? selectedCategory = null,
            int? selectedStage = null,
            string? selectedStatus = null)
        {
            try
            {
                // Validate pagination parameters
                pageSize = Math.Max(5, Math.Min(100, pageSize));
                page = Math.Max(1, page);

                // Get milestone eligible ideas with filters
                var pagedResult = await _milestoneService.GetMilestoneEligibleIdeasAsync(
                    page, pageSize, searchTerm, selectedDivision, selectedDepartment, selectedCategory, selectedStage, selectedStatus);

                // Return JSON with paginated results
                return Json(new
                {
                    success = true,
                    ideas = pagedResult.Items.Select(i => new
                    {
                        ideaCode = i.IdeaCode,
                        ideaName = i.IdeaName,
                        initiatorName = i.InitiatorUser?.Employee?.NAME,
                        initiatorBadge = i.InitiatorUser?.Employee?.EMP_ID,
                        divisionName = i.TargetDivision?.NameDivision,
                        departmentName = i.TargetDepartment?.NameDepartment,
                        categoryName = i.Category?.CategoryName,
                        eventName = i.Event?.EventName,
                        currentStage = i.CurrentStage,
                        savingCost = i.SavingCost,
                        savingCostValidated = i.SavingCostValidated,
                        currentStatus = i.CurrentStatus,
                        implementators = i.IdeaImplementators.Select(impl => new
                        {
                            name = impl.User.Name,
                            role = impl.Role
                        }).ToList(),
                        submittedDate = i.SubmittedDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        detailUrl = Url.Action("Detail", new { ideaId = i.Id })
                    }),
                    pagination = new
                    {
                        currentPage = pagedResult.Page,
                        pageSize = pagedResult.PageSize,
                        totalCount = pagedResult.TotalCount,
                        totalItems = pagedResult.TotalCount,
                        totalPages = pagedResult.TotalPages,
                        hasPreviousPage = pagedResult.HasPrevious,
                        hasNextPage = pagedResult.HasNext,
                        hasPrevious = pagedResult.HasPrevious,
                        hasNext = pagedResult.HasNext,
                        firstItemIndex = pagedResult.FirstItemIndex,
                        lastItemIndex = pagedResult.LastItemIndex
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering milestone ideas");
                return Json(new { success = false, message = "Error loading data" });
            }
        }

        // AJAX: Get departments by division
        [HttpGet]
        public async Task<IActionResult> GetDepartmentsByDivision(string divisionId)
        {
            try
            {
                var departments = await _lookupService.GetDepartmentsByDivisionForAjaxAsync(divisionId);
                return Json(new { success = true, departments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting departments for division {DivisionId}", divisionId);
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    // Request model for AJAX UpdateMilestone
    public class UpdateMilestoneRequest
    {
        public long MilestoneId { get; set; }
        public long IdeaId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<long> SelectedPICUserIds { get; set; } = new();
    }
}