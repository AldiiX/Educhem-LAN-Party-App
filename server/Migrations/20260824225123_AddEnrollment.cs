using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Schools_SchoolId",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_SchoolId",
                schema: "public",
                table: "Accounts");

            migrationBuilder.CreateTable(
                name: "Classes",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", maxLength: 255, nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                schema: "public",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<int>(type: "integer", nullable: false),
                    ClassId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_Enrollments_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Enrollments_Classes_ClassId",
                        column: x => x.ClassId,
                        principalSchema: "public",
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalSchema: "public",
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Classes_Name",
                schema: "public",
                table: "Classes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ClassId",
                schema: "public",
                table: "Enrollments",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_SchoolId",
                schema: "public",
                table: "Enrollments",
                column: "SchoolId");

            migrationBuilder.Sql(
                """
                INSERT INTO public."Classes" ("Name")
                SELECT DISTINCT btrim(account."Class")
                FROM public."Accounts" AS account
                WHERE account."SchoolId" IS NOT NULL
                  AND NULLIF(btrim(account."Class"), '') IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO public."Enrollments" ("AccountId", "SchoolId", "ClassId")
                SELECT account."Id", account."SchoolId", class_entity."Id"
                FROM public."Accounts" AS account
                INNER JOIN public."Classes" AS class_entity
                    ON class_entity."Name" = btrim(account."Class")
                WHERE account."SchoolId" IS NOT NULL
                  AND NULLIF(btrim(account."Class"), '') IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "Class",
                schema: "public",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                schema: "public",
                table: "Accounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Class",
                schema: "public",
                table: "Accounts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SchoolId",
                schema: "public",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE public."Accounts" AS account
                SET "Class" = class_entity."Name",
                    "SchoolId" = enrollment."SchoolId"
                FROM public."Enrollments" AS enrollment
                INNER JOIN public."Classes" AS class_entity
                    ON class_entity."Id" = enrollment."ClassId"
                WHERE account."Id" = enrollment."AccountId";
                """);

            migrationBuilder.DropTable(
                name: "Enrollments",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Classes",
                schema: "public");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_SchoolId",
                schema: "public",
                table: "Accounts",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Schools_SchoolId",
                schema: "public",
                table: "Accounts",
                column: "SchoolId",
                principalSchema: "public",
                principalTable: "Schools",
                principalColumn: "Id");
        }
    }
}
