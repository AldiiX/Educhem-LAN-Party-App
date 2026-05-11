using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class FixReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ComputerId",
                schema: "reservations",
                table: "Reservations",
                type: "character varying(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                schema: "reservations",
                table: "Reservations",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RoomId",
                schema: "reservations",
                table: "Reservations",
                type: "character varying(255)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ComputerId",
                schema: "reservations",
                table: "Reservations",
                column: "ComputerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RoomId",
                schema: "reservations",
                table: "Reservations",
                column: "RoomId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Computers_ComputerId",
                schema: "reservations",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Rooms_RoomId",
                schema: "reservations",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_ComputerId",
                schema: "reservations",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_RoomId",
                schema: "reservations",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ComputerId",
                schema: "reservations",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                schema: "reservations",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RoomId",
                schema: "reservations",
                table: "Reservations");
        }
    }
}
