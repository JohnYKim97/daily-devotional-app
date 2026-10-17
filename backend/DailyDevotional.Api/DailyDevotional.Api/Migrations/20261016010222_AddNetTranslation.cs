using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyDevotional.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNetTranslation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Translations",
                columns: new[] { "Id", "Code", "CopyrightNotice", "IsEnabled", "Language", "MaxCacheAgeDays", "MaxCachedVerses", "Name", "NonCommercialOnly", "ProviderKind", "SortOrder", "StorageMode" },
                values: new object[] { 3, "NET", "Quotations designated (NET) are from the NET Bible® copyright ©1996, 2019 by Biblical Studies Press, L.L.C. All rights reserved.", false, "en", null, null, "New English Translation", true, "Local", 3, "Full" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
