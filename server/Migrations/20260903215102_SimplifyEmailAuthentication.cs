using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyEmailAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE public."Accounts"
                SET "Email" = lower(btrim("Email"))
                WHERE "Email" <> lower(btrim("Email"));

                UPDATE public."EmailChangeRequests"
                SET "OldEmail" = lower(btrim("OldEmail")), "NewEmail" = lower(btrim("NewEmail"));
                """);

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
                table: "EmailChangeRequests");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                schema: "public",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                schema: "public",
                table: "Accounts");

            migrationBuilder.CreateTable(
                name: "AccountEmailLinks",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountEmailLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountEmailLinks_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Email",
                schema: "public",
                table: "Accounts",
                column: "Email",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Accounts_Email_Normalized",
                schema: "public",
                table: "Accounts",
                sql: "\"Email\" = lower(btrim(\"Email\"))");

            migrationBuilder.CreateIndex(
                name: "IX_AccountEmailLinks_AccountId",
                schema: "public",
                table: "AccountEmailLinks",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountEmailLinks",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Email",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Accounts_Email_Normalized",
                schema: "public",
                table: "Accounts");

            migrationBuilder.AddColumn<Guid>(
                name: "SecurityStamp",
                schema: "public",
                table: "EmailChangeRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_NormalizedEmail",
                schema: "public",
                table: "Accounts",
                column: "NormalizedEmail",
                unique: true);
        }
    }
}
