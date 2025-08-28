using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public interface IApproverRepository
    {
        Task<IEnumerable<Approver>> GetAllAsync();
        Task<Approver?> GetByIdAsync(int id);
        Task<Approver> AddAsync(Approver approver);
        Task<Approver> UpdateAsync(Approver approver);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<Role>> GetAllRolesAsync();
        Task<ApproverRole> AddApproverRoleAsync(ApproverRole approverRole);
        Task<ApproverRole?> GetApproverRoleByIdAsync(int approverRoleId);
        Task<bool> DeleteApproverRoleAsync(int approverRoleId);
    }
}