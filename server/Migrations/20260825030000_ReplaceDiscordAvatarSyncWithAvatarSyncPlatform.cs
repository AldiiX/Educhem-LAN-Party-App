using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using server.Data;

#nullable disable

namespace server.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260825030000_ReplaceDiscordAvatarSyncWithAvatarSyncPlatform")]
public partial class ReplaceDiscordAvatarSyncWithAvatarSyncPlatform : Migration {
	protected override void Up(MigrationBuilder migrationBuilder) {
		migrationBuilder.AddColumn<string>(
			name: "AvatarSyncPlatform",
			schema: "public",
			table: "Accounts",
			type: "text",
			nullable: true);

		migrationBuilder.Sql("""
			UPDATE public."Accounts" AS account
			SET "AvatarSyncPlatform" = 'Discord'
			FROM public."DiscordConnections" AS connection
			WHERE account."Id" = connection."AccountId"
				AND connection."AvatarSync" = TRUE;
			""");

		migrationBuilder.DropColumn(
			name: "AvatarSync",
			schema: "public",
			table: "DiscordConnections");
	}

	protected override void Down(MigrationBuilder migrationBuilder) {
		migrationBuilder.AddColumn<bool>(
			name: "AvatarSync",
			schema: "public",
			table: "DiscordConnections",
			type: "boolean",
			nullable: false,
			defaultValue: false);

		migrationBuilder.Sql("""
			UPDATE public."DiscordConnections" AS connection
			SET "AvatarSync" = TRUE
			FROM public."Accounts" AS account
			WHERE account."Id" = connection."AccountId"
				AND account."AvatarSyncPlatform" = 'Discord';
			""");

		migrationBuilder.DropColumn(
			name: "AvatarSyncPlatform",
			schema: "public",
			table: "Accounts");
	}
}
