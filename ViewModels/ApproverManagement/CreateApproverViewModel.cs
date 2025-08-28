using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.ApproverManagement
{
    public class CreateApproverViewModel
    {
        [Required(ErrorMessage = "Approver Name is required")]
        [StringLength(8, ErrorMessage = "Approver Name cannot exceed 8 characters (excluding APV_ prefix)")]
        [Display(Name = "Approver Name")]
        public string ApproverName { get; set; } = string.Empty;

        [Display(Name = "Active Approver")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "At least one role is required")]
        [Display(Name = "Roles")]
        public List<ApproverRoleViewModel> Roles { get; set; } = new List<ApproverRoleViewModel>();

        // For JSON serialization from form
        public string RolesJson { get; set; } = string.Empty;
    }
}