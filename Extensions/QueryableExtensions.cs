using Microsoft.EntityFrameworkCore;
using Ideku.ViewModels.Common;

namespace Ideku.Extensions
{
    /// <summary>
    /// Extension methods for IQueryable to support efficient database-level pagination
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Converts IQueryable to paginated result with database-level pagination
        /// Executes only 2 database queries: COUNT and SELECT with OFFSET/FETCH
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="source">Source queryable with filters already applied</param>
        /// <param name="page">Page number (1-based, minimum 1)</param>
        /// <param name="pageSize">Number of items per page (minimum 5, maximum 100)</param>
        /// <returns>Paginated result with items and metadata</returns>
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> source, 
            int page, 
            int pageSize) where T : class
        {
            // Validate and normalize parameters
            page = Math.Max(1, page);
            pageSize = Math.Max(5, Math.Min(100, pageSize));
            
            // Execute COUNT query to get total items
            var totalCount = await source.CountAsync();
            
            // If no items, return empty result
            if (totalCount == 0)
            {
                return new PagedResult<T>
                {
                    Items = new List<T>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }
            
            // Calculate skip amount
            var skip = (page - 1) * pageSize;
            
            // Execute SELECT query with OFFSET/FETCH for current page
            var items = await source
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
            
            return new PagedResult<T>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        
        /// <summary>
        /// Validates page size according to application rules
        /// </summary>
        /// <param name="requestedPageSize">Requested page size</param>
        /// <returns>Valid page size within allowed range</returns>
        public static int ValidatePageSize(int requestedPageSize)
        {
            var allowedSizes = new[] { 10, 25, 50, 100 };
            
            // If exact match, return as-is
            if (allowedSizes.Contains(requestedPageSize))
                return requestedPageSize;
                
            // Find closest allowed size
            return allowedSizes
                .OrderBy(size => Math.Abs(size - requestedPageSize))
                .First();
        }
    }
}