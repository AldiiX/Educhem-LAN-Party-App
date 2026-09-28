using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class OpaqueEmailTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Purpose",
                schema: "public",
                table: "AccountEmailLinks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                schema: "public",
                table: "AccountEmailLinks",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeRequests_CancelTokenHash",
                schema: "public",
                table: "EmailChangeRequests",
                column: "CancelTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeRequests_NewTokenHash",
                schema: "public",
                table: "EmailChangeRequests",
                column: "NewTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeRequests_OldTokenHash",
                schema: "public",
                table: "EmailChangeRequests",
                column: "OldTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountEmailLinks_TokenHash",
                schema: "public",
                table: "AccountEmailLinks",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailChangeRequests_CancelTokenHash",
                schema: "public",
                table: "EmailChangeRequests");

            migrationBuilder.DropIndex(
                name: "IX_EmailChangeRequests_NewTokenHash",
                schema: "public",
                table: "EmailChangeRequests");

            migrationBuilder.DropIndex(
                name: "IX_EmailChangeRequests_OldTokenHash",
                schema: "public",
                table: "EmailChangeRequests");

            migrationBuilder.DropIndex(
                name: "IX_AccountEmailLinks_TokenHash",
                schema: "public",
                table: "AccountEmailLinks");

            migrationBuilder.DropColumn(
                name: "Purpose",
                schema: "public",
                table: "AccountEmailLinks");

            migrationBuilder.DropColumn(
                name: "TokenHash",
                schema: "public",
                table: "AccountEmailLinks");
        }
    }
}
