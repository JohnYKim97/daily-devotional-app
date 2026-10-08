using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyDevotional.Api.Migrations
{
    /// <inheritdoc />
    public partial class RestrictBookAndTranslationDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deleting a book or translation must not silently delete readings or user settings.
            migrationBuilder.DropForeignKey(
                name: "FK_DailyReadings_Books_BookId",
                table: "DailyReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSettings_Translations_PreferredTranslationId",
                table: "UserSettings");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyReadings_Books_BookId",
                table: "DailyReadings",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSettings_Translations_PreferredTranslationId",
                table: "UserSettings",
                column: "PreferredTranslationId",
                principalTable: "Translations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyReadings_Books_BookId",
                table: "DailyReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSettings_Translations_PreferredTranslationId",
                table: "UserSettings");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyReadings_Books_BookId",
                table: "DailyReadings",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSettings_Translations_PreferredTranslationId",
                table: "UserSettings",
                column: "PreferredTranslationId",
                principalTable: "Translations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
