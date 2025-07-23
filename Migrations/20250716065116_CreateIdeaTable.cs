using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ideku.Migrations
{
    /// <inheritdoc />
    public partial class CreateIdeaTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ideas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    cInitiator = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    cDivision = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    cDepartment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    cIdea_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    cIdea_issue_background = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cIdea_solution = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nSaving_cost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    cAttachment_file = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    dSubmitted_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    dUpdated_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    nCurrent_stage = table.Column<int>(type: "int", nullable: true),
                    cCurrent_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    cImsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    flag_status = table.Column<bool>(type: "bit", nullable: true),
                    cSavingCostOption = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    rejectReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    catReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    nSavingCostValidated = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    cSavingCostOptionValidated = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    attachmentMonitoring = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cFlagFlow = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    cIdeaType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    flagFinance = table.Column<bool>(type: "bit", nullable: true),
                    rejected_state = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ideaFlowValidated = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    completedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ideaCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ideas", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ideas");
        }
    }
}
