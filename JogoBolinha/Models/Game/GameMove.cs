using System.ComponentModel.DataAnnotations;

namespace JogoBolinha.Models.Game
{
    public class GameMove
    {
        public int Id { get; set; }
        
        public int GameStateId { get; set; }
        public GameState GameState { get; set; } = null!;
        
        public int FromTubeId { get; set; }
        public int ToTubeId { get; set; }
        
        public string BallColor { get; set; } = string.Empty;
        
        public int MoveNumber { get; set; } // Sequential move number in the game
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public bool IsUndone { get; set; } = false;
    }
}