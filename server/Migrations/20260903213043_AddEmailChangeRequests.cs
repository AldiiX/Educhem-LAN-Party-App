using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailChangeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_Email",
                schema: "public",
                table: "Accounts");

            migrationBuilder.AddColumn<Guid>(
                name: "SecurityStamp",
                schema: "public",
                table: "AuthSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SecurityStamp",
                schema: "public",
                table: "Accounts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                schema: "public",
                table: "Accounts",
                type: "character varying(96)",
                maxLength: 96,
                nullable: false,
                computedColumnSql: "lower(btrim(\"Email\"))",
                stored: true);

            migrationBuilder.CreateTable(
                name: "EmailChangeAttempts",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailChangeAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailChangeAttempts_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailChangeRequests",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    OldEmail = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    NewEmail = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    OldTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    NewTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CancelTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OldConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NewConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailChangeRequests_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_NormalizedEmail",
                schema: "public",
                table: "Accounts",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeAttempts_AccountId_CreatedAtUtc",
                schema: "public",
                table: "EmailChangeAttempts",
                columns: new[] { "AccountId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeRequests_AccountId",
                schema: "public",
                table: "EmailChangeRequests",
                column: "AccountId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailChangeAttempts",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EmailChangeRequests",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_NormalizedEmail",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                schema: "public",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                schema: "public",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Email",
                schema: "public",
                table: "Accounts",
                column: "Email",
                unique: true);
        }
    }
}
