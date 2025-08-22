using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.LevelManagement
{
    public class LevelApproverViewModel
    {
        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "Approval Level is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Approval Level must be greater than 0")]
        
        [Display(Name = "Approval Level")]
        public int ApprovalLevel { get; set; }

        [Display(Name = "Primary Approver")]
        public bool IsPrimary { get; set; } = false;

        // Display properties (for showing in lists)
        public string RoleName { get; set; } = string.Empty;
        public int Id { get; set; } // For editing existing approvers
    }
}