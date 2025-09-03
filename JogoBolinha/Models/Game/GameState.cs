using System.ComponentModel.DataAnnotations;

namespace JogoBolinha.Models.Game
{
    public enum GameStatus
    {
        InProgress = 0,
        Completed = 1,
        Failed = 2,
        Paused = 3
    }
    
    public class GameState
    {
        public int Id { get; set; }
        
        public int LevelId { get; set; }
        public Level Level { get; set; } = null!;
        
        public int? PlayerId { get; set; }
        public User.Player? Player { get; set; }
        
        public GameStatus Status { get; set; } = GameStatus.InProgress;
        
        public int MovesCount { get; set; } = 0;
        
        public int HintsUsed { get; set; } = 0;
        
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        
        public DateTime? EndTime { get; set; }
        
        public int Score { get; set; } = 0;
        
        public ICollection<Tube> Tubes { get; set; } = new List<Tube>();
        
        public ICollection<Ball> Balls { get; set; } = new List<Ball>();
        
        public ICollection<GameMove> Moves { get; set; } = new List<GameMove>();
        
        public TimeSpan? Duration => EndTime?.Subtract(StartTime);
        
        public bool IsCompleted => Status == GameStatus.Completed;
        
        public bool CanUndo => Moves.Any() && MovesCount > 0;
        
        public bool IsWon()
        {
            return Tubes.All(tube => tube.IsEmpty || tube.IsComplete);
        }
    }
}