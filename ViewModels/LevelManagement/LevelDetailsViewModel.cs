using Microsoft.AspNetCore.Mvc.Rendering;
using Ideku.Models.Entities;

namespace Ideku.ViewModels.LevelManagement
{
    public class LevelDetailsViewModel
    {
        // Main Level Data
        public Level Level { get; set; } = null!;

        // Approvers with extended information
        public List<LevelApproverViewModel> Approvers { get; set; } = new List<LevelApproverViewModel>();

        // Add Approver Form
        public AddApproverViewModel AddApproverForm { get; set; } = new AddApproverViewModel();

        // Dropdown data
        public List<SelectListItem> RoleList { get; set; } = new List<SelectListItem>();

        // Statistics
        public int TotalApprovers => Approvers.Count;
    }
}