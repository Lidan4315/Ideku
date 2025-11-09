using Ideku.Models.Entities;

namespace Ideku.ViewModels.Milestone
{
    /// DTO for implementator with role information for PIC selection
    public class ImplementatorForPICDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Role { get; set; } = null!;
        public Employee? Employee { get; set; }
    }

    /// ViewModel for Milestone detail page showing idea details and milestones CRUD
    public class MilestoneDetailViewModel
    {
        public Idea Idea { get; set; } = null!;

        /// List of milestones for this idea
        public IEnumerable<Models.Entities.Milestone> Milestones { get; set; } = new List<Models.Entities.Milestone>();

        /// Implementators available for PIC assignment (from idea implementators with their roles)
        public IEnumerable<ImplementatorForPICDto> AvailablePICUsers { get; set; } = new List<ImplementatorForPICDto>();

        /// Whether the idea is eligible for milestone creation (S2+)
        public bool IsEligibleForMilestones { get; set; } = false;
    }
}