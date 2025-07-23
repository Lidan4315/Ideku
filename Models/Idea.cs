using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models // Sesuaikan dengan namespace proyek Anda
{
    [Table("ideas")]
    public class Idea
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("cInitiator")]
        public string Initiator { get; set; }

        [MaxLength(100)]
        [Column("cDivision")]
        public string? Division { get; set; }

        [MaxLength(100)]
        [Column("cDepartment")]
        public string? Department { get; set; }

        [Column("category_id")]
        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [Column("event_id")]
        public int? EventId { get; set; }
        [ForeignKey("EventId")]
        public Event? Event { get; set; }

        [Required]
        [Column("cIdea_name")]
        public string IdeaName { get; set; }

        [Column("cIdea_issue_background")]
        public string? IdeaIssueBackground { get; set; }

        [Column("cIdea_solution")]
        public string? IdeaSolution { get; set; }

        [Column("nSaving_cost", TypeName = "decimal(18, 2)")]
        public decimal? SavingCost { get; set; }

        [Column("cAttachment_file")]
        public string? AttachmentFile { get; set; }

        [Column("dSubmitted_date")]
        public DateTime SubmittedDate { get; set; }

        [Column("dUpdated_date")]
        public DateTime? UpdatedDate { get; set; }

        [Column("nCurrent_stage")]
        public int? CurrentStage { get; set; }

        [MaxLength(50)]
        [Column("cCurrent_status")]
        public string? CurrentStatus { get; set; }

        [MaxLength(50)]
        [Column("cImsCode")]
        public string? ImsCode { get; set; }

        [Column("payload")]
        public string? Payload { get; set; }

        [Column("flag_status")]
        public bool? FlagStatus { get; set; }

        [MaxLength(100)]
        [Column("cSavingCostOption")]
        public string? SavingCostOption { get; set; }

        [Column("rejectReason")]
        public string? RejectReason { get; set; }

        [MaxLength(100)]
        [Column("catReason")]
        public string? CatReason { get; set; }

        [Column("nSavingCostValidated", TypeName = "decimal(18, 2)")]
        public decimal? SavingCostValidated { get; set; }

        [MaxLength(100)]
        [Column("cSavingCostOptionValidated")]
        public string? SavingCostOptionValidated { get; set; }

        [Column("attachmentMonitoring")]
        public string? AttachmentMonitoring { get; set; }

        [MaxLength(50)]
        [Column("cFlagFlow")]
        public string? FlagFlow { get; set; }

        [MaxLength(100)]
        [Column("cIdeaType")]
        public string? IdeaType { get; set; }

        [Column("flagFinance")]
        public bool? FlagFinance { get; set; }

        [MaxLength(100)]
        [Column("rejected_state")]
        public string? RejectedState { get; set; }

        [Column("ideaFlowValidated")]
        public string? IdeaFlowValidated { get; set; }

        [Column("completedDate")]
        public DateTime? CompletedDate { get; set; }
        
        [MaxLength(100)]
        [Column("ideaCategory")]
        public string? IdeaCategory { get; set; }
    }
}