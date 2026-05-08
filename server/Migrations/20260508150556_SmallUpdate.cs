using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class SmallUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_School_SchoolId",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_School",
                table: "School");

            migrationBuilder.RenameTable(
                name: "School",
                newName: "Schools",
                newSchema: "public");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                schema: "public",
                table: "Schools",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Schools",
                schema: "public",
                table: "Schools",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Schools_Slug",
                schema: "public",
                table: "Schools",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Schools_SchoolId",
                schema: "public",
                table: "Accounts",
                column: "SchoolId",
                principalSchema: "public",
                principalTable: "Schools",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Schools_SchoolId",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Schools",
                schema: "public",
                table: "Schools");

            migrationBuilder.DropIndex(
                name: "IX_Schools_Slug",
                schema: "public",
                table: "Schools");

            migrationBuilder.RenameTable(
                name: "Schools",
                schema: "public",
                newName: "School");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "School",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AddPrimaryKey(
                name: "PK_School",
                table: "School",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_School_SchoolId",
                schema: "public",
                table: "Accounts",
                column: "SchoolId",
                principalTable: "School",
                principalColumn: "Id");
        }
    }
}
