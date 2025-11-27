using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ideku.Controllers;

[AllowAnonymous]
public class AboutController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
