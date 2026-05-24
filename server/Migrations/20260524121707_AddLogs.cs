using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "administration");

            migrationBuilder.CreateTable(
                name: "AppSettings",
                schema: "administration",
                columns: table => new
                {
                    Property = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Property);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", maxLength: 255, nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "text", nullable: false, defaultValue: "Info"),
                    ExactType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "basic"),
                    Message = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings",
                schema: "administration");

            migrationBuilder.DropTable(
                name: "Logs",
                schema: "administration");
        }
    }
}
