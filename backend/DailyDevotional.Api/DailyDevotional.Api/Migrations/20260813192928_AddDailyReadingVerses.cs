using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DailyDevotional.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyReadingVerses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Passage",
                table: "DailyReadings");

            migrationBuilder.RenameColumn(
                name: "PassageReference",
                table: "DailyReadings",
                newName: "Book");

            migrationBuilder.AddColumn<int>(
                name: "Chapter",
                table: "DailyReadings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EndVerse",
                table: "DailyReadings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StartVerse",
                table: "DailyReadings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DailyReadingVerses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DailyReadingId = table.Column<int>(type: "integer", nullable: false),
                    VerseNumber = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
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
                columns: new[] { "Id", "DailyReadingId", "Text", "VerseNumber" },
                values: new object[,]
                {
                    { 1, 1, "Placeholder text for Genesis 1:1.", 1 },
                    { 2, 1, "Placeholder text for Genesis 1:2.", 2 },
                    { 3, 1, "Placeholder text for Genesis 1:3.", 3 },
                    { 4, 1, "Placeholder text for Genesis 1:4.", 4 },
                    { 5, 1, "Placeholder text for Genesis 1:5.", 5 }
                });

            migrationBuilder.UpdateData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Book", "Chapter", "Commentary", "EndVerse", "StartVerse" },
                values: new object[] { "Genesis", 1, "God begins the creation story by bringing order and light into darkness.", 5, 1 });

            migrationBuilder.UpdateData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Book", "Chapter", "EndVerse", "StartVerse" },
                values: new object[] { "Genesis", 1, 10, 6 });

            migrationBuilder.UpdateData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Book", "Chapter", "EndVerse", "StartVerse" },
                values: new object[] { "Genesis", 1, 15, 11 });

            migrationBuilder.CreateIndex(
                name: "IX_DailyReadingVerses_DailyReadingId",
                table: "DailyReadingVerses",
                column: "DailyReadingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyReadingVerses");

            migrationBuilder.DropColumn(
                name: "Chapter",
                table: "DailyReadings");

            migrationBuilder.DropColumn(
                name: "EndVerse",
                table: "DailyReadings");

            migrationBuilder.DropColumn(
                name: "StartVerse",
                table: "DailyReadings");

            migrationBuilder.RenameColumn(
                name: "Book",
                table: "DailyReadings",
                newName: "PassageReference");

            migrationBuilder.AddColumn<string>(
                name: "Passage",
                table: "DailyReadings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Commentary", "Passage", "PassageReference" },
                values: new object[] { "God begins the story of creation by bringing order and light into darkness.", "In the beginning God created the heavens and the earth...", "Genesis 1:1-5" });

            migrationBuilder.UpdateData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Passage", "PassageReference" },
                values: new object[] { "And God said, Let there be a vault between the waters...", "Genesis 1:6-10" });

            migrationBuilder.UpdateData(
                table: "DailyReadings",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Passage", "PassageReference" },
                values: new object[] { "Then God said, Let the land produce vegetation...", "Genesis 1:11-15" });
        }
    }
}
