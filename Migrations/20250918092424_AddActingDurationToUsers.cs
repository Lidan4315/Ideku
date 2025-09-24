using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ideku.Migrations
{
    /// <inheritdoc />
    public partial class AddActingDurationToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActingEndDate",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActingStartDate",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentRoleId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CurrentRoleId",
                table: "Users",
                column: "CurrentRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_CurrentRoleId",
                table: "Users",
                column: "CurrentRoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_CurrentRoleId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_CurrentRoleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ActingEndDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ActingStartDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CurrentRoleId",
                table: "Users");
        }
    }
}
