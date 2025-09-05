using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoBolinha.Migrations
{
    /// <inheritdoc />
    public partial class AddGenerationSeedToLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "GenerationSeed",
                table: "Levels",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenerationSeed",
                table: "Levels");
        }
    }
}
