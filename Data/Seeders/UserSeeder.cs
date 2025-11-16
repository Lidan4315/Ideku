using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Seeders
{
    public static class UserSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // Check if data already exists
            if (context.Users.Any())
            {
                return; // DB has been seeded
            }

            // Seed Users
            var users = new[]
            {
                new User { EmployeeId = "EMP001", RoleId = 1, Username = "superuser", Name = "Super User", IsActing = false },
                new User { EmployeeId = "EMP002", RoleId = 2, Username = "faiqlidan", Name = "Faiq Lidan", IsActing = false },
                new User { EmployeeId = "EMP003", RoleId = 4, Username = "workstream.leader", Name = "Mike Johnson (WSL)", IsActing = false }, // WORKSTREAM LEADER
                new User { EmployeeId = "EMP004", RoleId = 1, Username = "sarahwilson", Name = "Sarah Wilson", IsActing = false },
                new User { EmployeeId = "EMP005", RoleId = 3, Username = "johndoe", Name = "John Doe", IsActing = false }
            };
            context.Users.AddRange(users);
            context.SaveChanges();
        }
    }
}
