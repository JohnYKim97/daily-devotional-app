using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyDevotional.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixedNamingErrorInSettingsResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShowFavoriteVerseNotes",
                table: "UserSettings",
                newName: "ShowFavoriteVerseInNotes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShowFavoriteVerseInNotes",
                table: "UserSettings",
                newName: "ShowFavoriteVerseNotes");
        }
    }
}
