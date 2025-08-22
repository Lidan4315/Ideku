using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.WorkflowManagement
{
    public class WorkflowConditionViewModel
    {
        [Required]
        public int WorkflowId { get; set; }

        [Required(ErrorMessage = "Condition Type is required")]
        [StringLength(50, ErrorMessage = "Condition Type cannot exceed 50 characters")]
        [Display(Name = "Condition Type")]
        public string ConditionType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Operator is required")]
        [StringLength(10, ErrorMessage = "Operator cannot exceed 10 characters")]
        [Display(Name = "Operator")]
        public string Operator { get; set; } = string.Empty;

        [Required(ErrorMessage = "Condition Value is required")]
        [StringLength(500, ErrorMessage = "Condition Value cannot exceed 500 characters")]
        [Display(Name = "Condition Value")]
        public string ConditionValue { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}