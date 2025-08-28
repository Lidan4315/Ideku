using Ideku.Models.Entities;

namespace Ideku.Services.Approver
{
    public interface IApproverService
    {
        Task<IEnumerable<Models.Entities.Approver>> GetAllApproversAsync();
        Task<Models.Entities.Approver?> GetApproverByIdAsync(int id);
        Task<Models.Entities.Approver> AddApproverAsync(Models.Entities.Approver approver);
        Task<bool> UpdateApproverAsync(Models.Entities.Approver approver);
        Task<bool> DeleteApproverAsync(int id);
        Task<IEnumerable<Role>> GetAllRolesAsync();
        Task<ApproverRole> AddApproverRoleAsync(ApproverRole approverRole);
        Task<ApproverRole?> GetApproverRoleByIdAsync(int approverRoleId);
        Task<bool> DeleteApproverRoleAsync(int approverRoleId);
    }
}