using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Ideku.Data.Context;
using Ideku.ViewModels;

namespace Ideku.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<UserSessionDto?> ValidateUserAsync(string username)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Employee)
                        .ThenInclude(e => e.DivisionNavigation)
                    .Include(u => u.Employee)
                        .ThenInclude(e => e.DepartmentNavigation)
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    _logger.LogWarning("Login attempt with invalid username: {Username}", username);
                    return null;
                }

                return new UserSessionDto
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Name = user.Name,
                    EmployeeId = user.EmployeeId,
                    Email = user.Employee.EMAIL,
                    RoleName = user.Role.RoleName,
                    Division = user.Employee.DivisionNavigation.NameDivision,
                    Department = user.Employee.DepartmentNavigation.NameDepartment,
                    IsActing = user.IsActing
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user: {Username}", username);
                return null;
            }
        }

        public async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(UserSessionDto user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.GivenName, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.RoleName),
                new Claim("EmployeeId", user.EmployeeId),
                new Claim("Division", user.Division),
                new Claim("Department", user.Department),
                new Claim("IsActing", user.IsActing.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }

        public async Task SignInAsync(UserSessionDto user, bool rememberMe)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            var principal = await CreateClaimsPrincipalAsync(user);
            
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
            
            _logger.LogInformation("User {Username} signed in successfully", user.Username);
        }

        public async Task SignOutAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            _logger.LogInformation("User signed out");
        }

        public UserSessionDto? GetCurrentUser()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return null;

            var user = httpContext.User;
            
            return new UserSessionDto
            {
                UserId = long.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"),
                Username = user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
                Name = user.FindFirst(ClaimTypes.GivenName)?.Value ?? string.Empty,
                EmployeeId = user.FindFirst("EmployeeId")?.Value ?? string.Empty,
                Email = user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                RoleName = user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
                Division = user.FindFirst("Division")?.Value ?? string.Empty,
                Department = user.FindFirst("Department")?.Value ?? string.Empty,
                IsActing = bool.Parse(user.FindFirst("IsActing")?.Value ?? "false")
            };
        }
    }
}