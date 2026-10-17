using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyDevotional.Api.Migrations
{
    /// <inheritdoc />
    public partial class KeepOnlyNasb2020AsNasb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The NASB 1995 (Id 6) is dropped. Anything that still points at it goes first: readers
            // who chose it fall back to the ESV, and its cached text and chapter structure are removed.
            migrationBuilder.Sql("UPDATE \"UserSettings\" SET \"PreferredTranslationId\" = 1 WHERE \"PreferredTranslationId\" = 6;");
            migrationBuilder.Sql("DELETE FROM \"Verses\" WHERE \"TranslationId\" = 6;");
            migrationBuilder.Sql("DELETE FROM \"TranslationChapters\" WHERE \"TranslationId\" = 6;");
            migrationBuilder.Sql("DELETE FROM \"TranslationBooks\" WHERE \"TranslationId\" = 6;");

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.UpdateData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Code", "Name", "SortOrder" },
                values: new object[] { "NASB", "New American Standard Bible (2020)", 6 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Code", "Name", "SortOrder" },
                values: new object[] { "NASB2020", "New American Standard Bible 2020", 7 });

            migrationBuilder.InsertData(
                table: "Translations",
                columns: new[] { "Id", "AttributionLinkText", "AttributionLinkUrl", "Code", "CopyrightNotice", "ExternalId", "IsEnabled", "Language", "MaxCacheAgeDays", "MaxCachedVerses", "MaxDisplayChapters", "MaxDisplayVerses", "Name", "NonCommercialOnly", "NoticeFromProvider", "ProviderKind", "SortOrder", "StorageMode" },
                values: new object[] { 6, "Lockman.org", "https://www.lockman.org", "NASB1995", "Scripture quotations from the New American Standard Bible – NASB 1995 Copyright © 1960, 1971, 1977, 1995 by The Lockman Foundation. All rights reserved. Used by permission.", 100, false, "en", 14, 500, null, null, "New American Standard Bible 1995", true, false, "YouVersionApi", 6, "Cache" });
        }
    }
}
