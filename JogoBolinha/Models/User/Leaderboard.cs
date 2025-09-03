using System.ComponentModel.DataAnnotations;

namespace JogoBolinha.Models.User
{
    public class Leaderboard
    {
        public int Id { get; set; }
        
        public int PlayerId { get; set; }
        public Player Player { get; set; } = null!;
        
        public int TotalScore { get; set; } = 0;
        
        public int WeeklyScore { get; set; } = 0;
        
        public int GlobalRank { get; set; } = 0;
        
        public int WeeklyRank { get; set; } = 0;
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        public DateTime WeeklyResetDate { get; set; } = DateTime.UtcNow;
    }
}