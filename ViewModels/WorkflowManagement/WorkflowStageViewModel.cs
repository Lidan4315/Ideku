using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.WorkflowManagement
{
    public class WorkflowStageViewModel
    {
        [Required]
        public int WorkflowId { get; set; }

        [Required(ErrorMessage = "Level is required")]
        [Display(Name = "Level")]
        public int LevelId { get; set; }

        [Required(ErrorMessage = "Stage number is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Stage number must be greater than 0")]
        [Display(Name = "Stage Number")]
        public int Stage { get; set; }

        [Display(Name = "Mandatory Stage")]
        public bool IsMandatory { get; set; } = true;

        [Display(Name = "Parallel Processing")]
        public bool IsParallel { get; set; } = false;
    }
}