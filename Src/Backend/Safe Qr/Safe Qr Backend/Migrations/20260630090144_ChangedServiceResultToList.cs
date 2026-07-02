using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safe_Qr_Backend.Migrations
{
    /// <inheritdoc />
    public partial class ChangedServiceResultToList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Result_reasons",
                table: "UrlReport");

            migrationBuilder.DropColumn(
                name: "Result_serviceResultVerdict",
                table: "UrlReport");

            migrationBuilder.AddColumn<string>(
                name: "Results",
                table: "UrlReport",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Results",
                table: "UrlReport");

            migrationBuilder.AddColumn<string[]>(
                name: "Result_reasons",
                table: "UrlReport",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "Result_serviceResultVerdict",
                table: "UrlReport",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
