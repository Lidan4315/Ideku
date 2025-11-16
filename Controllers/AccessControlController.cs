using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services.AccessControl;
using Ideku.Data.Repositories;
using Ideku.ViewModels;
using System.Security.Claims;
using Ideku.Helpers;

namespace Ideku.Controllers
{
    [Authorize]
    [ModuleAuthorize("access_control")]
    public class AccessControlController : Controller
    {
        private readonly IAccessControlService _accessControlService;
        private readonly IRolesRepository _rolesRepository;
        private readonly ILogger<AccessControlController> _logger;

        public AccessControlController(
            IAccessControlService accessControlService,
            IRolesRepository rolesRepository,
            ILogger<AccessControlController> logger)
        {
            _accessControlService = accessControlService;
            _rolesRepository = rolesRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var roles = await _rolesRepository.GetAllAsync();
                var modules = await _accessControlService.GetAllModulesAsync();
                var permissionMatrix = await _accessControlService.GetPermissionMatrixAsync();

                var viewModel = new AccessControlViewModel
                {
                    Roles = roles.ToList(),
                    Modules = modules.ToList(),
                    PermissionMatrix = permissionMatrix
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading access control index");
                TempData["ErrorMessage"] = "Error loading access control. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoleAccess([FromBody] UpdateRoleAccessRequest request)
        {
            try
            {
                if (request == null || request.RoleId <= 0)
                {
                    return Json(new { success = false, message = "Invalid request data." });
                }

                // Get current user ID
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdClaim, out long currentUserId))
                {
                    return Json(new { success = false, message = "User authentication failed." });
                }

                // Update role access
                var result = await _accessControlService.UpdateRoleAccessAsync(
                    request.RoleId,
                    request.ModuleIds,
                    currentUserId
                );

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Role access updated for RoleId {RoleId} by UserId {UserId}",
                        request.RoleId,
                        currentUserId
                    );
                }

                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role access for RoleId {RoleId}", request?.RoleId);
                return Json(new { success = false, message = "An error occurred while updating permissions." });
            }
        }

        /// POST: Toggle single permission
        /// Quick toggle for individual role-module permission
        [HttpPost]
        public async Task<IActionResult> TogglePermission([FromBody] TogglePermissionRequest request)
        {
            try
            {
                if (request == null || request.RoleId <= 0 || request.ModuleId <= 0)
                {
                    return Json(new { success = false, message = "Invalid request data." });
                }

                // Get current user ID
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdClaim, out long currentUserId))
                {
                    return Json(new { success = false, message = "User authentication failed." });
                }

                // Grant or revoke access based on current state
                var result = request.CanAccess
                    ? await _accessControlService.GrantAccessAsync(request.RoleId, request.ModuleId, currentUserId)
                    : await _accessControlService.RevokeAccessAsync(request.RoleId, request.ModuleId, currentUserId);

                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error toggling permission for RoleId {RoleId}, ModuleId {ModuleId}",
                    request?.RoleId,
                    request?.ModuleId
                );
                return Json(new { success = false, message = "An error occurred while updating permission." });
            }
        }
    }

    /// <summary>
    /// Request model for toggle permission action
    /// </summary>
    public class TogglePermissionRequest
    {
        public int RoleId { get; set; }
        public int ModuleId { get; set; }
        public bool CanAccess { get; set; }
    }
}
