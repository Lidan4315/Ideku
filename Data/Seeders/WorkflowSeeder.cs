using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Seeders
{
    public static class WorkflowSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // Seed in order: Approvers -> ApproverRoles -> Workflows -> WorkflowConditions -> WorkflowStages
            SeedApprovers(context);
            SeedApproverRoles(context);
            SeedWorkflows(context);
            SeedWorkflowConditions(context);
            SeedWorkflowStages(context);
        }

        private static void SeedApprovers(AppDbContext context)
        {
            // Check if data already exists
            if (context.Approvers.Any())
            {
                return; // DB has been seeded
            }

            // Seed Approvers
            var approvers = new[]
            {
                new Approver { ApproverName = "APV_1", IsActive = true, CreatedAt = DateTime.Now }, // Workstream Leader
                new Approver { ApproverName = "APV_2", IsActive = true, CreatedAt = DateTime.Now }, // Mgr. Dept
                new Approver { ApproverName = "APV_3", IsActive = true, CreatedAt = DateTime.Now }, // GM Division
                new Approver { ApproverName = "APV_4", IsActive = true, CreatedAt = DateTime.Now }, // GM BPID
                new Approver { ApproverName = "APV_5", IsActive = true, CreatedAt = DateTime.Now }, // COO
                new Approver { ApproverName = "APV_6", IsActive = true, CreatedAt = DateTime.Now }  // SCFO
            };
            context.Approvers.AddRange(approvers);
            context.SaveChanges();
        }

        private static void SeedApproverRoles(AppDbContext context)
        {
            // Check if data already exists
            if (context.ApproverRoles.Any())
            {
                return; // DB has been seeded
            }

            // Get approvers
            var workStream = context.Approvers.FirstOrDefault(a => a.ApproverName == "APV_1");
            var mgrDept = context.Approvers.FirstOrDefault(a => a.ApproverName == "APV_2");
            var gmDiv = context.Approvers.FirstOrDefault(a => a.ApproverName == "APV_3");
            var gmBpid = context.Approvers.FirstOrDefault(a => a.ApproverName == "APV_4");
            var coo = context.Approvers.FirstOrDefault(a => a.ApproverName == "APV_5");
            var scfo = context.Approvers.FirstOrDefault(a => a.ApproverName == "APV_6");

            // Get roles from database
            var workStreamRole = context.Roles.FirstOrDefault(r => r.RoleName == "Workstream Leader");
            var mgrDeptRole = context.Roles.FirstOrDefault(r => r.RoleName == "Mgr. Dept");
            var gmDivRole = context.Roles.FirstOrDefault(r => r.RoleName == "GM Division");
            var gmBpidRole = context.Roles.FirstOrDefault(r => r.RoleName == "GM BPID");
            var cooRole = context.Roles.FirstOrDefault(r => r.RoleName == "COO");
            var scfoRole = context.Roles.FirstOrDefault(r => r.RoleName == "SCFO");

            var approverRoles = new List<ApproverRole>();

            // Map approvers to roles
            if (workStream != null && workStreamRole != null)
                approverRoles.Add(new ApproverRole { ApproverId = workStream.Id, RoleId = workStreamRole.Id, CreatedAt = DateTime.Now });

            if (mgrDept != null && mgrDeptRole != null)
                approverRoles.Add(new ApproverRole { ApproverId = mgrDept.Id, RoleId = mgrDeptRole.Id, CreatedAt = DateTime.Now });

            if (gmDiv != null && gmDivRole != null)
                approverRoles.Add(new ApproverRole { ApproverId = gmDiv.Id, RoleId = gmDivRole.Id, CreatedAt = DateTime.Now });

            if (gmBpid != null && gmBpidRole != null)
                approverRoles.Add(new ApproverRole { ApproverId = gmBpid.Id, RoleId = gmBpidRole.Id, CreatedAt = DateTime.Now });

            if (coo != null && cooRole != null)
                approverRoles.Add(new ApproverRole { ApproverId = coo.Id, RoleId = cooRole.Id, CreatedAt = DateTime.Now });

            if (scfo != null && scfoRole != null)
                approverRoles.Add(new ApproverRole { ApproverId = scfo.Id, RoleId = scfoRole.Id, CreatedAt = DateTime.Now });

            if (approverRoles.Any())
            {
                context.ApproverRoles.AddRange(approverRoles);
                context.SaveChanges();
            }
        }

        private static void SeedWorkflows(AppDbContext context)
        {
            // Check if data already exists
            if (context.Workflows.Any())
            {
                return; // DB has been seeded
            }

            // Seed Workflows
            var workflows = new[]
            {
                new Workflow
                {
                    WorkflowName = "WF_High Value",
                    Desc = "Workflow for ideas with saving cost >= 20,000",
                    IsActive = true,
                    Priority = 1,
                    CreatedAt = DateTime.Now
                },
                new Workflow
                {
                    WorkflowName = "WF_Standard",
                    Desc = "Workflow for ideas with saving cost < 20,000",
                    IsActive = true,
                    Priority = 2,
                    CreatedAt = DateTime.Now
                }
            };
            context.Workflows.AddRange(workflows);
            context.SaveChanges();
        }

        private static void SeedWorkflowConditions(AppDbContext context)
        {
            // Check if data already exists
            if (context.WorkflowConditions.Any())
            {
                return; // DB has been seeded
            }

            // Get workflows
            var highValueWorkflow = context.Workflows.FirstOrDefault(w => w.WorkflowName == "WF_High Value");
            var standardWorkflow = context.Workflows.FirstOrDefault(w => w.WorkflowName == "WF_Standard");

            var conditions = new List<WorkflowCondition>();

            // High Value Workflow: Saving Cost >= 20,000
            if (highValueWorkflow != null)
            {
                conditions.Add(new WorkflowCondition
                {
                    WorkflowId = highValueWorkflow.Id,
                    ConditionType = "SAVING_COST",
                    Operator = ">=",
                    ConditionValue = "20000",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });
            }

            // Standard Workflow: Saving Cost < 20,000
            if (standardWorkflow != null)
            {
                conditions.Add(new WorkflowCondition
                {
                    WorkflowId = standardWorkflow.Id,
                    ConditionType = "SAVING_COST",
                    Operator = "<",
                    ConditionValue = "20000",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });
            }

            if (conditions.Any())
            {
                context.WorkflowConditions.AddRange(conditions);
                context.SaveChanges();
            }
        }

        private static void SeedWorkflowStages(AppDbContext context)
        {
            // Check if data already exists
            if (context.WorkflowStages.Any())
            {
                return; // DB has been seeded
            }

            // Get workflows
            var highValueWorkflow = context.Workflows.FirstOrDefault(w => w.WorkflowName == "WF_High Value");
            var standardWorkflow = context.Workflows.FirstOrDefault(w => w.WorkflowName == "WF_Standard");

            // Get approvers
            var workStream = context.Approvers.FirstOrDefault(a => a.ApproverName == "APV_1");
            var mgrDept = context.Approvers.FirstOrDefault(a => a.ApproverName == "APV_2");
            var gmDiv = context.Approvers.FirstOrDefault(a => a.ApproverName == "APV_3");
            var gmBpid = context.Approvers.FirstOrDefault(a => a.ApproverName == "APV_4");
            var coo = context.Approvers.FirstOrDefault(a => a.ApproverName == "APV_5");
            var scfo = context.Approvers.FirstOrDefault(a => a.ApproverName == "APV_6");

            var workflowStages = new List<WorkflowStage>();

            // High Value Workflow: S1-S6 (APV_1 -> APV_2 -> APV_3 -> APV_4 -> APV_5 -> APV_6)
            if (highValueWorkflow != null)
            {
                if (workStream != null)
                    workflowStages.Add(new WorkflowStage { WorkflowId = highValueWorkflow.Id, ApproverId = workStream.Id, Stage = 1, IsMandatory = true, IsParallel = false, CreatedAt = DateTime.Now });

                if (mgrDept != null)
                    workflowStages.Add(new WorkflowStage { WorkflowId = highValueWorkflow.Id, ApproverId = mgrDept.Id, Stage = 2, IsMandatory = true, IsParallel = false, CreatedAt = DateTime.Now });

                if (gmDiv != null)
                    workflowStages.Add(new WorkflowStage { WorkflowId = highValueWorkflow.Id, ApproverId = gmDiv.Id, Stage = 3, IsMandatory = true, IsParallel = false, CreatedAt = DateTime.Now });

                if (gmBpid != null)
                    workflowStages.Add(new WorkflowStage { WorkflowId = highValueWorkflow.Id, ApproverId = gmBpid.Id, Stage = 4, IsMandatory = true, IsParallel = false, CreatedAt = DateTime.Now });

                if (coo != null)
                    workflowStages.Add(new WorkflowStage { WorkflowId = highValueWorkflow.Id, ApproverId = coo.Id, Stage = 5, IsMandatory = true, IsParallel = false, CreatedAt = DateTime.Now });

                if (scfo != null)
                    workflowStages.Add(new WorkflowStage { WorkflowId = highValueWorkflow.Id, ApproverId = scfo.Id, Stage = 6, IsMandatory = true, IsParallel = false, CreatedAt = DateTime.Now });
            }

            // Standard Workflow: S1-S4 (APV_1 -> APV_2 -> APV_3 -> APV_4)
            if (standardWorkflow != null)
            {
                if (workStream != null)
                    workflowStages.Add(new WorkflowStage { WorkflowId = standardWorkflow.Id, ApproverId = workStream.Id, Stage = 1, IsMandatory = true, IsParallel = false, CreatedAt = DateTime.Now });

                if (mgrDept != null)
                    workflowStages.Add(new WorkflowStage { WorkflowId = standardWorkflow.Id, ApproverId = mgrDept.Id, Stage = 2, IsMandatory = true, IsParallel = false, CreatedAt = DateTime.Now });

                if (gmDiv != null)
                    workflowStages.Add(new WorkflowStage { WorkflowId = standardWorkflow.Id, ApproverId = gmDiv.Id, Stage = 3, IsMandatory = true, IsParallel = false, CreatedAt = DateTime.Now });

                if (gmBpid != null)
                    workflowStages.Add(new WorkflowStage { WorkflowId = standardWorkflow.Id, ApproverId = gmBpid.Id, Stage = 4, IsMandatory = true, IsParallel = false, CreatedAt = DateTime.Now });
            }

            if (workflowStages.Any())
            {
                context.WorkflowStages.AddRange(workflowStages);
                context.SaveChanges();
            }
        }
    }
}
