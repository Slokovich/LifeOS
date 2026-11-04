using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompetitiveChallenge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OpponentId",
                table: "Challenges",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChallengeListItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChallengeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeListItems_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Readers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Readers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReaderBooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReaderId = table.Column<int>(type: "INTEGER", nullable: false),
                    GoodreadsId = table.Column<long>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AuthorLastFirst = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Pages = table.Column<int>(type: "INTEGER", nullable: true),
                    MyRating = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    DateRead = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReaderBooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReaderBooks_Readers_ReaderId",
                        column: x => x.ReaderId,
                        principalTable: "Readers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_OpponentId",
                table: "Challenges",
                column: "OpponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeListItems_ChallengeId",
                table: "ChallengeListItems",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReaderBooks_DateRead",
                table: "ReaderBooks",
                column: "DateRead");

            migrationBuilder.CreateIndex(
                name: "IX_ReaderBooks_ReaderId_GoodreadsId",
                table: "ReaderBooks",
                columns: new[] { "ReaderId", "GoodreadsId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Readers_Name",
                table: "Readers",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_Readers_OpponentId",
                table: "Challenges",
                column: "OpponentId",
                principalTable: "Readers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_Readers_OpponentId",
                table: "Challenges");

            migrationBuilder.DropTable(
                name: "ChallengeListItems");

            migrationBuilder.DropTable(
                name: "ReaderBooks");

            migrationBuilder.DropTable(
                name: "Readers");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_OpponentId",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "OpponentId",
                table: "Challenges");
        }
    }
}
