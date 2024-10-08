using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomQotd.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configs",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BasicRoleId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    AdminRoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    QotdChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    QotdPingRoleId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    EnableAutomaticQotd = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableQotdPinMessage = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableQotdUnavailableMessage = table.Column<bool>(type: "INTEGER", nullable: false),
                    QotdTimeHourUtc = table.Column<int>(type: "INTEGER", nullable: false),
                    QotdTimeMinuteUtc = table.Column<int>(type: "INTEGER", nullable: false),
                    EnableSuggestions = table.Column<bool>(type: "INTEGER", nullable: false),
                    SuggestionsChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    SuggestionsPingRoleId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    LogsChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    LastSentDay = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentSuggestStreak = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentSuggestStreakUserId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    LastQotdMessageId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configs", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GuildDependentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    SubmittedByUserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AcceptedByUserId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    AcceptedTimestamp = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SentTimestamp = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SentNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    SuggestionMessageId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configs");

            migrationBuilder.DropTable(
                name: "Questions");
        }
    }
}
