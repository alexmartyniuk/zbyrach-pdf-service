using Microsoft.EntityFrameworkCore.Migrations;

namespace Zbyrach.PdfService.Migrations
{
    public partial class LastErrorField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "Articles",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastError",
                table: "Articles");
        }
    }
}
