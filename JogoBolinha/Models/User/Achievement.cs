using System.ComponentModel.DataAnnotations;

namespace JogoBolinha.Models.User
{
    public enum AchievementType
    {
        LevelsCompleted = 1,
        PerfectGames = 2,
        SpeedRun = 3,
        Efficiency = 4,
        Dedication = 5,
        Social = 6
    }
    
    public class Achievement
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public AchievementType Type { get; set; }
        
        public int RequiredValue { get; set; } // e.g., 10 levels, 5 perfect games
        
        [StringLength(100)]
        public string Icon { get; set; } = string.Empty;
        
        public int Points { get; set; } = 10; // Points awarded for achievement
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public ICollection<PlayerAchievement> PlayerAchievements { get; set; } = new List<PlayerAchievement>();
    }
    
    public class PlayerAchievement
    {
        public int Id { get; set; }
        
        public int PlayerId { get; set; }
        public Player Player { get; set; } = null!;
        
        public int AchievementId { get; set; }
        public Achievement Achievement { get; set; } = null!;
        
        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
        
        public int CurrentProgress { get; set; } = 0;
        
        public bool IsUnlocked => CurrentProgress >= Achievement.RequiredValue;
    }
}