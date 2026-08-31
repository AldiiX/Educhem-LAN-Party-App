using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateOAuthConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "DiscordConnections",
                schema: "public",
                newName: "OAuthConnections",
                newSchema: "public");

            migrationBuilder.RenameColumn(
                name: "DiscordId",
                schema: "public",
                table: "OAuthConnections",
                newName: "ProviderUserId");

            migrationBuilder.DropIndex(
                name: "IX_DiscordConnections_DiscordId",
                schema: "public",
                table: "OAuthConnections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DiscordConnections",
                schema: "public",
                table: "OAuthConnections");

            migrationBuilder.DropForeignKey(
                name: "FK_DiscordConnections_Accounts_AccountId",
                schema: "public",
                table: "OAuthConnections");

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                schema: "public",
                table: "OAuthConnections",
                type: "text",
                nullable: false,
                defaultValue: "Discord");

            migrationBuilder.AddColumn<string>(
                name: "ProfileUrl",
                schema: "public",
                table: "OAuthConnections",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                schema: "public",
                table: "OAuthConnections",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderUserId",
                schema: "public",
                table: "OAuthConnections",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                schema: "public",
                table: "OAuthConnections",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "AccessToken",
                schema: "public",
                table: "OAuthConnections",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048);

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                schema: "public",
                table: "OAuthConnections",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048);

            migrationBuilder.AlterColumn<DateTime>(
                name: "AccessTokenExpiresAtUtc",
                schema: "public",
                table: "OAuthConnections",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OAuthConnections",
                schema: "public",
                table: "OAuthConnections",
                columns: new[] { "AccountId", "Provider" });

            migrationBuilder.AddForeignKey(
                name: "FK_OAuthConnections_Accounts_AccountId",
                schema: "public",
                table: "OAuthConnections",
                column: "AccountId",
                principalSchema: "public",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthConnections_Provider_ProviderUserId",
                schema: "public",
                table: "OAuthConnections",
                columns: new[] { "Provider", "ProviderUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM public.\"OAuthConnections\" WHERE \"Provider\" <> 'Discord';");

            migrationBuilder.DropIndex(
                name: "IX_OAuthConnections_Provider_ProviderUserId",
                schema: "public",
                table: "OAuthConnections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OAuthConnections",
                schema: "public",
                table: "OAuthConnections");

            migrationBuilder.DropForeignKey(
                name: "FK_OAuthConnections_Accounts_AccountId",
                schema: "public",
                table: "OAuthConnections");

            migrationBuilder.AlterColumn<string>(
                name: "AccessToken",
                schema: "public",
                table: "OAuthConnections",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                schema: "public",
                table: "OAuthConnections",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "AccessTokenExpiresAtUtc",
                schema: "public",
                table: "OAuthConnections",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                schema: "public",
                table: "OAuthConnections",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderUserId",
                schema: "public",
                table: "OAuthConnections",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                schema: "public",
                table: "OAuthConnections");

            migrationBuilder.DropColumn(
                name: "ProfileUrl",
                schema: "public",
                table: "OAuthConnections");

            migrationBuilder.DropColumn(
                name: "Provider",
                schema: "public",
                table: "OAuthConnections");

            migrationBuilder.RenameColumn(
                name: "ProviderUserId",
                schema: "public",
                table: "OAuthConnections",
                newName: "DiscordId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DiscordConnections",
                schema: "public",
                table: "OAuthConnections",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordConnections_DiscordId",
                schema: "public",
                table: "OAuthConnections",
                column: "DiscordId",
                unique: true);

            migrationBuilder.RenameTable(
                name: "OAuthConnections",
                schema: "public",
                newName: "DiscordConnections",
                newSchema: "public");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordConnections_Accounts_AccountId",
                schema: "public",
                table: "DiscordConnections",
                column: "AccountId",
                principalSchema: "public",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
