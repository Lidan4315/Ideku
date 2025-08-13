using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services.Level;

namespace Ideku.Controllers
{
    [Authorize]
    public class LevelController : Controller
    {
        private readonly ILevelService _levelService;

        public LevelController(ILevelService levelService)
        {
            _levelService = levelService;
        }

        // GET: Level
        public async Task<IActionResult> Index()
        {
            try
            {
                var levels = await _levelService.GetAllLevelsAsync();
                return View(levels);
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

                // Load roles for dropdown
                ViewBag.Roles = await _levelService.GetAllRolesAsync();
                
                return View(level);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading level details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Level/Create
        [HttpPost]
        public async Task<IActionResult> Create(string levelName, string desc, bool isActive)
        {
            try
            {
                if (string.IsNullOrEmpty(levelName) || string.IsNullOrEmpty(desc))
                {
                    return Json(new { success = false, message = "Level Name and Description are required." });
                }

                var level = new Models.Entities.Level
                {
                    LevelName = levelName.Trim(),
                    Desc = desc.Trim(),
                    IsActive = isActive,
                    CreatedAt = DateTime.Now
                };

                await _levelService.AddLevelAsync(level);
                return Json(new { success = true, message = "Level added successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Level/AddApprover
        [HttpPost]
        public async Task<IActionResult> AddApprover(int levelId, int roleId, bool isPrimary, int approvalLevel)
        {
            try
            {
                // Check if level exists
                var level = await _levelService.GetLevelByIdAsync(levelId);
                if (level == null)
                {
                    return Json(new { success = false, message = "Level not found." });
                }

                // Check if role is already assigned to this level
                var existingApprover = level.LevelApprovers.FirstOrDefault(la => la.RoleId == roleId);
                if (existingApprover != null)
                {
                    return Json(new { success = false, message = "This role is already assigned as an approver for this level." });
                }

                var levelApprover = new Models.Entities.LevelApprover
                {
                    LevelId = levelId,
                    RoleId = roleId,
                    IsPrimary = isPrimary,
                    ApprovalLevel = approvalLevel,
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

        // GET: Level/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var level = await _levelService.GetLevelByIdAsync(id);
                if (level == null)
                {
                    TempData["ErrorMessage"] = "Level not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if level has approvers or is used in workflow stages
                if (level.LevelApprovers.Any() || level.WorkflowStages.Any())
                {
                    TempData["ErrorMessage"] = "Cannot delete level. It has assigned approvers or is used in workflow stages. Please remove them first.";
                    return RedirectToAction("Details", new { id = id });
                }

                var result = await _levelService.DeleteLevelAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Level deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete level.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting level: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}