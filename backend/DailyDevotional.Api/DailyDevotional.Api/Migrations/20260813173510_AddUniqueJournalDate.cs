using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyDevotional.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueJournalDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Journals_Date",
                table: "Journals",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Journals_Date",
                table: "Journals");
        }
    }
}
