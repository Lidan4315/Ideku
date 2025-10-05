using Ideku.Models.Entities;

namespace Ideku.ViewModels.Milestone
{
    /// <summary>
    /// DTO for implementator with role information for PIC selection
    /// </summary>
    public class ImplementatorForPICDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Role { get; set; } = null!;
        public Employee? Employee { get; set; }
    }

    /// <summary>
    /// ViewModel for Milestone detail page showing idea details and milestones CRUD
    /// </summary>
    public class MilestoneDetailViewModel
    {
        /// <summary>
        /// The idea being managed
        /// </summary>
        public Idea Idea { get; set; } = null!;

        /// <summary>
        /// List of milestones for this idea
        /// </summary>
        public IEnumerable<Models.Entities.Milestone> Milestones { get; set; } = new List<Models.Entities.Milestone>();

        /// <summary>
        /// Implementators available for PIC assignment (from idea implementators with their roles)
        /// </summary>
        public IEnumerable<ImplementatorForPICDto> AvailablePICUsers { get; set; } = new List<ImplementatorForPICDto>();

        /// <summary>
        /// Whether current user can manage milestones for this idea
        /// </summary>
        public bool CanManageMilestones { get; set; } = false;

        /// <summary>
        /// Whether the idea is eligible for milestone creation (S2+)
        /// </summary>
        public bool IsEligibleForMilestones { get; set; } = false;

        // Success/Error messages
        /// <summary>
        /// Success message to display
        /// </summary>
        public string? SuccessMessage { get; set; }

        /// <summary>
        /// Error message to display
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}