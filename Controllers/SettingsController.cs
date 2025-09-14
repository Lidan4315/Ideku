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
    }
}