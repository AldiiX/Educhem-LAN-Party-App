using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceAndLocationToAuthSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Browser",
                schema: "public",
                table: "AuthSessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "public",
                table: "AuthSessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "public",
                table: "AuthSessions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                schema: "public",
                table: "AuthSessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                schema: "public",
                table: "AuthSessions",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActiveUtc",
                schema: "public",
                table: "AuthSessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "OperatingSystem",
                schema: "public",
                table: "AuthSessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                schema: "public",
                table: "AuthSessions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Browser",
                schema: "public",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "public",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "public",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                schema: "public",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                schema: "public",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "LastActiveUtc",
                schema: "public",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                schema: "public",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                schema: "public",
                table: "AuthSessions");
        }
    }
}
