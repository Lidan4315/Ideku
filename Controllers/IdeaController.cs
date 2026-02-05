using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Ideku.Services.Idea;
using Ideku.ViewModels.MyIdeas;
using Ideku.Services.Workflow;
using Ideku.Services.IdeaImplementators;
using Ideku.Models;
using Ideku.Models.Entities;
using Ideku.Extensions;
using Ideku.Helpers;
using Ideku.Services.Lookup;
using Ideku.Services.Milestone;
using Ideku.Services.IdeaMonitoring;
using Ideku.Services.Notification;

namespace Ideku.Controllers
{
    [Authorize]
    public class IdeaController : Controller
    {
        private readonly IIdeaService _ideaService;
        private readonly IWorkflowService _workflowService;
        private readonly ILookupService _lookupService;
        private readonly IIdeaImplementatorService _implementatorService;
        private readonly IMilestoneService _milestoneService;
        private readonly IIdeaMonitoringService _monitoringService;
        private readonly INotificationCoordinatorService _notificationCoordinator;
        private readonly ILogger<IdeaController> _logger;

        public IdeaController(
            IIdeaService ideaService,
            IWorkflowService workflowService,
            ILookupService lookupService,
            IIdeaImplementatorService implementatorService,
            IMilestoneService milestoneService,
            IIdeaMonitoringService monitoringService,
            INotificationCoordinatorService notificationCoordinator,
            ILogger<IdeaController> logger)
        {
            _ideaService = ideaService;
            _workflowService = workflowService;
            _lookupService = lookupService;
            _milestoneService = milestoneService;
            _implementatorService = implementatorService;
            _monitoringService = monitoringService;
            _notificationCoordinator = notificationCoordinator;
            _logger = logger;
        }

