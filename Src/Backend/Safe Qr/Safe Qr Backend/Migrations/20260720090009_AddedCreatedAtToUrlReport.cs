using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safe_Qr_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddedCreatedAtToUrlReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "UrlReport",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UrlReport");
        }
    }
}
