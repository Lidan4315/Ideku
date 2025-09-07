using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ideku.Services.Lookup
{
    /// <summary>
    /// Service for handling lookup data (dropdowns, reference data)
    /// Centralized service for all lookup operations across the application
    /// </summary>
    public interface ILookupService
    {
        /// <summary>
        /// Gets all divisions for dropdown
        /// </summary>
        /// <returns>List of divisions as SelectListItem</returns>
        Task<List<SelectListItem>> GetDivisionsAsync();

        /// <summary>
        /// Gets departments by division ID for dropdown
        /// </summary>
        /// <param name="divisionId">Division ID to filter departments</param>
        /// <returns>List of departments as SelectListItem</returns>
        Task<List<SelectListItem>> GetDepartmentsByDivisionAsync(string divisionId);

        /// <summary>
        /// Gets all categories for dropdown
        /// </summary>
        /// <returns>List of categories as SelectListItem</returns>
        Task<List<SelectListItem>> GetCategoriesAsync();

        /// <summary>
        /// Gets all events for dropdown
        /// </summary>
        /// <returns>List of events as SelectListItem</returns>
        Task<List<SelectListItem>> GetEventsAsync();

        /// <summary>
        /// Gets departments by division ID for AJAX calls (returns anonymous objects)
        /// </summary>
        /// <param name="divisionId">Division ID to filter departments</param>
        /// <returns>List of departments as anonymous objects for JSON serialization</returns>
        Task<List<object>> GetDepartmentsByDivisionForAjaxAsync(string divisionId);
    }
}