        // GET: Idea/Create
        [ModuleAuthorize("idea_create")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Get current user for fallback
                var user = await _ideaService.GetUserByUsernameAsync(username);
                if (user == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Controller populates ViewModel (presentation concern)
                var viewModel = new CreateIdeaViewModel
                {
                    InitiatorUserId = user.EmployeeId, // Changed: Use EmployeeId (badge number)
                    BadgeNumber = "",
                    EmployeeName = "",
                    Position = "",
                    Email = "",
                    EmployeeId = "",

                    // Populate dropdown lists from LookupService
                    DivisionList = await _lookupService.GetDivisionsAsync(),
                    CategoryList = await _lookupService.GetCategoriesAsync(),
                    EventList = await _lookupService.GetEventsAsync(),
                    DepartmentList = await _lookupService.GetDepartmentsByDivisionAsync("")
                };

                return View(viewModel);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading form: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Idea/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ModuleAuthorize("idea_create")]
        public async Task<IActionResult> Create(CreateIdeaViewModel model)
        {
            // Validate badge number exists in EMPLIST first (before ModelState validation)
            var employee = await _ideaService.GetEmployeeByBadgeNumberAsync(model.BadgeNumber);
            if (employee == null)
            {
                return Json(new {
                    success = false,
                    message = "Employee with badge number not found"
                });
            }

            // Validate user account exists (before ModelState validation)
            var initiatorUser = await _ideaService.GetUserByEmployeeIdAsync(model.BadgeNumber);
            if (initiatorUser == null)
            {
                return Json(new {
                    success = false,
                    message = "Employee does not have a user account in the system. Please contact administrator."
                });
            }

            if (ModelState.IsValid)
            {

                // Controller maps ViewModel → Entity
                var idea = new Models.Entities.Idea
                {
                    InitiatorUserId = initiatorUser.EmployeeId, // Changed: FK to EmployeeId (badge number)
                    ToDivisionId = model.ToDivisionId,
                    ToDepartmentId = model.ToDepartmentId,
                    CategoryId = model.CategoryId,
                    EventId = model.EventId,
                    IdeaName = model.IdeaName,
                    IdeaIssueBackground = model.IdeaDescription,
                    IdeaSolution = model.Solution,
                    SavingCost = model.SavingCost ?? 0
                };

                // Service handles business logic
                var result = await _ideaService.CreateIdeaAsync(idea, model.AttachmentFiles);

                if (result.Success && result.CreatedIdea != null)
                {
                    _logger.LogInformation("Idea {IdeaId} - {IdeaName} created successfully by user {Username}",
                        result.CreatedIdea.Id, result.CreatedIdea.IdeaName, User.Identity?.Name);

                    // Send notification emails to next stage approvers in background
                    _notificationCoordinator.NotifyNextStageApproversInBackground(result.CreatedIdea.Id);

                    return Json(new {
                        success = true,
                        message = result.Message,
                        ideaCode = result.CreatedIdea.IdeaCode
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to create idea for user {Username}: {ErrorMessage}",
                        User.Identity?.Name, result.Message);

                    return Json(new {
                        success = false,
                        message = result.Message
                    });
                }
            }

            // Return validation errors as JSON
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new {
                success = false,
                message = "Please check your input",
                errors = errors
            });
        }

        // AJAX: Get Departments by Division
        [HttpGet]
        public async Task<JsonResult> GetDepartmentsByDivision(string divisionId)
        {
            var departments = await _lookupService.GetDepartmentsByDivisionForAjaxAsync(divisionId);
            return Json(departments);
        }

        // AJAX: Get Employee by Badge Number
        [HttpGet]
        public async Task<JsonResult> GetEmployeeByBadgeNumber(string badgeNumber)
        {
            var employee = await _ideaService.GetEmployeeByBadgeNumberAsync(badgeNumber);
            if (employee == null)
            {
                return Json(new { success = false, message = "Employee not found" });
            }
            return Json(new { success = true, data = employee });
        }

        // AJAX: Check if Idea Name exists
        [HttpGet]
        public async Task<JsonResult> CheckIdeaNameExists(string ideaName)
        {
            if (string.IsNullOrWhiteSpace(ideaName))
            {
                return Json(new { exists = false });
            }

            var exists = await _ideaService.IsIdeaNameExistsAsync(ideaName);
            return Json(new { exists = exists });
        }

        // GET: Idea/Index (My Ideas)
        [ModuleAuthorize("idea_list")]
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
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Validate and normalize pagination parameters
                pageSize = Ideku.Helpers.PaginationHelper.ValidatePageSize(pageSize);
                page = Math.Max(1, page);

                // Get base queryable for user's own ideas
                var user = await _ideaService.GetUserByUsernameAsync(username);
                if (user == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var ideasQuery = await _ideaService.GetUserIdeasAsync(username);

                // Apply filters
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

                // Get lookup data for filters
                var divisions = await _lookupService.GetDivisionsAsync();
                var categories = await _lookupService.GetCategoriesAsync();

                // Get available stages from database
                var stages = await _ideaService.GetAvailableStagesAsync();

                // Get available statuses from database
                var statuses = await _ideaService.GetAvailableStatusesAsync();

                var viewModel = new MyIdeasViewModel
                {
                    PagedIdeas = pagedResult,
                    SearchTerm = searchTerm,
                    SelectedDivision = selectedDivision,
                    SelectedDepartment = selectedDepartment,
                    SelectedCategory = selectedCategory,
                    SelectedStage = selectedStage,
                    SelectedStatus = selectedStatus,
                    AvailableStages = stages.Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = s.ToString(),
                        Text = $"Stage S{s}"
                    }).ToList(),
                    StatusOptions = statuses
                };

                // Pass lookup data to view
                ViewBag.Divisions = divisions;
                ViewBag.Categories = categories;

