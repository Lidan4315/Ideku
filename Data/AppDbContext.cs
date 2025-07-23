// File: Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using Ideku.Models;

namespace Ideku.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Daftarkan model Anda di sini
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Idea> Ideas { get; set; }
        public DbSet<Divisi> Divisi { get; set; }
        public DbSet<Departement> Departement { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<Event> Event { get; set; }
    }
}