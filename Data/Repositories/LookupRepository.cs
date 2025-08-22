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

            // Debug: Get ALL departments first to see what's in database
            var allDepartments = await _context.Departments.ToListAsync();
            
            // Debug: Get departments for this specific division
            var departments = await _context.Departments
                .Where(d => d.DivisiId == divisionId && d.IsActive)
                .OrderBy(d => d.NameDepartment)
                .ToListAsync();

            // Debug: If no departments found, try without IsActive filter
            if (!departments.Any())
            {
                var allDepartmentsForDivision = await _context.Departments
                    .Where(d => d.DivisiId == divisionId) // Remove IsActive filter
                    .OrderBy(d => d.NameDepartment)
                    .ToListAsync();
                    
                if (allDepartmentsForDivision.Any())
                {
                    return allDepartmentsForDivision.Select(d => new SelectListItem
                    {
                        Value = d.Id,
                        Text = d.NameDepartment + " (Inactive)"
                    }).ToList();
                }
                
                // If still no data, return test data
                return new List<SelectListItem>
                {
                    new SelectListItem { Value = "TEST1", Text = "Test Department 1" },
                    new SelectListItem { Value = "TEST2", Text = "Test Department 2" }
                };
            }

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