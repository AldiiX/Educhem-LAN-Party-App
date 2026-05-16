using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using server.Data.Entities;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "achievements");

            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.EnsureSchema(
                name: "reservations");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:public.AccountGender", "Female,Male,Other")
                .Annotation("Npgsql:Enum:public.AccountType", "Admin,Student,SuperAdmin,Teacher,TeacherOrg");

            migrationBuilder.CreateTable(
                name: "Achievements",
                schema: "achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", maxLength: 255, nullable: false, defaultValueSql: "uuidv7()"),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IconUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Badges",
                schema: "achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", maxLength: 255, nullable: false, defaultValueSql: "uuidv7()"),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IconUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Badges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                schema: "reservations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Available = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schools",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", maxLength: 255, nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BadgeRequirements",
                schema: "achievements",
                columns: table => new
                {
                    BadgeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeRequirements", x => new { x.BadgeId, x.AchievementId });
                    table.ForeignKey(
                        name: "FK_BadgeRequirements_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalSchema: "achievements",
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BadgeRequirements_Badges_BadgeId",
                        column: x => x.BadgeId,
                        principalSchema: "achievements",
                        principalTable: "Badges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Computers",
                schema: "reservations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RoomId = table.Column<string>(type: "character varying(255)", nullable: true),
                    Available = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsTeachersComputer = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Computers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Computers_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalSchema: "reservations",
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", maxLength: 255, nullable: false, defaultValueSql: "uuidv7()"),
                    FirstName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Email = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Class = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Gender = table.Column<Gender>(type: "public.\"AccountGender\"", nullable: true),
                    SchoolId = table.Column<int>(type: "integer", nullable: true),
                    LastActiveUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    AccountType = table.Column<AccountType>(type: "public.\"AccountType\"", nullable: false, defaultValue: AccountType.Student),
                    AvatarUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    BannerUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    EnableReservations = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalSchema: "public",
                        principalTable: "Schools",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccountAchievements",
                schema: "achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", maxLength: 255, nullable: false, defaultValueSql: "uuidv7()"),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountAchievements_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountAchievements_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalSchema: "achievements",
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountBadges",
                schema: "achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", maxLength: 255, nullable: false, defaultValueSql: "uuidv7()"),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsTakenOut = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountBadges_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountBadges_Badges_BadgeId",
                        column: x => x.BadgeId,
                        principalSchema: "achievements",
                        principalTable: "Badges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "Reservations",
                schema: "reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", maxLength: 255, nullable: false, defaultValueSql: "uuidv7()"),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    Discriminator = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    ComputerId = table.Column<string>(type: "character varying(255)", nullable: true),
                    RoomId = table.Column<string>(type: "character varying(255)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "public",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reservations_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalSchema: "reservations",
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reservations_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalSchema: "reservations",
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountAchievements_AccountId_AchievementId",
                schema: "achievements",
                table: "AccountAchievements",
                columns: new[] { "AccountId", "AchievementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountAchievements_AchievementId",
                schema: "achievements",
                table: "AccountAchievements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountBadges_AccountId_BadgeId",
                schema: "achievements",
                table: "AccountBadges",
                columns: new[] { "AccountId", "BadgeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountBadges_BadgeId",
                schema: "achievements",
                table: "AccountBadges",
                column: "BadgeId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Email",
                schema: "public",
                table: "Accounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_SchoolId",
                schema: "public",
                table: "Accounts",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeRequirements_AchievementId",
                schema: "achievements",
                table: "BadgeRequirements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_Computers_RoomId",
                schema: "reservations",
                table: "Computers",
                column: "RoomId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_AccountId",
                schema: "reservations",
                table: "Reservations",
                column: "AccountId",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Schools_Slug",
                schema: "public",
                table: "Schools",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountAchievements",
                schema: "achievements");

            migrationBuilder.DropTable(
                name: "AccountBadges",
                schema: "achievements");

            migrationBuilder.DropTable(
                name: "BadgeRequirements",
                schema: "achievements");

            migrationBuilder.DropTable(
                name: "ProblemReports",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Reservations",
                schema: "reservations");

            migrationBuilder.DropTable(
                name: "Achievements",
                schema: "achievements");

            migrationBuilder.DropTable(
                name: "Badges",
                schema: "achievements");

            migrationBuilder.DropTable(
                name: "Accounts",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Computers",
                schema: "reservations");

            migrationBuilder.DropTable(
                name: "Schools",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Rooms",
                schema: "reservations");
        }
    }
}
