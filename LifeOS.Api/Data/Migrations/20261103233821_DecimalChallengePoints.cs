using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DecimalChallengePoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Points",
                table: "ChallengeListItems",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "ChallengeListItems",
                type: "TEXT",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Points",
                table: "ChallengeListItems",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "ChallengeListItems",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
