using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DailyDevotional.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTranslationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyReadingVerses");

            migrationBuilder.AddColumn<int>(
                name: "PreferredTranslationId",
                table: "UserSettings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "BookId",
                table: "DailyReadings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Testament = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Translations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    StorageMode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ProviderKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MaxCachedVerses = table.Column<int>(type: "integer", nullable: true),
                    MaxCacheAgeDays = table.Column<int>(type: "integer", nullable: true),
                    CopyrightNotice = table.Column<string>(type: "text", nullable: false),
                    NonCommercialOnly = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Translations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TranslationBooks",
                columns: table => new
                {
                    TranslationId = table.Column<int>(type: "integer", nullable: false),
                    BookId = table.Column<int>(type: "integer", nullable: false),
                    ChapterCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranslationBooks", x => new { x.TranslationId, x.BookId });
                    table.ForeignKey(
                        name: "FK_TranslationBooks_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TranslationBooks_Translations_TranslationId",
                        column: x => x.TranslationId,
                        principalTable: "Translations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TranslationChapters",
                columns: table => new
                {
                    TranslationId = table.Column<int>(type: "integer", nullable: false),
                    BookId = table.Column<int>(type: "integer", nullable: false),
                    Chapter = table.Column<int>(type: "integer", nullable: false),
                    VerseCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranslationChapters", x => new { x.TranslationId, x.BookId, x.Chapter });
                    table.ForeignKey(
                        name: "FK_TranslationChapters_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TranslationChapters_Translations_TranslationId",
                        column: x => x.TranslationId,
                        principalTable: "Translations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Verses",
                columns: table => new
                {
                    TranslationId = table.Column<int>(type: "integer", nullable: false),
                    BookId = table.Column<int>(type: "integer", nullable: false),
                    Chapter = table.Column<int>(type: "integer", nullable: false),
                    VerseNumber = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Verses", x => new { x.TranslationId, x.BookId, x.Chapter, x.VerseNumber });
                    table.ForeignKey(
                        name: "FK_Verses_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Verses_Translations_TranslationId",
                        column: x => x.TranslationId,
                        principalTable: "Translations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Books",
                columns: new[] { "Id", "Code", "Name", "Testament" },
                values: new object[,]
                {
                    { 1, "GEN", "Genesis", "OT" },
                    { 2, "EXO", "Exodus", "OT" },
                    { 3, "LEV", "Leviticus", "OT" },
                    { 4, "NUM", "Numbers", "OT" },
                    { 5, "DEU", "Deuteronomy", "OT" },
                    { 6, "JOS", "Joshua", "OT" },
                    { 7, "JDG", "Judges", "OT" },
                    { 8, "RUT", "Ruth", "OT" },
                    { 9, "1SA", "1 Samuel", "OT" },
                    { 10, "2SA", "2 Samuel", "OT" },
                    { 11, "1KI", "1 Kings", "OT" },
                    { 12, "2KI", "2 Kings", "OT" },
                    { 13, "1CH", "1 Chronicles", "OT" },
                    { 14, "2CH", "2 Chronicles", "OT" },
                    { 15, "EZR", "Ezra", "OT" },
                    { 16, "NEH", "Nehemiah", "OT" },
                    { 17, "EST", "Esther", "OT" },
                    { 18, "JOB", "Job", "OT" },
                    { 19, "PSA", "Psalms", "OT" },
                    { 20, "PRO", "Proverbs", "OT" },
                    { 21, "ECC", "Ecclesiastes", "OT" },
                    { 22, "SNG", "Song of Songs", "OT" },
                    { 23, "ISA", "Isaiah", "OT" },
                    { 24, "JER", "Jeremiah", "OT" },
                    { 25, "LAM", "Lamentations", "OT" },
                    { 26, "EZK", "Ezekiel", "OT" },
                    { 27, "DAN", "Daniel", "OT" },
                    { 28, "HOS", "Hosea", "OT" },
                    { 29, "JOL", "Joel", "OT" },
                    { 30, "AMO", "Amos", "OT" },
                    { 31, "OBA", "Obadiah", "OT" },
                    { 32, "JON", "Jonah", "OT" },
                    { 33, "MIC", "Micah", "OT" },
                    { 34, "NAM", "Nahum", "OT" },
                    { 35, "HAB", "Habakkuk", "OT" },
                    { 36, "ZEP", "Zephaniah", "OT" },
                    { 37, "HAG", "Haggai", "OT" },
                    { 38, "ZEC", "Zechariah", "OT" },
                    { 39, "MAL", "Malachi", "OT" },
                    { 40, "MAT", "Matthew", "NT" },
                    { 41, "MRK", "Mark", "NT" },
                    { 42, "LUK", "Luke", "NT" },
                    { 43, "JHN", "John", "NT" },
                    { 44, "ACT", "Acts", "NT" },
                    { 45, "ROM", "Romans", "NT" },
                    { 46, "1CO", "1 Corinthians", "NT" },
                    { 47, "2CO", "2 Corinthians", "NT" },
                    { 48, "GAL", "Galatians", "NT" },
                    { 49, "EPH", "Ephesians", "NT" },
                    { 50, "PHP", "Philippians", "NT" },
                    { 51, "COL", "Colossians", "NT" },
                    { 52, "1TH", "1 Thessalonians", "NT" },
                    { 53, "2TH", "2 Thessalonians", "NT" },
                    { 54, "1TI", "1 Timothy", "NT" },
                    { 55, "2TI", "2 Timothy", "NT" },
                    { 56, "TIT", "Titus", "NT" },
                    { 57, "PHM", "Philemon", "NT" },
                    { 58, "HEB", "Hebrews", "NT" },
                    { 59, "JAS", "James", "NT" },
                    { 60, "1PE", "1 Peter", "NT" },
                    { 61, "2PE", "2 Peter", "NT" },
                    { 62, "1JN", "1 John", "NT" },
                    { 63, "2JN", "2 John", "NT" },
                    { 64, "3JN", "3 John", "NT" },
                    { 65, "JUD", "Jude", "NT" },
                    { 66, "REV", "Revelation", "NT" }
                });

            migrationBuilder.Sql(
                @"UPDATE ""DailyReadings"" AS dr
                  SET ""BookId"" = b.""Id""
                  FROM ""Books"" AS b
                  WHERE lower(b.""Name"") = lower(dr.""Book"");");

            migrationBuilder.DropColumn(
                name: "Book",
                table: "DailyReadings");

            migrationBuilder.UpdateData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 1,
                column: "BookId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 2,
                column: "BookId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 3,
                column: "BookId",
                value: 1);

            migrationBuilder.InsertData(
                table: "Translations",
                columns: new[] { "Id", "Code", "CopyrightNotice", "IsEnabled", "Language", "MaxCacheAgeDays", "MaxCachedVerses", "Name", "NonCommercialOnly", "ProviderKind", "SortOrder", "StorageMode" },
                values: new object[,]
                {
                    { 1, "ESV", "Scripture quotations are from the ESV® Bible (The Holy Bible, English Standard Version®), © 2001 by Crossway, a publishing ministry of Good News Publishers. Used by permission. All rights reserved.", true, "en", 14, 500, "English Standard Version", true, "EsvApi", 1, "Cache" },
                    { 2, "KJV", "King James Version (public domain in the United States).", false, "en", null, null, "King James Version", false, "Local", 2, "Full" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_PreferredTranslationId",
                table: "UserSettings",
                column: "PreferredTranslationId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReadings_BookId",
                table: "DailyReadings",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Code",
                table: "Books",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_Name",
                table: "Books",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TranslationBooks_BookId",
                table: "TranslationBooks",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_TranslationChapters_BookId",
                table: "TranslationChapters",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_Translations_Code",
                table: "Translations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Verses_BookId",
                table: "Verses",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_Verses_TranslationId_FetchedAt",
                table: "Verses",
                columns: new[] { "TranslationId", "FetchedAt" });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyReadings_Books_BookId",
                table: "DailyReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSettings_Translations_PreferredTranslationId",
                table: "UserSettings");

            migrationBuilder.DropTable(
                name: "TranslationBooks");

            migrationBuilder.DropTable(
                name: "TranslationChapters");

            migrationBuilder.DropTable(
                name: "Verses");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropTable(
                name: "Translations");

            migrationBuilder.DropIndex(
                name: "IX_UserSettings_PreferredTranslationId",
                table: "UserSettings");

            migrationBuilder.DropIndex(
                name: "IX_DailyReadings_BookId",
                table: "DailyReadings");

            migrationBuilder.DropColumn(
                name: "PreferredTranslationId",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "BookId",
                table: "DailyReadings");

            migrationBuilder.AddColumn<string>(
                name: "Book",
                table: "DailyReadings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DailyReadingVerses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DailyReadingId = table.Column<int>(type: "integer", nullable: false),
                    Chapter = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    VerseNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyReadingVerses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyReadingVerses_DailyReadings_DailyReadingId",
                        column: x => x.DailyReadingId,
                        principalTable: "DailyReadings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DailyReadingVerses",
                columns: new[] { "Id", "Chapter", "DailyReadingId", "Text", "VerseNumber" },
                values: new object[,]
                {
                    { 1, 0, 1, "Placeholder text for Genesis 1:1.", 1 },
                    { 2, 0, 1, "Placeholder text for Genesis 1:2.", 2 },
                    { 3, 0, 1, "Placeholder text for Genesis 1:3.", 3 },
                    { 4, 0, 1, "Placeholder text for Genesis 1:4.", 4 },
                    { 5, 0, 1, "Placeholder text for Genesis 1:5.", 5 }
                });

            migrationBuilder.UpdateData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 1,
                column: "Book",
                value: "Genesis");

            migrationBuilder.UpdateData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 2,
                column: "Book",
                value: "Genesis");

            migrationBuilder.UpdateData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 3,
                column: "Book",
                value: "Genesis");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReadingVerses_DailyReadingId",
                table: "DailyReadingVerses",
                column: "DailyReadingId");
        }
    }
}
