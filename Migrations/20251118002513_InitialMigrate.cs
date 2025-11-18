using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ideku.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Approvers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApproverName = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Approvers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Divisions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false),
                    NameDivision = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Divisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModuleKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ControllerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false),
                    NameDepartment = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DivisiId = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Divisions_DivisiId",
                        column: x => x.DivisiId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApproverRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApproverId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApproverRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApproverRoles_Approvers_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Approvers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApproverRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowId = table.Column<int>(type: "int", nullable: false),
                    ConditionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Operator = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ConditionValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowConditions_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowId = table.Column<int>(type: "int", nullable: false),
                    ApproverId = table.Column<int>(type: "int", nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    IsParallel = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStages_Approvers_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Approvers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowStages_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EMPLIST",
                columns: table => new
                {
                    EMP_ID = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NAME = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    POSITION_TITLE = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DIVISION = table.Column<string>(type: "char(3)", maxLength: 10, nullable: false),
                    DEPARTEMENT = table.Column<string>(type: "char(3)", maxLength: 10, nullable: false),
                    EMAIL = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    POSITION_LVL = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EMP_STATUS = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMPLIST", x => x.EMP_ID);
                    table.ForeignKey(
                        name: "FK_EMPLIST_Departments_DEPARTEMENT",
                        column: x => x.DEPARTEMENT,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EMPLIST_Divisions_DIVISION",
                        column: x => x.DIVISION,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActing = table.Column<bool>(type: "bit", nullable: false),
                    CurrentRoleId = table.Column<int>(type: "int", nullable: true),
                    ActingStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActingEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActingDivisionId = table.Column<string>(type: "char(3)", maxLength: 3, nullable: true),
                    ActingDepartmentId = table.Column<string>(type: "char(3)", maxLength: 3, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Departments_ActingDepartmentId",
                        column: x => x.ActingDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Divisions_ActingDivisionId",
                        column: x => x.ActingDivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_EMPLIST_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "EMPLIST",
                        principalColumn: "EMP_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Roles_CurrentRoleId",
                        column: x => x.CurrentRoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ideas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InitiatorUserId = table.Column<long>(type: "bigint", nullable: false),
                    ToDivisionId = table.Column<string>(type: "char(3)", maxLength: 3, nullable: false),
                    ToDepartmentId = table.Column<string>(type: "char(3)", maxLength: 3, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: true),
                    IdeaName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IdeaIssueBackground = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IdeaSolution = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SavingCost = table.Column<long>(type: "bigint", nullable: false),
                    SavingCostValidated = table.Column<long>(type: "bigint", nullable: true),
                    AttachmentFiles = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkflowId = table.Column<int>(type: "int", nullable: false),
                    CurrentStage = table.Column<int>(type: "int", nullable: false),
                    MaxStage = table.Column<int>(type: "int", nullable: false),
                    CurrentStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsRejected = table.Column<bool>(type: "bit", nullable: false),
                    RejectedReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IdeaCode = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsMilestoneCreated = table.Column<bool>(type: "bit", nullable: false),
                    RelatedDivisions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ideas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ideas_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ideas_Departments_ToDepartmentId",
                        column: x => x.ToDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ideas_Divisions_ToDivisionId",
                        column: x => x.ToDivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ideas_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Ideas_Users_InitiatorUserId",
                        column: x => x.InitiatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ideas_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoleAccessModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ModuleId = table.Column<int>(type: "int", nullable: false),
                    CanAccess = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAccessModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleAccessModules_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleAccessModules_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleAccessModules_Users_ModifiedBy",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "IdeaImplementators",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdeaId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeaImplementators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdeaImplementators_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IdeaImplementators_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdeaMonitorings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdeaId = table.Column<long>(type: "bigint", nullable: false),
                    MonitoringName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MeasurementUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MonthFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MonthTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CostSavePlan = table.Column<long>(type: "bigint", nullable: true),
                    CostSaveActual = table.Column<long>(type: "bigint", nullable: true),
                    CostSaveActualValidated = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeaMonitorings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdeaMonitorings_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Milestones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdeaId = table.Column<long>(type: "bigint", nullable: false),
                    CreatorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatorEmployeeId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TitleMilestone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Milestones_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdeaId = table.Column<long>(type: "bigint", nullable: false),
                    ActorUserId = table.Column<long>(type: "bigint", nullable: false),
                    FromStage = table.Column<int>(type: "int", nullable: false),
                    ToStage = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowHistory_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowHistory_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MilestonePICs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MilestoneId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MilestonePICs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MilestonePICs_Milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "Milestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MilestonePICs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApproverRoles_ApproverId",
                table: "ApproverRoles",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_ApproverRoles_RoleId",
                table: "ApproverRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Approvers_IsActive",
                table: "Approvers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DivisiId",
                table: "Departments",
                column: "DivisiId");

            migrationBuilder.CreateIndex(
                name: "IX_EMPLIST_DEPARTEMENT",
                table: "EMPLIST",
                column: "DEPARTEMENT");

            migrationBuilder.CreateIndex(
                name: "IX_EMPLIST_DIVISION",
                table: "EMPLIST",
                column: "DIVISION");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaImplementators_IdeaId",
                table: "IdeaImplementators",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaImplementators_IdeaId_Role",
                table: "IdeaImplementators",
                columns: new[] { "IdeaId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_IdeaImplementators_IdeaId_UserId",
                table: "IdeaImplementators",
                columns: new[] { "IdeaId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_IdeaImplementators_Role",
                table: "IdeaImplementators",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaImplementators_UserId",
                table: "IdeaImplementators",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaMonitorings_IdeaId",
                table: "IdeaMonitorings",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaMonitorings_IdeaId_Period",
                table: "IdeaMonitorings",
                columns: new[] { "IdeaId", "MonthFrom", "MonthTo" });

            migrationBuilder.CreateIndex(
                name: "IX_IdeaMonitorings_MonthFrom",
                table: "IdeaMonitorings",
                column: "MonthFrom");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaMonitorings_MonthTo",
                table: "IdeaMonitorings",
                column: "MonthTo");

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_CategoryId",
                table: "Ideas",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_EventId",
                table: "Ideas",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_InitiatorUserId",
                table: "Ideas",
                column: "InitiatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_IsDeleted",
                table: "Ideas",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_IsDeleted_CurrentStatus",
                table: "Ideas",
                columns: new[] { "IsDeleted", "CurrentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_ToDepartmentId",
                table: "Ideas",
                column: "ToDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_ToDivisionId",
                table: "Ideas",
                column: "ToDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_WorkflowId",
                table: "Ideas",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_MilestonePICs_MilestoneId",
                table: "MilestonePICs",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_MilestonePICs_MilestoneId_UserId",
                table: "MilestonePICs",
                columns: new[] { "MilestoneId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MilestonePICs_UserId",
                table: "MilestonePICs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_IdeaId",
                table: "Milestones",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_ControllerName",
                table: "Modules",
                column: "ControllerName");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_IsActive_SortOrder",
                table: "Modules",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UQ_Modules_ModuleKey",
                table: "Modules",
                column: "ModuleKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleAccessModules_ModifiedBy",
                table: "RoleAccessModules",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAccessModules_ModuleId",
                table: "RoleAccessModules",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAccessModules_RoleId_CanAccess",
                table: "RoleAccessModules",
                columns: new[] { "RoleId", "CanAccess" });

            migrationBuilder.CreateIndex(
                name: "UQ_RoleAccessModules_RoleModule",
                table: "RoleAccessModules",
                columns: new[] { "RoleId", "ModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ActingDepartmentId",
                table: "Users",
                column: "ActingDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ActingDivisionId",
                table: "Users",
                column: "ActingDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CurrentRoleId",
                table: "Users",
                column: "CurrentRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId",
                table: "Users",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConditions_ConditionType_IsActive",
                table: "WorkflowConditions",
                columns: new[] { "ConditionType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConditions_WorkflowId",
                table: "WorkflowConditions",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowHistory_ActorUserId",
                table: "WorkflowHistory",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowHistory_IdeaId",
                table: "WorkflowHistory",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_IsActive",
                table: "Workflows",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_IsActive_Priority",
                table: "Workflows",
                columns: new[] { "IsActive", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStages_ApproverId",
                table: "WorkflowStages",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStages_WorkflowId",
                table: "WorkflowStages",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStages_WorkflowId_Stage",
                table: "WorkflowStages",
                columns: new[] { "WorkflowId", "Stage" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApproverRoles");

            migrationBuilder.DropTable(
                name: "IdeaImplementators");

            migrationBuilder.DropTable(
                name: "IdeaMonitorings");

            migrationBuilder.DropTable(
                name: "MilestonePICs");

            migrationBuilder.DropTable(
                name: "RoleAccessModules");

            migrationBuilder.DropTable(
                name: "WorkflowConditions");

            migrationBuilder.DropTable(
                name: "WorkflowHistory");

            migrationBuilder.DropTable(
                name: "WorkflowStages");

            migrationBuilder.DropTable(
                name: "Milestones");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "Approvers");

            migrationBuilder.DropTable(
                name: "Ideas");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Workflows");

            migrationBuilder.DropTable(
                name: "EMPLIST");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Divisions");
        }
    }
}
