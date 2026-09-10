using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeKit.Api.Migrations.SqlServer.Migrations.Auth
{
    /// <inheritdoc />
    public partial class AddAuthSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.RenameTable(
                name: "verification",
                newName: "verification",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "user",
                newName: "user",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "session",
                newName: "session",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "jwks",
                newName: "jwks",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "account",
                newName: "account",
                newSchema: "auth");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "verification",
                schema: "auth",
                newName: "verification");

            migrationBuilder.RenameTable(
                name: "user",
                schema: "auth",
                newName: "user");

            migrationBuilder.RenameTable(
                name: "session",
                schema: "auth",
                newName: "session");

            migrationBuilder.RenameTable(
                name: "jwks",
                schema: "auth",
                newName: "jwks");

            migrationBuilder.RenameTable(
                name: "account",
                schema: "auth",
                newName: "account");
        }
    }
}
