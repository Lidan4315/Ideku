using Ideku.ViewModels;
using System.Security.Claims;

namespace Ideku.Services.Auth
{
    public interface IAuthService
    {
        Task<UserSessionDto?> ValidateUserAsync(string username);
        Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(UserSessionDto user);
        Task SignInAsync(UserSessionDto user, bool rememberMe);
        Task SignOutAsync();
        UserSessionDto? GetCurrentUser();
    }
}