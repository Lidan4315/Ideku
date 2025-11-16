using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services.Idea;
using Ideku.Services.Lookup;
using Ideku.Services.ChangeWorkflow;
using Ideku.Data.Repositories;
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
    [Authorize]
    [ModuleAuthorize("change_workflow")]
    public class ChangeWorkflowController : Controller
    {
        private readonly IIdeaService _ideaService;
        private readonly IIdeaRepository _ideaRepository;
        private readonly ILookupService _lookupService;
        private readonly IChangeWorkflowService _changeWorkflowService;
        private readonly IWorkflowManagementRepository _workflowManagementRepository;
        private readonly ILogger<ChangeWorkflowController> _logger;

        public ChangeWorkflowController(
            IIdeaService ideaService,
            IIdeaRepository ideaRepository,
            ILookupService lookupService,
            IChangeWorkflowService changeWorkflowService,
            IWorkflowManagementRepository workflowManagementRepository,
            ILogger<ChangeWorkflowController> logger)
        {
            _ideaService = ideaService;
            _ideaRepository = ideaRepository;
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
                // Validate and normalize pagination parameters
                pageSize = PaginationHelper.ValidatePageSize(pageSize);
                page = Math.Max(1, page);

                // Get all ideas using IdeaService (use "superuser" to get all ideas without role filtering)
                IQueryable<Idea> ideasQuery = await _ideaService.GetAllIdeasQueryAsync("superuser");

                // Apply filters using helper method
                ideasQuery = ApplyIdeasFilters(ideasQuery, searchTerm, selectedDivision, selectedDepartment,
                    selectedCategory, selectedWorkflow, selectedStatus);

                // Apply pagination - this executes the database queries
                var pagedResult = await ideasQuery.ToPagedResultAsync(page, pageSize);

                // Get lookup data for filters using LookupService
                var divisions = await _lookupService.GetDivisionsAsync();
                var categories = await _lookupService.GetCategoriesAsync();
                var workflows = await _workflowManagementRepository.GetAllWorkflowsAsync();

                var statuses = await _ideaService.GetAvailableStatusesAsync();

                var viewModel = new ChangeWorkflowViewModel
                {
                    PagedIdeas = pagedResult,

                    // Filter properties (preserve state)
                    SearchTerm = searchTerm,
                    SelectedDivision = selectedDivision,
                    SelectedDepartment = selectedDepartment,
                    SelectedCategory = selectedCategory,
                    SelectedWorkflow = selectedWorkflow,
                    SelectedStatus = selectedStatus,
                    StatusOptions = statuses
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
                return RedirectToAction("Index", "Home");
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
                // Validate and normalize pagination parameters
                pageSize = PaginationHelper.ValidatePageSize(pageSize);
                page = Math.Max(1, page);

                // Get all ideas using IdeaService (use "superuser" to get all ideas without role filtering)
                IQueryable<Idea> ideasQuery = await _ideaService.GetAllIdeasQueryAsync("superuser");

                // Apply filters using helper method
                ideasQuery = ApplyIdeasFilters(ideasQuery, searchTerm, selectedDivision, selectedDepartment,
                    selectedCategory, selectedWorkflow, selectedStatus);

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
                        savingCost = idea.SavingCost,
                        workflowId = idea.WorkflowId,
                        workflowName = idea.Workflow?.WorkflowName,
                        currentStage = idea.CurrentStage,
                        maxStage = idea.MaxStage,
                        currentStatus = idea.CurrentStatus,
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
    
        /// GET: Get departments by division for cascading dropdown
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

        /// POST: Update workflow for an idea
        [HttpPost]
        public async Task<IActionResult> UpdateWorkflow(long ideaId, int newWorkflowId)
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
                    return Json(new { success = false, message = "Cannot change workflow for inactive idea." });
                }

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

        #region Private Helper Methods

        /// <summary>
        /// Apply filters to ideas query
        /// Extracted to avoid code duplication between Index and FilterIdeas
        /// </summary>
        private IQueryable<Idea> ApplyIdeasFilters(
            IQueryable<Idea> query,
            string? searchTerm,
            string? selectedDivision,
            string? selectedDepartment,
            int? selectedCategory,
            int? selectedWorkflow,
            string? selectedStatus)
        {
            // Filter out deleted ideas
            query = query.Where(i => !i.IsDeleted);

            // Apply progressive filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(i =>
                    i.IdeaCode.Contains(searchTerm) ||
                    i.IdeaName.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                query = query.Where(i => i.ToDivisionId == selectedDivision);
            }

            if (!string.IsNullOrWhiteSpace(selectedDepartment))
            {
                query = query.Where(i => i.ToDepartmentId == selectedDepartment);
            }

            if (selectedCategory.HasValue)
            {
                query = query.Where(i => i.CategoryId == selectedCategory.Value);
            }

            if (selectedWorkflow.HasValue)
            {
                query = query.Where(i => i.WorkflowId == selectedWorkflow.Value);
            }

            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(i => i.CurrentStatus == selectedStatus);
            }

            // Apply ordering after all filters
            return query.OrderByDescending(i => i.SubmittedDate);
        }

        #endregion
    }
}
