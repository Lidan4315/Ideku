using Ideku.Models.Entities;
using Ideku.ViewModels.Common;

namespace Ideku.ViewModels.BypassStage
{
    public class BypassStageViewModel
    {
        public PagedResult<Idea> PagedIdeas { get; set; } = new PagedResult<Idea>();

        // Filter Properties
        public string? SearchTerm { get; set; }
        public string? SelectedDivision { get; set; }
        public string? SelectedDepartment { get; set; }
        public int? SelectedCategory { get; set; }
        public int? SelectedWorkflow { get; set; }
        public string? SelectedStatus { get; set; }

        public List<string>? StatusOptions { get; set; }

        // Convenience Properties
        public IEnumerable<Idea> Ideas => PagedIdeas.Items;
        public int CurrentPage => PagedIdeas.Page;
        public int PageSize => PagedIdeas.PageSize;
        public int TotalItems => PagedIdeas.TotalCount;
        public int TotalPages => PagedIdeas.TotalPages;
        public bool HasIdeas => PagedIdeas.HasItems;
        public bool ShowPagination => PagedIdeas.ShowPagination;
    }
}
