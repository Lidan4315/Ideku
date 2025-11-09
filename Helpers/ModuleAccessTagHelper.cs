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
        private readonly ILogger<ModuleAccessTagHelper> _logger;

        [HtmlAttributeName("module-access")]
        public string ModuleKey { get; set; } = string.Empty;

        public ModuleAccessTagHelper(
            IAccessControlService accessControlService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ModuleAccessTagHelper> logger)
        {
            _accessControlService = accessControlService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            _logger.LogInformation("[ModuleAccessTagHelper] Processing module-access for ModuleKey: {ModuleKey}", ModuleKey);

            // Remove the module-access attribute from output
            output.Attributes.RemoveAll("module-access");

            // Get current user
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                _logger.LogWarning("[ModuleAccessTagHelper] User not authenticated - suppressing output");
                // If not authenticated, suppress output
                output.SuppressOutput();
                return;
            }

            // Get user ID
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("[ModuleAccessTagHelper] UserIdClaim: {UserIdClaim}", userIdClaim);

            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
            {
                _logger.LogWarning("[ModuleAccessTagHelper] Invalid UserId claim - suppressing output");
                output.SuppressOutput();
                return;
            }

            // Check if user has access to this module
            var hasAccess = await _accessControlService.CanAccessModuleAsync(userId, ModuleKey);

            _logger.LogInformation("[ModuleAccessTagHelper] UserId={UserId}, ModuleKey={ModuleKey}, HasAccess={HasAccess}",
                userId, ModuleKey, hasAccess);

            if (!hasAccess)
            {
                _logger.LogInformation("[ModuleAccessTagHelper] Suppressing output - user has no access");
                // User doesn't have access - suppress output (hide the element)
                output.SuppressOutput();
            }
            else
            {
                _logger.LogInformation("[ModuleAccessTagHelper] Allowing output - user has access");
            }
        }
    }
}
