using Microsoft.AspNetCore.Razor.TagHelpers;
using Ideku.Services.AccessControl;
using System.Security.Claims;

namespace Ideku.Helpers
{
    /// Tag Helper to conditionally render elements based on module access
    /// Usage: <li module-access="dashboard">...</li>
    [HtmlTargetElement(Attributes = "module-access")]
    public class ModuleAccessTagHelper : TagHelper
    {
        private readonly IAccessControlService _accessControlService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        [HtmlAttributeName("module-access")]
        public string ModuleKey { get; set; } = string.Empty;

        public ModuleAccessTagHelper(
            IAccessControlService accessControlService,
            IHttpContextAccessor httpContextAccessor)
        {
            _accessControlService = accessControlService;
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            // Remove the module-access attribute from output
            output.Attributes.RemoveAll("module-access");

            // Get current user
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                // If not authenticated, suppress output
                output.SuppressOutput();
                return;
            }

            // Get user ID
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
            {
                output.SuppressOutput();
                return;
            }

            // Check if user has access to this module
            var hasAccess = await _accessControlService.CanAccessModuleAsync(userId, ModuleKey);

            if (!hasAccess)
            {
                // User doesn't have access - suppress output (hide the element)
                output.SuppressOutput();
            }
        }
    }
}
