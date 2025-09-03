using Microsoft.EntityFrameworkCore;
using JogoBolinha.Models.Game;
using JogoBolinha.Models.User;

namespace JogoBolinha.Data
{
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
        {
        }
        
        // Game entities
        public DbSet<GameState> GameStates { get; set; }
        public DbSet<Level> Levels { get; set; }
        public DbSet<Tube> Tubes { get; set; }
        public DbSet<Ball> Balls { get; set; }
        public DbSet<GameMove> GameMoves { get; set; }
        public DbSet<GameSession> GameSessions { get; set; }
        
        // User entities
        public DbSet<Player> Players { get; set; }
        public DbSet<PlayerStats> PlayerStats { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<PlayerAchievement> PlayerAchievements { get; set; }
        public DbSet<Leaderboard> Leaderboards { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure relationships
            
            // Player -> PlayerStats (One-to-One)
            modelBuilder.Entity<Player>()
                .HasOne(p => p.Stats)
                .WithOne(s => s.Player)
                .HasForeignKey<PlayerStats>(s => s.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // GameState -> Level (Many-to-One)
            modelBuilder.Entity<GameState>()
                .HasOne(gs => gs.Level)
                .WithMany()
                .HasForeignKey(gs => gs.LevelId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // GameState -> Player (Many-to-One, Optional)
            modelBuilder.Entity<GameState>()
                .HasOne(gs => gs.Player)
                .WithMany(p => p.GameStates)
                .HasForeignKey(gs => gs.PlayerId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // GameState -> Tubes (One-to-Many)
            modelBuilder.Entity<Tube>()
                .HasOne(t => t.GameState)
                .WithMany(gs => gs.Tubes)
                .HasForeignKey(t => t.GameStateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // GameState -> Balls (One-to-Many)
            modelBuilder.Entity<Ball>()
                .HasOne(b => b.GameState)
                .WithMany(gs => gs.Balls)
                .HasForeignKey(b => b.GameStateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Tube -> Balls (One-to-Many)
            modelBuilder.Entity<Ball>()
                .HasOne(b => b.Tube)
                .WithMany(t => t.Balls)
                .HasForeignKey(b => b.TubeId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // GameState -> GameMoves (One-to-Many)
            modelBuilder.Entity<GameMove>()
                .HasOne(gm => gm.GameState)
                .WithMany(gs => gs.Moves)
                .HasForeignKey(gm => gm.GameStateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // GameSession relationships
            modelBuilder.Entity<GameSession>()
                .HasOne(gs => gs.Player)
                .WithMany(p => p.GameSessions)
                .HasForeignKey(gs => gs.PlayerId)
                .OnDelete(DeleteBehavior.SetNull);
            
            modelBuilder.Entity<GameSession>()
                .HasOne(gs => gs.Level)
                .WithMany(l => l.GameSessions)
                .HasForeignKey(gs => gs.LevelId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Achievement relationships
            modelBuilder.Entity<PlayerAchievement>()
                .HasOne(pa => pa.Player)
                .WithMany(p => p.PlayerAchievements)
                .HasForeignKey(pa => pa.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<PlayerAchievement>()
                .HasOne(pa => pa.Achievement)
                .WithMany(a => a.PlayerAchievements)
                .HasForeignKey(pa => pa.AchievementId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Leaderboard relationships
            modelBuilder.Entity<Leaderboard>()
                .HasOne(lb => lb.Player)
                .WithOne()
                .HasForeignKey<Leaderboard>(lb => lb.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Configure indexes for better performance
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.Username)
                .IsUnique();
            
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.Email)
                .IsUnique();
            
            modelBuilder.Entity<Level>()
                .HasIndex(l => l.Number)
                .IsUnique();
            
            modelBuilder.Entity<GameSession>()
                .HasIndex(gs => new { gs.PlayerId, gs.LevelId });
            
            modelBuilder.Entity<Leaderboard>()
                .HasIndex(lb => lb.TotalScore);
            
            modelBuilder.Entity<Leaderboard>()
                .HasIndex(lb => lb.WeeklyScore);
        }
    }
}