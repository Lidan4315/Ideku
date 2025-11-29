using Ideku.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ideku.ViewModels.MyIdeas
{
    /// <summary>
    /// ViewModel for My Idea Details (Idea/Details) page
    /// </summary>
    public class MyIdeasDetailViewModel
    {
        /// <summary>
        /// The idea being displayed
        /// </summary>
        public Models.Entities.Idea Idea { get; set; } = null!;

        /// <summary>
        /// List of implementators (team members) assigned to this idea
        /// </summary>
        public List<IdeaImplementator> Implementators { get; set; } = new List<IdeaImplementator>();

        /// <summary>
        /// Available users for team assignment (dropdown list)
        /// </summary>
        public List<SelectListItem> AvailableUsers { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// All users for Edit Team modal
        /// </summary>
        public List<SelectListItem> AllUsers { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// Milestone data for calendar view
        /// </summary>
        public IEnumerable<Models.Entities.Milestone> Milestones { get; set; } = new List<Models.Entities.Milestone>();

        /// <summary>
        /// Workflow history for approval timeline
        /// </summary>
        public IEnumerable<WorkflowHistory> WorkflowHistory { get; set; } = new List<WorkflowHistory>();

        /// <summary>
        /// Monitoring data for monitoring tables
        /// </summary>
        public IEnumerable<Models.Entities.IdeaMonitoring> Monitorings { get; set; } = new List<Models.Entities.IdeaMonitoring>();

        /// <summary>
        /// Whether this idea has monitoring records
        /// </summary>
        public bool HasMonitoring => Monitorings != null && Monitorings.Any();

        /// <summary>
        /// Permission: Can the current user edit cost savings data?
        /// </summary>
        public bool CanEditCostSavings { get; set; } = false;

        /// <summary>
        /// Permission: Can the current user validate cost savings data?
        /// </summary>
        public bool CanValidateCostSavings { get; set; } = false;
    }

    /// <summary>
    /// DTO for batch implementator assignment request
    /// </summary>
    public class AssignMultipleImplementatorsRequest
    {
        public long IdeaId { get; set; }
        public List<ImplementatorDto> Implementators { get; set; } = new List<ImplementatorDto>();
    }

    /// <summary>
    /// DTO for single implementator in batch request
    /// </summary>
    public class ImplementatorDto
    {
        public long UserId { get; set; }
        public string Role { get; set; } = null!;
    }

    /// <summary>
    /// DTO for update team implementators request (remove + add)
    /// </summary>
    public class UpdateTeamImplementatorsRequest
    {
        public long IdeaId { get; set; }
        public List<long> ImplementatorsToRemove { get; set; } = new List<long>();
        public List<ImplementatorDto> ImplementatorsToAdd { get; set; } = new List<ImplementatorDto>();
    }
}
