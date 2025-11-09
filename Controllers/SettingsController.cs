using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Helpers;

namespace Ideku.Controllers
{
    [Authorize]
    [ModuleAuthorize("settings")]
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // Management Actions
        public IActionResult WorkflowManagement()
        {
            return RedirectToAction("Index", "WorkflowManagement");
        }

        public IActionResult ApproverManagement()
        {
            return RedirectToAction("Index", "ApproverManagement");
        }

        public IActionResult RoleManagement()
        {
            return RedirectToAction("Index", "RoleManagement");
        }

        public IActionResult UserManagement()
        {
            return RedirectToAction("Index", "UserManagement");
        }

        public IActionResult ChangeWorkflow()
        {
            return RedirectToAction("Index", "ChangeWorkflow");
        }
    }
}