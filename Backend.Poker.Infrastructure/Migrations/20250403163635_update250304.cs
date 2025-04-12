using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Poker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class update250304 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsFolded",
                table: "Players",
                newName: "HasToRevealCards");

            migrationBuilder.AddColumn<bool>(
                name: "SkipActions",
                table: "Hands",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkipActions",
                table: "Hands");

            migrationBuilder.RenameColumn(
                name: "HasToRevealCards",
                table: "Players",
                newName: "IsFolded");
        }
    }
}
