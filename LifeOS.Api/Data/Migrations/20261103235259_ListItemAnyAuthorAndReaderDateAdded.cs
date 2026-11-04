using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ListItemAnyAuthorAndReaderDateAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DateAdded",
                table: "ReaderBooks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AnyBookByAuthor",
                table: "ChallengeListItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateAdded",
                table: "ReaderBooks");

            migrationBuilder.DropColumn(
                name: "AnyBookByAuthor",
                table: "ChallengeListItems");
        }
    }
}
