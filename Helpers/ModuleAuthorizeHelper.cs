using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Ideku.Services.AccessControl;
using System.Security.Claims;

namespace Ideku.Helpers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class ModuleAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _moduleKey;
        /// Initialize ModuleAuthorize with module key
        /// <param name="moduleKey">Module key to check access (e.g., "dashboard", "submit_idea")</param>
        public ModuleAuthorizeAttribute(string moduleKey)
        {
            _moduleKey = moduleKey;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<ModuleAuthorizeAttribute>>();

            // Check if user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                logger?.LogWarning("[ModuleAuthorize] User not authenticated - redirecting to Login");
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Get user ID from claims
            var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            logger?.LogInformation("[ModuleAuthorize] UserIdClaim: {UserIdClaim}", userIdClaim);

            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
            {
                logger?.LogWarning("[ModuleAuthorize] Invalid UserId claim - redirecting to Login");
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Get AccessControlService from DI
            var accessControlService = context.HttpContext.RequestServices
                .GetService<IAccessControlService>();

            if (accessControlService == null)
            {
                logger?.LogError("[ModuleAuthorize] AccessControlService not found in DI - denying access");
                // If service not available, deny access for safety
                context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                return;
            }

            // Get ALL ModuleAuthorize attributes from both class and method level
            var controllerActionDescriptor = context.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;

            if (controllerActionDescriptor == null)
            {
                logger?.LogWarning("[ModuleAuthorize] ActionDescriptor is not a ControllerActionDescriptor - allowing access");
                return;
            }

            var controllerType = controllerActionDescriptor.ControllerTypeInfo;
            var methodInfo = controllerActionDescriptor.MethodInfo;

            var allModuleKeys = new List<string>();

            // Get attributes from class level
            if (controllerType != null)
            {
                var classAttributes = controllerType.GetCustomAttributes(typeof(ModuleAuthorizeAttribute), true)
                    .Cast<ModuleAuthorizeAttribute>();
                allModuleKeys.AddRange(classAttributes.Select(a => a._moduleKey));
            }

            // Get attributes from method level
            if (methodInfo != null)
            {
                var methodAttributes = methodInfo.GetCustomAttributes(typeof(ModuleAuthorizeAttribute), true)
                    .Cast<ModuleAuthorizeAttribute>();
                allModuleKeys.AddRange(methodAttributes.Select(a => a._moduleKey));
            }

            // Remove duplicates
            allModuleKeys = allModuleKeys.Distinct().ToList();

            logger?.LogInformation("[ModuleAuthorize] Checking {Count} module(s): {Modules}",
                allModuleKeys.Count, string.Join(", ", allModuleKeys));

            // If no modules specified, allow access (failsafe for controllers without ModuleAuthorize)
            if (allModuleKeys.Count == 0)
            {
                logger?.LogWarning("[ModuleAuthorize] No module keys found - allowing access");
                return;
            }

            // Check if user has access to ALL required modules
            foreach (var moduleKey in allModuleKeys)
            {
                var hasAccess = await accessControlService.CanAccessModuleAsync(userId, moduleKey);

                logger?.LogInformation("[ModuleAuthorize] UserId={UserId}, ModuleKey={ModuleKey}, HasAccess={HasAccess}",
                    userId, moduleKey, hasAccess);

                if (!hasAccess)
                {
                    // User doesn't have access to at least one required module
                    logger?.LogWarning("[ModuleAuthorize] Access DENIED for user {UserId} to module {ModuleKey} - Redirecting to AccessDenied",
                        userId, moduleKey);
                    context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                    return;
                }
            }

            logger?.LogInformation("[ModuleAuthorize] Access GRANTED for user {UserId} to all required modules", userId);
            // User has access to all required modules - allow request to proceed
        }
    }

    /// Alternative attribute for controller-based authorization
    /// Checks access based on controller name instead of module key
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ControllerAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string? _controllerName;
        /// Initialize ControllerAuthorize
        /// If no controller name provided, uses current controller name
        /// <param name="controllerName">Optional controller name to check</param>
        public ControllerAuthorizeAttribute(string? controllerName = null)
        {
            _controllerName = controllerName;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Get user ID from claims
            var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Get controller name
            var controllerName = _controllerName;
            if (string.IsNullOrEmpty(controllerName))
            {
                // Get from route data if not specified
                controllerName = context.RouteData.Values["controller"]?.ToString();
            }

            if (string.IsNullOrEmpty(controllerName))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                return;
            }

            // Get AccessControlService from DI
            var accessControlService = context.HttpContext.RequestServices
                .GetService<IAccessControlService>();

            if (accessControlService == null)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                return;
            }

            // Check if user has access to this controller
            var hasAccess = await accessControlService.CanAccessControllerAsync(userId, controllerName);

            if (!hasAccess)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                return;
            }

            // User has access - allow request to proceed
        }
    }
}
