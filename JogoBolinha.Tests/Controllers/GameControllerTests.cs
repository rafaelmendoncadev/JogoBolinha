using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JogoBolinha.Controllers;
using JogoBolinha.Data;
using JogoBolinha.Services;
using JogoBolinha.Models.Game;
using System.Reflection;

namespace JogoBolinha.Tests.Controllers
{
    public class GameControllerTests : IDisposable
    {
        private readonly GameDbContext _context;
        private readonly GameController _controller;
        private readonly LevelGeneratorServiceV2 _levelGenerator;

        public GameControllerTests()
        {
            var options = new DbContextOptionsBuilder<GameDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new GameDbContext(options);
            _levelGenerator = new LevelGeneratorServiceV2();
            var scoreCalculationService = new ScoreCalculationService(_context);
            var gameLogicService = new GameLogicService(_context);
            var gameStateManager = new GameStateManager(_context, scoreCalculationService);
            var achievementService = new AchievementService(_context);
            var hintService = new HintService(_context, gameLogicService);
            var gameSessionService = new GameSessionService(_context, scoreCalculationService, achievementService);

            _controller = new GameController(
                _context,
                gameLogicService,
                _levelGenerator,
                scoreCalculationService,
                gameSessionService,
                hintService,
                gameStateManager
            );
        }

        [Fact]
        public void IsCompactFormat_ShouldDetectCompactFormat()
        {
            // Arrange
            var compactFormat = "T1=0,1,2,0;T2=1,2,0,1;T3=2,0,1,2;T4=;T5=";
            var jsonFormat = "{\"Tubes\":[{\"Id\":0,\"Balls\":[{\"Color\":\"#FF6B6B\",\"Position\":0}]}]}";

            // Act & Assert using reflection to access private method
            var method = typeof(GameController).GetMethod("IsCompactFormat", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            bool isCompact1 = (bool)method!.Invoke(_controller, new object[] { compactFormat })!;
            bool isCompact2 = (bool)method!.Invoke(_controller, new object[] { jsonFormat })!;

            Assert.True(isCompact1);
            Assert.False(isCompact2);
        }

        [Fact]
        public async Task CreateNewGameStateAsync_WithCompactFormat_ShouldWork()
        {
            // Arrange
            // Gerar manualmente um estado compacto simples equivalente ao nível 1
            var level = new Level
            {
                Number = 1,
                Difficulty = Difficulty.Easy,
                Colors = 2,
                Tubes = 4,
                BallsPerColor = 2,
                InitialState = "T1=0,1;T2=1,0;T3=;T4=",
                MinimumMoves = 2,
                GenerationSeed = 123
            };
            _context.Levels.Add(level);
            await _context.SaveChangesAsync();

            // Act - Access private method using reflection
            var method = typeof(GameController).GetMethod("CreateNewGameStateAsync", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            var task = (Task<GameState?>)method!.Invoke(_controller, new object[] { level, null })!;
            var result = await task;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(level.Id, result.LevelId);
            Assert.True(result.Tubes.Count > 0);
        }

        [Fact]
        public async Task CreateNewGameStateAsync_WithJsonFormat_ShouldWork()
        {
            // Arrange - Create a level with JSON format
            var jsonLevel = new Level
            {
                Number = 1,
                Difficulty = Difficulty.Easy,
                Colors = 2,
                Tubes = 3,
                BallsPerColor = 2,
                InitialState = "{\"Tubes\":[{\"Id\":0,\"Balls\":[{\"Color\":\"#FF6B6B\",\"Position\":0},{\"Color\":\"#4ECDC4\",\"Position\":1}]},{\"Id\":1,\"Balls\":[{\"Color\":\"#4ECDC4\",\"Position\":0},{\"Color\":\"#FF6B6B\",\"Position\":1}]},{\"Id\":2,\"Balls\":[]}]}",
                MinimumMoves = 4,
                GenerationSeed = 12345
            };

            _context.Levels.Add(jsonLevel);
            await _context.SaveChangesAsync();

            // Act - Access private method using reflection
            var method = typeof(GameController).GetMethod("CreateNewGameStateAsync", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            var task = (Task<GameState?>)method!.Invoke(_controller, new object[] { jsonLevel, null })!;
            var result = await task;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(jsonLevel.Id, result.LevelId);
            Assert.Equal(3, result.Tubes.Count);
            
            // Verify tubes were parsed correctly
            var tube1 = result.Tubes.First(t => t.Position == 0);
            var tube3 = result.Tubes.First(t => t.Position == 2);
            
            Assert.Equal(2, tube1.Balls.Count);
            Assert.Equal(0, tube3.Balls.Count); // Empty tube
        }

        [Fact]
        public async Task ParseFormats_ShouldProduceSameResult()
        {
            // Arrange - Create levels with equivalent data in both formats
            var compactLevel = new Level
            {
                Number = 1,
                Difficulty = Difficulty.Easy,
                Colors = 2,
                Tubes = 3,
                BallsPerColor = 2,
                InitialState = "T1=0,1;T2=1,0;T3=",
                MinimumMoves = 4,
                GenerationSeed = 12345
            };

            var jsonLevel = new Level
            {
                Number = 2,
                Difficulty = Difficulty.Easy,
                Colors = 2,
                Tubes = 3,
                BallsPerColor = 2,
                InitialState = "{\"Tubes\":[{\"Id\":0,\"Balls\":[{\"Color\":\"#FF6B6B\",\"Position\":0},{\"Color\":\"#4ECDC4\",\"Position\":1}]},{\"Id\":1,\"Balls\":[{\"Color\":\"#4ECDC4\",\"Position\":0},{\"Color\":\"#FF6B6B\",\"Position\":1}]},{\"Id\":2,\"Balls\":[]}]}",
                MinimumMoves = 4,
                GenerationSeed = 12345
            };

            _context.Levels.AddRange(compactLevel, jsonLevel);
            await _context.SaveChangesAsync();

            // Act
            var method = typeof(GameController).GetMethod("CreateNewGameStateAsync", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            var task1 = (Task<GameState?>)method!.Invoke(_controller, new object[] { compactLevel, null })!;
            var result1 = await task1;
            
            var task2 = (Task<GameState?>)method!.Invoke(_controller, new object[] { jsonLevel, null })!;
            var result2 = await task2;

            // Assert both formats produce similar structures
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.Tubes.Count, result2.Tubes.Count);
            
            // Both should have same number of non-empty tubes
            var nonEmptyTubes1 = result1.Tubes.Count(t => t.Balls.Count > 0);
            var nonEmptyTubes2 = result2.Tubes.Count(t => t.Balls.Count > 0);
            Assert.Equal(nonEmptyTubes1, nonEmptyTubes2);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
