using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Poker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addpottohand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCurrentRoundFinished",
                table: "Hands");

            migrationBuilder.DropColumn(
                name: "Pot",
                table: "Hands");

            migrationBuilder.AddColumn<int>(
                name: "CurrentRoundPot",
                table: "Hands",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MainPot",
                table: "Hands",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PlayerContribution",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    HandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerContribution", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerContribution_Hands_HandId",
                        column: x => x.HandId,
                        principalTable: "Hands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SidePot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    EligiblePlayerIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SidePot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SidePot_Hands_HandId",
                        column: x => x.HandId,
                        principalTable: "Hands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerContribution_HandId",
                table: "PlayerContribution",
                column: "HandId");

            migrationBuilder.CreateIndex(
                name: "IX_SidePot_HandId",
                table: "SidePot",
                column: "HandId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerContribution");

            migrationBuilder.DropTable(
                name: "SidePot");

            migrationBuilder.DropColumn(
                name: "CurrentRoundPot",
                table: "Hands");

            migrationBuilder.DropColumn(
                name: "MainPot",
                table: "Hands");

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrentRoundFinished",
                table: "Hands",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Pot",
                table: "Hands",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
