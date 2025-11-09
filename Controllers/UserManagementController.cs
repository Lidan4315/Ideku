using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Ideku.Services.UserManagement;
using Ideku.Services.Lookup;
using Ideku.ViewModels.UserManagement;
using Ideku.Models.Statistics;
using Ideku.Extensions;
using Ideku.Helpers;

namespace Ideku.Controllers
{
    [Authorize]
    [ModuleAuthorize("user_management")]
    public class UserManagementController : Controller
    {
        private readonly IUserManagementService _userManagementService;
        private readonly ILookupService _lookupService;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            IUserManagementService userManagementService,
            ILookupService lookupService,
            ILogger<UserManagementController> logger)
        {
            _userManagementService = userManagementService;
            _lookupService = lookupService;
            _logger = logger;
        }

        /// GET: User Management Index page with pagination
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            int? selectedRole = null)
        {
            try
            {
                // Validate and normalize pagination parameters (same as IdeaListController)
                pageSize = PaginationHelper.ValidatePageSize(pageSize);
                page = Math.Max(1, page);

                // Get users query for pagination
                var usersQuery = await _userManagementService.GetAllUsersQueryAsync();
                
                // Apply progressive filters (same pattern as IdeaListController)
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    usersQuery = usersQuery.Where(u => 
                        u.Username.Contains(searchTerm) ||
                        u.Employee.NAME.Contains(searchTerm) ||
                        u.EmployeeId.Contains(searchTerm));
                }
                
                if (selectedRole.HasValue)
                {
                    usersQuery = usersQuery.Where(u => u.RoleId == selectedRole.Value);
                }
                
                // Apply pagination - this executes the database queries
                var pagedResult = await usersQuery.ToPagedResultAsync(page, pageSize);
                
                // Get dropdown data
                var roles = await _userManagementService.GetAvailableRolesAsync();

                var viewModel = new UserIndexViewModel
                {
                    PagedUsers = pagedResult,
                    CreateUserForm = new CreateUserViewModel(),
                    
                    // Filter properties (preserve state)
                    SearchTerm = searchTerm,
                    SelectedRole = selectedRole,
                    
                    // Using input field with AJAX validation - no dropdown needed
                    AvailableEmployees = Enumerable.Empty<SelectListItem>(),
                    
                    AvailableRoles = roles.Select(r => new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = r.RoleName
                    })
                };

                // Set ViewBag for generic pagination partial
                ViewBag.ItemName = "Users";

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user management index");
                TempData["ErrorMessage"] = "Error loading users. Please try again.";
                return RedirectToAction("Index", "Settings");
            }
        }

        /// POST: Create new user via AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join("; ", errors) });
                }

                var result = await _userManagementService.CreateUserAsync(
                    model.EmployeeId, 
                    model.Username, 
                    model.RoleId, 
                    model.IsActing);
                
                if (result.Success)
                {
                    _logger.LogInformation("User '{Username}' created successfully for employee {EmployeeId}", 
                        model.Username, model.EmployeeId);
                    return Json(new { success = true, message = result.Message });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user '{Username}' for employee {EmployeeId}", 
                    model.Username, model.EmployeeId);
                return Json(new { success = false, message = "An error occurred while creating the user." });
            }
        }

        /// GET: Get user data for editing via AJAX
        [HttpGet]
        public async Task<IActionResult> GetUser(long id)
        {
            try
            {
                var user = await _userManagementService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Get dependency info for warning display
                var dependencyInfo = await _userManagementService.GetUserDependencyInfoAsync(user.Id);

                return Json(new
                {
                    success = true,
                    user = new
                    {
                        id = user.Id,
                        username = user.Username,
                        roleId = user.RoleId,
                        roleName = user.Role.RoleName,
                        isActing = user.IsActing,
                        
                        // Employee information (read-only)
                        employeeId = user.EmployeeId,
                        employeeName = user.Employee.NAME,
                        employeePosition = user.Employee.POSITION_TITLE,
                        employeeEmail = user.Employee.EMAIL,
                        divisionName = user.Employee.DivisionNavigation?.NameDivision ?? "N/A",
                        departmentName = user.Employee.DepartmentNavigation?.NameDepartment ?? "N/A",
                        
                        // Dependency information
                        dependencyCount = dependencyInfo.TotalDependencies,
                        canDelete = dependencyInfo.CanDelete,
                        dependencyMessage = dependencyInfo.GetDependencyMessage()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID {UserId}", id);
                return Json(new { success = false, message = "Error retrieving user information." });
            }
        }

        /// POST: Update existing user via AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EditUserViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    return Json(new { success = false, message = "Invalid user ID." });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join("; ", errors) });
                }

                // PRE-VALIDATION: Check if user is acting and role is being changed
                var currentUser = await _userManagementService.GetUserByIdAsync(id);
                if (currentUser != null && ActingHelper.IsCurrentlyActing(currentUser) && model.RoleId != currentUser.CurrentRoleId)
                {
                    return Json(new { success = false, message = "Cannot change role while user is acting. Please stop acting first." });
                }

                var result = await _userManagementService.UpdateUserAsync(
                    id,
                    model.Username,
                    model.RoleId,
                    model.IsActing);

                if (result.Success)
                {
                    _logger.LogInformation("User ID {UserId} updated successfully", id);
                    return Json(new { success = true, message = result.Message });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {UserId}", id);
                return Json(new { success = false, message = "An error occurred while updating the user." });
            }
        }

        /// GET: Filter users via AJAX
        [HttpGet]
        public async Task<IActionResult> FilterUsers(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            int? selectedRole = null)
        {
            try
            {
                // Same logic as Index method
                pageSize = PaginationHelper.ValidatePageSize(pageSize);
                page = Math.Max(1, page);

                var usersQuery = await _userManagementService.GetAllUsersQueryAsync();
                
                // Apply progressive filters
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    usersQuery = usersQuery.Where(u => 
                        u.Username.Contains(searchTerm) ||
                        u.Employee.NAME.Contains(searchTerm) ||
                        u.EmployeeId.Contains(searchTerm));
                }
                
                if (selectedRole.HasValue)
                {
                    usersQuery = usersQuery.Where(u => u.RoleId == selectedRole.Value);
                }
                
                var pagedResult = await usersQuery.ToPagedResultAsync(page, pageSize);

                // Return JSON response
                return Json(new
                {
                    success = true,
                    users = pagedResult.Items.Select(user => new
                    {
                        id = user.Id,
                        username = user.Username,
                        name = user.Name,
                        employeeId = user.EmployeeId,
                        divisionName = user.Employee.DivisionNavigation?.NameDivision ?? "N/A",
                        departmentName = user.Employee.DepartmentNavigation?.NameDepartment ?? "N/A",
                        roleName = user.Role.RoleName,
                        isActing = user.IsActing
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
                _logger.LogError(ex, "Error filtering users");
                return Json(new { success = false, message = "Error loading filtered users." });
            }
        }

        /// POST: Delete user via AJAX
        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _userManagementService.DeleteUserAsync(id);
                
                if (result.Success)
                {
                    _logger.LogInformation("User with ID {UserId} deleted successfully", id);
                    return Json(new { success = true, message = result.Message });
                }
                else
                {
                    _logger.LogWarning("Failed to delete user with ID {UserId}: {Message}", id, result.Message);
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {UserId}", id);
                return Json(new { success = false, message = "An error occurred while deleting the user." });
            }
        }

        /// GET: Validate and get employee information by Employee ID
        [HttpGet]
        public async Task<IActionResult> ValidateEmployee(string employeeId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employeeId))
                {
                    return Json(new { success = false, message = "Employee ID is required." });
                }

                var result = await _userManagementService.ValidateEmployeeForUserCreationAsync(employeeId.Trim());
                
                if (result.IsValid)
                {
                    return Json(new
                    {
                        success = true,
                        employee = new
                        {
                            id = result.Employee.EMP_ID,
                            name = result.Employee.NAME,
                            position = result.Employee.POSITION_TITLE,
                            email = result.Employee.EMAIL,
                            division = result.Employee.DivisionNavigation?.NameDivision ?? "N/A",
                            department = result.Employee.DepartmentNavigation?.NameDepartment ?? "N/A"
                        }
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating employee with ID {EmployeeId}", employeeId);
                return Json(new { success = false, message = "Error validating employee." });
            }
        }
        // ACTING DURATION ENDPOINTS

        /// POST: Set user acting role with duration
        [HttpPost]
        public async Task<IActionResult> SetActing([FromBody] SetActingViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                // Additional business validation
                if (!model.IsValidActingPeriod())
                {
                    return Json(new {
                        success = false,
                        message = "Invalid acting period. Period must be future dates and max 1 year."
                    });
                }

                var result = await _userManagementService.SetUserActingAsync(
                    model.UserId,
                    model.ActingRoleId,
                    model.ActingStartDate,
                    model.ActingEndDate,
                    model.ActingDivisionId,
                    model.ActingDepartmentId
                );

                if (result.Success)
                {
                    _logger.LogInformation("User {UserId} set as acting successfully", model.UserId);
                    return Json(new { success = true, message = result.Message });
                }
                else
                {
                    _logger.LogWarning("Failed to set user {UserId} as acting: {Message}", model.UserId, result.Message);
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting user {UserId} as acting", model?.UserId);
                return Json(new { success = false, message = "An error occurred while setting acting role." });
            }
        }

        /// POST: Stop user acting immediately and revert to original role
        [HttpPost]
        public async Task<IActionResult> StopActing([FromBody] StopActingViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                long userId = model.UserId;
                var result = await _userManagementService.StopUserActingAsync(userId);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserId} acting stopped successfully", userId);
                    return Json(new { success = true, message = result.Message });
                }
                else
                {
                    _logger.LogWarning("Failed to stop acting for user {UserId}: {Message}", userId, result.Message);
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping acting for user");
                return Json(new { success = false, message = $"An error occurred while stopping acting role: {ex.Message}" });
            }
        }

        /// POST: Extend user acting period to new end date
        [HttpPost]
        public async Task<IActionResult> ExtendActing([FromBody] ExtendActingViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                // Business validation - new date must be after current end date
                if (model.NewActingEndDate <= model.CurrentActingEndDate)
                {
                    return Json(new
                    {
                        success = false,
                        message = "New end date must be after current end date."
                    });
                }

                var result = await _userManagementService.ExtendUserActingAsync(model.UserId, model.NewActingEndDate);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserId} acting period extended successfully", model.UserId);
                    return Json(new { success = true, message = result.Message });
                }
                else
                {
                    _logger.LogWarning("Failed to extend acting for user {UserId}: {Message}", model.UserId, result.Message);
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending acting for user {UserId}", model?.UserId);
                return Json(new { success = false, message = "An error occurred while extending acting period." });
            }
        }

        /// GET: Get users whose acting period is about to expire (for notifications)
        [HttpGet]
        public async Task<IActionResult> GetExpiringActingUsers(int withinDays = 7)
        {
            try
            {
                var users = await _userManagementService.GetExpiringActingUsersAsync(withinDays);

                return Json(new
                {
                    success = true,
                    users = users.Select(u => new
                    {
                        id = u.Id,
                        username = u.Username,
                        name = u.Name,
                        actingRoleName = u.Role.RoleName,
                        originalRoleName = u.CurrentRole?.RoleName,
                        actingEndDate = u.ActingEndDate?.ToString("yyyy-MM-dd"),
                        daysRemaining = ActingHelper.GetActingDaysRemaining(u),
                        division = u.Employee.DivisionNavigation?.NameDivision,
                        department = u.Employee.DepartmentNavigation?.NameDepartment
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring acting users");
                return Json(new { success = false, message = "Error loading expiring acting users." });
            }
        }

        /// GET: Get acting statistics for dashboard
        [HttpGet]
        public async Task<IActionResult> GetActingStatistics()
        {
            try
            {
                var statistics = await _userManagementService.GetActingStatisticsAsync();

                return Json(new
                {
                    success = true,
                    statistics = new
                    {
                        totalActingUsers = statistics.TotalActingUsers,
                        totalRegularUsers = statistics.TotalRegularUsers,
                        actingPercentage = Math.Round(statistics.ActingPercentage, 1),
                        expiringIn7Days = statistics.ExpiringIn7Days,
                        expiringIn30Days = statistics.ExpiringIn30Days,
                        expiredActingUsers = statistics.ExpiredActingUsers,
                        urgentExpirations = statistics.UrgentExpirations,
                        hasUrgentExpirations = statistics.HasUrgentExpirations,
                        averageActingDurationDays = statistics.AverageActingDurationDays,
                        mostCommonActingRole = statistics.MostCommonActingRole,
                        mostCommonActingRoleCount = statistics.MostCommonActingRoleCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting acting statistics");
                return Json(new { success = false, message = "Error loading acting statistics." });
            }
        }
        // ACTING LOCATION ENDPOINTS

        /// GET: Get divisions for acting location dropdown
        [HttpGet]
        public async Task<IActionResult> GetActingDivisions()
        {
            try
            {
                var divisions = await _lookupService.GetDivisionsAsync();
                return Json(new { success = true, divisions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting divisions for acting location");
                return Json(new { success = false, message = "Error loading divisions." });
            }
        }

        /// GET: Get departments by division for acting location dropdown
        [HttpGet]
        public async Task<IActionResult> GetActingDepartmentsByDivision(string divisionId)
        {
            try
            {
                if (string.IsNullOrEmpty(divisionId))
                {
                    return Json(new { success = false, message = "Division ID is required." });
                }

                var departments = await _lookupService.GetDepartmentsByDivisionForAjaxAsync(divisionId);
                return Json(new { success = true, departments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting departments for division {DivisionId}", divisionId);
                return Json(new { success = false, message = "Error loading departments." });
            }
        }
    }
}