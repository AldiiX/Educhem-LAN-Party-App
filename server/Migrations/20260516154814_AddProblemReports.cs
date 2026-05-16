using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddProblemReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Computers_Rooms_RoomId",
                schema: "reservations",
                table: "Computers");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Computers_ComputerId",
                schema: "reservations",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Rooms_RoomId",
                schema: "reservations",
                table: "Reservations");

            migrationBuilder.CreateTable(
                name: "ProblemReports",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", maxLength: 255, nullable: false, defaultValueSql: "uuidv7()"),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Contact = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ResolutionNote = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProblemReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProblemReports_Accounts_ReporterId",
                        column: x => x.ReporterId,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProblemReports_Accounts_ResolvedById",
                        column: x => x.ResolvedById,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProblemReports_ReporterId",
                schema: "public",
                table: "ProblemReports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_ProblemReports_ResolvedById",
                schema: "public",
                table: "ProblemReports",
                column: "ResolvedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Computers_Rooms_RoomId",
                schema: "reservations",
                table: "Computers",
                column: "RoomId",
                principalSchema: "reservations",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Computers_ComputerId",
                schema: "reservations",
                table: "Reservations",
                column: "ComputerId",
                principalSchema: "reservations",
                principalTable: "Computers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Rooms_RoomId",
                schema: "reservations",
                table: "Reservations",
                column: "RoomId",
                principalSchema: "reservations",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Computers_Rooms_RoomId",
                schema: "reservations",
                table: "Computers");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Computers_ComputerId",
                schema: "reservations",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Rooms_RoomId",
                schema: "reservations",
                table: "Reservations");

            migrationBuilder.DropTable(
                name: "ProblemReports",
                schema: "public");

            migrationBuilder.AddForeignKey(
                name: "FK_Computers_Rooms_RoomId",
                schema: "reservations",
                table: "Computers",
                column: "RoomId",
                principalSchema: "reservations",
                principalTable: "Rooms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Computers_ComputerId",
                schema: "reservations",
                table: "Reservations",
                column: "ComputerId",
                principalSchema: "reservations",
                principalTable: "Computers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Rooms_RoomId",
                schema: "reservations",
                table: "Reservations",
                column: "RoomId",
                principalSchema: "reservations",
                principalTable: "Rooms",
                principalColumn: "Id");
        }
    }
}
