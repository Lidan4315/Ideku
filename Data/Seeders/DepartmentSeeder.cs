using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Seeders
{
    public static class DepartmentSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // Check if data already exists
            if (context.Departments.Any())
            {
                return; // DB has been seeded
            }

            // Seed Departments
            var departments = new[]
            {
                new Department { Id = "P01", NameDepartment = "Business & Performance Improvement", DivisiId = "D01", IsActive = true },
                new Department { Id = "P02", NameDepartment = "Business Dev. & Risk Management", DivisiId = "D02", IsActive = true },
                new Department { Id = "P03", NameDepartment = "Chief Executive Officer", DivisiId = "D03", IsActive = true },
                new Department { Id = "P04", NameDepartment = "Business Analysis", DivisiId = "D04", IsActive = true },
                new Department { Id = "P05", NameDepartment = "Chief Financial Officer", DivisiId = "D04", IsActive = true },
                new Department { Id = "P06", NameDepartment = "Chief Operating Officer", DivisiId = "D05", IsActive = true },
                new Department { Id = "P07", NameDepartment = "CHT Operations", DivisiId = "D06", IsActive = true },
                new Department { Id = "P08", NameDepartment = "Coal Processing & Handling", DivisiId = "D06", IsActive = true },
                new Department { Id = "P09", NameDepartment = "Coal Technology", DivisiId = "D06", IsActive = true },
                new Department { Id = "P10", NameDepartment = "CPP Maintenance", DivisiId = "D06", IsActive = true },
                new Department { Id = "P11", NameDepartment = "CPP Operations", DivisiId = "D06", IsActive = true },
                new Department { Id = "P12", NameDepartment = "Infrastructure", DivisiId = "D06", IsActive = true },
                new Department { Id = "P13", NameDepartment = "Plant Engineering & Project Services", DivisiId = "D06", IsActive = true },
                new Department { Id = "P14", NameDepartment = "Power Generation & Transmission", DivisiId = "D06", IsActive = true },
                new Department { Id = "P15", NameDepartment = "CHT Maintenance", DivisiId = "D06", IsActive = true },
                new Department { Id = "P16", NameDepartment = "Contract Mining", DivisiId = "D07", IsActive = true },
                new Department { Id = "P17", NameDepartment = "Contract Mining Issues & Analysis", DivisiId = "D07", IsActive = true },
                new Department { Id = "P18", NameDepartment = "Mining Contract Bengalon", DivisiId = "D07", IsActive = true },
                new Department { Id = "P19", NameDepartment = "Mining Contract Pama", DivisiId = "D07", IsActive = true },
                new Department { Id = "P20", NameDepartment = "Mining Contract Sangatta", DivisiId = "D07", IsActive = true },
                new Department { Id = "P21", NameDepartment = "Mining Contract TCI Pits", DivisiId = "D07", IsActive = true },
                new Department { Id = "P22", NameDepartment = "Internal Audit", DivisiId = "D08", IsActive = true },
                new Department { Id = "P23", NameDepartment = "Bengalon Community Rels & Dev", DivisiId = "D09", IsActive = true },
                new Department { Id = "P24", NameDepartment = "Community Empowerment", DivisiId = "D09", IsActive = true },
                new Department { Id = "P25", NameDepartment = "Ext. Affairs & Sustainable Dev.", DivisiId = "D09", IsActive = true },
                new Department { Id = "P26", NameDepartment = "External Relations", DivisiId = "D09", IsActive = true },
                new Department { Id = "P27", NameDepartment = "Land Management", DivisiId = "D09", IsActive = true },
                new Department { Id = "P28", NameDepartment = "Project Management & Evaluation", DivisiId = "D09", IsActive = true },
                new Department { Id = "P29", NameDepartment = "Accounting and Reporting", DivisiId = "D10", IsActive = true },
                new Department { Id = "P30", NameDepartment = "Finance", DivisiId = "D10", IsActive = true },
                new Department { Id = "P31", NameDepartment = "Tax & Government Impost", DivisiId = "D10", IsActive = true },
                new Department { Id = "P32", NameDepartment = "Treasury", DivisiId = "D10", IsActive = true }
            };
            context.Departments.AddRange(departments);
            context.SaveChanges();
        }
    }
}
