using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoBolinha.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredValue = table.Column<int>(type: "INTEGER", nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Levels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    Difficulty = table.Column<int>(type: "INTEGER", nullable: false),
                    Colors = table.Column<int>(type: "INTEGER", nullable: false),
                    Tubes = table.Column<int>(type: "INTEGER", nullable: false),
                    BallsPerColor = table.Column<int>(type: "INTEGER", nullable: false),
                    InitialState = table.Column<string>(type: "TEXT", nullable: false),
                    SolutionMoves = table.Column<string>(type: "TEXT", nullable: true),
                    MinimumMoves = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Levels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    LevelId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    MovesUsed = table.Column<int>(type: "INTEGER", nullable: false),
                    HintsUsed = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameSessions_Levels_LevelId",
                        column: x => x.LevelId,
                        principalTable: "Levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameSessions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GameStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LevelId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    MovesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HintsUsed = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Score = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameStates_Levels_LevelId",
                        column: x => x.LevelId,
                        principalTable: "Levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameStates_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Leaderboards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalScore = table.Column<int>(type: "INTEGER", nullable: false),
                    WeeklyScore = table.Column<int>(type: "INTEGER", nullable: false),
                    GlobalRank = table.Column<int>(type: "INTEGER", nullable: false),
                    WeeklyRank = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WeeklyResetDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaderboards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leaderboards_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerAchievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    AchievementId = table.Column<int>(type: "INTEGER", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CurrentProgress = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerAchievements_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerAchievements_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalScore = table.Column<int>(type: "INTEGER", nullable: false),
                    WeeklyScore = table.Column<int>(type: "INTEGER", nullable: false),
                    LevelsCompleted = table.Column<int>(type: "INTEGER", nullable: false),
                    HighestLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalGamesPlayed = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalMovesUsed = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalHintsUsed = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTimePlayed = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    PerfectGames = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WeeklyScoreResetDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerStats_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameMoves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameStateId = table.Column<int>(type: "INTEGER", nullable: false),
                    FromTubeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ToTubeId = table.Column<int>(type: "INTEGER", nullable: false),
                    BallColor = table.Column<string>(type: "TEXT", nullable: false),
                    MoveNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsUndone = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameMoves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameMoves_GameStates_GameStateId",
                        column: x => x.GameStateId,
                        principalTable: "GameStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tubes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    GameStateId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tubes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tubes_GameStates_GameStateId",
                        column: x => x.GameStateId,
                        principalTable: "GameStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Balls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    TubeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    GameStateId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Balls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Balls_GameStates_GameStateId",
                        column: x => x.GameStateId,
                        principalTable: "GameStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Balls_Tubes_TubeId",
                        column: x => x.TubeId,
                        principalTable: "Tubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Balls_GameStateId",
                table: "Balls",
                column: "GameStateId");

            migrationBuilder.CreateIndex(
                name: "IX_Balls_TubeId",
                table: "Balls",
                column: "TubeId");

            migrationBuilder.CreateIndex(
                name: "IX_GameMoves_GameStateId",
                table: "GameMoves",
                column: "GameStateId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_LevelId",
                table: "GameSessions",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_PlayerId_LevelId",
                table: "GameSessions",
                columns: new[] { "PlayerId", "LevelId" });

            migrationBuilder.CreateIndex(
                name: "IX_GameStates_LevelId",
                table: "GameStates",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_GameStates_PlayerId",
                table: "GameStates",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboards_PlayerId",
                table: "Leaderboards",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboards_TotalScore",
                table: "Leaderboards",
                column: "TotalScore");

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboards_WeeklyScore",
                table: "Leaderboards",
                column: "WeeklyScore");

            migrationBuilder.CreateIndex(
                name: "IX_Levels_Number",
                table: "Levels",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAchievements_AchievementId",
                table: "PlayerAchievements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAchievements_PlayerId",
                table: "PlayerAchievements",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_Email",
                table: "Players",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_Username",
                table: "Players",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStats_PlayerId",
                table: "PlayerStats",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tubes_GameStateId",
                table: "Tubes",
                column: "GameStateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Balls");

            migrationBuilder.DropTable(
                name: "GameMoves");

            migrationBuilder.DropTable(
                name: "GameSessions");

            migrationBuilder.DropTable(
                name: "Leaderboards");

            migrationBuilder.DropTable(
                name: "PlayerAchievements");

            migrationBuilder.DropTable(
                name: "PlayerStats");

            migrationBuilder.DropTable(
                name: "Tubes");

            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropTable(
                name: "GameStates");

            migrationBuilder.DropTable(
                name: "Levels");

            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
