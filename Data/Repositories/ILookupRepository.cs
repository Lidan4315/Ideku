using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ideku.Data.Repositories
{
    public interface ILookupRepository
    {
        Task<List<SelectListItem>> GetDivisionsAsync();
        Task<List<SelectListItem>> GetDepartmentsByDivisionAsync(string divisionId);
        Task<List<SelectListItem>> GetCategoriesAsync();
        Task<List<SelectListItem>> GetEventsAsync();
    }
}