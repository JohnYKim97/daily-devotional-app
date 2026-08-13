using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DailyDevotional.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedDailyReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DailyReadings",
                columns: new[] { "Id", "Commentary", "Date", "Passage", "PassageReference" },
                values: new object[,]
                {
                    { 1, "God begins the story of creation by bringing order and light into darkness.", new DateOnly(2026, 8, 11), "In the beginning God created the heavens and the earth...", "Genesis 1:1-5" },
                    { 2, "God continues bringing structure and distinction to creation.", new DateOnly(2026, 8, 12), "And God said, Let there be a vault between the waters...", "Genesis 1:6-10" },
                    { 3, "Creation continues as God brings forth life and establishes the rhythms of the world.", new DateOnly(2026, 8, 13), "Then God said, Let the land produce vegetation...", "Genesis 1:11-15" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
