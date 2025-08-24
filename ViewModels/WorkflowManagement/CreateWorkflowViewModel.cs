using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.WorkflowManagement
{
    public class CreateWorkflowViewModel
    {
        [Required(ErrorMessage = "Workflow Name is required")]
        [StringLength(17, ErrorMessage = "Workflow Name cannot exceed 17 characters (excluding WF_ prefix)")]
        [Display(Name = "Workflow Name")]
        public string WorkflowName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        [Display(Name = "Description")]
        public string? Desc { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Priority is required")]
        [Range(1, 100, ErrorMessage = "Priority must be between 1 and 100")]
        [Display(Name = "Priority")]
        public int Priority { get; set; } = 1;
    }
}