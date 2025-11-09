using Ideku.Models.Entities;
using Ideku.ViewModels.Common;

namespace Ideku.ViewModels.ChangeWorkflow
{
    /// <summary>
    /// ViewModel for Change Workflow page with pagination and filtering support
    /// </summary>
    public class ChangeWorkflowViewModel
    {
        /// <summary>
        /// Paginated ideas for workflow management
        /// </summary>
        public PagedResult<Idea> PagedIdeas { get; set; } = new PagedResult<Idea>();

        // Filter Properties
        /// <summary>
        /// Search term for idea code or name
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Selected division ID for filtering
        /// </summary>
        public string? SelectedDivision { get; set; }

        /// <summary>
        /// Selected department ID for filtering
        /// </summary>
        public string? SelectedDepartment { get; set; }

        /// <summary>
        /// Selected category ID for filtering
        /// </summary>
        public int? SelectedCategory { get; set; }

        /// <summary>
        /// Selected workflow ID for filtering
        /// </summary>
        public int? SelectedWorkflow { get; set; }

        /// <summary>
        /// Selected status for filtering
        /// </summary>
        public string? SelectedStatus { get; set; }

        public List<string>? StatusOptions { get; set; }

        // Convenience Properties
        /// <summary>
        /// Ideas for current page
        /// </summary>
        public IEnumerable<Idea> Ideas => PagedIdeas.Items;

        /// <summary>
        /// Current page number
        /// </summary>
        public int CurrentPage => PagedIdeas.Page;

        /// <summary>
        /// Items per page
        /// </summary>
        public int PageSize => PagedIdeas.PageSize;

        /// <summary>
        /// Total number of ideas matching current filters
        /// </summary>
        public int TotalItems => PagedIdeas.TotalCount;

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages => PagedIdeas.TotalPages;

        /// <summary>
        /// Whether there are any ideas to display
        /// </summary>
        public bool HasIdeas => PagedIdeas.HasItems;

        /// <summary>
        /// Whether to show pagination controls
        /// </summary>
        public bool ShowPagination => PagedIdeas.ShowPagination;
    }
}
