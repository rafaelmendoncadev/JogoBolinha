using JogoBolinha.Models.Game;

namespace JogoBolinha.Tests.Models
{
    public class GameStateTests
    {
        [Fact]
        public void GameState_InitialState_HasCorrectDefaults()
        {
            // Arrange & Act
            var gameState = new GameState();

            // Assert
            Assert.Equal(GameStatus.InProgress, gameState.Status);
            Assert.Equal(0, gameState.Score);
            Assert.Equal(0, gameState.MovesCount);
            Assert.Equal(0, gameState.HintsUsed);
            Assert.False(gameState.IsCompleted);
        }

        [Fact]
        public void GameState_WhenCompleted_IsCompletedReturnsTrue()
        {
            // Arrange
            var gameState = new GameState
            {
                Status = GameStatus.Completed
            };

            // Act & Assert
            Assert.True(gameState.IsCompleted);
        }

        [Fact]
        public void GameState_WithMoves_CanUndoReturnsTrue()
        {
            // Arrange
            var gameState = new GameState
            {
                MovesCount = 1,
                Moves = new List<GameMove>
                {
                    new GameMove { Id = 1, FromTubeId = 1, ToTubeId = 2 }
                }
            };

            // Act & Assert
            Assert.True(gameState.CanUndo);
        }

        [Fact]
        public void GameState_WithoutMoves_CanUndoReturnsFalse()
        {
            // Arrange
            var gameState = new GameState
            {
                MovesCount = 0,
                Moves = new List<GameMove>()
            };

            // Act & Assert
            Assert.False(gameState.CanUndo);
        }

        [Fact]
        public void GameState_WithEndTime_CalculatesDuration()
        {
            // Arrange
            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddMinutes(5);
            var gameState = new GameState
            {
                StartTime = startTime,
                EndTime = endTime
            };

            // Act
            var duration = gameState.Duration;

            // Assert
            Assert.NotNull(duration);
            Assert.Equal(TimeSpan.FromMinutes(5), duration);
        }

        [Fact]
        public void GameState_WithoutEndTime_DurationIsNull()
        {
            // Arrange
            var gameState = new GameState
            {
                StartTime = DateTime.UtcNow
            };

            // Act
            var duration = gameState.Duration;

            // Assert
            Assert.Null(duration);
        }

        [Fact]
        public void GameState_AllTubesCompleteOrEmpty_IsWonReturnsTrue()
        {
            // Arrange
            var completeTube = new Tube
            {
                Id = 1,
                Position = 0,
                Balls = new List<Ball>
                {
                    new Ball { Id = 1, Color = "#ff0000", Position = 0 },
                    new Ball { Id = 2, Color = "#ff0000", Position = 1 }
                }
            };

            var emptyTube = new Tube
            {
                Id = 2,
                Position = 1,
                Balls = new List<Ball>()
            };

            var gameState = new GameState
            {
                Tubes = new List<Tube> { completeTube, emptyTube }
            };

            // Act
            var result = gameState.IsWon();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GameState_IncompleteTubes_IsWonReturnsFalse()
        {
            // Arrange
            var incompleteTube = new Tube
            {
                Id = 1,
                Position = 0,
                Balls = new List<Ball>
                {
                    new Ball { Id = 1, Color = "#ff0000", Position = 0 },
                    new Ball { Id = 2, Color = "#00ff00", Position = 1 } // Mixed colors
                }
            };

            var gameState = new GameState
            {
                Tubes = new List<Tube> { incompleteTube }
            };

            // Act
            var result = gameState.IsWon();

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(GameStatus.InProgress, false)]
        [InlineData(GameStatus.Completed, true)]
        [InlineData(GameStatus.Failed, false)]
        [InlineData(GameStatus.Paused, false)]
        public void GameState_StatusCheck_IsCompletedReturnsCorrectValue(GameStatus status, bool expectedIsCompleted)
        {
            // Arrange
            var gameState = new GameState
            {
                Status = status
            };

            // Act & Assert
            Assert.Equal(expectedIsCompleted, gameState.IsCompleted);
        }
    }
}