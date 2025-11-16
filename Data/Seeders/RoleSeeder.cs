using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Seeders
{
    public static class RoleSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // Check if data already exists
            if (context.Roles.Any())
            {
                return; // DB has been seeded
            }

            // Seed Roles
            var roles = new[]
            {
                new Role { RoleName = "Superuser", Desc = "System Superuser" },
                new Role { RoleName = "Admin", Desc = "Manager with approval authority" },
                new Role { RoleName = "Initiator", Desc = "Idea Initiator" },
                new Role { RoleName = "Workstream Leader", Desc = "Workstream Leader" },
                new Role { RoleName = "Implementor", Desc = "Idea Implementor" },
                new Role { RoleName = "Mgr. Dept", Desc = "Department Manager" },
                new Role { RoleName = "GM Division", Desc = "General Manager Division" },
                new Role { RoleName = "GM Finance", Desc = "General Manager Finance" },
                new Role { RoleName = "GM BPID", Desc = "General Manager BPID" },
                new Role { RoleName = "COO", Desc = "Chief Operating Officer" },
                new Role { RoleName = "SCFO", Desc = "Senior Chief Financial Officer" },
                new Role { RoleName = "CEO", Desc = "Chief Executive Officer" },
                new Role { RoleName = "GM Division Act.", Desc = "GM Division Acting" },
                new Role { RoleName = "GM Finance Act.", Desc = "GM Finance Acting" },
                new Role { RoleName = "GM BPID Act.", Desc = "GM BPID Acting" },
                new Role { RoleName = "Mgr. Dept. Act.", Desc = "Department Manager Acting" },
                new Role { RoleName = "COO Act.", Desc = "COO Acting" },
                new Role { RoleName = "CEO Act.", Desc = "CEO Acting" },
                new Role { RoleName = "SCFO Act.", Desc = "SCFO Acting" }
            };
            context.Roles.AddRange(roles);
            context.SaveChanges();
        }
    }
}
