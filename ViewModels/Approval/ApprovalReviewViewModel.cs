using Ideku.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.Approval
{
    public class ApprovalReviewViewModel
    {
        // Data to display
        public Idea Idea { get; set; } = null!;

        // Data from form inputs
        [Display(Name = "Validated Saving Cost (USD)")]
        [Required(ErrorMessage = "Validated saving cost is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Validated saving cost must be a positive number")]
        public decimal? ValidatedSavingCost { get; set; }

        [Display(Name = "Approval Comments")]
        [Required(ErrorMessage = "Approval comments are required")]
        [StringLength(1000, ErrorMessage = "Approval comments cannot exceed 1000 characters")]
        public string ApprovalComments { get; set; } = null!;

        [Display(Name = "Rejection Reason")]
        [Required(ErrorMessage = "Rejection reason is required")]
        [StringLength(1000, ErrorMessage = "Rejection reason cannot exceed 1000 characters")]
        public string? RejectionReason { get; set; }
    }
}
