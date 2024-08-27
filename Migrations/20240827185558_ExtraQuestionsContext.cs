using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomQotd.Migrations
{
    /// <inheritdoc />
    public partial class ExtraQuestionsContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "AcceptedByUserId",
                table: "Questions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AcceptedTimestamp",
                table: "Questions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SentNumber",
                table: "Questions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentTimestamp",
                table: "Questions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedByUserId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "AcceptedTimestamp",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "SentNumber",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "SentTimestamp",
                table: "Questions");
        }
    }
}
