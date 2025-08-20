using System.Collections.Generic;
using System.Linq;

namespace Ideku.ViewModels.Common
{
    /// <summary>
    /// Generic paginated result wrapper for any entity type
    /// Provides metadata and items for pagination display
    /// </summary>
    /// <typeparam name="T">Entity type to paginate</typeparam>
    public class PagedResult<T> where T : class
    {
        /// <summary>
        /// Items for current page
        /// </summary>
        public IReadOnlyList<T> Items { get; init; } = new List<T>();
        
        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int Page { get; init; } = 1;
        
        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; init; } = 25;
        
        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalCount { get; init; }
        
        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages => TotalCount == 0 ? 1 : (int)System.Math.Ceiling((double)TotalCount / PageSize);
        
        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPrevious => Page > 1;
        
        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNext => Page < TotalPages;
        
        /// <summary>
        /// Index of first item on current page (1-based)
        /// </summary>
        public int FirstItemIndex => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        
        /// <summary>
        /// Index of last item on current page (1-based)
        /// </summary>
        public int LastItemIndex => System.Math.Min(Page * PageSize, TotalCount);
        
        /// <summary>
        /// Whether current result set has any items
        /// </summary>
        public bool HasItems => Items?.Any() == true;
        
        /// <summary>
        /// Whether pagination controls should be displayed
        /// </summary>
        public bool ShowPagination => TotalPages > 1;
    }
}