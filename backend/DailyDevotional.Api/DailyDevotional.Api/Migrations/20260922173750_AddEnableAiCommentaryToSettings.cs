using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyDevotional.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEnableAiCommentaryToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableAiCommentary",
                table: "UserSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableAiCommentary",
                table: "UserSettings");
        }
    }
}
