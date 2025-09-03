using System.ComponentModel.DataAnnotations;

namespace JogoBolinha.Models.User
{
    public class Player
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLogin { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public PlayerStats? Stats { get; set; }
        
        public ICollection<Game.GameSession> GameSessions { get; set; } = new List<Game.GameSession>();
        
        public ICollection<PlayerAchievement> PlayerAchievements { get; set; } = new List<PlayerAchievement>();
        
        public ICollection<Game.GameState> GameStates { get; set; } = new List<Game.GameState>();
    }
}