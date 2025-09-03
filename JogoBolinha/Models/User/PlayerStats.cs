using System.ComponentModel.DataAnnotations;

namespace JogoBolinha.Models.User
{
    public class PlayerStats
    {
        public int Id { get; set; }
        
        public int PlayerId { get; set; }
        public Player Player { get; set; } = null!;
        
        public int TotalScore { get; set; } = 0;
        
        public int WeeklyScore { get; set; } = 0;
        
        public int LevelsCompleted { get; set; } = 0;
        
        public int HighestLevel { get; set; } = 0;
        
        public int TotalGamesPlayed { get; set; } = 0;
        
        public int TotalMovesUsed { get; set; } = 0;
        
        public int TotalHintsUsed { get; set; } = 0;
        
        public TimeSpan TotalTimePlayed { get; set; } = TimeSpan.Zero;
        
        public int PerfectGames { get; set; } = 0; // Games completed with minimum moves
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        public DateTime WeeklyScoreResetDate { get; set; } = DateTime.UtcNow;
        
        public double AverageMovesPerGame => TotalGamesPlayed > 0 ? (double)TotalMovesUsed / TotalGamesPlayed : 0;
        
        public double CompletionRate => TotalGamesPlayed > 0 ? (double)LevelsCompleted / TotalGamesPlayed * 100 : 0;
    }
}