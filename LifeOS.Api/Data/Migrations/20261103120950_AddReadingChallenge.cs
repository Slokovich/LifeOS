using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReadingChallenge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReadingChallenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetBooks = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingChallenges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReadingChallenges_Year",
                table: "ReadingChallenges",
                column: "Year",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReadingChallenges");
        }
    }
}
