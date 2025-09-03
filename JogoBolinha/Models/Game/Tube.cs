using System.ComponentModel.DataAnnotations;

namespace JogoBolinha.Models.Game
{
    public class Tube
    {
        public int Id { get; set; }
        
        public int Capacity { get; set; } = 4; // Default tube capacity
        
        public int Position { get; set; } // Position in the game board (0-based)
        
        public int GameStateId { get; set; }
        public GameState GameState { get; set; } = null!;
        
        public ICollection<Ball> Balls { get; set; } = new List<Ball>();
        
        public bool IsEmpty => !Balls.Any();
        public bool IsFull => Balls.Count >= Capacity;
        public bool IsComplete => Balls.Count == Capacity && Balls.All(b => b.Color == Balls.First().Color);
        
        public Ball? GetTopBall() => Balls.OrderByDescending(b => b.Position).FirstOrDefault();
        public string? GetTopBallColor() => GetTopBall()?.Color;
        
        public bool CanReceiveBall(Ball ball)
        {
            if (IsFull) return false;
            if (IsEmpty) return true;
            return GetTopBallColor() == ball.Color;
        }
    }
}