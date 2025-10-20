using Ideku.Data.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ideku.Services.Lookup
{
    /// Handles all lookup data operations (dropdowns, reference data)
    public class LookupService : ILookupService
    {
        private readonly ILookupRepository _lookupRepository;

        public LookupService(ILookupRepository lookupRepository)
        {
            _lookupRepository = lookupRepository;
        }

        public async Task<List<SelectListItem>> GetDivisionsAsync()
        {
            return await _lookupRepository.GetDivisionsAsync();
        }

        public async Task<List<SelectListItem>> GetDepartmentsByDivisionAsync(string divisionId)
        {
            return await _lookupRepository.GetDepartmentsByDivisionAsync(divisionId);
        }

        public async Task<List<SelectListItem>> GetCategoriesAsync()
        {
            return await _lookupRepository.GetCategoriesAsync();
        }

        public async Task<List<SelectListItem>> GetEventsAsync()
        {
            return await _lookupRepository.GetEventsAsync();
        }

        public async Task<List<object>> GetDepartmentsByDivisionForAjaxAsync(string divisionId)
        {
            var departments = await _lookupRepository.GetDepartmentsByDivisionAsync(divisionId);
            // Return id and name to match JavaScript expectations (same format as ApprovalController)
            return departments
                .Where(d => !string.IsNullOrEmpty(d.Value)) // Filter out empty options
                .Select(d => new { id = d.Value, name = d.Text })
                .ToList<object>();
        }
    }
}