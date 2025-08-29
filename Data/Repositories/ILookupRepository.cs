using Microsoft.AspNetCore.Mvc.Rendering;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public interface ILookupRepository
    {
        Task<List<SelectListItem>> GetDivisionsAsync();
        Task<List<SelectListItem>> GetDepartmentsByDivisionAsync(string divisionId);
        Task<List<SelectListItem>> GetCategoriesAsync();
        Task<List<SelectListItem>> GetEventsAsync();
        Task<List<Division>> GetActiveDivisionsAsync();
    }
}