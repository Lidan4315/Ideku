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

        public ApprovalController(IWorkflowService workflowService, IUserRepository userRepository, IWorkflowRepository workflowRepository)
        {
            _workflowService = workflowService;
            _userRepository = userRepository;
            _workflowRepository = workflowRepository;
        }

        // GET: /Approval or /Approval/Index
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Challenge(); // Or redirect to login
            }

            var ideas = await _workflowService.GetPendingApprovalsForUserAsync(username);
            var user = await _userRepository.GetByUsernameAsync(username);

            var viewModel = new ApprovalListViewModel
            {
                IdeasForApproval = ideas
            };

            // Pass user role to view for button logic
            ViewBag.UserRole = user?.Role?.RoleName ?? "";

            return View(viewModel);
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