                return View(viewModel);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading ideas: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        // AJAX endpoint for real-time filtering
        [HttpGet]
        [ModuleAuthorize("idea_list")]
        public async Task<IActionResult> FilterMyIdeas(
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
            pageSize = Ideku.Helpers.PaginationHelper.ValidatePageSize(pageSize);
            page = Math.Max(1, page);

            // Get base queryable with role-based filtering
            var ideasQuery = await _ideaService.GetUserIdeasAsync(username);

            // Apply filters (same logic as Index method)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                ideasQuery = ideasQuery.Where(i => 
                    i.IdeaCode.Contains(searchTerm) ||
                    i.IdeaName.Contains(searchTerm)
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
                    divisionName = i.TargetDivision?.NameDivision,
                    departmentName = i.TargetDepartment?.NameDepartment,
                    categoryName = i.Category?.CategoryName,
                    eventName = i.Event?.EventName,
                    currentStage = i.CurrentStage,
                    savingCost = i.SavingCost,
                    savingCostValidated = i.SavingCostValidated,
                    currentStatus = i.CurrentStatus,
                    submittedDate = i.SubmittedDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    detailUrl = Url.Action("Details", new { id = i.Id })
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

        // AJAX endpoint for cascading dropdown
        [HttpGet]
        public async Task<JsonResult> GetDepartmentsByDivisionForMyIdeas(string divisionId)
        {
            if (string.IsNullOrWhiteSpace(divisionId))
            {
                return Json(new { success = true, departments = new List<object>() });
            }

            try
            {
                // Use real data from LookupService (returns List<object>)
                var departments = await _lookupService.GetDepartmentsByDivisionForAjaxAsync(divisionId);

                return Json(new { success = true, departments });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Idea/Details/{id}
        [ModuleAuthorize("idea_list")]
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Get the idea
                var ideasQuery = await _ideaService.GetUserIdeasAsync(username);
                var idea = await ideasQuery
                    .Where(i => i.Id == id)
                    .FirstOrDefaultAsync();

                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found or you don't have permission to view it";
                    return RedirectToAction("Index");
                }

                // Get implementators for the idea (view-only)
                var implementators = await _implementatorService.GetImplementatorsByIdeaIdAsync(id);

                // Get milestones for the idea
                var milestones = await _milestoneService.GetMilestonesByIdeaIdAsync(id);

                // Get workflow history for the idea
                var workflowHistory = await _workflowService.GetWorkflowHistoryByIdeaIdAsync(id);

                // Get monitoring data for the idea (read-only)
                var monitorings = await _monitoringService.GetMonitoringsByIdeaIdAsync(id);

                // Check permissions for monitoring (read-only for My Ideas page)
                var canEdit = await _monitoringService.CanEditCostSavingsAsync(id, username);
                var canValidate = await _monitoringService.CanValidateCostSavingsAsync(username);

                // Create view model with idea and all related data
                var viewModel = new MyIdeasDetailViewModel
                {
                    Idea = idea,
                    Implementators = implementators.ToList(),
                    Milestones = milestones.ToList(),
                    WorkflowHistory = workflowHistory,
                    Monitorings = monitorings.ToList(),
                    CanEditCostSavings = false, // Always false for My Ideas page (read-only)
                    CanValidateCostSavings = false // Always false for My Ideas page (read-only)
                };

                // Pass dropdown data to view for edit modal
                ViewBag.Divisions = await _lookupService.GetDivisionsAsync();
                ViewBag.Categories = await _lookupService.GetCategoriesAsync();
                ViewBag.Events = await _lookupService.GetEventsAsync();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading idea details: {IdeaId}", id);
                TempData["ErrorMessage"] = "Error loading idea details";
                return RedirectToAction("Index");
            }
        }

        // GET: Idea/Edit/{id}
        [ModuleAuthorize("idea_edit_delete")]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Get the idea
                var ideasQuery = await _ideaService.GetUserIdeasAsync(username);
                var idea = await ideasQuery
                    .Where(i => i.Id == id)
                    .FirstOrDefaultAsync();

                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found or you don't have permission to edit it";
                    return RedirectToAction("Index");
                }

                // Check if idea is deleted
                if (idea.IsDeleted)
                {
                    TempData["ErrorMessage"] = "Cannot edit deleted idea";
                    return RedirectToAction("Index");
                }

