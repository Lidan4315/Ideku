using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services.Idea;
using Ideku.Services.Lookup;
using Ideku.Services.ChangeWorkflow;
using Ideku.Data.Repositories.WorkflowManagement;
using Ideku.ViewModels.ChangeWorkflow;
using Ideku.Extensions;
using Ideku.Helpers;
using Ideku.Models.Entities;

namespace Ideku.Controllers
{
    /// <summary>
    /// Controller for Change Workflow operations
    /// Allows admins to change workflow assignments for existing ideas
    /// Follows the same pattern as UserManagementController for consistency
    /// </summary>
    [Authorize(Roles = "Superuser,Admin")]
    public class ChangeWorkflowController : Controller
    {
        private readonly IIdeaService _ideaService;
        private readonly ILookupService _lookupService;
        private readonly IChangeWorkflowService _changeWorkflowService;
        private readonly IWorkflowManagementRepository _workflowManagementRepository;
        private readonly ILogger<ChangeWorkflowController> _logger;

        public ChangeWorkflowController(
            IIdeaService ideaService,
            ILookupService lookupService,
            IChangeWorkflowService changeWorkflowService,
            IWorkflowManagementRepository workflowManagementRepository,
            ILogger<ChangeWorkflowController> logger)
        {
            _ideaService = ideaService;
            _lookupService = lookupService;
            _changeWorkflowService = changeWorkflowService;
            _workflowManagementRepository = workflowManagementRepository;
            _logger = logger;
        }

        /// <summary>
        /// GET: Change Workflow Index page with pagination
        /// Same pattern as UserManagementController.Index
        /// </summary>
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
                // Validate and normalize pagination parameters (same as UserManagement)
                pageSize = PaginationHelper.ValidatePageSize(pageSize);
                page = Math.Max(1, page);

                // Get all ideas using IdeaService (reuse existing service)
                // Use "superuser" to get all ideas without role filtering
                IQueryable<Idea> ideasQuery = await _ideaService.GetAllIdeasQueryAsync("superuser");

                // Filter out deleted ideas
                ideasQuery = ideasQuery.Where(i => !i.IsDeleted);

                // Apply progressive filters (same pattern as UserManagement)
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

                // Apply ordering after all filters
                ideasQuery = ideasQuery.OrderByDescending(i => i.SubmittedDate);

                // Apply pagination - this executes the database queries
                var pagedResult = await ideasQuery.ToPagedResultAsync(page, pageSize);

                // Get lookup data for filters using LookupService
                var divisions = await _lookupService.GetDivisionsAsync();
                var categories = await _lookupService.GetCategoriesAsync();
                var workflows = await _workflowManagementRepository.GetAllWorkflowsAsync();

                var viewModel = new ChangeWorkflowViewModel
                {
                    PagedIdeas = pagedResult,

                    // Filter properties (preserve state)
                    SearchTerm = searchTerm,
                    SelectedDivision = selectedDivision,
                    SelectedDepartment = selectedDepartment,
                    SelectedCategory = selectedCategory,
                    SelectedWorkflow = selectedWorkflow,
                    SelectedStatus = selectedStatus
                };

                // Pass lookup data to view
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
                _logger.LogError(ex, "Error loading change workflow index");
                TempData["ErrorMessage"] = "Error loading ideas. Please try again.";
                return RedirectToAction("Index", "Settings");
            }
        }

        /// <summary>
        /// GET: Filter ideas via AJAX (same pattern as UserManagementController.FilterUsers)
        /// Returns JSON with filtered ideas and pagination data
        /// </summary>
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
                // Same logic as Index method
                pageSize = PaginationHelper.ValidatePageSize(pageSize);
                page = Math.Max(1, page);

                // Get all ideas using IdeaService (reuse existing service)
                IQueryable<Idea> ideasQuery = await _ideaService.GetAllIdeasQueryAsync("superuser");
                ideasQuery = ideasQuery.Where(i => !i.IsDeleted);

                // Apply progressive filters
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

                // Apply ordering after all filters
                ideasQuery = ideasQuery.OrderByDescending(i => i.SubmittedDate);

                var pagedResult = await ideasQuery.ToPagedResultAsync(page, pageSize);

                // Return JSON response (same pattern as UserManagement)
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
                        submittedDate = idea.SubmittedDate.ToString("yyyy-MM-ddTHH:mm:ss")
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

        /// <summary>
        /// GET: Get departments by division for cascading dropdown
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetDepartmentsByDivision(string divisionId)
        {
            if (string.IsNullOrWhiteSpace(divisionId))
            {
                return Json(new { success = true, departments = new List<object>() });
            }

            try
            {
                // Use LookupService for consistency
                var departments = await _lookupService.GetDepartmentsByDivisionForAjaxAsync(divisionId);
                return Json(new { success = true, departments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading departments for division {DivisionId}", divisionId);
                return Json(new { success = false, message = "Error loading departments" });
            }
        }

        /// <summary>
        /// GET: Get available workflows for dropdown in change modal
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetAvailableWorkflows()
        {
            try
            {
                var workflows = await _workflowManagementRepository.GetAllWorkflowsAsync();
                var workflowList = workflows
                    .Where(w => w.IsActive)
                    .Select(w => new { id = w.Id, name = w.WorkflowName })
                    .ToList();

                return Json(new { success = true, workflows = workflowList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading workflows");
                return Json(new { success = false, message = "Error loading workflows" });
            }
        }

        /// <summary>
        /// POST: Update workflow for an idea
        /// Returns JSON response for modal handling
        /// Uses ChangeWorkflowService for business logic
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateWorkflow(long ideaId, int newWorkflowId)
        {
            try
            {
                var username = User.Identity?.Name ?? "Unknown";

                // Use service layer for business logic and validation
                var result = await _changeWorkflowService.UpdateIdeaWorkflowAsync(ideaId, newWorkflowId, username);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    workflowName = result.WorkflowName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workflow for idea {IdeaId}", ideaId);
                return Json(new { success = false, message = "An unexpected error occurred while updating workflow." });
            }
        }
    }
}
