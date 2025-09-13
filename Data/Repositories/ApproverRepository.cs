using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class ApproverRepository : IApproverRepository
    {
        private readonly AppDbContext _context;
        private readonly IRolesRepository _rolesRepository;

        public ApproverRepository(AppDbContext context, IRolesRepository rolesRepository)
        {
            _context = context;
            _rolesRepository = rolesRepository;
        }

        public async Task<IEnumerable<Approver>> GetAllAsync()
        {
            return await _context.Approvers
                .OrderBy(a => a.ApproverName)
                .ToListAsync();
        }

        public async Task<Approver?> GetByIdAsync(int id)
        {
            return await _context.Approvers
                .Include(a => a.ApproverRoles)
                    .ThenInclude(ar => ar.Role)
                .Include(a => a.WorkflowStages)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Approver> AddAsync(Approver approver)
        {
            _context.Approvers.Add(approver);
            await _context.SaveChangesAsync();
            return approver;
        }

        public async Task<Approver> UpdateAsync(Approver approver)
        {
            _context.Entry(approver).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return approver;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var approver = await _context.Approvers.FindAsync(id);
            if (approver == null) return false;

            _context.Approvers.Remove(approver);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            return await _rolesRepository.GetAllAsync();
        }

        public async Task<ApproverRole> AddApproverRoleAsync(ApproverRole approverRole)
        {
            _context.ApproverRoles.Add(approverRole);
            await _context.SaveChangesAsync();
            return approverRole;
        }

        public async Task<ApproverRole?> GetApproverRoleByIdAsync(int approverRoleId)
        {
            return await _context.ApproverRoles
                .Include(ar => ar.Role)
                .Include(ar => ar.Approver)
                .FirstOrDefaultAsync(ar => ar.Id == approverRoleId);
        }

        public async Task<bool> DeleteApproverRoleAsync(int approverRoleId)
        {
            var approverRole = await _context.ApproverRoles.FindAsync(approverRoleId);
            if (approverRole == null) return false;

            _context.ApproverRoles.Remove(approverRole);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}