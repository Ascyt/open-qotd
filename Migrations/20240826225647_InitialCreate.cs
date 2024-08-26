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
                    QotdTimeHourUtc = table.Column<int>(type: "INTEGER", nullable: false),
                    QotdTimeMinuteUtc = table.Column<int>(type: "INTEGER", nullable: false),
                    SuggestionsChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    SuggestionsPingRoleId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    LogsChannelId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configs", x => x.GuildId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configs");
        }
    }
}
