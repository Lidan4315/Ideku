using Microsoft.EntityFrameworkCore;
using Ideku.Models.Entities;

namespace Ideku.Data.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<Division> Divisions { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Idea> Ideas { get; set; }
        public DbSet<WorkflowHistory> WorkflowHistories { get; set; }
        public DbSet<Milestone> Milestones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =================== EXPLICIT COLUMN TYPES ===================
            
            // Division - char(3)
            modelBuilder.Entity<Division>()
                .Property(d => d.Id)
                .HasColumnType("char(3)")
                .IsFixedLength();

            // Department - char(3)
            modelBuilder.Entity<Department>()
                .Property(d => d.Id)
                .HasColumnType("char(3)")
                .IsFixedLength();

            modelBuilder.Entity<Department>()
                .Property(d => d.DivisiId)
                .HasColumnType("char(3)")
                .IsFixedLength();

            // =================== UNIQUE INDEXES ===================
            
            modelBuilder.Entity<User>()
                .HasIndex(u => u.EmployeeId)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // =================== RELATIONSHIPS ===================

            // Employee-User relationship (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Employee)
                .WithOne(e => e.User)
                .HasForeignKey<User>(u => u.EmployeeId)
                .HasPrincipalKey<Employee>(e => e.EMP_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // Department-Division relationship
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Division)
                .WithMany(div => div.Departments)
                .HasForeignKey(d => d.DivisiId)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee-Division relationship
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.DivisionNavigation)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DIVISION)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee-Department relationship
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.DepartmentNavigation)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DEPARTEMENT)
                .OnDelete(DeleteBehavior.Restrict);

            // User-Role relationship
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // =================== IDEA RELATIONSHIPS ===================

            // Idea-User relationship (Initiator)
            modelBuilder.Entity<Idea>()
                .HasOne(i => i.InitiatorUser)
                .WithMany(u => u.InitiatedIdeas)
                .HasForeignKey(i => i.InitiatorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Idea-Division relationship (CORRECTED)
            modelBuilder.Entity<Idea>()
                .HasOne(i => i.TargetDivision)
                .WithMany(d => d.TargetIdeas)
                .HasForeignKey(i => i.ToDivisionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Idea-Department relationship (CORRECTED)
            modelBuilder.Entity<Idea>()
                .HasOne(i => i.TargetDepartment)
                .WithMany(d => d.TargetIdeas)
                .HasForeignKey(i => i.ToDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Idea-Category relationship
            modelBuilder.Entity<Idea>()
                .HasOne(i => i.Category)
                .WithMany(c => c.Ideas)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Idea-Event relationship (Optional)
            modelBuilder.Entity<Idea>()
                .HasOne(i => i.Event)
                .WithMany(e => e.Ideas)
                .HasForeignKey(i => i.EventId)
                .OnDelete(DeleteBehavior.SetNull);

            // =================== DECIMAL PRECISION ===================
            
            modelBuilder.Entity<Idea>()
                .Property(i => i.SavingCost)
                .HasColumnType("decimal(18,2)")
                .HasPrecision(18, 2);

            modelBuilder.Entity<Idea>()
                .Property(i => i.SavingCostVaidated)
                .HasColumnType("decimal(18,2)")
                .HasPrecision(18, 2);

            // =================== CASCADE DELETE BEHAVIORS ===================
            
            modelBuilder.Entity<WorkflowHistory>()
                .HasOne(wh => wh.Idea)
                .WithMany(i => i.WorkflowHistories)
                .HasForeignKey(wh => wh.IdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkflowHistory>()
                .HasOne(wh => wh.ActorUser)
                .WithMany(u => u.WorkflowActions)
                .HasForeignKey(wh => wh.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Milestone>()
                .HasOne(m => m.Idea)
                .WithMany(i => i.Milestones)
                .HasForeignKey(m => m.IdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Milestone>()
                .HasOne(m => m.CreatorUser)
                .WithMany(u => u.CreatedMilestones)
                .HasForeignKey(m => m.CreatorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}