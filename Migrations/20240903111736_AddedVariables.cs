using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomQotd.Migrations
{
    /// <inheritdoc />
    public partial class AddedVariables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentSuggestStreak",
                table: "Configs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "CurrentSuggestStreakUserId",
                table: "Configs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableQotdNotAvailableMessage",
                table: "Configs",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableQotdPinMessage",
                table: "Configs",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableSuggestions",
                table: "Configs",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<ulong>(
                name: "LastQotdMessageId",
                table: "Configs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastSentDay",
                table: "Configs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QotdCounter",
                table: "Configs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentSuggestStreak",
                table: "Configs");

            migrationBuilder.DropColumn(
                name: "CurrentSuggestStreakUserId",
                table: "Configs");

            migrationBuilder.DropColumn(
                name: "EnableQotdNotAvailableMessage",
                table: "Configs");

            migrationBuilder.DropColumn(
                name: "EnableQotdPinMessage",
                table: "Configs");

            migrationBuilder.DropColumn(
                name: "EnableSuggestions",
                table: "Configs");

            migrationBuilder.DropColumn(
                name: "LastQotdMessageId",
                table: "Configs");

            migrationBuilder.DropColumn(
                name: "LastSentDay",
                table: "Configs");

            migrationBuilder.DropColumn(
                name: "QotdCounter",
                table: "Configs");
        }
    }
}
