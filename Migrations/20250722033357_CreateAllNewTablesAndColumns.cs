using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ideku.Migrations
{
    /// <inheritdoc />
    public partial class CreateAllNewTablesAndColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "category_id",
                table: "ideas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "event_id",
                table: "ideas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "category",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nama_category = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "divisi",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    nama_divisi = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_divisi", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nama_event = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "departement",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    nama_departement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    divisi_id = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departement", x => x.id);
                    table.ForeignKey(
                        name: "FK_departement_divisi_divisi_id",
                        column: x => x.divisi_id,
                        principalTable: "divisi",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ideas_category_id",
                table: "ideas",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_ideas_event_id",
                table: "ideas",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_departement_divisi_id",
                table: "departement",
                column: "divisi_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ideas_category_category_id",
                table: "ideas",
                column: "category_id",
                principalTable: "category",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_ideas_event_event_id",
                table: "ideas",
                column: "event_id",
                principalTable: "event",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ideas_category_category_id",
                table: "ideas");

            migrationBuilder.DropForeignKey(
                name: "FK_ideas_event_event_id",
                table: "ideas");

            migrationBuilder.DropTable(
                name: "category");

            migrationBuilder.DropTable(
                name: "departement");

            migrationBuilder.DropTable(
                name: "event");

            migrationBuilder.DropTable(
                name: "divisi");

            migrationBuilder.DropIndex(
                name: "IX_ideas_category_id",
                table: "ideas");

            migrationBuilder.DropIndex(
                name: "IX_ideas_event_id",
                table: "ideas");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "ideas");

            migrationBuilder.DropColumn(
                name: "event_id",
                table: "ideas");
        }
    }
}
