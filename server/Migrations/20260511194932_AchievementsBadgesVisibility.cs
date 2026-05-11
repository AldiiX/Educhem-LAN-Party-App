using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AchievementsBadgesVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Achievements",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
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
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
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
                name: "AccountAchievements",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
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
                        principalSchema: "public",
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountBadges",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
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
                        principalSchema: "public",
                        principalTable: "Badges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BadgeAchievements",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    BadgeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BadgeAchievements_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalSchema: "public",
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BadgeAchievements_Badges_BadgeId",
                        column: x => x.BadgeId,
                        principalSchema: "public",
                        principalTable: "Badges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountAchievements_AccountId_AchievementId",
                schema: "public",
                table: "AccountAchievements",
                columns: new[] { "AccountId", "AchievementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountAchievements_AchievementId",
                schema: "public",
                table: "AccountAchievements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountBadges_AccountId_BadgeId",
                schema: "public",
                table: "AccountBadges",
                columns: new[] { "AccountId", "BadgeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountBadges_BadgeId",
                schema: "public",
                table: "AccountBadges",
                column: "BadgeId");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeAchievements_AchievementId",
                schema: "public",
                table: "BadgeAchievements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeAchievements_BadgeId_AchievementId",
                schema: "public",
                table: "BadgeAchievements",
                columns: new[] { "BadgeId", "AchievementId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountAchievements",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AccountBadges",
                schema: "public");

            migrationBuilder.DropTable(
                name: "BadgeAchievements",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Achievements",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Badges",
                schema: "public");
        }
    }
}