                // Map idea to EditIdeaViewModel
                var viewModel = new EditIdeaViewModel
                {
                    Id = idea.Id,
                    IdeaCode = idea.IdeaCode,
                    BadgeNumber = idea.InitiatorUser?.Employee?.EMP_ID ?? "",
                    EmployeeName = idea.InitiatorUser?.Employee?.NAME ?? "",
                    Position = idea.InitiatorUser?.Employee?.POSITION_TITLE ?? "",
                    Division = idea.InitiatorUser?.Employee?.DivisionNavigation?.NameDivision ?? "",
                    Department = idea.InitiatorUser?.Employee?.DepartmentNavigation?.NameDepartment ?? "",
                    Email = idea.InitiatorUser?.Employee?.EMAIL ?? "",
                    ToDivisionId = idea.ToDivisionId,
                    ToDepartmentId = idea.ToDepartmentId,
                    CategoryId = idea.CategoryId,
                    EventId = idea.EventId,
                    IdeaName = idea.IdeaName,
                    IdeaDescription = idea.IdeaIssueBackground,
                    Solution = idea.IdeaSolution,
                    SavingCost = idea.SavingCost,
                    ExistingAttachments = idea.AttachmentFiles,
                    InitiatorUserId = idea.InitiatorUserId,
                    EmployeeId = idea.InitiatorUser?.EmployeeId ?? "",

                    // Populate dropdown lists
                    DivisionList = await _lookupService.GetDivisionsAsync(),
                    DepartmentList = await _lookupService.GetDepartmentsByDivisionAsync(idea.ToDivisionId),
                    CategoryList = await _lookupService.GetCategoriesAsync(),
                    EventList = await _lookupService.GetEventsAsync()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for idea: {IdeaId}", id);
                TempData["ErrorMessage"] = $"Error loading edit form: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Idea/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ModuleAuthorize("idea_edit_delete")]
        public async Task<IActionResult> Edit(EditIdeaViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Map EditIdeaViewModel → Idea entity
                var idea = new Idea
                {
                    Id = model.Id,
                    InitiatorUserId = model.InitiatorUserId,
                    ToDivisionId = model.ToDivisionId,
                    ToDepartmentId = model.ToDepartmentId,
                    CategoryId = model.CategoryId,
                    EventId = model.EventId,
                    IdeaName = model.IdeaName,
                    IdeaIssueBackground = model.IdeaDescription,
                    IdeaSolution = model.Solution,
                    SavingCost = model.SavingCost ?? 0
                };

                var result = await _ideaService.UpdateIdeaAsync(idea, model.NewAttachmentFiles);

                if (result.Success)
                {
                    _logger.LogInformation("Idea {IdeaId} updated successfully by user {Username}",
                        model.Id, User.Identity?.Name);

                    // Check if request is AJAX
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = result.Message });
                    }

                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction("Details", new { id = model.Id });
                }
                else
                {
                    _logger.LogWarning("Failed to update idea {IdeaId} for user {Username}: {ErrorMessage}",
                        model.Id, User.Identity?.Name, result.Message);

                    // Check if request is AJAX
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = result.Message });
                    }

                    TempData["ErrorMessage"] = result.Message;

                    // Repopulate dropdown lists
                    model.DivisionList = await _lookupService.GetDivisionsAsync();
                    model.DepartmentList = await _lookupService.GetDepartmentsByDivisionAsync(model.ToDivisionId);
                    model.CategoryList = await _lookupService.GetCategoriesAsync();
                    model.EventList = await _lookupService.GetEventsAsync();

                    return View(model);
                }
            }

            // Check if request is AJAX
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            // Return validation errors
            TempData["ErrorMessage"] = "Please check your input";

            // Repopulate dropdown lists
            model.DivisionList = await _lookupService.GetDivisionsAsync();
            model.DepartmentList = await _lookupService.GetDepartmentsByDivisionAsync(model.ToDivisionId);
            model.CategoryList = await _lookupService.GetCategoriesAsync();
            model.EventList = await _lookupService.GetEventsAsync();

            return View(model);
        }

        // POST: Idea/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ModuleAuthorize("idea_edit_delete")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var result = await _ideaService.SoftDeleteIdeaAsync(id, username);

                if (result.Success)
                {
                    _logger.LogInformation("Idea {IdeaId} deleted successfully by user {Username}", id, username);
                    return Json(new { success = true, message = result.Message });
                }
                else
                {
                    _logger.LogWarning("Failed to delete idea {IdeaId} for user {Username}: {ErrorMessage}",
                        id, username, result.Message);
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting idea: {IdeaId}", id);
                return Json(new { success = false, message = $"Error deleting idea: {ex.Message}" });
            }
        }
    }
}
