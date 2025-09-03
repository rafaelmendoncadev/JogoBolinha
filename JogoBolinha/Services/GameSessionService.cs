using JogoBolinha.Models.Game;
using JogoBolinha.Models.User;
using JogoBolinha.Data;
using Microsoft.EntityFrameworkCore;

namespace JogoBolinha.Services
{
    public class GameSessionService
    {
        private readonly GameDbContext _context;
        private readonly ScoreCalculationService _scoreCalculationService;
        private readonly AchievementService _achievementService;

        public GameSessionService(GameDbContext context, ScoreCalculationService scoreCalculationService, AchievementService achievementService)
        {
            _context = context;
            _scoreCalculationService = scoreCalculationService;
            _achievementService = achievementService;
        }

        public async Task<GameSession> StartGameSessionAsync(int levelId, int? playerId = null)
        {
            var level = await _context.Levels.FindAsync(levelId);
            if (level == null) throw new ArgumentException("Level not found");

            var session = new GameSession
            {
                LevelId = levelId,
                Level = level,
                PlayerId = playerId,
                StartTime = DateTime.UtcNow,
                IsCompleted = false
            };

            _context.GameSessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<GameSession> CompleteGameSessionAsync(int gameStateId, bool isWon = true)
        {
            var gameState = await _context.GameStates
                .Include(gs => gs.Level)
                .Include(gs => gs.Player)
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId);

            if (gameState == null) throw new ArgumentException("Game state not found");

            // Find or create game session
            var session = await _context.GameSessions
                .FirstOrDefaultAsync(gs => gs.PlayerId == gameState.PlayerId && gs.LevelId == gameState.LevelId && !gs.IsCompleted);

            if (session == null)
            {
                session = await StartGameSessionAsync(gameState.LevelId, gameState.PlayerId);
            }

            // Update session data
            session.EndTime = DateTime.UtcNow;
            session.IsCompleted = isWon;
            session.MovesUsed = gameState.MovesCount;
            session.HintsUsed = gameState.HintsUsed;
            
            if (isWon)
            {
                // Calculate final score
                gameState.EndTime = DateTime.UtcNow;
                session.Score = _scoreCalculationService.CalculateGameScore(gameState);
                gameState.Score = session.Score;
                gameState.Status = GameStatus.Completed;
            }
            else
            {
                session.Score = 0;
                gameState.Status = GameStatus.Failed;
            }

            await _context.SaveChangesAsync();

            // Update player stats and achievements if player exists
            if (gameState.PlayerId.HasValue)
            {
                await _scoreCalculationService.UpdatePlayerStatsAsync(gameState.PlayerId.Value, session);
                await _achievementService.CheckAndUnlockAchievementsAsync(gameState.PlayerId.Value, session);
            }

            return session;
        }

        public async Task<ScoreBreakdown> GetScoreBreakdownAsync(int gameStateId)
        {
            var gameState = await _context.GameStates
                .Include(gs => gs.Level)
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId);

            if (gameState == null) return new ScoreBreakdown();

            var breakdown = new ScoreBreakdown
            {
                BaseScore = 100,
                EfficiencyBonus = CalculateEfficiencyBonus(gameState),
                SpeedBonus = CalculateSpeedBonus(gameState),
                DifficultyMultiplier = GetDifficultyMultiplier(gameState.Level.Difficulty),
                HintPenalty = gameState.HintsUsed * 20,
                PerfectGameBonus = IsPerfectGame(gameState) ? 100 : 0
            };

            breakdown.TotalScore = (int)((breakdown.BaseScore + breakdown.EfficiencyBonus + breakdown.SpeedBonus + breakdown.PerfectGameBonus) * breakdown.DifficultyMultiplier - breakdown.HintPenalty);
            breakdown.TotalScore = Math.Max(0, breakdown.TotalScore);

            return breakdown;
        }

        private int CalculateEfficiencyBonus(GameState gameState)
        {
            var minimumMoves = gameState.Level.MinimumMoves;
            var allowedExtraMoves = 10;
            var optimalMoves = minimumMoves + allowedExtraMoves;

            if (gameState.MovesCount <= optimalMoves)
            {
                var unusedMoves = optimalMoves - gameState.MovesCount;
                return unusedMoves * 10;
            }

            return 0;
        }

        private int CalculateSpeedBonus(GameState gameState)
        {
            if (!gameState.Duration.HasValue) return 0;

            var duration = gameState.Duration.Value;
            int bonus = 0;

            if (duration.TotalMinutes < 2)
                bonus += 50;
            if (duration.TotalMinutes < 1)
                bonus += 30;
            if (duration.TotalSeconds < 30)
                bonus += 20;

            return bonus;
        }

        private double GetDifficultyMultiplier(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Easy => 1.0,
                Difficulty.Medium => 1.2,
                Difficulty.Hard => 1.5,
                Difficulty.Expert => 2.0,
                _ => 1.0
            };
        }

        private bool IsPerfectGame(GameState gameState)
        {
            return gameState.MovesCount == gameState.Level.MinimumMoves;
        }

        public async Task<List<GameSession>> GetPlayerHistoryAsync(int playerId, int count = 10)
        {
            return await _context.GameSessions
                .Include(gs => gs.Level)
                .Where(gs => gs.PlayerId == playerId)
                .OrderByDescending(gs => gs.StartTime)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<GameSession>> GetLevelLeaderboardAsync(int levelId, int count = 10)
        {
            return await _context.GameSessions
                .Include(gs => gs.Player)
                .Where(gs => gs.LevelId == levelId && gs.IsCompleted)
                .OrderByDescending(gs => gs.Score)
                .ThenBy(gs => gs.MovesUsed)
                .ThenBy(gs => gs.Duration)
                .Take(count)
                .ToListAsync();
        }

        public async Task SaveGameProgressAsync(GameState gameState)
        {
            // Auto-save functionality - save current game state
            gameState.Score = (await GetScoreBreakdownAsync(gameState.Id)).TotalScore;
            await _context.SaveChangesAsync();
        }
    }

    public class ScoreBreakdown
    {
        public int BaseScore { get; set; }
        public int EfficiencyBonus { get; set; }
        public int SpeedBonus { get; set; }
        public double DifficultyMultiplier { get; set; }
        public int HintPenalty { get; set; }
        public int PerfectGameBonus { get; set; }
        public int TotalScore { get; set; }
        
        public string GetBreakdownText()
        {
            var parts = new List<string>();
            
            parts.Add($"Base: {BaseScore}");
            if (EfficiencyBonus > 0) parts.Add($"Efficiency: +{EfficiencyBonus}");
            if (SpeedBonus > 0) parts.Add($"Speed: +{SpeedBonus}");
            if (PerfectGameBonus > 0) parts.Add($"Perfect: +{PerfectGameBonus}");
            if (Math.Abs(DifficultyMultiplier - 1.0) > 0.01) parts.Add($"Difficulty: x{DifficultyMultiplier:F1}");
            if (HintPenalty > 0) parts.Add($"Hints: -{HintPenalty}");
            
            return string.Join(", ", parts);
        }
    }
}