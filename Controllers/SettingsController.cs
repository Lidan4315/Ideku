using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Ideku.Controllers
{
    [Authorize(Roles = "Superuser,Admin")]
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // Workflow Management Actions
        public IActionResult WorkflowManagement()
        {
            return View("WorkflowManagement/Index");
        }

        public IActionResult Workflows()
        {
            return View("WorkflowManagement/Workflows");
        }

        public IActionResult Stages()
        {
            return View("WorkflowManagement/Stages");
        }

        public IActionResult Conditions()
        {
            return View("WorkflowManagement/Conditions");
        }
    }
}