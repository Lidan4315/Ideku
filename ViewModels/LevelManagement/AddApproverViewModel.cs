using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.LevelManagement
{
    public class AddApproverViewModel
    {
        [Required]
        public int LevelId { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

    }
}