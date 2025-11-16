using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services.WorkflowManagement;
using Ideku.Services.Approver;
using Ideku.Services.Lookup;
using Ideku.ViewModels.WorkflowManagement;
using Microsoft.AspNetCore.Mvc.Rendering;
using Ideku.Helpers;

namespace Ideku.Controllers
{
    [Authorize]
    [ModuleAuthorize("workflow_management")]
    public class WorkflowManagementController : Controller
    {
        private readonly IWorkflowManagementService _workflowService;
        private readonly IApproverService _approverService;
        private readonly ILookupService _lookupService;

        public WorkflowManagementController(IWorkflowManagementService workflowService, IApproverService approverService, ILookupService lookupService)
        {
            _workflowService = workflowService;
            _approverService = approverService;
            _lookupService = lookupService;
        }

        // GET: WorkflowManagement - List semua workflow
        public async Task<IActionResult> Index()
        {
            try
            {
                Console.WriteLine("DEBUG: Entering WorkflowManagement Index");
                var workflows = await _workflowService.GetAllWorkflowsAsync();
                Console.WriteLine($"DEBUG: Found {workflows.Count()} workflows");
                return View(workflows);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Exception in WorkflowManagement: {ex.Message}");
                TempData["ErrorMessage"] = $"Error loading workflows: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: WorkflowManagement/Create - Tambah workflow baru  
        [HttpPost]
        public async Task<IActionResult> Create(CreateWorkflowViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Validation failed. Please check your input." });
                }

                // Format workflow name - ensure it starts with WF_ and is uppercase
                var formattedWorkflowName = model.WorkflowName.Trim().ToUpper();
                if (!formattedWorkflowName.StartsWith("WF_"))
                {
                    formattedWorkflowName = "WF_" + formattedWorkflowName;
                }

                var workflow = new Models.Entities.Workflow
                {
                    WorkflowName = formattedWorkflowName,
                    Desc = string.IsNullOrEmpty(model.Desc) ? null : model.Desc.Trim(),
                    Priority = model.Priority,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                };

                await _workflowService.AddWorkflowAsync(workflow);
                return Json(new { success = true, message = "Workflow added successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // GET: WorkflowManagement/Details/5 - Detail workflow dengan stages dan conditions
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var workflow = await _workflowService.GetWorkflowByIdAsync(id);
                if (workflow == null)
                {
                    TempData["ErrorMessage"] = "Workflow not found.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new WorkflowDetailsViewModel
                {
                    Workflow = workflow,
                    AddStageForm = new WorkflowStageViewModel { WorkflowId = workflow.Id },
                    AddConditionForm = new WorkflowConditionViewModel { WorkflowId = workflow.Id }
                };

                // Load dropdown data
                var approvers = await _approverService.GetAllApproversAsync();
                viewModel.LevelList = approvers.Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = a.ApproverName
                }).ToList();

                viewModel.CategoryList = await _lookupService.GetCategoriesAsync();
                viewModel.DivisionList = await _lookupService.GetDivisionsAsync();
                viewModel.DepartmentList = await _lookupService.GetDepartmentsByDivisionAsync("");
                viewModel.EventList = await _lookupService.GetEventsAsync();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading workflow details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: WorkflowManagement/AddStage - Tambah stage ke workflow
        [HttpPost]
        public async Task<IActionResult> AddStage(WorkflowStageViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Validation failed. Please check your input." });
                }

                var workflowStage = new Models.Entities.WorkflowStage
                {
                    WorkflowId = model.WorkflowId,
                    ApproverId = model.LevelId,
                    Stage = model.Stage,
                    IsMandatory = model.IsMandatory,
                    IsParallel = model.IsParallel,
                    CreatedAt = DateTime.Now
                };

                await _workflowService.AddWorkflowStageAsync(workflowStage);
                return Json(new { success = true, message = "Workflow stage added successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: WorkflowManagement/DeleteStage - Hapus stage dari workflow
        [HttpPost]
        public async Task<IActionResult> DeleteStage(int stageId)
        {
            try
            {
                var result = await _workflowService.DeleteWorkflowStageAsync(stageId);
                if (result)
                {
                    return Json(new { success = true, message = "Workflow stage deleted successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete workflow stage." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: WorkflowManagement/AddCondition - Tambah condition ke workflow
        [HttpPost]
        public async Task<IActionResult> AddCondition(WorkflowConditionViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { 
                            Field = x.Key, 
                            Errors = x.Value.Errors.Select(e => e.ErrorMessage) 
                        })
                        .ToList();
                    
                    var errorMessage = "Validation failed: " + string.Join("; ", 
                        errors.SelectMany(e => e.Errors.Select(err => $"{e.Field}: {err}")));
                    
                    return Json(new { success = false, message = errorMessage, validationErrors = errors });
                }

                var workflowCondition = new Models.Entities.WorkflowCondition
                {
                    WorkflowId = model.WorkflowId,
                    ConditionType = model.ConditionType,
                    Operator = model.Operator,
                    ConditionValue = model.ConditionValue,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                };

                await _workflowService.AddWorkflowConditionAsync(workflowCondition);
                return Json(new { success = true, message = "Workflow condition added successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: WorkflowManagement/DeleteCondition - Hapus condition dari workflow
        [HttpPost]
        public async Task<IActionResult> DeleteCondition(int conditionId)
        {
            try
            {
                var result = await _workflowService.DeleteWorkflowConditionAsync(conditionId);
                if (result)
                {
                    return Json(new { success = true, message = "Workflow condition deleted successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete workflow condition." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // GET: WorkflowManagement/Edit/5 - Show edit form
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var workflow = await _workflowService.GetWorkflowByIdAsync(id);
                if (workflow == null)
                {
                    TempData["ErrorMessage"] = "Workflow not found.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new EditWorkflowViewModel
                {
                    Id = workflow.Id,
                    WorkflowName = workflow.WorkflowName,
                    Desc = workflow.Desc,
                    Priority = workflow.Priority,
                    IsActive = workflow.IsActive
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading workflow: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: WorkflowManagement/Edit/5 - Update workflow
        [HttpPost]
        public async Task<IActionResult> Edit(int id, EditWorkflowViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    return Json(new { success = false, message = "Invalid workflow ID." });
                }

                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Validation failed. Please check your input." });
                }

                // Format workflow name - ensure it starts with WF_ and is uppercase
                var formattedWorkflowName = model.WorkflowName.Trim().ToUpper();
                if (!formattedWorkflowName.StartsWith("WF_"))
                {
                    formattedWorkflowName = "WF_" + formattedWorkflowName;
                }

                var updateModel = new EditWorkflowViewModel
                {
                    Id = model.Id,
                    WorkflowName = formattedWorkflowName,
                    Desc = model.Desc?.Trim(),
                    Priority = model.Priority,
                    IsActive = model.IsActive
                };

                var result = await _workflowService.UpdateWorkflowAsync(updateModel);
                if (result)
                {
                    return Json(new { success = true, message = "Workflow updated successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update workflow." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // GET: WorkflowManagement/Delete/5 - Hapus workflow
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var workflow = await _workflowService.GetWorkflowByIdAsync(id);
                if (workflow == null)
                {
                    // Check if this is an AJAX request
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, error = "Workflow not found." });
                    }
                    
                    TempData["ErrorMessage"] = "Workflow not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Validasi: workflow tidak boleh dihapus jika masih ada stages atau conditions
                if (workflow.WorkflowStages.Any() || workflow.WorkflowConditions.Any())
                {
                    var errorMessage = "Cannot delete workflow. It has assigned stages or conditions. Please remove them first.";
                    
                    // Check if this is an AJAX request
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, error = errorMessage });
                    }
                    
                    TempData["ErrorMessage"] = errorMessage;
                    return RedirectToAction(nameof(Index));
                }

                var result = await _workflowService.DeleteWorkflowAsync(id);
                
                // Check if this is an AJAX request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    if (result)
                    {
                        return Json(new { success = true, message = "Workflow deleted successfully." });
                    }
                    else
                    {
                        return Json(new { success = false, error = "Failed to delete workflow." });
                    }
                }
                
                // For non-AJAX requests
                if (result)
                {
                    TempData["SuccessMessage"] = "Workflow deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete workflow.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error deleting workflow: {ex.Message}";
                
                // Check if this is an AJAX request
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