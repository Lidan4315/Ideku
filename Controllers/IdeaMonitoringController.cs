using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services.IdeaMonitoring;
using Ideku.Services.Idea;
using Ideku.Services.Lookup;
using Ideku.ViewModels.IdeaMonitoring;
using Ideku.ViewModels.IdeaList;
using Ideku.Data.Repositories;
using Ideku.Extensions;
using Ideku.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Ideku.Controllers
{
    [Authorize]
    public class IdeaMonitoringController : Controller
    {
        private readonly IIdeaMonitoringService _monitoringService;
        private readonly IIdeaService _ideaService;
        private readonly IIdeaRepository _ideaRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILookupService _lookupService;
        private readonly ILogger<IdeaMonitoringController> _logger;

        public IdeaMonitoringController(
            IIdeaMonitoringService monitoringService,
            IIdeaService ideaService,
            IIdeaRepository ideaRepository,
            IUserRepository userRepository,
            ILookupService lookupService,
            ILogger<IdeaMonitoringController> logger)
        {
            _monitoringService = monitoringService;
            _ideaService = ideaService;
            _ideaRepository = ideaRepository;
            _userRepository = userRepository;
            _lookupService = lookupService;
            _logger = logger;
        }

        // GET: IdeaMonitoring/Index - List all ideas (same as IdeaList)
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
                return Challenge();
            }

            // Validate and normalize pagination parameters
            pageSize = PaginationHelper.ValidatePageSize(pageSize);
            page = Math.Max(1, page);

            // Get user info for ViewBag
            var user = await _userRepository.GetByUsernameAsync(username);

            // Get ideas based on user role (same as IdeaList)
            var ideasQuery = await _ideaService.GetAllIdeasQueryAsync(username);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                ideasQuery = ideasQuery.Where(i =>
                    i.IdeaCode.Contains(searchTerm) ||
                    i.IdeaName.Contains(searchTerm) ||
                    i.InitiatorUser.Name.Contains(searchTerm));
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

            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                ideasQuery = ideasQuery.Where(i => i.CurrentStatus == selectedStatus);
            }

            if (selectedStage.HasValue)
            {
                ideasQuery = ideasQuery.Where(i => i.CurrentStage == selectedStage.Value);
            }

            // Apply pagination
            var pagedResult = await ideasQuery.ToPagedResultAsync(page, pageSize);

            // Get lookup data for filters
            var divisions = await _lookupService.GetDivisionsAsync();
            var categories = await _lookupService.GetCategoriesAsync();

            // Get available stages from database
            var stages = await _ideaService.GetAvailableStagesAsync();

            var viewModel = new IdeaListViewModel
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
                }).ToList()
            };

            // Pass lookup data to view
            ViewBag.Divisions = divisions;
            ViewBag.Categories = categories;
            ViewBag.UserRole = user?.Role?.RoleName ?? "";

            return View(viewModel);
        }

        // AJAX endpoint for real-time filtering (same as IdeaList)
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
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            // Validate pagination parameters
            pageSize = PaginationHelper.ValidatePageSize(pageSize);
            page = Math.Max(1, page);

            // Get base queryable with role-based filtering
            var ideasQuery = await _ideaService.GetAllIdeasQueryAsync(username);

            // Apply filters (same logic as Index method)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                ideasQuery = ideasQuery.Where(i =>
                    i.IdeaCode.Contains(searchTerm) ||
                    i.IdeaName.Contains(searchTerm) ||
                    i.InitiatorUser.Name.Contains(searchTerm)
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

            // Return JSON with paginated results - detailUrl points to IdeaMonitoring/Details
            return Json(new {
                success = true,
                ideas = pagedResult.Items.Select(i => new {
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
                    submittedDate = i.SubmittedDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    detailUrl = Url.Action("Details", new { ideaId = i.Id }) // Change to IdeaMonitoring Details
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
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: IdeaMonitoring/Details/{ideaId} - Monitoring detail for specific idea
        public async Task<IActionResult> Details(long ideaId)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var idea = await _ideaRepository.GetByIdAsync(ideaId);
                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found";
                    return RedirectToAction("Index");
                }

                var monitorings = await _monitoringService.GetMonitoringsByIdeaIdAsync(ideaId);
                var canEdit = await _monitoringService.CanEditCostSavingsAsync(ideaId, username);
                var canValidate = await _monitoringService.CanValidateCostSavingsAsync(username);

                var viewModel = new MonitoringDetailViewModel
                {
                    Idea = idea,
                    Monitorings = monitorings,
                    CanEditCostSavings = canEdit,
                    CanValidateCostSavings = canValidate,
                    HasMonitoring = monitorings.Any()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading monitoring details for Idea {IdeaId}", ideaId);
                TempData["ErrorMessage"] = "Error loading monitoring details";
                return RedirectToAction("Index");
            }
        }

        // POST: IdeaMonitoring/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMonitoringViewModel model)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = "Validation failed", errors });
            }

            var result = await _monitoringService.CreateMonitoringAsync(
                model.IdeaId,
                model.MonthFrom,
                model.DurationMonths,
                username);

            if (result.Success)
            {
                _logger.LogInformation("Monitoring created for Idea {IdeaId} by user {Username}",
                    model.IdeaId, username);

                return Json(new
                {
                    success = true,
                    message = result.Message,
                    monitoringId = result.Monitoring?.Id
                });
            }

            return Json(new { success = false, message = result.Message });
        }

        // POST: IdeaMonitoring/UpdateCostSavings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCostSavings(UpdateCostSavingsViewModel model)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = "Validation failed", errors });
            }

            var result = await _monitoringService.UpdateCostSavingsAsync(
                model.MonitoringId,
                model.CostSavePlan,
                model.CostSaveActual,
                username);

            if (result.Success)
            {
                _logger.LogInformation("Cost savings updated for Monitoring {MonitoringId} by user {Username}",
                    model.MonitoringId, username);
            }

            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: IdeaMonitoring/UpdateCostSaveValidated
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCostSaveValidated(UpdateCostSaveValidatedViewModel model)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = "Validation failed", errors });
            }

            var result = await _monitoringService.UpdateCostSaveValidatedAsync(
                model.MonitoringId,
                model.CostSaveActualValidated,
                username);

            if (result.Success)
            {
                _logger.LogInformation("Cost savings validated for Monitoring {MonitoringId} by SCFO {Username}",
                    model.MonitoringId, username);
            }

            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: IdeaMonitoring/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _monitoringService.DeleteMonitoringAsync(id, username);

            if (result.Success)
            {
                _logger.LogInformation("Monitoring {MonitoringId} deleted by user {Username}", id, username);
            }

            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: IdeaMonitoring/ExtendDuration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendDuration(ExtendDurationViewModel model)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = "Validation failed", errors });
            }

            var result = await _monitoringService.ExtendDurationAsync(
                model.IdeaId,
                model.AdditionalMonths,
                username);

            if (result.Success)
            {
                _logger.LogInformation("Monitoring duration extended for Idea {IdeaId} by {AdditionalMonths} months by user {Username}",
                    model.IdeaId, model.AdditionalMonths, username);
            }

            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
