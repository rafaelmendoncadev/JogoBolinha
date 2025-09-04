using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoBolinha.Migrations
{
    /// <inheritdoc />
    public partial class AddLastModifiedToGameState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "GameStates",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "GameStates");
        }
    }
}
