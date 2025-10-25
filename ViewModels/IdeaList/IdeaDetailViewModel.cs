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
    }
}