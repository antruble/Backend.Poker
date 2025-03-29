using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Poker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addedasd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hands_Players_CurrentPlayerId",
                table: "Hands");

            migrationBuilder.DropIndex(
                name: "IX_Hands_CurrentPlayerId",
                table: "Hands");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Hands_CurrentPlayerId",
                table: "Hands",
                column: "CurrentPlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Hands_Players_CurrentPlayerId",
                table: "Hands",
                column: "CurrentPlayerId",
                principalTable: "Players",
                principalColumn: "Id");
        }
    }
}
