using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Reservations",
                schema: "reservations",
                table: "Reservations");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                schema: "reservations",
                table: "Reservations",
                type: "uuid",
                maxLength: 255,
                nullable: false,
                defaultValueSql: "uuidv7()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reservations",
                schema: "reservations",
                table: "Reservations",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_AccountId",
                schema: "reservations",
                table: "Reservations",
                column: "AccountId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Reservations",
                schema: "reservations",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_AccountId",
                schema: "reservations",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: "reservations",
                table: "Reservations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reservations",
                schema: "reservations",
                table: "Reservations",
                column: "AccountId");
        }
    }
}
