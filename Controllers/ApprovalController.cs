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

        public ApprovalController(IWorkflowService workflowService)
        {
            _workflowService = workflowService;
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

            var viewModel = new ApprovalListViewModel
            {
                IdeasForApproval = ideas
            };

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

            var viewModel = new ApprovalReviewViewModel
            {
                Idea = idea
                // Populate other properties as needed
            };

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

            if (ModelState.IsValid)
            {
                await _workflowService.ProcessApprovalAsync(id, username, viewModel.ApprovalComments, viewModel.ValidatedSavingCost);
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

            // We only need to validate the rejection reason
            if (string.IsNullOrWhiteSpace(viewModel.RejectionReason))
            {
                ModelState.AddModelError("RejectionReason", "Rejection reason is required.");
            }

            if (ModelState.IsValid)
            {
                await _workflowService.ProcessRejectionAsync(id, username, viewModel.RejectionReason);
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
