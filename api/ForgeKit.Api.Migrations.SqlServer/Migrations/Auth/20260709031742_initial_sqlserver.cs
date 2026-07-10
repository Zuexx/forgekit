using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeKit.Api.Migrations.SqlServer.Migrations.Auth
{
    /// <inheritdoc />
    public partial class initial_sqlserver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "jwks",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    publicKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    privateKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jwks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    emailVerified = table.Column<bool>(type: "bit", nullable: false),
                    image = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    role = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    banned = table.Column<bool>(type: "bit", nullable: true),
                    banReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    banExpires = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "verification",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    identifier = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    expiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "account",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    accountId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    providerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    userId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    accessToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    refreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    idToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    accessTokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    refreshTokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    scope = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_user_userId",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "session",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    expiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    token = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ipAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    userAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    userId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    impersonatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session", x => x.id);
                    table.ForeignKey(
                        name: "FK_session_user_userId",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_userId",
                table: "account",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_session_token",
                table: "session",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_session_userId",
                table: "session",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_user_email",
                table: "user",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_verification_identifier",
                table: "verification",
                column: "identifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account");

            migrationBuilder.DropTable(
                name: "jwks");

            migrationBuilder.DropTable(
                name: "session");

            migrationBuilder.DropTable(
                name: "verification");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
