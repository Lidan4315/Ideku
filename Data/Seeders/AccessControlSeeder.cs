using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Seeders
{
    public static class AccessControlSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // 1. Seed Modules
            if (!context.Modules.Any())
            {
                var modules = new List<Module>
                {
                    new Module
                    {
                        ModuleName = "Dashboard",
                        ModuleKey = "dashboard",
                        ControllerName = "Home",
                        ActionName = "Index",
                        SortOrder = 1,
                        IsActive = true
                    },
                    new Module
                    {
                        ModuleName = "Submit New Idea",
                        ModuleKey = "idea_create",
                        ControllerName = "Idea",
                        ActionName = "Create",
                        SortOrder = 2,
                        IsActive = true
                    },
                    new Module
                    {
                        ModuleName = "My Ideas",
                        ModuleKey = "idea_list",
                        ControllerName = "Idea",
                        ActionName = "Index",
                        SortOrder = 3,
                        IsActive = true
                    },
                    new Module
                    {
                        ModuleName = "All Ideas",
                        ModuleKey = "idea_all",
                        ControllerName = "IdeaList",
                        ActionName = "Index",
                        SortOrder = 4,
                        IsActive = true
                    },
                    new Module
                    {
                        ModuleName = "Needs Approval",
                        ModuleKey = "approval",
                        ControllerName = "Approval",
                        ActionName = "Index",
                        SortOrder = 5,
                        IsActive = true
                    },
                    new Module
                    {
                        ModuleName = "Milestone Management",
                        ModuleKey = "milestone",
                        ControllerName = "Milestone",
                        ActionName = "Index",
                        SortOrder = 6,
                        IsActive = true
                    },
                    new Module
                    {
                        ModuleName = "Milestone - Send to Approval",
                        ModuleKey = "milestone_send_approval",
                        ControllerName = "Milestone",
                        ActionName = "SendToStage3Approval",
                        SortOrder = 7,
                        IsActive = true
                    },
                    new Module
                    {
                        ModuleName = "Monitoring",
                        ModuleKey = "monitoring",
                        ControllerName = "IdeaMonitoring",
                        ActionName = "Index",
                        SortOrder = 8,
                        IsActive = true
                    },
                    new Module
                    {
                        ModuleName = "User Management",
                        ModuleKey = "user_management",
                        ControllerName = "UserManagement",
                        ActionName = "Index",
                        SortOrder = 9,
                        IsActive = true
                    },
                    new Module
                    {
                        ModuleName = "Role Management",
                        ModuleKey = "role_management",
                        ControllerName = "RoleManagement",
                        ActionName = "Index",
                        SortOrder = 10,
                        IsActive = true
                    },
                    new Module
                    {
                        ModuleName = "Workflow Management",
                        ModuleKey = "workflow_management",
                        ControllerName = "WorkflowManagement",
                        ActionName = "Index",
                        SortOrder = 11,
                        IsActive = true
                    },
                    new Module
                    {
                        ModuleName = "Approver Management",
                        ModuleKey = "approver_management",
                        ControllerName = "ApproverManagement",
                        ActionName = "Index",
                        SortOrder = 12,
                        IsActive = true
                    },
                    new Module
                    {
                        ModuleName = "Change Workflow",
                        ModuleKey = "change_workflow",
                        ControllerName = "ChangeWorkflow",
                        ActionName = "Index",
                        SortOrder = 13,
                        IsActive = true
                    },
                    new Module
                    {
                        ModuleName = "Bypass Stage",
                        ModuleKey = "bypass_stage",
                        ControllerName = "BypassStage",
                        ActionName = "Index",
                        SortOrder = 14,
                        IsActive = true
                    },
                    new Module
                    {
                        ModuleName = "Access Control",
                        ModuleKey = "access_control",
                        ControllerName = "AccessControl",
                        ActionName = "Index",
                        SortOrder = 15,
                        IsActive = true
                    }
                };

                context.Modules.AddRange(modules);
                context.SaveChanges();
            }

            // 2. Grant all module access to Superuser role
            var superuserRole = context.Roles.FirstOrDefault(r => r.RoleName == "Superuser");

            if (superuserRole != null)
            {
                var existingAccess = context.RoleAccessModules
                    .Where(ram => ram.RoleId == superuserRole.Id)
                    .Count();

                if (existingAccess == 0)
                {
                    var allModules = context.Modules.ToList();
                    var roleAccessModules = allModules.Select(m => new RoleAccessModule
                    {
                        RoleId = superuserRole.Id,
                        ModuleId = m.Id,
                        CanAccess = true,
                        ModifiedAt = DateTime.Now,
                        ModifiedBy = null // System seed
                    }).ToList();

                    context.RoleAccessModules.AddRange(roleAccessModules);
                    context.SaveChanges();
                }
            }
        }
    }
}
