using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Seeders
{
    public static class CategorySeeder
    {
        public static void Seed(AppDbContext context)
        {
            // Check if data already exists
            if (context.Categories.Any())
            {
                return; // DB has been seeded
            }

            // Seed Categories
            var categories = new[]
            {
                new Category { CategoryName = "General Transformation", IsActive = true },
                new Category { CategoryName = "Increase Revenue", IsActive = true },
                new Category { CategoryName = "Cost Reduction (CR)", IsActive = true },
                new Category { CategoryName = "Digitalization", IsActive = true },
            };
            context.Categories.AddRange(categories);
            context.SaveChanges();
        }
    }
}
