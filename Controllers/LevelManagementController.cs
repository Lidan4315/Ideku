using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services.Level;
using System.Text.Json;
using Ideku.ViewModels.LevelManagement;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ideku.Controllers
{
    [Authorize]
    public class LevelManagementController : Controller
    {
        private readonly ILevelService _levelService;

        public LevelManagementController(ILevelService levelService)
        {
            _levelService = levelService;
        }

        // GET: Level
        public async Task<IActionResult> Index()
        {
            try
            {
                var levels = await _levelService.GetAllLevelsAsync();
                var roles = await _levelService.GetAllRolesAsync();

                var viewModel = new LevelIndexViewModel
                {
                    Levels = levels,
                    CreateLevelForm = new CreateLevelViewModel(),
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
                TempData["ErrorMessage"] = $"Error loading levels: {ex.Message}";
                return RedirectToAction("Index", "Settings");
            }
        }

        // GET: Level/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var level = await _levelService.GetLevelByIdAsync(id);
                if (level == null)
                {
                    TempData["ErrorMessage"] = "Level not found.";
                    return RedirectToAction(nameof(Index));
                }

                var roles = await _levelService.GetAllRolesAsync();

                var viewModel = new LevelDetailsViewModel
                {
                    Level = level,
                    AddApproverForm = new AddApproverViewModel { LevelId = level.Id },
                    RoleList = roles.Select(r => new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = r.RoleName
                    }).ToList(),
                    Approvers = level.LevelApprovers.Select(la => new LevelApproverViewModel
                    {
                        Id = la.Id,
                        RoleId = la.RoleId,
                        RoleName = la.Role.RoleName,
                        ApprovalLevel = la.ApprovalLevel,
                        IsPrimary = la.IsPrimary
                    }).ToList()
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading level details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Level/Create
        [HttpPost]
        public async Task<IActionResult> Create(CreateLevelViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Validation failed. Please check your input." });
                }

                if (string.IsNullOrEmpty(model.ApproversJson))
                {
                    return Json(new { success = false, message = "At least one approver is required." });
                }

                // Format level name - ensure it starts with LV and is uppercase
                var formattedLevelName = model.LevelName.Trim().ToUpper();
                if (!formattedLevelName.StartsWith("LV"))
                {
                    formattedLevelName = "LV" + formattedLevelName;
                }

                var level = new Models.Entities.Level
                {
                    LevelName = formattedLevelName,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                };

                // Parse approvers JSON
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var approversList = System.Text.Json.JsonSerializer.Deserialize<List<ApproverData>>(model.ApproversJson, options);
                
                if (approversList == null || !approversList.Any())
                {
                    return Json(new { success = false, message = "At least one approver is required." });
                }

                // Validate all RoleIds exist before proceeding
                var allRoles = await _levelService.GetAllRolesAsync();
                var validRoleIds = allRoles.Select(r => r.Id).ToList();
                
                foreach (var approverData in approversList)
                {
                    if (!validRoleIds.Contains(approverData.RoleId))
                    {
                        return Json(new { success = false, message = $"Invalid role selected (ID: {approverData.RoleId}). Please refresh the page and try again." });
                    }
                }

                // Add level first
                await _levelService.AddLevelAsync(level);

                // Add approvers
                foreach (var approverData in approversList)
                {
                    var levelApprover = new Models.Entities.LevelApprover
                    {
                        LevelId = level.Id,
                        RoleId = approverData.RoleId,
                        IsPrimary = approverData.IsPrimary,
                        ApprovalLevel = approverData.ApprovalLevel,
                        CreatedAt = DateTime.Now
                    };

                    await _levelService.AddLevelApproverAsync(levelApprover);
                }

                return Json(new { success = true, message = "Level and approvers added successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public class ApproverData
        {
            public int RoleId { get; set; }
            public int ApprovalLevel { get; set; }
            public bool IsPrimary { get; set; }
        }

        // POST: Level/AddApprover
        [HttpPost]
        public async Task<IActionResult> AddApprover(AddApproverViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Validation failed. Please check your input." });
                }

                // Check if level exists
                var level = await _levelService.GetLevelByIdAsync(model.LevelId);
                if (level == null)
                {
                    return Json(new { success = false, message = "Level not found." });
                }

                // Check if role is already assigned to this level
                var existingApprover = level.LevelApprovers.FirstOrDefault(la => la.RoleId == model.RoleId);
                if (existingApprover != null)
                {
                    return Json(new { success = false, message = "This role is already assigned as an approver for this level." });
                }

                var levelApprover = new Models.Entities.LevelApprover
                {
                    LevelId = model.LevelId,
                    RoleId = model.RoleId,
                    IsPrimary = model.IsPrimary,
                    ApprovalLevel = model.ApprovalLevel,
                    CreatedAt = DateTime.Now
                };

                await _levelService.AddLevelApproverAsync(levelApprover);
                return Json(new { success = true, message = "Approver added successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Level/DeleteApprover
        [HttpPost]
        public async Task<IActionResult> DeleteApprover(int levelApproverId)
        {
            try
            {
                var result = await _levelService.DeleteLevelApproverAsync(levelApproverId);
                if (result)
                {
                    return Json(new { success = true, message = "Approver deleted successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Approver not found." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // GET: LevelManagement/GetLevel/5 - Get level details for editing
        [HttpGet]
        public async Task<IActionResult> GetLevel(int id)
        {
            try
            {
                var level = await _levelService.GetLevelByIdAsync(id);
                if (level == null)
                {
                    return Json(new { success = false, message = "Level not found." });
                }

                var roles = await _levelService.GetAllRolesAsync();
                var roleList = roles.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.RoleName
                }).ToList();

                return Json(new { 
                    success = true, 
                    level = new {
                        id = level.Id,
                        levelName = level.LevelName,
                        isActive = level.IsActive,
                        levelApprovers = level.LevelApprovers.Select(la => new {
                            id = la.Id,
                            roleId = la.RoleId,
                            roleName = la.Role.RoleName,
                            isPrimary = la.IsPrimary,
                            approvalLevel = la.ApprovalLevel
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

        // POST: LevelManagement/Edit/5 - Update level
        [HttpPost]
        public async Task<IActionResult> Edit(int id, EditLevelViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    return Json(new { success = false, message = "Invalid level ID." });
                }

                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Validation failed. Please check your input." });
                }

                // Format level name with LV prefix
                var formattedLevelName = model.LevelName.Trim().ToUpperInvariant();
                if (!formattedLevelName.StartsWith("LV"))
                {
                    formattedLevelName = "LV" + formattedLevelName;
                }

                // Parse approvers JSON
                List<ApproverData> approversList = new List<ApproverData>();
                if (!string.IsNullOrEmpty(model.ApproversJson))
                {
                    try
                    {
                        var deserializedList = JsonSerializer.Deserialize<List<ApproverData>>(model.ApproversJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        approversList = deserializedList ?? new List<ApproverData>();
                    }
                    catch (JsonException)
                    {
                        return Json(new { success = false, message = "Invalid approvers data format." });
                    }
                }

                if (approversList.Count == 0)
                {
                    return Json(new { success = false, message = "At least one approver is required." });
                }

                // Update level basic info
                var level = await _levelService.GetLevelByIdAsync(id);
                if (level == null)
                {
                    return Json(new { success = false, message = "Level not found." });
                }

                level.LevelName = formattedLevelName;
                level.IsActive = model.IsActive;
                level.UpdatedAt = DateTime.Now;

                await _levelService.UpdateLevelAsync(level);

                // Update approvers - remove all existing and add new ones
                var existingApprovers = level.LevelApprovers.ToList();
                foreach (var approver in existingApprovers)
                {
                    await _levelService.DeleteLevelApproverAsync(approver.Id);
                }

                // Add new approvers
                foreach (var approverData in approversList)
                {
                    var levelApprover = new Models.Entities.LevelApprover
                    {
                        LevelId = level.Id,
                        RoleId = approverData.RoleId,
                        IsPrimary = approverData.IsPrimary,
                        ApprovalLevel = approverData.ApprovalLevel,
                        CreatedAt = DateTime.Now
                    };

                    await _levelService.AddLevelApproverAsync(levelApprover);
                }

                return Json(new { success = true, message = "Level updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // GET: Level/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var level = await _levelService.GetLevelByIdAsync(id);
                if (level == null)
                {
                    // Check if request is AJAX
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, error = "Level not found." });
                    }
                    TempData["ErrorMessage"] = "Level not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if level has approvers or is used in workflow stages
                if (level.LevelApprovers.Any() || level.WorkflowStages.Any())
                {
                    var errorMessage = "Cannot delete level. It has assigned approvers or is used in workflow stages. Please remove them first.";
                    
                    // Check if request is AJAX
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, error = errorMessage });
                    }
                    
                    TempData["ErrorMessage"] = errorMessage;
                    return RedirectToAction("Details", new { id = id });
                }

                var result = await _levelService.DeleteLevelAsync(id);
                if (result)
                {
                    // Check if request is AJAX
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Level deleted successfully." });
                    }
                    
                    TempData["SuccessMessage"] = "Level deleted successfully.";
                }
                else
                {
                    var errorMessage = "Failed to delete level.";
                    
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
                var errorMessage = $"Error deleting level: {ex.Message}";
                
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