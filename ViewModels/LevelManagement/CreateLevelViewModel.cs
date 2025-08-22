using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.LevelManagement
{
    public class CreateLevelViewModel
    {
        [Required(ErrorMessage = "Level Name is required")]
        [StringLength(8, ErrorMessage = "Level Name cannot exceed 8 characters (excluding LV prefix)")]
        [Display(Name = "Level Name")]
        public string LevelName { get; set; } = string.Empty;

        [Display(Name = "Active Level")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "At least one approver is required")]
        [Display(Name = "Approvers")]
        public List<LevelApproverViewModel> Approvers { get; set; } = new List<LevelApproverViewModel>();

        // For JSON serialization from form
        public string ApproversJson { get; set; } = string.Empty;
    }
}