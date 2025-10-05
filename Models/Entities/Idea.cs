using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Ideku.Models.Entities
{
    [Table("Ideas")]
    public class Idea
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }

        [Required]
        [Column("InitiatorUserId")]
        public long InitiatorUserId { get; set; }

        [ForeignKey("InitiatorUserId")]
        public User InitiatorUser { get; set; } = null!;

        [Required]
        [Column("ToDivisionId")]
        [StringLength(3)] // ← FIXED: char(3) untuk ref ke Division.Id
        public string ToDivisionId { get; set; } = string.Empty;

        [ForeignKey("ToDivisionId")]
        public Division TargetDivision { get; set; } = null!;

        [Required]
        [Column("ToDepartmentId")]
        [StringLength(3)] // ← FIXED: char(3) untuk ref ke Departments.Id
        public string ToDepartmentId { get; set; } = string.Empty;

        [ForeignKey("ToDepartmentId")]
        public Department TargetDepartment { get; set; } = null!;

        [Required]
        [Column("CategoryId")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category Category { get; set; } = null!;

        [Column("EventId")]
        public int? EventId { get; set; }

        [ForeignKey("EventId")]
        public Event? Event { get; set; }

        [Required]
        [Column("IdeaName")]
        [StringLength(150)]
        public string IdeaName { get; set; } = string.Empty;

        [Required]
        [Column("IdeaIssueBackground")]
        [StringLength(2000)]
        public string IdeaIssueBackground { get; set; } = string.Empty;

        [Required]
        [Column("IdeaSolution")]
        [StringLength(2000)]
        public string IdeaSolution { get; set; } = string.Empty;

        [Required]
        [Column("SavingCost")]
        public long SavingCost { get; set; }

        [Column("SavingCostValidated")]
        public long? SavingCostValidated { get; set; }

        [Required]
        [Column("AttachmentFiles")]
        public string AttachmentFiles { get; set; } = string.Empty;

        [Required]
        [Column("WorkflowId")]
        public int WorkflowId { get; set; }

        [ForeignKey("WorkflowId")]
        public Workflow Workflow { get; set; } = null!;

        [Column("CurrentStage")]
        public int CurrentStage { get; set; } = 0;

        [Column("MaxStage")]
        public int MaxStage { get; set; } = 0;

        [Required]
        [Column("CurrentStatus")]
        [StringLength(20)]
        public string CurrentStatus { get; set; } = string.Empty;

        [Column("IsRejected")]
        public bool IsRejected { get; set; } = false;

        [Column("RejectedReason")]
        [StringLength(1000)]
        public string? RejectedReason { get; set; }

        [Required]
        [Column("IdeaCode")]
        [StringLength(11)]
        public string IdeaCode { get; set; } = string.Empty;

        [Column("SubmittedDate")]
        public DateTime SubmittedDate { get; set; } = DateTime.Now;

        [Column("UpdatedDate")]
        public DateTime? UpdatedDate { get; set; }

        [Column("CompletedDate")]
        public DateTime? CompletedDate { get; set; }

        [Column("IsDeleted")]
        public bool IsDeleted { get; set; } = false;

        // Related Divisions - JSON storage
        [Column("RelatedDivisions")]
        [StringLength(500)]
        public string? RelatedDivisionsJson { get; set; }

        // Related Divisions - Application property (auto-converts JSON ↔ List)
        [NotMapped]
        public List<string> RelatedDivisions
        {
            get => string.IsNullOrEmpty(RelatedDivisionsJson)
                   ? new List<string>()
                   : JsonSerializer.Deserialize<List<string>>(RelatedDivisionsJson) ?? new List<string>();
            set => RelatedDivisionsJson = value?.Any() == true
                   ? JsonSerializer.Serialize(value)
                   : null;
        }

        public ICollection<WorkflowHistory> WorkflowHistories { get; set; } = new List<WorkflowHistory>();
        public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
        public ICollection<IdeaImplementator> IdeaImplementators { get; set; } = new List<IdeaImplementator>();
    }
}