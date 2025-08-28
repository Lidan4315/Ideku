using Ideku.Data.Repositories;
using Ideku.Models.Entities;

namespace Ideku.Services.Approver
{
    public class ApproverService : IApproverService
    {
        private readonly IApproverRepository _approverRepository;

        public ApproverService(IApproverRepository approverRepository)
        {
            _approverRepository = approverRepository;
        }

        public async Task<IEnumerable<Models.Entities.Approver>> GetAllApproversAsync()
        {
            return await _approverRepository.GetAllAsync();
        }

        public async Task<Models.Entities.Approver?> GetApproverByIdAsync(int id)
        {
            return await _approverRepository.GetByIdAsync(id);
        }

        public async Task<Models.Entities.Approver> AddApproverAsync(Models.Entities.Approver approver)
        {
            return await _approverRepository.AddAsync(approver);
        }

        public async Task<bool> UpdateApproverAsync(Models.Entities.Approver approver)
        {
            var updatedApprover = await _approverRepository.UpdateAsync(approver);
            return updatedApprover != null;
        }

        public async Task<bool> DeleteApproverAsync(int id)
        {
            return await _approverRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            return await _approverRepository.GetAllRolesAsync();
        }

        public async Task<ApproverRole> AddApproverRoleAsync(ApproverRole approverRole)
        {
            return await _approverRepository.AddApproverRoleAsync(approverRole);
        }

        public async Task<ApproverRole?> GetApproverRoleByIdAsync(int approverRoleId)
        {
            return await _approverRepository.GetApproverRoleByIdAsync(approverRoleId);
        }

        public async Task<bool> DeleteApproverRoleAsync(int approverRoleId)
        {
            return await _approverRepository.DeleteApproverRoleAsync(approverRoleId);
        }
    }
}