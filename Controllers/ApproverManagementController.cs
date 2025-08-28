using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services.Approver;
using System.Text.Json;
using Ideku.ViewModels.ApproverManagement;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ideku.Controllers
{
    [Authorize]
    public class ApproverManagementController : Controller
    {
        private readonly IApproverService _approverService;

        public ApproverManagementController(IApproverService approverService)
        {
            _approverService = approverService;
        }

        // GET: Approver
        public async Task<IActionResult> Index()
        {
            try
            {
                var approvers = await _approverService.GetAllApproversAsync();
                var roles = await _approverService.GetAllRolesAsync();

                var viewModel = new ApproverIndexViewModel
                {
                    Approvers = approvers,
                    CreateApproverForm = new CreateApproverViewModel(),
                    RoleList = roles.Select(r => new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = r.RoleName
                    }).ToList()
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading approvers: {ex.Message}";
                return RedirectToAction("Index", "Settings");
            }
        }

        // GET: Approver/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var approver = await _approverService.GetApproverByIdAsync(id);
                if (approver == null)
                {
                    TempData["ErrorMessage"] = "Approver not found.";
                    return RedirectToAction(nameof(Index));
                }

                var roles = await _approverService.GetAllRolesAsync();

                var viewModel = new ApproverDetailsViewModel
                {
                    Approver = approver,
                    AddApproverRoleForm = new AddApproverRoleViewModel { ApproverId = approver.Id },
                    RoleList = roles.Select(r => new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = r.RoleName
                    }).ToList(),
                    ApproverRoles = approver.ApproverRoles.Select(ar => new ApproverRoleViewModel
                    {
                        Id = ar.Id,
                        RoleId = ar.RoleId,
                        RoleName = ar.Role.RoleName
                    }).ToList()
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading approver details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Approver/Create
        [HttpPost]
        public async Task<IActionResult> Create(CreateApproverViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Validation failed. Please check your input." });
                }

                if (string.IsNullOrEmpty(model.RolesJson))
                {
                    return Json(new { success = false, message = "At least one role is required." });
                }

                // Format approver name - ensure it starts with APV_ and is uppercase
                var formattedApproverName = model.ApproverName.Trim().ToUpper();
                if (!formattedApproverName.StartsWith("APV_"))
                {
                    formattedApproverName = "APV_" + formattedApproverName;
                }

                var approver = new Models.Entities.Approver
                {
                    ApproverName = formattedApproverName,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                };

                // Parse approvers JSON
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var rolesList = System.Text.Json.JsonSerializer.Deserialize<List<ApproverRoleData>>(model.RolesJson, options);
                
                if (rolesList == null || !rolesList.Any())
                {
                    return Json(new { success = false, message = "At least one role is required." });
                }

                // Validate all RoleIds exist before proceeding
                var allRoles = await _approverService.GetAllRolesAsync();
                var validRoleIds = allRoles.Select(r => r.Id).ToList();
                
                foreach (var roleData in rolesList)
                {
                    if (!validRoleIds.Contains(roleData.RoleId))
                    {
                        return Json(new { success = false, message = $"Invalid role selected (ID: {roleData.RoleId}). Please refresh the page and try again." });
                    }
                }

                // Add approver first
                await _approverService.AddApproverAsync(approver);

                // Add roles
                foreach (var roleData in rolesList)
                {
                    var approverRole = new Models.Entities.ApproverRole
                    {
                        ApproverId = approver.Id,
                        RoleId = roleData.RoleId,
                        CreatedAt = DateTime.Now
                    };

                    await _approverService.AddApproverRoleAsync(approverRole);
                }

                return Json(new { success = true, message = "Approver and roles added successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public class ApproverRoleData
        {
            public int RoleId { get; set; }
        }

        // POST: Approver/AddApproverRole
        [HttpPost]
        public async Task<IActionResult> AddApproverRole(AddApproverRoleViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Validation failed. Please check your input." });
                }

                // Check if approver exists
                var approver = await _approverService.GetApproverByIdAsync(model.ApproverId);
                if (approver == null)
                {
                    return Json(new { success = false, message = "Approver not found." });
                }

                // Check if role is already assigned to this approver
                var existingRole = approver.ApproverRoles.FirstOrDefault(ar => ar.RoleId == model.RoleId);
                if (existingRole != null)
                {
                    return Json(new { success = false, message = "This role is already assigned to this approver." });
                }

                var approverRole = new Models.Entities.ApproverRole
                {
                    ApproverId = model.ApproverId,
                    RoleId = model.RoleId,
                    CreatedAt = DateTime.Now
                };

                await _approverService.AddApproverRoleAsync(approverRole);
                return Json(new { success = true, message = "Role added successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Approver/DeleteApproverRole
        [HttpPost]
        public async Task<IActionResult> DeleteApproverRole(int approverRoleId)
        {
            try
            {
                // First, get the approver role to find which approver it belongs to
                var approverRole = await _approverService.GetApproverRoleByIdAsync(approverRoleId);
                if (approverRole == null)
                {
                    return Json(new { success = false, message = "Role not found." });
                }

                // Get the approver to check how many roles they have
                var approver = await _approverService.GetApproverByIdAsync(approverRole.ApproverId);
                if (approver == null)
                {
                    return Json(new { success = false, message = "Approver not found." });
                }

                // Check if this is the last role for the approver
                if (approver.ApproverRoles.Count <= 1)
                {
                    return Json(new { 
                        success = false, 
                        message = "Cannot delete role. An approver must have at least one role assigned." 
                    });
                }

                // Proceed with deletion if approver has more than one role
                var result = await _approverService.DeleteApproverRoleAsync(approverRoleId);
                if (result)
                {
                    return Json(new { success = true, message = "Role deleted successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete role." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // GET: ApproverManagement/GetApprover/5 - Get approver details for editing
        [HttpGet]
        public async Task<IActionResult> GetApprover(int id)
        {
            try
            {
                var approver = await _approverService.GetApproverByIdAsync(id);
                if (approver == null)
                {
                    return Json(new { success = false, message = "Approver not found." });
                }

                var roles = await _approverService.GetAllRolesAsync();
                var roleList = roles.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.RoleName
                }).ToList();

                return Json(new { 
                    success = true, 
                    approver = new {
                        id = approver.Id,
                        approverName = approver.ApproverName,
                        isActive = approver.IsActive,
                        approverRoles = approver.ApproverRoles.Select(ar => new {
                            id = ar.Id,
                            roleId = ar.RoleId,
                            roleName = ar.Role.RoleName
                        }).ToList()
                    },
                    roleList = roleList
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: ApproverManagement/Edit/5 - Update approver
        [HttpPost]
        public async Task<IActionResult> Edit(int id, EditApproverViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    return Json(new { success = false, message = "Invalid approver ID." });
                }

                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Validation failed. Please check your input." });
                }

                // Format approver name with APV_ prefix
                var formattedApproverName = model.ApproverName.Trim().ToUpperInvariant();
                if (!formattedApproverName.StartsWith("APV_"))
                {
                    formattedApproverName = "APV_" + formattedApproverName;
                }

                // Parse roles JSON
                List<ApproverRoleData> rolesList = new List<ApproverRoleData>();
                if (!string.IsNullOrEmpty(model.RolesJson))
                {
                    try
                    {
                        var deserializedList = JsonSerializer.Deserialize<List<ApproverRoleData>>(model.RolesJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        rolesList = deserializedList ?? new List<ApproverRoleData>();
                    }
                    catch (JsonException)
                    {
                        return Json(new { success = false, message = "Invalid roles data format." });
                    }
                }

                if (rolesList.Count == 0)
                {
                    return Json(new { success = false, message = "At least one role is required." });
                }

                // Update approver basic info
                var approver = await _approverService.GetApproverByIdAsync(id);
                if (approver == null)
                {
                    return Json(new { success = false, message = "Approver not found." });
                }

                approver.ApproverName = formattedApproverName;
                approver.IsActive = model.IsActive;
                approver.UpdatedAt = DateTime.Now;

                await _approverService.UpdateApproverAsync(approver);

                // Update roles - remove all existing and add new ones
                var existingRoles = approver.ApproverRoles.ToList();
                foreach (var role in existingRoles)
                {
                    await _approverService.DeleteApproverRoleAsync(role.Id);
                }

                // Add new roles
                foreach (var roleData in rolesList)
                {
                    var approverRole = new Models.Entities.ApproverRole
                    {
                        ApproverId = approver.Id,
                        RoleId = roleData.RoleId,
                        CreatedAt = DateTime.Now
                    };

                    await _approverService.AddApproverRoleAsync(approverRole);
                }

                return Json(new { success = true, message = "Approver updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // GET: Approver/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var approver = await _approverService.GetApproverByIdAsync(id);
                if (approver == null)
                {
                    // Check if request is AJAX
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, error = "Approver not found." });
                    }
                    TempData["ErrorMessage"] = "Approver not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if approver is used in workflow stages (critical dependency)
                if (approver.WorkflowStages.Any())
                {
                    var errorMessage = "Cannot delete approver. It is used in workflow stages. Please remove it from workflow stages first.";
                    
                    // Check if request is AJAX
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, error = errorMessage });
                    }
                    
                    TempData["ErrorMessage"] = errorMessage;
                    return RedirectToAction("Details", new { id = id });
                }

                // Note: ApproverRoles will be automatically deleted by cascade delete in database

                var result = await _approverService.DeleteApproverAsync(id);
                if (result)
                {
                    // Check if request is AJAX
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Approver deleted successfully." });
                    }
                    
                    TempData["SuccessMessage"] = "Approver deleted successfully.";
                }
                else
                {
                    var errorMessage = "Failed to delete approver.";
                    
                    // Check if request is AJAX
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, error = errorMessage });
                    }
                    
                    TempData["ErrorMessage"] = errorMessage;
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error deleting approver: {ex.Message}";
                
                // Check if request is AJAX
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, error = errorMessage });
                }
                
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}