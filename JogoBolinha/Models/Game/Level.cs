using System.ComponentModel.DataAnnotations;

namespace JogoBolinha.Models.Game
{
    public enum Difficulty
    {
        Easy = 1,
        Medium = 2,
        Hard = 3,
        Expert = 4
    }
    
    public class Level
    {
        public int Id { get; set; }
        
        public int Number { get; set; }
        
        public Difficulty Difficulty { get; set; }
        
        public int Colors { get; set; } // Number of different colors
        
        public int Tubes { get; set; } // Number of tubes
        
        public int BallsPerColor { get; set; } // Number of balls per color
        
        [Required]
        public string InitialState { get; set; } = string.Empty; // JSON representation
        
        public string? SolutionMoves { get; set; } // JSON array of optimal moves
        
        public int MinimumMoves { get; set; } // Minimum moves needed to solve

        public long GenerationSeed { get; set; } // Seed used to generate the level
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();
    }
}