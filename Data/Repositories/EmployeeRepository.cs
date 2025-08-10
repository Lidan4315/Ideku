using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly AppDbContext _context;

        public EmployeeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Employee?> GetByEmployeeIdAsync(string employeeId)
        {
            return await _context.Employees
                .Include(e => e.DivisionNavigation)
                .Include(e => e.DepartmentNavigation)
                .FirstOrDefaultAsync(e => e.EMP_ID == employeeId && e.EMP_STATUS == "Active");
        }

        public async Task<IEnumerable<Employee>> GetAllActiveAsync()
        {
            return await _context.Employees
                .Include(e => e.DivisionNavigation)
                .Include(e => e.DepartmentNavigation)
                .Where(e => e.EMP_STATUS == "Active")
                .OrderBy(e => e.NAME)
                .ToListAsync();
        }
    }
}