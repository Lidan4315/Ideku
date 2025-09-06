using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services.Idea;
using Ideku.ViewModels;
using Ideku.Services.Workflow;
using Ideku.Models;

namespace Ideku.Controllers
{
    [Authorize]
    public class IdeaController : Controller
    {
        private readonly IIdeaService _ideaService;
        private readonly IWorkflowService _workflowService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<IdeaController> _logger;

        public IdeaController(
            IIdeaService ideaService, 
            IWorkflowService workflowService,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<IdeaController> logger)
        {
            _ideaService = ideaService;
            _workflowService = workflowService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        // GET: Idea/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var viewModel = await _ideaService.PrepareCreateViewModelAsync(username);
                return View(viewModel);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading form: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Idea/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateIdeaViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _ideaService.CreateIdeaAsync(model, model.AttachmentFiles);
                
                if (result.Success && result.CreatedIdea != null)
                {
                    _logger.LogInformation("Idea {IdeaId} - {IdeaName} created successfully by user {Username}", 
                        result.CreatedIdea.Id, result.CreatedIdea.IdeaName, User.Identity?.Name);

                    // Send notification emails in background
                    SendEmailInBackground(result.CreatedIdea);

                    return Json(new { 
                        success = true, 
                        message = result.Message, 
                        ideaCode = result.CreatedIdea.IdeaCode 
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to create idea for user {Username}: {ErrorMessage}", 
                        User.Identity?.Name, result.Message);
                    
                    return Json(new { 
                        success = false, 
                        message = result.Message 
                    });
                }
            }

            // Return validation errors as JSON
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { 
                success = false, 
                message = "Please check your input", 
                errors = errors 
            });
        }

        /// <summary>
        /// Helper method to send emails in background with proper scope management
        /// </summary>
        private void SendEmailInBackground(Models.Entities.Idea idea)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting background email process for idea {IdeaId} - {IdeaName}", 
                        idea.Id, idea.IdeaName);

                    // Create new scope for background task
                    using var scope = _serviceScopeFactory.CreateScope();
                    var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();
                    
                    // Send notification emails
                    await workflowService.InitiateWorkflowAsync(idea);
                    
                    _logger.LogInformation("Background email sent successfully for idea {IdeaId}", idea.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send background email for idea {IdeaId}: {ErrorMessage}", 
                        idea.Id, ex.Message);
                    
                    // Future: Could add retry mechanism or admin notification
                    // await _retryService.AddToRetryQueueAsync(idea.Id);
                }
            });
        }

        // AJAX: Get Departments by Division
        [HttpGet]
        public async Task<JsonResult> GetDepartmentsByDivision(string divisionId)
        {
            var departments = await _ideaService.GetDepartmentsByDivisionAsync(divisionId);
            return Json(departments);
        }

        // AJAX: Get Employee by Badge Number
        [HttpGet]
        public async Task<JsonResult> GetEmployeeByBadgeNumber(string badgeNumber)
        {
            var employee = await _ideaService.GetEmployeeByBadgeNumberAsync(badgeNumber);
            if (employee == null)
            {
                return Json(new { success = false, message = "Employee not found" });
            }
            return Json(new { success = true, data = employee });
        }

        // GET: Idea/Index (My Ideas)
        public async Task<IActionResult> Index()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var myIdeas = await _ideaService.GetUserIdeasAsync(username);
                return View(myIdeas);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading ideas: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
