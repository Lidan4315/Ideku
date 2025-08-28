using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.ApproverManagement
{
    public class AddApproverRoleViewModel
    {
        [Required]
        public int ApproverId { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

    }
}