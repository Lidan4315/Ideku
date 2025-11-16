using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services.Idea;
using Ideku.Services.Lookup;
using Ideku.Services.BypassStage;
using Ideku.Data.Repositories;
using Ideku.Data.Repositories.WorkflowManagement;
using Ideku.ViewModels.BypassStage;
using Ideku.Extensions;
using Ideku.Helpers;
using Ideku.Models.Entities;

namespace Ideku.Controllers
{
    [Authorize(Roles = "Superuser,Admin")]
    public class BypassStageController : Controller
    {
        private readonly IIdeaService _ideaService;
        private readonly ILookupService _lookupService;
        private readonly IBypassStageService _bypassStageService;
        private readonly IIdeaRepository _ideaRepository;
        private readonly IWorkflowManagementRepository _workflowManagementRepository;
        private readonly ILogger<BypassStageController> _logger;

        public BypassStageController(
            IIdeaService ideaService,
            ILookupService lookupService,
            IBypassStageService bypassStageService,
            IIdeaRepository ideaRepository,
            IWorkflowManagementRepository workflowManagementRepository,
            ILogger<BypassStageController> logger)
        {
            _ideaService = ideaService;
            _lookupService = lookupService;
            _bypassStageService = bypassStageService;
            _ideaRepository = ideaRepository;
            _workflowManagementRepository = workflowManagementRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? selectedDivision = null,
            string? selectedDepartment = null,
            int? selectedCategory = null,
            int? selectedWorkflow = null,
            string? selectedStatus = null)
        {
            try
            {
                pageSize = PaginationHelper.ValidatePageSize(pageSize);
                page = Math.Max(1, page);

                IQueryable<Idea> ideasQuery = await _ideaService.GetAllIdeasQueryAsync("superuser");
                // Show all ideas including inactive, but exclude deleted and approved
                ideasQuery = ideasQuery.Where(i => !i.IsDeleted && i.CurrentStatus != "Approved");

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    ideasQuery = ideasQuery.Where(i =>
                        i.IdeaCode.Contains(searchTerm) ||
                        i.IdeaName.Contains(searchTerm));
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

                if (selectedWorkflow.HasValue)
                {
                    ideasQuery = ideasQuery.Where(i => i.WorkflowId == selectedWorkflow.Value);
                }

                if (!string.IsNullOrWhiteSpace(selectedStatus))
                {
                    ideasQuery = ideasQuery.Where(i => i.CurrentStatus == selectedStatus);
                }

                ideasQuery = ideasQuery.OrderByDescending(i => i.SubmittedDate);

                var pagedResult = await ideasQuery.ToPagedResultAsync(page, pageSize);

                var divisions = await _lookupService.GetDivisionsAsync();
                var categories = await _lookupService.GetCategoriesAsync();
                var workflows = await _workflowManagementRepository.GetAllWorkflowsAsync();

                var statuses = await _ideaService.GetAvailableStatusesAsync();

                var viewModel = new BypassStageViewModel
                {
                    PagedIdeas = pagedResult,
                    SearchTerm = searchTerm,
                    SelectedDivision = selectedDivision,
                    SelectedDepartment = selectedDepartment,
                    SelectedCategory = selectedCategory,
                    SelectedWorkflow = selectedWorkflow,
                    SelectedStatus = selectedStatus,
                    StatusOptions = statuses
                };

                ViewBag.Divisions = divisions;
                ViewBag.Categories = categories;
                ViewBag.Workflows = workflows.Select(w => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = w.Id.ToString(),
                    Text = w.WorkflowName
                }).ToList();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bypass stage index");
                TempData["ErrorMessage"] = "Error loading ideas. Please try again.";
                return RedirectToAction("Index", "Settings");
            }
        }

