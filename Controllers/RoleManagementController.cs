using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services.Roles;
using Ideku.ViewModels.RoleManagement;

namespace Ideku.Controllers
{
    /// <summary>
    /// Controller for Role Management operations
    /// Handles CRUD operations for roles with proper authorization
    /// </summary>
    [Authorize(Roles = "Superuser,Admin")]
    public class RoleManagementController : Controller
    {
        private readonly IRolesService _rolesService;
        private readonly ILogger<RoleManagementController> _logger;

        public RoleManagementController(IRolesService rolesService, ILogger<RoleManagementController> logger)
        {
            _rolesService = rolesService;
            _logger = logger;
        }

        /// <summary>
        /// GET: Role Management Index page
        /// Displays all roles with create form and statistics
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var roles = await _rolesService.GetAllRolesAsync();
                var statistics = await _rolesService.GetRoleStatisticsAsync();

                var viewModel = new RoleIndexViewModel
                {
                    Roles = roles,
                    Statistics = statistics,
                    CreateRoleForm = new CreateRoleViewModel()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading role management index");
                TempData["ErrorMessage"] = "Error loading roles. Please try again.";
                return RedirectToAction("Index", "Settings");
            }
        }

        /// <summary>
        /// POST: Create new role via AJAX
        /// Returns JSON response for modal form handling
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRoleViewModel model)
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

                var result = await _rolesService.CreateRoleAsync(model.RoleName, model.Description);
                
                if (result.Success)
                {
                    _logger.LogInformation("Role '{RoleName}' created successfully", model.RoleName);
                    return Json(new { success = true, message = result.Message });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role '{RoleName}'", model.RoleName);
                return Json(new { success = false, message = "An error occurred while creating the role." });
            }
        }

        /// <summary>
        /// GET: Get role data for editing via AJAX
        /// Returns role information for edit modal
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRole(int id)
        {
            try
            {
                var role = await _rolesService.GetRoleByIdAsync(id);
                if (role == null)
                {
                    return Json(new { success = false, message = "Role not found." });
                }

                // Get usage count for warning display
                var userCount = await _rolesService.GetRoleUsageCountAsync(role.Id);

                return Json(new
                {
                    success = true,
                    role = new
                    {
                        id = role.Id,
                        roleName = role.RoleName,
                        description = role.Desc,
                        userCount = userCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role with ID {RoleId}", id);
                return Json(new { success = false, message = "Error retrieving role information." });
            }
        }

        /// <summary>
        /// POST: Update existing role via AJAX
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditRoleViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    return Json(new { success = false, message = "Invalid role ID." });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join("; ", errors) });
                }

                var result = await _rolesService.UpdateRoleAsync(id, model.RoleName, model.Description);
                
                if (result.Success)
                {
                    _logger.LogInformation("Role '{RoleName}' updated successfully", model.RoleName);
                    return Json(new { success = true, message = result.Message });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role with ID {RoleId}", id);
                return Json(new { success = false, message = "An error occurred while updating the role." });
            }
        }

        /// <summary>
        /// POST: Delete role via AJAX
        /// Includes dependency checking
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _rolesService.DeleteRoleAsync(id);
                
                if (result.Success)
                {
                    _logger.LogInformation("Role with ID {RoleId} deleted successfully", id);
                    return Json(new { success = true, message = result.Message });
                }
                else
                {
                    _logger.LogWarning("Failed to delete role with ID {RoleId}: {Message}", id, result.Message);
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role with ID {RoleId}", id);
                return Json(new { success = false, message = "An error occurred while deleting the role." });
            }
        }

        // Details method removed - Role Management doesn't need separate details page
        // All role information is available in the main index table with inline editing
    }
}