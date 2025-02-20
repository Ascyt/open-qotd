using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomQotd.Migrations
{
    /// <inheritdoc />
    public partial class AddNoticesLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableQotdAutomaticPresets",
                table: "Configs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NoticesLevel",
                table: "Configs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "PresetSents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    PresetIndex = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresetSents", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PresetSents");

            migrationBuilder.DropColumn(
                name: "EnableQotdAutomaticPresets",
                table: "Configs");

            migrationBuilder.DropColumn(
                name: "NoticesLevel",
                table: "Configs");
        }
    }
}
