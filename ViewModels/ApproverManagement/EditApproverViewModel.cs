using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.ApproverManagement
{
    public class EditApproverViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Approver Name is required")]
        [StringLength(20, ErrorMessage = "Approver Name cannot exceed 20 characters")]
        [Display(Name = "Approver Name")]
        public string ApproverName { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // For roles - will be sent as JSON string from client
        public string? RolesJson { get; set; }
    }
}