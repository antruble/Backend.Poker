using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Poker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removeasd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPlayerId",
                table: "Hands");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentPlayerId",
                table: "Hands",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
