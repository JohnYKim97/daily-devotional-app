using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyDevotional.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNltTranslation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Translations",
                columns: new[] { "Id", "Code", "CopyrightNotice", "IsEnabled", "Language", "MaxCacheAgeDays", "MaxCachedVerses", "Name", "NonCommercialOnly", "ProviderKind", "SortOrder", "StorageMode" },
                values: new object[] { 4, "NLT", "Scripture quotations are taken from the Holy Bible, New Living Translation, copyright ©1996, 2004, 2015 by Tyndale House Foundation. Used by permission of Tyndale House Publishers, Carol Stream, Illinois 60188. All rights reserved.", true, "en", 14, 500, "New Living Translation", true, "NltApi", 4, "Cache" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
