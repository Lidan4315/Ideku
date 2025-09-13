using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public interface IEmployeeRepository
    {
        Task<Employee?> GetByEmployeeIdAsync(string employeeId);
        Task<IEnumerable<Employee>> GetAllActiveAsync();

        /// <summary>
        /// Get active employees that don't have user accounts yet
        /// Used for Create User dropdown options
        /// </summary>
        Task<IEnumerable<Employee>> GetEmployeesWithoutUsersAsync();
    }
}