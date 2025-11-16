using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Seeders
{
    public static class DivisionSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // Check if data already exists
            if (context.Divisions.Any())
            {
                return; // DB has been seeded
            }

            // Seed Divisions
            var divisions = new[]
            {
                new Division { Id = "D01", NameDivision = "Business & Performance Improvement", IsActive = true },
                new Division { Id = "D02", NameDivision = "Business Dev. & Risk Management", IsActive = true },
                new Division { Id = "D03", NameDivision = "Chief Executive Officer", IsActive = true },
                new Division { Id = "D04", NameDivision = "Chief Financial Officer", IsActive = true },
                new Division { Id = "D05", NameDivision = "Chief Operating Officer", IsActive = true },
                new Division { Id = "D06", NameDivision = "Coal Processing & Handling", IsActive = true },
                new Division { Id = "D07", NameDivision = "Contract Mining", IsActive = true },
                new Division { Id = "D08", NameDivision = "Director of Finance", IsActive = true },
                new Division { Id = "D09", NameDivision = "External Affairs & Sustainable Development", IsActive = true },
                new Division { Id = "D10", NameDivision = "Finance", IsActive = true }
            };
            context.Divisions.AddRange(divisions);
            context.SaveChanges();
        }
    }
}
