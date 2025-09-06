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
        
        // Dynamic Workflow System DbSets
        public DbSet<Workflow> Workflows { get; set; }
        public DbSet<Approver> Approvers { get; set; }
        public DbSet<ApproverRole> ApproverRoles { get; set; }
        public DbSet<WorkflowStage> WorkflowStages { get; set; }
        public DbSet<WorkflowCondition> WorkflowConditions { get; set; }

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

            // =================== DYNAMIC WORKFLOW INDEXES ===================

            // Workflow indexes
            modelBuilder.Entity<Workflow>()
                .HasIndex(w => w.IsActive)
                .HasDatabaseName("IX_Workflows_IsActive");

            modelBuilder.Entity<Workflow>()
                .HasIndex(w => new { w.IsActive, w.Priority })
                .HasDatabaseName("IX_Workflows_IsActive_Priority");

            // Approver indexes
            modelBuilder.Entity<Approver>()
                .HasIndex(a => a.IsActive)
                .HasDatabaseName("IX_Approvers_IsActive");

            // WorkflowStage indexes for performance
            modelBuilder.Entity<WorkflowStage>()
                .HasIndex(ws => ws.WorkflowId)
                .HasDatabaseName("IX_WorkflowStages_WorkflowId");

            modelBuilder.Entity<WorkflowStage>()
                .HasIndex(ws => ws.ApproverId)
                .HasDatabaseName("IX_WorkflowStages_ApproverId");

            modelBuilder.Entity<WorkflowStage>()
                .HasIndex(ws => new { ws.WorkflowId, ws.Stage })
                .IsUnique()
                .HasDatabaseName("IX_WorkflowStages_WorkflowId_Stage");

            // WorkflowCondition indexes
            modelBuilder.Entity<WorkflowCondition>()
                .HasIndex(wc => wc.WorkflowId)
                .HasDatabaseName("IX_WorkflowConditions_WorkflowId");

            modelBuilder.Entity<WorkflowCondition>()
                .HasIndex(wc => new { wc.ConditionType, wc.IsActive })
                .HasDatabaseName("IX_WorkflowConditions_ConditionType_IsActive");

            // ApproverRole indexes
            modelBuilder.Entity<ApproverRole>()
                .HasIndex(ar => ar.ApproverId)
                .HasDatabaseName("IX_ApproverRoles_ApproverId");

            modelBuilder.Entity<ApproverRole>()
                .HasIndex(ar => ar.RoleId)
                .HasDatabaseName("IX_ApproverRoles_RoleId");


            // Idea indexes for workflow
            modelBuilder.Entity<Idea>()
                .HasIndex(i => i.WorkflowId)
                .HasDatabaseName("IX_Ideas_WorkflowId");

            // Idea indexes for soft delete
            modelBuilder.Entity<Idea>()
                .HasIndex(i => i.IsDeleted)
                .HasDatabaseName("IX_Ideas_IsDeleted");

            // Composite index for active ideas
            modelBuilder.Entity<Idea>()
                .HasIndex(i => new { i.IsDeleted, i.CurrentStatus })
                .HasDatabaseName("IX_Ideas_IsDeleted_CurrentStatus");

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
                .Property(i => i.SavingCostValidated)
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

            // =================== DYNAMIC WORKFLOW RELATIONSHIPS ===================

            // WorkflowStage-Workflow relationship
            modelBuilder.Entity<WorkflowStage>()
                .HasOne(ws => ws.Workflow)
                .WithMany(w => w.WorkflowStages)
                .HasForeignKey(ws => ws.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkflowStage-Approver relationship
            modelBuilder.Entity<WorkflowStage>()
                .HasOne(ws => ws.Approver)
                .WithMany(a => a.WorkflowStages)
                .HasForeignKey(ws => ws.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);

            // WorkflowCondition-Workflow relationship
            modelBuilder.Entity<WorkflowCondition>()
                .HasOne(wc => wc.Workflow)
                .WithMany(w => w.WorkflowConditions)
                .HasForeignKey(wc => wc.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            // ApproverRole-Approver relationship
            modelBuilder.Entity<ApproverRole>()
                .HasOne(ar => ar.Approver)
                .WithMany(a => a.ApproverRoles)
                .HasForeignKey(ar => ar.ApproverId)
                .OnDelete(DeleteBehavior.Cascade);

            // ApproverRole-Role relationship
            modelBuilder.Entity<ApproverRole>()
                .HasOne(ar => ar.Role)
                .WithMany()
                .HasForeignKey(ar => ar.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Idea-Workflow relationship
            modelBuilder.Entity<Idea>()
                .HasOne(i => i.Workflow)
                .WithMany()
                .HasForeignKey(i => i.WorkflowId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}