        [HttpGet]
        public async Task<IActionResult> FilterIdeas(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? selectedDivision = null,
            string? selectedDepartment = null,
            int? selectedCategory = null,
            int? selectedWorkflow = null,
            string? selectedStatus = null)
        {
            try
            {
                pageSize = PaginationHelper.ValidatePageSize(pageSize);
                page = Math.Max(1, page);

                IQueryable<Idea> ideasQuery = await _ideaService.GetAllIdeasQueryAsync("superuser");
                // Show all ideas including inactive, but exclude deleted and approved
                ideasQuery = ideasQuery.Where(i => !i.IsDeleted && i.CurrentStatus != "Approved");

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    ideasQuery = ideasQuery.Where(i =>
                        i.IdeaCode.Contains(searchTerm) ||
                        i.IdeaName.Contains(searchTerm));
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

                if (selectedWorkflow.HasValue)
                {
                    ideasQuery = ideasQuery.Where(i => i.WorkflowId == selectedWorkflow.Value);
                }

                if (!string.IsNullOrWhiteSpace(selectedStatus))
                {
                    ideasQuery = ideasQuery.Where(i => i.CurrentStatus == selectedStatus);
                }

                ideasQuery = ideasQuery.OrderByDescending(i => i.SubmittedDate);

                var pagedResult = await ideasQuery.ToPagedResultAsync(page, pageSize);

                return Json(new
                {
                    success = true,
                    ideas = pagedResult.Items.Select(idea => new
                    {
                        id = idea.Id,
                        ideaCode = idea.IdeaCode,
                        ideaName = idea.IdeaName,
                        initiatorName = idea.InitiatorUser?.Employee?.NAME,
                        divisionName = idea.TargetDivision?.NameDivision,
                        departmentName = idea.TargetDepartment?.NameDepartment,
                        categoryName = idea.Category?.CategoryName,
                        eventName = idea.Event?.EventName,
                        workflowId = idea.WorkflowId,
                        workflowName = idea.Workflow?.WorkflowName,
                        currentStage = idea.CurrentStage,
                        maxStage = idea.MaxStage,
                        currentStatus = idea.CurrentStatus,
                        savingCost = idea.SavingCost,
                        submittedDate = idea.SubmittedDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        isInactive = idea.IsRejected && idea.CurrentStatus == "Inactive"
                    }),
                    pagination = new
                    {
                        page = pagedResult.Page,
                        pageSize = pagedResult.PageSize,
                        totalCount = pagedResult.TotalCount,
                        totalPages = pagedResult.TotalPages,
                        hasItems = pagedResult.HasItems,
                        showPagination = pagedResult.ShowPagination,
                        firstItemIndex = pagedResult.FirstItemIndex,
                        lastItemIndex = pagedResult.LastItemIndex,
                        hasPrevious = pagedResult.HasPrevious,
                        hasNext = pagedResult.HasNext
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering ideas");
                return Json(new { success = false, message = "Error loading filtered ideas." });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetDepartmentsByDivision(string divisionId)
        {
            if (string.IsNullOrWhiteSpace(divisionId))
            {
                return Json(new { success = true, departments = new List<object>() });
            }

            try
            {
                var departments = await _lookupService.GetDepartmentsByDivisionForAjaxAsync(divisionId);
                return Json(new { success = true, departments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading departments for division {DivisionId}", divisionId);
                return Json(new { success = false, message = "Error loading departments" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetAvailableStages(long ideaId)
        {
            try
            {
                var idea = await _ideaRepository.GetByIdAsync(ideaId);
                if (idea == null)
                {
                    return Json(new { success = false, message = "Idea not found" });
                }

                var stages = new List<object>();

                // Backward stages (stages before current stage, including Stage 0)
                for (int i = 0; i < idea.CurrentStage; i++)
                {
                    stages.Add(new
                    {
                        value = i,
                        text = $"Stage {i} (Backward)",
                        isBackward = true
                    });
                }

                // Forward stages (stages after current stage)
                for (int i = idea.CurrentStage + 1; i <= idea.MaxStage; i++)
                {
                    stages.Add(new
                    {
                        value = i,
                        text = i == idea.MaxStage ? $"Stage {i} (Approved)" : $"Stage {i}",
                        isBackward = false
                    });
                }

                return Json(new { success = true, stages, currentStage = idea.CurrentStage, maxStage = idea.MaxStage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available stages for idea {IdeaId}", ideaId);
                return Json(new { success = false, message = "Error loading stages" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BypassStage(long ideaId, int targetStage, string reason)
        {
            try
            {
                var username = User.Identity?.Name ?? "Unknown";

                // Validate idea is not inactive
                var idea = await _ideaRepository.GetByIdAsync(ideaId);
                if (idea == null)
                {
                    return Json(new { success = false, message = "Idea not found." });
                }

                if (idea.IsRejected && idea.CurrentStatus == "Inactive")
                {
                    return Json(new { success = false, message = "Cannot bypass stage for inactive idea." });
                }

                var result = await _bypassStageService.BypassStageAsync(ideaId, targetStage, reason, username);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    newStatus = result.NewStatus
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bypassing stage for idea {IdeaId}", ideaId);
                return Json(new { success = false, message = "An unexpected error occurred while bypassing stage." });
            }
        }
    }
}
