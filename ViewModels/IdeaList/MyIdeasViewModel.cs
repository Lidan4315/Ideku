using Ideku.Models.Entities;
using Ideku.ViewModels.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Ideku.ViewModels.IdeaList
{
    /// <summary>
    /// ViewModel for My Ideas (Idea/Index) page with pagination and filtering support
    /// </summary>
    public class MyIdeasViewModel
    {
        /// <summary>
        /// Paginated ideas for the current user
        /// </summary>
        public PagedResult<Models.Entities.Idea> PagedIdeas { get; set; } = new PagedResult<Models.Entities.Idea>();

        // Filter Properties
        /// <summary>
        /// Search term for idea code, name
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
        /// Selected workflow stage for filtering
        /// </summary>
        public int? SelectedStage { get; set; }

        /// <summary>
        /// Selected status for filtering
        /// </summary>
        public string? SelectedStatus { get; set; }

        /// <summary>
        /// Available stages for filtering (dynamically loaded from database)
        /// </summary>
        public List<SelectListItem> AvailableStages { get; set; } = new List<SelectListItem>();
        /// <summary>
        /// Available statuses for filtering (dynamically loaded from database)
        /// </summary>
        public List<string>? StatusOptions { get; set; }

        // Convenience Properties for backward compatibility and ease of use
        /// <summary>
        /// Ideas for current page (shortcut to PagedIdeas.Items)
        /// </summary>
        public IEnumerable<Models.Entities.Idea> Ideas => PagedIdeas.Items;
        
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