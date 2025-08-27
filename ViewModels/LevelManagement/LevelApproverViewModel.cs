using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.LevelManagement
{
    public class LevelApproverViewModel
    {
        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public int RoleId { get; set; }


        // Display properties (for showing in lists)
        public string RoleName { get; set; } = string.Empty;
        public int Id { get; set; } // For editing existing approvers
    }
}