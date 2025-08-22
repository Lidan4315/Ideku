using System.Linq;

namespace Ideku.Helpers
{
    /// <summary>
    /// Helper class for pagination calculations and utilities
    /// Contains logic for smart page range calculations
    /// </summary>
    public static class PaginationHelper
    {
        /// <summary>
        /// Calculates which page numbers to display in pagination controls
        /// Uses Option 1: Adjacent Pages Only (No Dots, Always 5 pages centered)
        /// </summary>
        /// <param name="currentPage">Current page number</param>
        /// <param name="totalPages">Total number of pages</param>
        /// <param name="maxPagesToShow">Maximum page numbers to display (default: 5)</param>
        /// <returns>List of page numbers to display (no ellipsis needed)</returns>
        public static List<int> GetVisiblePages(int currentPage, int totalPages, int maxPagesToShow = 5)
        {
            // If total pages <= maxVisible, show all pages
            if (totalPages <= maxPagesToShow)
                return Enumerable.Range(1, totalPages).ToList();
            
            // Calculate the start position to center current page
            int halfVisible = maxPagesToShow / 2;
            int startPage = Math.Max(1, currentPage - halfVisible);
            int endPage = Math.Min(totalPages, startPage + maxPagesToShow - 1);
            
            // Adjust if we're near the end (ensure we always show maxPagesToShow pages)
            if (endPage - startPage + 1 < maxPagesToShow)
                startPage = Math.Max(1, endPage - maxPagesToShow + 1);
            
            return Enumerable.Range(startPage, endPage - startPage + 1).ToList();
        }
        
        /// <summary>
        /// Get mobile-optimized visible pages (max 3 pages centered on current)
        /// </summary>
        /// <param name="currentPage">Current page number</param>
        /// <param name="totalPages">Total number of pages</param>
        /// <returns>List of page numbers for mobile display</returns>
        public static List<int> GetMobileVisiblePages(int currentPage, int totalPages)
        {
            const int mobileMaxPages = 3;
            
            // If total pages <= 3, show all pages
            if (totalPages <= mobileMaxPages)
                return Enumerable.Range(1, totalPages).ToList();
            
            // Center current page with max 3 pages
            int startPage = Math.Max(1, currentPage - 1);
            int endPage = Math.Min(totalPages, startPage + mobileMaxPages - 1);
            
            // Adjust if we're near the end
            if (endPage - startPage + 1 < mobileMaxPages)
                startPage = Math.Max(1, endPage - mobileMaxPages + 1);
            
            return Enumerable.Range(startPage, endPage - startPage + 1).ToList();
        }
        
        /// <summary>
        /// Legacy method for backward compatibility - now redirects to GetVisiblePages
        /// </summary>
        /// <param name="currentPage">Current page number</param>
        /// <param name="totalPages">Total number of pages</param>
        /// <param name="maxPagesToShow">Maximum page numbers to display (default: 5)</param>
        /// <returns>Tuple with start page, end page, and no ellipsis (always false)</returns>
        public static (int startPage, int endPage, bool showFirstEllipsis, bool showLastEllipsis) 
            CalculatePageRange(int currentPage, int totalPages, int maxPagesToShow = 5)
        {
            var visiblePages = GetVisiblePages(currentPage, totalPages, maxPagesToShow);
            return (visiblePages.First(), visiblePages.Last(), false, false);
        }
        
        /// <summary>
        /// Builds route data dictionary for pagination links while preserving filters
        /// </summary>
        /// <param name="currentRouteData">Current route values from Request.Query</param>
        /// <param name="page">Page number to set</param>
        /// <param name="pageSize">Page size to set (optional)</param>
        /// <returns>Dictionary for URL generation with all filters preserved</returns>
        public static Dictionary<string, string> BuildRouteData(
            IDictionary<string, Microsoft.Extensions.Primitives.StringValues> currentRouteData,
            int page,
            int? pageSize = null)
        {
            var routeData = new Dictionary<string, string>();
            
            // Preserve all current query parameters (filters)
            foreach (var kvp in currentRouteData)
            {
                if (!string.IsNullOrEmpty(kvp.Value) && 
                    !kvp.Key.Equals("page", StringComparison.OrdinalIgnoreCase) &&
                    !kvp.Key.Equals("pageSize", StringComparison.OrdinalIgnoreCase))
                {
                    routeData[kvp.Key] = kvp.Value.ToString();
                }
            }
            
            // Set pagination parameters
            routeData["page"] = page.ToString();
            if (pageSize.HasValue)
                routeData["pageSize"] = pageSize.Value.ToString();
            
            return routeData;
        }
        
        /// <summary>
        /// Validates and normalizes page size according to application rules
        /// </summary>
        /// <param name="requestedPageSize">Requested page size</param>
        /// <returns>Valid page size within allowed range</returns>
        public static int ValidatePageSize(int requestedPageSize)
        {
            // If exact match with allowed sizes, return as-is
            if (PageSizeOptions.Contains(requestedPageSize))
                return requestedPageSize;
                
            // If outside bounds, return closest valid size
            if (requestedPageSize <= 0)
                return DefaultPageSize;
                
            if (requestedPageSize > MaxPageSize)
                return MaxPageSize;
                
            // Find closest allowed size
            return PageSizeOptions
                .OrderBy(size => Math.Abs(size - requestedPageSize))
                .First();
        }
        
        /// <summary>
        /// Default page size options available to users
        /// </summary>
        public static readonly int[] PageSizeOptions = { 10, 25, 50, 100 };
        
        /// <summary>
        /// Default page size for new sessions
        /// </summary>
        public const int DefaultPageSize = 10;
        
        /// <summary>
        /// Maximum allowed page size for performance protection
        /// </summary>
        public const int MaxPageSize = 100;
    }
}