using Ideku.Data.Repositories;
using Ideku.Services.Workflow;
using Ideku.ViewModels.Approval;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ideku.Controllers
{
    [Authorize]
    public class ApprovalController : Controller
    {
        private readonly IWorkflowService _workflowService;
        private readonly IUserRepository _userRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly ILookupRepository _lookupRepository;

        public ApprovalController(IWorkflowService workflowService, IUserRepository userRepository, IWorkflowRepository workflowRepository, ILookupRepository lookupRepository)
        {
            _workflowService = workflowService;
            _userRepository = userRepository;
            _workflowRepository = workflowRepository;
            _lookupRepository = lookupRepository;
        }

        // GET: /Approval or /Approval/Index
        public async Task<IActionResult> Index(
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
                return Challenge(); // Or redirect to login
            }

            var ideas = await _workflowService.GetPendingApprovalsForUserAsync(username);
            var user = await _userRepository.GetByUsernameAsync(username);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                ideas = ideas.Where(i => 
                    i.IdeaCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    i.IdeaName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (i.InitiatorUser?.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                );
            }

            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                ideas = ideas.Where(i => i.ToDivisionId == selectedDivision);
            }

            if (!string.IsNullOrWhiteSpace(selectedDepartment))
            {
                ideas = ideas.Where(i => i.ToDepartmentId == selectedDepartment);
            }

            if (selectedCategory.HasValue)
            {
                ideas = ideas.Where(i => i.CategoryId == selectedCategory.Value);
            }

            if (selectedStage.HasValue)
            {
                ideas = ideas.Where(i => i.CurrentStage == selectedStage.Value);
            }

            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                ideas = ideas.Where(i => i.CurrentStatus == selectedStatus);
            }

            // Get lookup data for dropdowns
            var divisions = await _lookupRepository.GetDivisionsAsync();
            var categories = await _lookupRepository.GetCategoriesAsync();

            var ideaList = ideas.ToList();
            
            var viewModel = new ApprovalListViewModel
            {
                IdeasForApproval = ideaList,
                SearchTerm = searchTerm,
                SelectedDivision = selectedDivision,
                SelectedDepartment = selectedDepartment,
                SelectedCategory = selectedCategory,
                SelectedStage = selectedStage,
                SelectedStatus = selectedStatus
            };

            // Pass lookup data to view
            ViewBag.Divisions = divisions;
            ViewBag.Categories = categories;
            ViewBag.UserRole = user?.Role?.RoleName ?? "";

            return View(viewModel);
        }

        // AJAX endpoint for real-time filtering
        [HttpGet]
        public async Task<IActionResult> FilterIdeas(
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

            var ideas = await _workflowService.GetPendingApprovalsForUserAsync(username);

            // Apply filters (same logic as Index method)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                ideas = ideas.Where(i => 
                    i.IdeaCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    i.IdeaName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (i.InitiatorUser?.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                );
            }

            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                ideas = ideas.Where(i => i.ToDivisionId == selectedDivision);
            }

            if (!string.IsNullOrWhiteSpace(selectedDepartment))
            {
                ideas = ideas.Where(i => i.ToDepartmentId == selectedDepartment);
            }

            if (selectedCategory.HasValue)
            {
                ideas = ideas.Where(i => i.CategoryId == selectedCategory.Value);
            }

            if (selectedStage.HasValue)
            {
                ideas = ideas.Where(i => i.CurrentStage == selectedStage.Value);
            }

            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                ideas = ideas.Where(i => i.CurrentStatus == selectedStatus);
            }

            var ideaList = ideas.ToList();
            
            // Return JSON with filtered ideas and count
            return Json(new { 
                success = true, 
                ideas = ideaList.Select(i => new {
                    ideaCode = i.IdeaCode,
                    ideaName = i.IdeaName,
                    initiatorName = i.InitiatorUser?.Name,
                    divisionName = i.TargetDivision?.NameDivision,
                    departmentName = i.TargetDepartment?.NameDepartment,
                    categoryName = i.Category?.CategoryName,
                    eventName = i.Event?.EventName,
                    currentStage = i.CurrentStage,
                    savingCost = i.SavingCost,
                    currentStatus = i.CurrentStatus,
                    submittedDate = i.SubmittedDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    reviewUrl = Url.Action("Review", new { id = i.Id })
                }),
                totalCount = ideaList.Count
            });
        }


        // AJAX endpoint for cascading dropdown - Get departments by division
        [HttpGet]
        public async Task<JsonResult> GetDepartmentsByDivision(string divisionId)
        {
            if (string.IsNullOrWhiteSpace(divisionId))
            {
                return Json(new { success = true, departments = new List<object>() });
            }

            try
            {
                // Use real data from LookupRepository
                var departmentSelectList = await _lookupRepository.GetDepartmentsByDivisionAsync(divisionId);
                
                // Convert SelectListItem to simple object for JSON
                var departments = departmentSelectList
                    .Where(d => !string.IsNullOrEmpty(d.Value)) // Filter out empty options
                    .Select(d => new { id = d.Value, name = d.Text })
                    .ToList();

                return Json(new { success = true, departments });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error loading departments" });
            }
        }

        // GET: /Approval/Review/{id}
        public async Task<IActionResult> Review(int id)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Challenge();
            }

            var idea = await _workflowService.GetIdeaForReview(id, username);
            if (idea == null)
            {
                return NotFound(); // Or a custom access denied page
            }

            // Determine if user can actually approve/reject this idea
            var user = await _userRepository.GetByUsernameAsync(username);
            bool canTakeAction = false;
            
            if (user?.Role?.RoleName == "Superuser" && idea.CurrentStatus.StartsWith("Waiting Approval"))
            {
                canTakeAction = true;
            }
            else if (user?.Role?.RoleName == "Workstream Leader" && 
                     idea.CurrentStage == 0 && 
                     idea.CurrentStatus == "Waiting Approval S1")
            {
                canTakeAction = true;
            }

            // Get workflow history for this idea
            var workflowHistory = await _workflowRepository.GetByIdeaIdAsync(idea.Id);

            var viewModel = new ApprovalReviewViewModel
            {
                Idea = idea
                // Populate other properties as needed
            };

            // Pass action capability and workflow history to view
            ViewBag.CanTakeAction = canTakeAction;
            ViewBag.UserRole = user?.Role?.RoleName ?? "";
            ViewBag.WorkflowHistory = workflowHistory;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, ApprovalReviewViewModel viewModel)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Challenge();
            }

            // Remove validation errors for properties not needed for approval
            ModelState.Remove("Idea");
            ModelState.Remove("RejectionReason");

            if (ModelState.IsValid)
            {
                await _workflowService.ProcessApprovalAsync((long)id, username, viewModel.ApprovalComments, viewModel.ValidatedSavingCost);
                TempData["SuccessMessage"] = "Idea has been successfully approved!";
                return RedirectToAction(nameof(Index));
            }

            // If model state is invalid, reload the review page
            var idea = await _workflowService.GetIdeaForReview(id, username);
            if (idea == null)
            {
                return NotFound();
            }
            viewModel.Idea = idea;
            return View("Review", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, ApprovalReviewViewModel viewModel)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Challenge();
            }

            // Remove validation errors for properties not needed for rejection
            ModelState.Remove("Idea");
            ModelState.Remove("ValidatedSavingCost");
            ModelState.Remove("ApprovalComments");
            
            // We only need to validate the rejection reason
            if (string.IsNullOrWhiteSpace(viewModel.RejectionReason))
            {
                ModelState.AddModelError("RejectionReason", "Rejection reason is required.");
            }

            if (ModelState.IsValid)
            {
                await _workflowService.ProcessRejectionAsync((long)id, username, viewModel.RejectionReason ?? "No reason provided");
                TempData["SuccessMessage"] = "Idea has been successfully rejected!";
                return RedirectToAction(nameof(Index));
            }

            // If model state is invalid, reload the review page
            var idea = await _workflowService.GetIdeaForReview(id, username);
            if (idea == null)
            {
                return NotFound();
            }
            viewModel.Idea = idea;
            return View("Review", viewModel);
        }
    }
}
