using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Poker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removeWinners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Winner_Hands_HandId",
                table: "Winner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Winner_Hands_HandId",
                table: "Winner",
                column: "HandId",
                principalTable: "Hands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
