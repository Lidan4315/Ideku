using Ideku.Data.Repositories;
using Ideku.Services.Idea;
using Ideku.Services.IdeaRelation;
using Ideku.Services.Lookup;
using Ideku.Services.IdeaImplementators;
using Ideku.ViewModels.IdeaList;
using Ideku.Extensions;
using Ideku.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ideku.Controllers
{
    [Authorize]
    [ModuleAuthorize("idea_list")]
    public class IdeaListController : Controller
    {
        private readonly IIdeaService _ideaService;
        private readonly IUserRepository _userRepository;
        private readonly ILookupService _lookupService;
        private readonly IIdeaRelationService _ideaRelationService;
        private readonly IIdeaImplementatorService _implementatorService;
        private readonly ILogger<IdeaListController> _logger;

        public IdeaListController(
            IIdeaService ideaService,
            IUserRepository userRepository,
            ILookupService lookupService,
            IIdeaRelationService ideaRelationService,
            IIdeaImplementatorService implementatorService,
            ILogger<IdeaListController> logger)
        {
            _ideaService = ideaService;
            _userRepository = userRepository;
            _lookupService = lookupService;
            _ideaRelationService = ideaRelationService;
            _implementatorService = implementatorService;
            _logger = logger;
        }

        // GET: /IdeaList or /IdeaList/Index
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

            // Get ideas based on user role (superuser sees all, others filtered by role)
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

            // Apply pagination - this executes the database queries
            var pagedResult = await ideasQuery.ToPagedResultAsync(page, pageSize);

            // Get lookup data for filters
            var divisions = await _lookupService.GetDivisionsAsync();
            var categories = await _lookupService.GetCategoriesAsync();

            // Get available stages from database
            var stages = await _ideaService.GetAvailableStagesAsync();

            var statuses = await _ideaService.GetAvailableStatusesAsync();

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
                }).ToList(),
                StatusOptions = statuses
            };

            // Pass lookup data to view
            ViewBag.Divisions = divisions;
            ViewBag.Categories = categories;
            ViewBag.UserRole = user?.Role?.RoleName ?? "";

            return View(viewModel);
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
            
            // Return JSON with paginated results
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
        public async Task<JsonResult> GetDepartmentsByDivision(string divisionId)
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

        // GET: IdeaList/Detail/{id}
        public async Task<IActionResult> Detail(long id)
        {
            try
            {
                // Get idea details
                var ideasQuery = await _ideaService.GetAllIdeasQueryAsync(User.Identity?.Name ?? "");
                var idea = await ideasQuery
                    .Where(i => i.Id == id)
                    .FirstOrDefaultAsync();

                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found";
                    return RedirectToAction("Index");
                }

                // Get implementators for the idea
                var implementators = await _implementatorService.GetImplementatorsByIdeaIdAsync(id);

                // Get available users for dropdown (server-side)
                var availableUsers = await _implementatorService.GetAvailableUsersForAssignmentAsync(id);

                var viewModel = new IdeaDetailViewModel
                {
                    Idea = idea,
                    Implementators = implementators.ToList(),
                    AvailableUsers = availableUsers.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = GetUserProperty(u, "id")?.ToString(),
                        Text = GetUserProperty(u, "displayText")?.ToString()
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading idea detail: {IdeaId}", id);
                TempData["ErrorMessage"] = "Error loading idea details";
                return RedirectToAction("Index");
            }
        }

        // AJAX: Get implementators for an idea
        [HttpGet]
        public async Task<JsonResult> GetImplementators(long ideaId)
        {
            try
            {
                var implementators = await _implementatorService.GetImplementatorsByIdeaIdAsync(ideaId);

                var result = implementators.Select(i => new
                {
                    id = i.Id,
                    userId = i.UserId,
                    role = i.Role,
                    userName = i.User?.Name,
                    employeeId = i.User?.EmployeeId,
                    division = i.User?.Employee?.DivisionNavigation?.NameDivision,
                    department = i.User?.Employee?.DepartmentNavigation?.NameDepartment,
                    assignedDate = i.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting implementators for idea {IdeaId}", ideaId);
                return Json(new { success = false, message = "Error loading implementators" });
            }
        }

        // AJAX: Get available users for assignment
        [HttpGet]
        public async Task<JsonResult> GetAvailableUsers(long ideaId)
        {
            try
            {
                var availableUsers = await _implementatorService.GetAvailableUsersForAssignmentAsync(ideaId);
                return Json(new { success = true, data = availableUsers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available users for idea {IdeaId}", ideaId);
                return Json(new { success = false, message = "Error loading available users" });
            }
        }

        // AJAX: Assign implementator
        [HttpPost]
        public async Task<JsonResult> AssignImplementator(long ideaId, long userId, string role)
        {
            try
            {
                // Check if trying to add member and limit is reached
                if (role == "Member")
                {
                    var canAddMore = await _implementatorService.CanAddMoreMembersAsync(User.Identity!.Name!, ideaId);
                    if (!canAddMore)
                    {
                        return Json(new { success = false, message = "Maximum limit of 5 members has been reached." });
                    }
                }

                var result = await _implementatorService.AssignImplementatorAsync(ideaId, userId, role);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully assigned user {UserId} as {Role} to idea {IdeaId} by {Username}",
                        userId, role, ideaId, User.Identity!.Name);
                }

                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning implementator: IdeaId={IdeaId}, UserId={UserId}, Role={Role}",
                    ideaId, userId, role);
                return Json(new { success = false, message = "An error occurred while assigning implementator" });
            }
        }

        // AJAX: Remove implementator
        [HttpPost]
        public async Task<JsonResult> RemoveImplementator(long implementatorId, long ideaId)
        {
            try
            {
                var result = await _implementatorService.RemoveImplementatorAsync(implementatorId);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully removed implementator {ImplementatorId} by {Username}",
                        implementatorId, User.Identity!.Name);
                }

                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing implementator {ImplementatorId}", implementatorId);
                return Json(new { success = false, message = "An error occurred while removing implementator" });
            }
        }

        // Helper method to get property from anonymous object
        private object? GetUserProperty(object user, string propertyName)
        {
            var type = user.GetType();
            var property = type.GetProperty(propertyName);
            return property?.GetValue(user);
        }

    }
}