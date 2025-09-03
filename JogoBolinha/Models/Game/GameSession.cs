using System.ComponentModel.DataAnnotations;

namespace JogoBolinha.Models.Game
{
    public class GameSession
    {
        public int Id { get; set; }
        
        public int? PlayerId { get; set; }
        public User.Player? Player { get; set; }
        
        public int LevelId { get; set; }
        public Level Level { get; set; } = null!;
        
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        
        public DateTime? EndTime { get; set; }
        
        public int Score { get; set; } = 0;
        
        public int MovesUsed { get; set; } = 0;
        
        public int HintsUsed { get; set; } = 0;
        
        public bool IsCompleted { get; set; } = false;
        
        public TimeSpan? Duration => EndTime?.Subtract(StartTime);
    }
}