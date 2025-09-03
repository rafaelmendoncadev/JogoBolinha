using JogoBolinha.Models.Game;
using JogoBolinha.Models.User;
using JogoBolinha.Data;
using Microsoft.EntityFrameworkCore;

namespace JogoBolinha.Services
{
    public class ScoreCalculationService
    {
        private readonly GameDbContext _context;
        
        public ScoreCalculationService(GameDbContext context)
        {
            _context = context;
        }
        
        public int CalculateGameScore(GameState gameState)
        {
            const int baseScore = 100;
            const int efficiencyBonus = 10;
            const int speedBonusPoints = 50;
            const int hintPenalty = 20;
            
            int score = baseScore;
            
            // Efficiency bonus for unused moves
            var minimumMoves = gameState.Level.MinimumMoves;
            var allowedExtraMoves = 10; // Allow some extra moves without penalty
            var optimalMoves = minimumMoves + allowedExtraMoves;
            
            if (gameState.MovesCount <= optimalMoves)
            {
                var unusedMoves = optimalMoves - gameState.MovesCount;
                score += unusedMoves * efficiencyBonus;
            }
            
            // Speed bonus for completing in less than 2 minutes
            if (gameState.Duration.HasValue && gameState.Duration.Value.TotalMinutes < 2)
            {
                score += speedBonusPoints;
            }
            
            // Additional speed bonuses for very fast completion
            if (gameState.Duration.HasValue)
            {
                if (gameState.Duration.Value.TotalMinutes < 1)
                {
                    score += 30; // Extra bonus for sub-1-minute
                }
                if (gameState.Duration.Value.TotalSeconds < 30)
                {
                    score += 20; // Extra bonus for sub-30-second
                }
            }
            
            // Difficulty multiplier
            var difficultyMultiplier = gameState.Level.Difficulty switch
            {
                Difficulty.Easy => 1.0,
                Difficulty.Medium => 1.2,
                Difficulty.Hard => 1.5,
                Difficulty.Expert => 2.0,
                _ => 1.0
            };
            
            score = (int)(score * difficultyMultiplier);
            
            // Hint penalties
            score -= gameState.HintsUsed * hintPenalty;
            
            // Perfect game bonus (minimum moves)
            if (gameState.MovesCount == minimumMoves)
            {
                score += 100; // Perfect game bonus
            }
            
            return Math.Max(0, score); // Ensure score is never negative
        }
        
        public async Task UpdatePlayerStatsAsync(int playerId, GameSession gameSession)
        {
            var player = await _context.Players
                .Include(p => p.Stats)
                .FirstOrDefaultAsync(p => p.Id == playerId);
            
            if (player == null) return;
            
            // Initialize stats if they don't exist
            if (player.Stats == null)
            {
                player.Stats = new PlayerStats { PlayerId = playerId };
                _context.PlayerStats.Add(player.Stats);
            }
            
            var stats = player.Stats;
            
            // Update basic stats
            stats.TotalGamesPlayed++;
            
            if (gameSession.IsCompleted)
            {
                stats.LevelsCompleted++;
                stats.TotalScore += gameSession.Score;
                stats.WeeklyScore += gameSession.Score;
                
                // Update highest level
                if (gameSession.Level.Number > stats.HighestLevel)
                {
                    stats.HighestLevel = gameSession.Level.Number;
                }
                
                // Check for perfect game
                if (gameSession.MovesUsed == gameSession.Level.MinimumMoves)
                {
                    stats.PerfectGames++;
                }
            }
            
            stats.TotalMovesUsed += gameSession.MovesUsed;
            stats.TotalHintsUsed += gameSession.HintsUsed;
            
            if (gameSession.Duration.HasValue)
            {
                stats.TotalTimePlayed = stats.TotalTimePlayed.Add(gameSession.Duration.Value);
            }
            
            stats.LastUpdated = DateTime.UtcNow;
            
            // Reset weekly score if needed (every Monday)
            if (ShouldResetWeeklyScore(stats.WeeklyScoreResetDate))
            {
                stats.WeeklyScore = gameSession.IsCompleted ? gameSession.Score : 0;
                stats.WeeklyScoreResetDate = GetNextMondayDate();
            }
            
            await _context.SaveChangesAsync();
            
            // Update leaderboard
            await UpdateLeaderboardAsync(playerId);
        }
        
        public async Task UpdateLeaderboardAsync(int playerId)
        {
            var playerStats = await _context.PlayerStats
                .Include(ps => ps.Player)
                .FirstOrDefaultAsync(ps => ps.PlayerId == playerId);
            
            if (playerStats == null) return;
            
            var leaderboard = await _context.Leaderboards
                .FirstOrDefaultAsync(lb => lb.PlayerId == playerId);
            
            if (leaderboard == null)
            {
                leaderboard = new Leaderboard
                {
                    PlayerId = playerId,
                    Player = playerStats.Player
                };
                _context.Leaderboards.Add(leaderboard);
            }
            
            leaderboard.TotalScore = playerStats.TotalScore;
            leaderboard.WeeklyScore = playerStats.WeeklyScore;
            leaderboard.LastUpdated = DateTime.UtcNow;
            
            if (ShouldResetWeeklyScore(leaderboard.WeeklyResetDate))
            {
                leaderboard.WeeklyScore = 0;
                leaderboard.WeeklyResetDate = GetNextMondayDate();
            }
            
            await _context.SaveChangesAsync();
            
            // Recalculate ranks
            await RecalculateLeaderboardRanksAsync();
        }
        
        private async Task RecalculateLeaderboardRanksAsync()
        {
            // Update global ranks
            var globalRankings = await _context.Leaderboards
                .OrderByDescending(lb => lb.TotalScore)
                .ToListAsync();
            
            for (int i = 0; i < globalRankings.Count; i++)
            {
                globalRankings[i].GlobalRank = i + 1;
            }
            
            // Update weekly ranks
            var weeklyRankings = await _context.Leaderboards
                .OrderByDescending(lb => lb.WeeklyScore)
                .ToListAsync();
            
            for (int i = 0; i < weeklyRankings.Count; i++)
            {
                weeklyRankings[i].WeeklyRank = i + 1;
            }
            
            await _context.SaveChangesAsync();
        }
        
        private bool ShouldResetWeeklyScore(DateTime lastResetDate)
        {
            var now = DateTime.UtcNow;
            var daysSinceReset = (now - lastResetDate).TotalDays;
            var daysSinceMonday = ((int)now.DayOfWeek - 1 + 7) % 7;
            
            return daysSinceReset >= 7 || (daysSinceReset > daysSinceMonday && daysSinceMonday == 0);
        }
        
        private DateTime GetNextMondayDate()
        {
            var now = DateTime.UtcNow;
            var daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0 && now.DayOfWeek == DayOfWeek.Monday)
            {
                daysUntilMonday = 7; // Next Monday if today is Monday
            }
            
            return now.AddDays(daysUntilMonday).Date;
        }
        
        public async Task<List<Leaderboard>> GetTopPlayersAsync(int count = 100, bool weekly = false)
        {
            var query = _context.Leaderboards.Include(lb => lb.Player);
            
            if (weekly)
            {
                return await query
                    .OrderByDescending(lb => lb.WeeklyScore)
                    .Take(count)
                    .ToListAsync();
            }
            else
            {
                return await query
                    .OrderByDescending(lb => lb.TotalScore)
                    .Take(count)
                    .ToListAsync();
            }
        }
    }
}