using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safe_Qr_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddedRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "User",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "UrlReport",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "User");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "UrlReport");
        }
    }
}
