using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactBattleArena.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBattles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Battles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InitiatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InitiatorTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpponentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OpponentTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    TurnCount = table.Column<int>(type: "int", nullable: false),
                    IsHighlighted = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemOpponent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Battles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Battles_Teams_InitiatorTeamId",
                        column: x => x.InitiatorTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Battles_Teams_OpponentTeamId",
                        column: x => x.OpponentTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Battles_Users_InitiatorUserId",
                        column: x => x.InitiatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Battles_Users_OpponentUserId",
                        column: x => x.OpponentUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BattleEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BattleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TurnNumber = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    AttackerSide = table.Column<int>(type: "int", nullable: false),
                    AttackerSlot = table.Column<int>(type: "int", nullable: false),
                    TargetSide = table.Column<int>(type: "int", nullable: false),
                    TargetSlot = table.Column<int>(type: "int", nullable: false),
                    StrikeKind = table.Column<int>(type: "int", nullable: false),
                    HitResult = table.Column<int>(type: "int", nullable: false),
                    Damage = table.Column<int>(type: "int", nullable: false),
                    TargetRemainingHealth = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BattleEvents_Battles_BattleId",
                        column: x => x.BattleId,
                        principalTable: "Battles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BattleFighters",
                columns: table => new
                {
                    BattleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Side = table.Column<int>(type: "int", nullable: false),
                    SlotIndex = table.Column<int>(type: "int", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Universe = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rarity = table.Column<int>(type: "int", nullable: false),
                    BaseAttack = table.Column<int>(type: "int", nullable: false),
                    BaseDefense = table.Column<int>(type: "int", nullable: false),
                    BaseSpeed = table.Column<int>(type: "int", nullable: false),
                    BaseHealth = table.Column<int>(type: "int", nullable: false),
                    BaseTechnique = table.Column<int>(type: "int", nullable: false),
                    BaseAccuracy = table.Column<int>(type: "int", nullable: false),
                    BaseForesight = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleFighters", x => new { x.BattleId, x.Side, x.SlotIndex });
                    table.ForeignKey(
                        name: "FK_BattleFighters_Battles_BattleId",
                        column: x => x.BattleId,
                        principalTable: "Battles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BattleFighters_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BattleEvents_BattleId_TurnNumber_Sequence",
                table: "BattleEvents",
                columns: new[] { "BattleId", "TurnNumber", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_BattleFighters_CharacterId",
                table: "BattleFighters",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_CreatedAtUtc",
                table: "Battles",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_InitiatorTeamId",
                table: "Battles",
                column: "InitiatorTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_InitiatorUserId",
                table: "Battles",
                column: "InitiatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_IsHighlighted",
                table: "Battles",
                column: "IsHighlighted");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_OpponentTeamId",
                table: "Battles",
                column: "OpponentTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_OpponentUserId",
                table: "Battles",
                column: "OpponentUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BattleEvents");

            migrationBuilder.DropTable(
                name: "BattleFighters");

            migrationBuilder.DropTable(
                name: "Battles");
        }
    }
}
