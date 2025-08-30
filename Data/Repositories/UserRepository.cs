using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByIdAsync(long id)
        {
            return await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserByRoleAsync(string roleName)
        {
            return await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Role.RoleName == roleName);
        }

        public async Task<User?> GetByEmployeeIdAsync(string employeeId)
        {
            return await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        }

        public async Task<List<User>> GetWorkstreamLeadersByDivisionsAsync(List<string> divisionIds)
        {
            if (!divisionIds?.Any() == true)
                return new List<User>();

            return await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.DivisionNavigation)
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "Workstream Leader" && 
                           divisionIds.Contains(u.Employee.DIVISION) &&
                           u.Employee.EMP_STATUS == "Active")
                .OrderBy(u => u.Employee.DivisionNavigation.NameDivision)
                .ThenBy(u => u.Name)
                .ToListAsync();
        }
    }
}
