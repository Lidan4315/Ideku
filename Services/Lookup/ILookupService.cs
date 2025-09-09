using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ideku.Services.Lookup
{
    public interface ILookupService
    {
        Task<List<SelectListItem>> GetDivisionsAsync();
        Task<List<SelectListItem>> GetDepartmentsByDivisionAsync(string divisionId);
        Task<List<SelectListItem>> GetCategoriesAsync();
        Task<List<SelectListItem>> GetEventsAsync();
        Task<List<object>> GetDepartmentsByDivisionForAjaxAsync(string divisionId);
    }
}