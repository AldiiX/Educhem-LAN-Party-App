using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class EnrollmentClassAsString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Class",
                schema: "public",
                table: "Enrollments",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE public."Enrollments" AS enrollment
                SET "Class" = class_entity."Name"
                FROM public."Classes" AS class_entity
                WHERE class_entity."Id" = enrollment."ClassId";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Class",
                schema: "public",
                table: "Enrollments",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Classes_ClassId",
                schema: "public",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_ClassId",
                schema: "public",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ClassId",
                schema: "public",
                table: "Enrollments");

            migrationBuilder.DropTable(
                name: "Classes",
                schema: "public");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                schema: "public",
                table: "Enrollments",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                INSERT INTO public."Classes" ("Name")
                SELECT DISTINCT enrollment."Class"
                FROM public."Enrollments" AS enrollment;

                UPDATE public."Enrollments" AS enrollment
                SET "ClassId" = class_entity."Id"
                FROM public."Classes" AS class_entity
                WHERE class_entity."Name" = enrollment."Class";
                """);

            migrationBuilder.AlterColumn<int>(
                name: "ClassId",
                schema: "public",
                table: "Enrollments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Class",
                schema: "public",
                table: "Enrollments");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Classes_ClassId",
                schema: "public",
                table: "Enrollments",
                column: "ClassId",
                principalSchema: "public",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
