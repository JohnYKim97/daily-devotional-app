using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DailyDevotional.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddYouVersionTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttributionLinkText",
                table: "Translations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttributionLinkUrl",
                table: "Translations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExternalId",
                table: "Translations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxDisplayChapters",
                table: "Translations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxDisplayVerses",
                table: "Translations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NoticeFromProvider",
                table: "Translations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AttributionLinkText", "AttributionLinkUrl", "ExternalId", "MaxDisplayChapters", "MaxDisplayVerses", "NoticeFromProvider" },
                values: new object[] { null, null, null, null, null, false });

            migrationBuilder.UpdateData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AttributionLinkText", "AttributionLinkUrl", "ExternalId", "MaxDisplayChapters", "MaxDisplayVerses", "NoticeFromProvider" },
                values: new object[] { null, null, null, null, null, false });

            migrationBuilder.UpdateData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AttributionLinkText", "AttributionLinkUrl", "ExternalId", "MaxDisplayChapters", "MaxDisplayVerses", "NoticeFromProvider" },
                values: new object[] { "NET", "https://netbible.org", null, null, null, false });

            migrationBuilder.UpdateData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AttributionLinkText", "AttributionLinkUrl", "ExternalId", "MaxDisplayChapters", "MaxDisplayVerses", "NoticeFromProvider" },
                values: new object[] { null, null, null, null, null, false });

            migrationBuilder.InsertData(
                table: "Translations",
                columns: new[] { "Id", "AttributionLinkText", "AttributionLinkUrl", "Code", "CopyrightNotice", "ExternalId", "IsEnabled", "Language", "MaxCacheAgeDays", "MaxCachedVerses", "MaxDisplayChapters", "MaxDisplayVerses", "Name", "NonCommercialOnly", "NoticeFromProvider", "ProviderKind", "SortOrder", "StorageMode" },
                values: new object[,]
                {
                    { 5, "Biblica", "https://www.biblica.com", "NIV", "The Holy Bible, New International Version® NIV®\nCopyright © 1973, 1978, 1984, 2011 by Biblica, Inc.®\nUsed by Permission of Biblica, Inc.® All rights reserved worldwide.", 111, false, "en", 14, 500, 2, 25, "New International Version", true, true, "YouVersionApi", 5, "Cache" },
                    { 6, "Lockman.org", "https://www.lockman.org", "NASB1995", "Scripture quotations from the New American Standard Bible – NASB 1995 Copyright © 1960, 1971, 1977, 1995 by The Lockman Foundation. All rights reserved. Used by permission.", 100, false, "en", 14, 500, null, null, "New American Standard Bible 1995", true, false, "YouVersionApi", 6, "Cache" },
                    { 7, "Lockman.org", "https://www.lockman.org", "NASB2020", "Scripture quotations from the New American Standard Bible – NASB 2020 Copyright © 1960, 1971, 1977, 1995, 2020 by The Lockman Foundation. All rights reserved. Used by permission.", 2692, false, "en", 14, 500, null, null, "New American Standard Bible 2020", true, false, "YouVersionApi", 7, "Cache" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DropColumn(
                name: "AttributionLinkText",
                table: "Translations");

            migrationBuilder.DropColumn(
                name: "AttributionLinkUrl",
                table: "Translations");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Translations");

            migrationBuilder.DropColumn(
                name: "MaxDisplayChapters",
                table: "Translations");

            migrationBuilder.DropColumn(
                name: "MaxDisplayVerses",
                table: "Translations");

            migrationBuilder.DropColumn(
                name: "NoticeFromProvider",
                table: "Translations");
        }
    }
}
