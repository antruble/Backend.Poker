using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Poker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class added : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Winner_Players_PlayerId",
                table: "Winner");

            migrationBuilder.DropColumn(
                name: "CurrentPlayerSeat",
                table: "Hands");

            migrationBuilder.DropColumn(
                name: "LastAction",
                table: "Hands");

            migrationBuilder.DropColumn(
                name: "PlaceOfBB",
                table: "Hands");

            migrationBuilder.RenameColumn(
                name: "IsRoundFinished",
                table: "Hands",
                newName: "IsCurrentRoundFinished");

            migrationBuilder.AddColumn<int>(
                name: "BlindStatus",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsFolded",
                table: "Players",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PlayerStatus",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "FirstPlayerId",
                table: "Hands",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PivotPlayerId",
                table: "Hands",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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

            migrationBuilder.AddForeignKey(
                name: "FK_Winner_Players_PlayerId",
                table: "Winner",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hands_Players_CurrentPlayerId",
                table: "Hands");

            migrationBuilder.DropForeignKey(
                name: "FK_Winner_Players_PlayerId",
                table: "Winner");

            migrationBuilder.DropIndex(
                name: "IX_Hands_CurrentPlayerId",
                table: "Hands");

            migrationBuilder.DropColumn(
                name: "BlindStatus",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "IsFolded",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PlayerStatus",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "FirstPlayerId",
                table: "Hands");

            migrationBuilder.DropColumn(
                name: "PivotPlayerId",
                table: "Hands");

            migrationBuilder.RenameColumn(
                name: "IsCurrentRoundFinished",
                table: "Hands",
                newName: "IsRoundFinished");

            migrationBuilder.AddColumn<int>(
                name: "CurrentPlayerSeat",
                table: "Hands",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastAction",
                table: "Hands",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlaceOfBB",
                table: "Hands",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Winner_Players_PlayerId",
                table: "Winner",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
