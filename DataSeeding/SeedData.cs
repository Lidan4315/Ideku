// File: DataSeeding/SeedData.cs
using Ideku.Data;
using Ideku.Models;
using Microsoft.EntityFrameworkCore;

namespace Ideku.DataSeeding
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new AppDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>()))
            {
                // Hentikan jika sudah ada data di tabel Roles (menandakan sudah di-seed)
                if (context.Roles.Any())
                {
                    return;
                }

                // 1. Buat Dummy Roles
                context.Roles.AddRange(
                    new Role { RoleName = "Administrator", CreatedAt = DateTime.Now },
                    new Role { RoleName = "Standard User", CreatedAt = DateTime.Now },
                    new Role { RoleName = "Manager", CreatedAt = DateTime.Now }
                );

                // 2. Buat Dummy Employees
                context.Employees.AddRange(
                    new Employee 
                    { 
                        Id = "EMP001", 
                        Name = "Budi Santoso", 
                        Email = "budi.s@email.com", 
                        PositionTitle = "IT Support", 
                        Department = "Information Technology", // DIISI
                        Division = "Infrastructure",      // DIISI
                    },
                    new Employee 
                    { 
                        Id = "EMP002", 
                        Name = "Citra Lestari", 
                        Email = "citra.l@email.com", 
                        PositionTitle = "Sales Executive",
                        Department = "Sales & Marketing", // DIISI
                        Division = "Sales",               // DIISI
                    },
                    new Employee 
                    { 
                        Id = "EMP003", 
                        Name = "Agus Wijaya", 
                        Email = "agus.w@email.com", 
                        PositionTitle = "Sales Manager",
                        Department = "Sales & Marketing", // DIISI
                        Division = "Sales",               // DIISI
                    }
                );

                // Simpan semua perubahan ke database
                context.SaveChanges();
            }
        }
    }
}