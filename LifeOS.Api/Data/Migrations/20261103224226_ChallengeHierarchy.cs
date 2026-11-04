using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChallengeHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorLastFirst",
                table: "Books",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TargetBooks = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_Year",
                table: "Challenges",
                column: "Year");

            // MODIFICA A MANO (CLAUDE.md §8): EF cancellava ReadingChallenges per prima cosa, perdendo
            // gli obiettivi salvati. Li copiamo come challenge "a numero" e solo dopo cancelliamo la tabella.
            migrationBuilder.Sql(
                "INSERT INTO Challenges (Name, Year, CreatedAt, Kind, TargetBooks) " +
                "SELECT TargetBooks || ' libri nel ' || Year, Year, datetime('now', 'localtime'), 'Count', TargetBooks " +
                "FROM ReadingChallenges;");

            migrationBuilder.DropTable(
                name: "ReadingChallenges");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Challenges");

            migrationBuilder.DropColumn(
                name: "AuthorLastFirst",
                table: "Books");

            migrationBuilder.CreateTable(
                name: "ReadingChallenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TargetBooks = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false)
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
    }
}
