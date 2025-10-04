using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ideku.Migrations
{
    /// <inheritdoc />
    public partial class FixActingLocationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActingDepartmentId",
                table: "Users",
                type: "char(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActingDivisionId",
                table: "Users",
                type: "char(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ActingDepartmentId",
                table: "Users",
                column: "ActingDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ActingDivisionId",
                table: "Users",
                column: "ActingDivisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_ActingDepartmentId",
                table: "Users",
                column: "ActingDepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Divisions_ActingDivisionId",
                table: "Users",
                column: "ActingDivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Departments_ActingDepartmentId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Divisions_ActingDivisionId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ActingDepartmentId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ActingDivisionId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ActingDepartmentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ActingDivisionId",
                table: "Users");
        }
    }
}
