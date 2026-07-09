using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safe_Qr_Backend.Migrations
{
    /// <inheritdoc />
    public partial class addedRoleAsEnumAndIndexedEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "email",
                table: "User",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "password",
                table: "User",
                newName: "Role");

            migrationBuilder.AddColumn<string>(
                name: "HashedPassword",
                table: "User",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "User",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_Email",
                table: "User");

            migrationBuilder.DropColumn(
                name: "HashedPassword",
                table: "User");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "User",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "User",
                newName: "password");
        }
    }
}
