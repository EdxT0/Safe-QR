using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safe_Qr_Backend.Migrations
{
    /// <inheritdoc />
    public partial class MadeUrlAnUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UrlReport_Url",
                table: "UrlReport",
                column: "Url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UrlReport_Url",
                table: "UrlReport");
        }
    }
}
