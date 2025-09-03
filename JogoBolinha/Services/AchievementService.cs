using JogoBolinha.Models.User;
using JogoBolinha.Models.Game;
using JogoBolinha.Data;
using Microsoft.EntityFrameworkCore;

namespace JogoBolinha.Services
{
    public class AchievementService
    {
        private readonly GameDbContext _context;
        
        public AchievementService(GameDbContext context)
        {
            _context = context;
        }
        
        public async Task CheckAndUnlockAchievementsAsync(int playerId, GameSession gameSession)
        {
            var playerStats = await _context.PlayerStats
                .FirstOrDefaultAsync(ps => ps.PlayerId == playerId);
            
            if (playerStats == null) return;
            
            var achievements = await _context.Achievements.Where(a => a.IsActive).ToListAsync();
            
            foreach (var achievement in achievements)
            {
                await CheckSpecificAchievementAsync(playerId, achievement, playerStats, gameSession);
            }
        }
        
        private async Task CheckSpecificAchievementAsync(int playerId, Achievement achievement, PlayerStats stats, GameSession gameSession)
        {
            var existingPlayerAchievement = await _context.PlayerAchievements
                .FirstOrDefaultAsync(pa => pa.PlayerId == playerId && pa.AchievementId == achievement.Id);
            
            if (existingPlayerAchievement != null && existingPlayerAchievement.IsUnlocked)
                return; // Already unlocked
            
            var currentProgress = CalculateAchievementProgress(achievement, stats, gameSession);
            
            if (existingPlayerAchievement == null)
            {
                existingPlayerAchievement = new PlayerAchievement
                {
                    PlayerId = playerId,
                    AchievementId = achievement.Id,
                    CurrentProgress = currentProgress
                };
                _context.PlayerAchievements.Add(existingPlayerAchievement);
            }
            else
            {
                existingPlayerAchievement.CurrentProgress = currentProgress;
            }
            
            // Check if achievement is now unlocked
            if (currentProgress >= achievement.RequiredValue && !existingPlayerAchievement.IsUnlocked)
            {
                existingPlayerAchievement.UnlockedAt = DateTime.UtcNow;
                
                // Award points to player stats (if you want to track achievement points)
                // This could be used for a separate achievement score system
            }
            
            await _context.SaveChangesAsync();
        }
        
        private int CalculateAchievementProgress(Achievement achievement, PlayerStats stats, GameSession gameSession)
        {
            return achievement.Type switch
            {
                AchievementType.LevelsCompleted => stats.LevelsCompleted,
                AchievementType.PerfectGames => stats.PerfectGames,
                AchievementType.SpeedRun => CalculateSpeedRunProgress(gameSession),
                AchievementType.Efficiency => CalculateEfficiencyProgress(stats),
                AchievementType.Dedication => CalculateDedicationProgress(stats),
                AchievementType.Social => CalculateSocialProgress(stats),
                _ => 0
            };
        }
        
        private int CalculateSpeedRunProgress(GameSession gameSession)
        {
            // Count games completed in under 1 minute (for speed achievements)
            if (gameSession.IsCompleted && gameSession.Duration.HasValue && gameSession.Duration.Value.TotalMinutes < 1)
            {
                return 1; // This would be accumulated across multiple sessions
            }
            return 0;
        }
        
        private int CalculateEfficiencyProgress(PlayerStats stats)
        {
            // Calculate efficiency based on average moves per game
            if (stats.TotalGamesPlayed == 0) return 0;
            
            var efficiency = stats.AverageMovesPerGame;
            if (efficiency <= 15) return 1; // Very efficient
            if (efficiency <= 20) return 1; // Moderately efficient
            return 0;
        }
        
        private int CalculateDedicationProgress(PlayerStats stats)
        {
            // Count total hours played
            return (int)stats.TotalTimePlayed.TotalHours;
        }
        
        private int CalculateSocialProgress(PlayerStats stats)
        {
            // This could be used for future social features
            // For now, return based on total score (community contribution)
            return stats.TotalScore / 1000; // 1 point per 1000 total score
        }
        
