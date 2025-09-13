using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Ideku.Services.UserManagement;
using Ideku.ViewModels.UserManagement;

namespace Ideku.Controllers
{
    /// <summary>
    /// Controller for User Management operations
    /// Handles CRUD operations for users with proper authorization and validation
    /// Follows the same pattern as RoleManagementController for consistency
    /// </summary>
    [Authorize]
    public class UserManagementController : Controller
    {
        private readonly IUserManagementService _userManagementService;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            IUserManagementService userManagementService, 
            ILogger<UserManagementController> logger)
        {
            _userManagementService = userManagementService;
            _logger = logger;
        }

        /// <summary>
        /// GET: User Management Index page
        /// Displays all users with create form, statistics, and dropdown data
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get all required data sequentially to avoid DbContext concurrency issues
                var users = await _userManagementService.GetAllUsersAsync();
                var roles = await _userManagementService.GetAvailableRolesAsync();

                var viewModel = new UserIndexViewModel
                {
                    Users = users,
                    CreateUserForm = new CreateUserViewModel(),
                    
                    // Using input field with AJAX validation - no dropdown needed
                    AvailableEmployees = Enumerable.Empty<SelectListItem>(),
                    
                    AvailableRoles = roles.Select(r => new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = r.RoleName
                    })
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user management index");
                TempData["ErrorMessage"] = "Error loading users. Please try again.";
                return RedirectToAction("Index", "Settings");
            }
        }

        /// <summary>
        /// POST: Create new user via AJAX
        /// Returns JSON response for modal form handling
        /// </summary>
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

        /// <summary>
        /// GET: Get user data for editing via AJAX
        /// Returns user information for edit modal
        /// </summary>
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

        /// <summary>
        /// POST: Update existing user via AJAX
        /// </summary>
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

        /// <summary>
        /// POST: Delete user via AJAX
        /// Includes comprehensive dependency checking
        /// </summary>
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


        /// <summary>
        /// GET: Validate and get employee information by Employee ID
        /// Returns employee details if valid and available for user creation
        /// </summary>
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
    }
}