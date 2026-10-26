using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactBattleArena.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserContactVisibilityColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsContactPublic",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsContactPublic",
                table: "Users");
        }
    }
}
