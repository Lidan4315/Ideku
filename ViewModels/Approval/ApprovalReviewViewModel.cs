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
        public decimal? ValidatedSavingCost { get; set; }

        [Display(Name = "Approval Comments")]
        [StringLength(1000)]
        public string? ApprovalComments { get; set; }

        [Display(Name = "Rejection Reason")]
        [StringLength(1000)]
        public string? RejectionReason { get; set; }
    }
}
