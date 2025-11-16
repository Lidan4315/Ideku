using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Seeders
{
    public static class EmployeeSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // Check if data already exists
            if (context.Employees.Any())
            {
                return; // DB has been seeded
            }

            // Seed Employees
            var employees = new[]
            {
                new Employee { EMP_ID = "EMP001", NAME = "Super User", POSITION_TITLE = "System Administrator", DIVISION = "D01", DEPARTEMENT = "P01", EMAIL = "some.other.email@example.com", POSITION_LVL = "Supervisor", EMP_STATUS = "Active" },
                new Employee { EMP_ID = "EMP002", NAME = "Faiq Lidan", POSITION_TITLE = "Frondend Developer", DIVISION = "D01", DEPARTEMENT = "P01", EMAIL = "faiqlidan03@gmail.com", POSITION_LVL = "Supervisor", EMP_STATUS = "Active" },
                new Employee { EMP_ID = "EMP003", NAME = "Mike Johnson", POSITION_TITLE = "Software Developer", DIVISION = "D05", DEPARTEMENT = "P06", EMAIL = "bpidstudent@gmail.com", POSITION_LVL = "Supervisor", EMP_STATUS = "Active" },
                new Employee { EMP_ID = "EMP004", NAME = "Sarah Wilson", POSITION_TITLE = "Finance Analyst", DIVISION = "D04", DEPARTEMENT = "P05", EMAIL = "sarah.wilson@company.com", POSITION_LVL = "Supervisor", EMP_STATUS = "Active" },
                new Employee { EMP_ID = "EMP005", NAME = "John Doe", POSITION_TITLE = "System Administrator", DIVISION = "D06", DEPARTEMENT = "P07", EMAIL = "admin@company.com", POSITION_LVL = "Supervisor", EMP_STATUS = "Active" }
            };
            context.Employees.AddRange(employees);
            context.SaveChanges();
        }
    }
}
