using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomQotd.Migrations
{
    /// <inheritdoc />
    public partial class VariablesUnavailableRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EnableQotdNotAvailableMessage",
                table: "Configs",
                newName: "EnableQotdUnavailableMessage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EnableQotdUnavailableMessage",
                table: "Configs",
                newName: "EnableQotdNotAvailableMessage");
        }
    }
}
