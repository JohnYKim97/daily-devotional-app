using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyDevotional.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVerseHeadingsAndFootnotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "Footnotes",
                table: "Verses",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::text[]");

            // Cached rows of license-restricted translations were fetched without headings or
            // footnotes. Dropping them makes the next request refetch the full text.
            migrationBuilder.Sql(
                @"DELETE FROM ""Verses"" WHERE ""TranslationId"" IN (SELECT ""Id"" FROM ""Translations"" WHERE ""StorageMode"" = 'Cache');");

            migrationBuilder.AddColumn<string>(
                name: "Heading",
                table: "Verses",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Footnotes",
                table: "Verses");

            migrationBuilder.DropColumn(
                name: "Heading",
                table: "Verses");
        }
    }
}
