using Ideku.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ideku.ViewModels.IdeaList
{
    public class IdeaDetailViewModel
    {
        public Idea Idea { get; set; } = null!;
        public List<IdeaImplementator> Implementators { get; set; } = new List<IdeaImplementator>();
        public List<SelectListItem> AvailableUsers { get; set; } = new List<SelectListItem>();
        public IEnumerable<Models.Entities.Milestone> Milestones { get; set; } = new List<Models.Entities.Milestone>();
        public IEnumerable<WorkflowHistory> WorkflowHistory { get; set; } = new List<WorkflowHistory>();
    }

    /// DTO for batch implementator assignment request
    public class AssignMultipleImplementatorsRequest
    {
        public long IdeaId { get; set; }
        public List<ImplementatorDto> Implementators { get; set; } = new List<ImplementatorDto>();
    }

    /// DTO for single implementator in batch request
    public class ImplementatorDto
    {
        public long UserId { get; set; }
        public string Role { get; set; } = null!;
    }

    /// DTO for update team implementators request (remove + add)
    public class UpdateTeamImplementatorsRequest
    {
        public long IdeaId { get; set; }
        public List<long> ImplementatorsToRemove { get; set; } = new List<long>();
        public List<ImplementatorDto> ImplementatorsToAdd { get; set; } = new List<ImplementatorDto>();
    }
}