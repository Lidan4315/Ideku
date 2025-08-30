using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.DTOs
{
    /// <summary>
    /// DTO for processing approval with related divisions
    /// </summary>
    public class ApprovalProcessDto
    {
        [Required]
        public long IdeaId { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Validated saving cost must be a positive number")]
        public decimal ValidatedSavingCost { get; set; }

        [Required]
        [StringLength(1000, ErrorMessage = "Comments cannot exceed 1000 characters")]
        public string ApprovalComments { get; set; } = string.Empty;

        /// <summary>
        /// List of division IDs that should be notified
        /// Example: ["D01", "D02", "D05"]
        /// </summary>
        public List<string> RelatedDivisions { get; set; } = new List<string>();

        [Required]
        public long ApprovedBy { get; set; }

        /// <summary>
        /// Timestamp when approval was processed
        /// </summary>
        public DateTime ProcessedAt { get; set; } = DateTime.Now;
    }
}