using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyDevotional.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEndChapterSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Chapter",
                table: "DailyReadingVerses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EndChapter",
                table: "DailyReadings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "DailyReadingVerses",
                keyColumn: "Id",
                keyValue: 1,
                column: "Chapter",
                value: 0);

            migrationBuilder.UpdateData(
                table: "DailyReadingVerses",
                keyColumn: "Id",
                keyValue: 2,
                column: "Chapter",
                value: 0);

            migrationBuilder.UpdateData(
                table: "DailyReadingVerses",
                keyColumn: "Id",
                keyValue: 3,
                column: "Chapter",
                value: 0);

            migrationBuilder.UpdateData(
                table: "DailyReadingVerses",
                keyColumn: "Id",
                keyValue: 4,
                column: "Chapter",
                value: 0);

            migrationBuilder.UpdateData(
                table: "DailyReadingVerses",
                keyColumn: "Id",
                keyValue: 5,
                column: "Chapter",
                value: 0);

            migrationBuilder.UpdateData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 1,
                column: "EndChapter",
                value: 0);

            migrationBuilder.UpdateData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 2,
                column: "EndChapter",
                value: 0);

            migrationBuilder.UpdateData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 3,
                column: "EndChapter",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Chapter",
                table: "DailyReadingVerses");

            migrationBuilder.DropColumn(
                name: "EndChapter",
                table: "DailyReadings");
        }
    }
}
