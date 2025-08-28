using Microsoft.AspNetCore.Mvc.Rendering;
using Ideku.Models.Entities;

namespace Ideku.ViewModels.ApproverManagement
{
    public class ApproverDetailsViewModel
    {
        // Main Approver Data
        public Approver Approver { get; set; } = null!;

        // Roles with extended information
        public List<ApproverRoleViewModel> ApproverRoles { get; set; } = new List<ApproverRoleViewModel>();

        // Add Role Form
        public AddApproverRoleViewModel AddApproverRoleForm { get; set; } = new AddApproverRoleViewModel();

        // Dropdown data
        public List<SelectListItem> RoleList { get; set; } = new List<SelectListItem>();

        // Statistics
        public int TotalRoles => ApproverRoles.Count;
    }
}