using Microsoft.EntityFrameworkCore.Migrations;

namespace Zbyrach.PdfService.Migrations
{
    public partial class IndexForStoredAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Articles_StoredAt",
                table: "Articles",
                column: "StoredAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Articles_StoredAt",
                table: "Articles");
        }
    }
}
