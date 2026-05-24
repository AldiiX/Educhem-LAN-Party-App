using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "attendance");

            migrationBuilder.CreateTable(
                name: "AttendanceEntries",
                schema: "attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", maxLength: 255, nullable: false, defaultValueSql: "uuidv7()"),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceEntries_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceEntries_Accounts_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEntries_AccountId_CreatedAtUtc",
                schema: "attendance",
                table: "AttendanceEntries",
                columns: new[] { "AccountId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEntries_CreatedById",
                schema: "attendance",
                table: "AttendanceEntries",
                column: "CreatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceEntries",
                schema: "attendance");
        }
    }
}
