using Microsoft.AspNetCore.Mvc.Rendering;
using Ideku.Models.Entities;

namespace Ideku.ViewModels.ApproverManagement
{
    public class ApproverIndexViewModel
    {
        // Main data
        public IEnumerable<Approver> Approvers { get; set; } = new List<Approver>();

        // For Add Approver Modal
        public CreateApproverViewModel CreateApproverForm { get; set; } = new CreateApproverViewModel();

        // Dropdown data
        public List<SelectListItem> RoleList { get; set; } = new List<SelectListItem>();
    }
}