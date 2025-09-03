using System.ComponentModel.DataAnnotations;

namespace JogoBolinha.Models.Game
{
    public class Ball
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(7)] // For hex color codes like #FF0000
        public string Color { get; set; } = string.Empty;
        
        public int TubeId { get; set; }
        public Tube Tube { get; set; } = null!;
        
        public int Position { get; set; } // Position in the tube (0 = bottom)
        
        public int GameStateId { get; set; }
        public GameState GameState { get; set; } = null!;
    }
}