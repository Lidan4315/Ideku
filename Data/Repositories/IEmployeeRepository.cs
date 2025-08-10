using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public interface IEmployeeRepository
    {
        Task<Employee?> GetByEmployeeIdAsync(string employeeId);
        Task<IEnumerable<Employee>> GetAllActiveAsync();
    }
}