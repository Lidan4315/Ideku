using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Ideku.Data.Context;

namespace Ideku.Data.Repositories
{
    public class LookupRepository : ILookupRepository
    {
        private readonly AppDbContext _context;

        public LookupRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SelectListItem>> GetDivisionsAsync()
        {
            var divisions = await _context.Divisions
                .Where(d => d.IsActive)
                .OrderBy(d => d.NameDivision)
                .ToListAsync();

            var selectList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Select To Division --" }
            };

            selectList.AddRange(divisions.Select(d => new SelectListItem
            {
                Value = d.Id,
                Text = d.NameDivision
            }));

            return selectList;
        }

        public async Task<List<SelectListItem>> GetDepartmentsByDivisionAsync(string divisionId)
        {
            if (string.IsNullOrEmpty(divisionId))
            {
                return new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "-- Select To Division First --", Disabled = true }
                };
            }

            var departments = await _context.Departments
                .Where(d => d.DivisiId == divisionId && d.IsActive)
                .OrderBy(d => d.NameDepartment)
                .ToListAsync();

            // Don't add placeholder here, let JavaScript handle it
            return departments.Select(d => new SelectListItem
            {
                Value = d.Id,
                Text = d.NameDepartment
            }).ToList();
        }

        public async Task<List<SelectListItem>> GetCategoriesAsync()
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var selectList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Select Category --" }
            };

            selectList.AddRange(categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.CategoryName
            }));

            return selectList;
        }

        public async Task<List<SelectListItem>> GetEventsAsync()
        {
            var events = await _context.Events
                .Where(e => e.IsActive)
                .OrderBy(e => e.EventName)
                .ToListAsync();

            var selectList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Select Event (Optional) --" }
            };

            selectList.AddRange(events.Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text = e.EventName
            }));

            return selectList;
        }
    }
}