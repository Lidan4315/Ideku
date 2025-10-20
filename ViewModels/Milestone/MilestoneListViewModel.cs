using Ideku.Models.Entities;
using Ideku.ViewModels.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ideku.ViewModels.Milestone
{
    /// ViewModel for Milestone list page showing S2+ ideas with pagination and filtering
    public class MilestoneListViewModel
    {
        /// Paginated ideas eligible for milestone management (S2+)
        public PagedResult<Idea> PagedIdeas { get; set; } = new PagedResult<Idea>();

        // Filter Properties
        /// Search term for idea code, name, or initiator name
        public string? SearchTerm { get; set; }

        /// Selected division ID for filtering
        public string? SelectedDivision { get; set; }

        /// Selected department ID for filtering
        public string? SelectedDepartment { get; set; }

        /// Selected category ID for filtering
        public int? SelectedCategory { get; set; }

        /// Selected stage for filtering
        public int? SelectedStage { get; set; }

        /// Selected status for filtering
        public string? SelectedStatus { get; set; }

        /// Available stages dynamically loaded from database
        public List<SelectListItem> AvailableStages { get; set; } = new();

        // Convenience Properties
        /// Ideas for current page (shortcut to PagedIdeas.Items)
        public IEnumerable<Idea> Ideas => PagedIdeas.Items;

        /// Current page number
        public int CurrentPage => PagedIdeas.Page;

        /// Items per page
        public int PageSize => PagedIdeas.PageSize;

        /// Total number of ideas matching current filters
        public int TotalItems => PagedIdeas.TotalCount;

        /// Total number of pages
        public int TotalPages => PagedIdeas.TotalPages;

        /// Whether there are any ideas to display
        public bool HasIdeas => PagedIdeas.HasItems;

        /// Whether to show pagination controls
        public bool ShowPagination => PagedIdeas.ShowPagination;
    }
}