        public async Task InitializeDefaultAchievementsAsync()
        {
            var existingAchievements = await _context.Achievements.AnyAsync();
            if (existingAchievements) return;
            
            var defaultAchievements = new List<Achievement>
            {
                // Levels Completed Achievements
                new Achievement
                {
                    Name = "Primeiro Passo",
                    Description = "Complete seu primeiro nível",
                    Type = AchievementType.LevelsCompleted,
                    RequiredValue = 1,
                    Icon = "🎯",
                    Points = 10
                },
                new Achievement
                {
                    Name = "Em Progresso",
                    Description = "Complete 10 níveis",
                    Type = AchievementType.LevelsCompleted,
                    RequiredValue = 10,
                    Icon = "🏃",
                    Points = 25
                },
                new Achievement
                {
                    Name = "Experiente",
                    Description = "Complete 25 níveis",
                    Type = AchievementType.LevelsCompleted,
                    RequiredValue = 25,
                    Icon = "🎖️",
                    Points = 50
                },
                new Achievement
                {
                    Name = "Mestre dos Tubos",
                    Description = "Complete 50 níveis",
                    Type = AchievementType.LevelsCompleted,
                    RequiredValue = 50,
                    Icon = "👑",
                    Points = 100
                },
                
                // Perfect Games Achievements
                new Achievement
                {
                    Name = "Perfeição",
                    Description = "Complete um nível com movimentos mínimos",
                    Type = AchievementType.PerfectGames,
                    RequiredValue = 1,
                    Icon = "⭐",
                    Points = 20
                },
                new Achievement
                {
                    Name = "Estrategista",
                    Description = "Complete 5 níveis com movimentos mínimos",
                    Type = AchievementType.PerfectGames,
                    RequiredValue = 5,
                    Icon = "🧠",
                    Points = 50
                },
                new Achievement
                {
                    Name = "Grande Mestre",
                    Description = "Complete 10 níveis com movimentos mínimos",
                    Type = AchievementType.PerfectGames,
                    RequiredValue = 10,
                    Icon = "💎",
                    Points = 100
                },
                
                // Speed Run Achievements
                new Achievement
                {
                    Name = "Velocista",
                    Description = "Complete um nível em menos de 1 minuto",
                    Type = AchievementType.SpeedRun,
                    RequiredValue = 1,
                    Icon = "⚡",
                    Points = 30
                },
                new Achievement
                {
                    Name = "Raio",
                    Description = "Complete 5 níveis em menos de 1 minuto",
                    Type = AchievementType.SpeedRun,
                    RequiredValue = 5,
                    Icon = "🔥",
                    Points = 75
                },
                
                // Dedication Achievements
                new Achievement
                {
                    Name = "Dedicado",
                    Description = "Jogue por 1 hora total",
                    Type = AchievementType.Dedication,
                    RequiredValue = 1,
                    Icon = "⏰",
                    Points = 15
                },
                new Achievement
                {
                    Name = "Viciado",
                    Description = "Jogue por 10 horas totais",
                    Type = AchievementType.Dedication,
                    RequiredValue = 10,
                    Icon = "🎮",
                    Points = 50
                },
                
                // Efficiency Achievements
                new Achievement
                {
                    Name = "Eficiente",
                    Description = "Mantenha média de movimentos baixa",
                    Type = AchievementType.Efficiency,
                    RequiredValue = 1,
                    Icon = "📊",
                    Points = 40
                },
                
                // Social Achievements
                new Achievement
                {
                    Name = "Competitivo",
                    Description = "Alcance 1000 pontos totais",
                    Type = AchievementType.Social,
                    RequiredValue = 1,
                    Icon = "🏆",
                    Points = 25
                },
                new Achievement
                {
                    Name = "Lenda",
                    Description = "Alcance 10000 pontos totais",
                    Type = AchievementType.Social,
                    RequiredValue = 10,
                    Icon = "🌟",
                    Points = 100
                },
                new Achievement
                {
                    Name = "Imortal",
                    Description = "Alcance 50000 pontos totais",
                    Type = AchievementType.Social,
                    RequiredValue = 50,
                    Icon = "🔱",
                    Points = 200
                }
            };
            
            _context.Achievements.AddRange(defaultAchievements);
            await _context.SaveChangesAsync();
        }
        
        public async Task<List<PlayerAchievement>> GetPlayerAchievementsAsync(int playerId)
        {
            return await _context.PlayerAchievements
                .Include(pa => pa.Achievement)
                .Where(pa => pa.PlayerId == playerId)
                .OrderBy(pa => pa.Achievement.Type)
                .ThenBy(pa => pa.Achievement.RequiredValue)
                .ToListAsync();
        }
        
        public async Task<List<Achievement>> GetAvailableAchievementsAsync()
        {
            return await _context.Achievements
                .Where(a => a.IsActive)
                .OrderBy(a => a.Type)
                .ThenBy(a => a.RequiredValue)
                .ToListAsync();
        }
    }
}