using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Ideku.Services.Idea;
using Ideku.ViewModels;
using Ideku.ViewModels.IdeaList;
using Ideku.Services.Workflow;
using Ideku.Services.IdeaImplementators;
using Ideku.Models;
using Ideku.Extensions;
using Ideku.Helpers;
using Ideku.Services.Lookup;
using Ideku.Services.Milestone;

namespace Ideku.Controllers
{
    [Authorize]
    public class IdeaController : Controller
    {
        private readonly IIdeaService _ideaService;
        private readonly IWorkflowService _workflowService;
        private readonly ILookupService _lookupService;
        private readonly IIdeaImplementatorService _implementatorService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMilestoneService _milestoneService;
        private readonly ILogger<IdeaController> _logger;

        public IdeaController(
            IIdeaService ideaService,
            IWorkflowService workflowService,
            ILookupService lookupService,
            IIdeaImplementatorService implementatorService,
            IMilestoneService milestoneService,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<IdeaController> logger)
        {
            _ideaService = ideaService;
            _workflowService = workflowService;
            _lookupService = lookupService;
            _milestoneService = milestoneService;
            _implementatorService = implementatorService;
            _serviceScopeFactory = serviceScopeFactory;

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

                var viewModel = await _ideaService.PrepareCreateViewModelAsync(username);
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
            if (ModelState.IsValid)
            {
                var result = await _ideaService.CreateIdeaAsync(model, model.AttachmentFiles);
                
                if (result.Success && result.CreatedIdea != null)
                {
                    _logger.LogInformation("Idea {IdeaId} - {IdeaName} created successfully by user {Username}", 
                        result.CreatedIdea.Id, result.CreatedIdea.IdeaName, User.Identity?.Name);

                    // Send notification emails in background
                    SendEmailInBackground(result.CreatedIdea);

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

        /// Helper method to send emails in background with proper scope management
        private void SendEmailInBackground(Models.Entities.Idea idea)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting background email process for idea {IdeaId} - {IdeaName}", 
                        idea.Id, idea.IdeaName);

                    // Create new scope for background task
                    using var scope = _serviceScopeFactory.CreateScope();
                    var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();
                    
                    // Send notification emails
                    await workflowService.InitiateWorkflowAsync(idea);
                    
                    _logger.LogInformation("Background email sent successfully for idea {IdeaId}", idea.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send background email for idea {IdeaId}: {ErrorMessage}", 
                        idea.Id, ex.Message);
                    
                    // Future: Could add retry mechanism or admin notification
                    // await _retryService.AddToRetryQueueAsync(idea.Id);
                }
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

                // Create view model with idea and implementators
                var viewModel = new IdeaDetailViewModel
                {
                    Idea = idea,
                    Implementators = implementators.ToList(),
                    Milestones = milestones.ToList(),
                    WorkflowHistory = workflowHistory
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading idea details: {IdeaId}", id);
                TempData["ErrorMessage"] = "Error loading idea details";
                return RedirectToAction("Index");
            }
        }
    }
}
