using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.LevelManagement
{
    public class EditLevelViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Level Name is required")]
        [StringLength(20, ErrorMessage = "Level Name cannot exceed 20 characters")]
        [Display(Name = "Level Name")]
        public string LevelName { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // For approvers - will be sent as JSON string from client
        public string? ApproversJson { get; set; }
    }
}