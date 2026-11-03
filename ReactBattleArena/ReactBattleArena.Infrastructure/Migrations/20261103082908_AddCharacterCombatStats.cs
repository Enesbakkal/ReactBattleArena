using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactBattleArena.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterCombatStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BaseAccuracy",
                table: "Characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BaseForesight",
                table: "Characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BaseHealth",
                table: "Characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BaseTechnique",
                table: "Characters",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseAccuracy",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "BaseForesight",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "BaseHealth",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "BaseTechnique",
                table: "Characters");
        }
    }
}
