using JogoBolinha.Models.Game;

namespace JogoBolinha.Tests.Models
{
    public class TubeTests
    {
        [Fact]
        public void Tube_WhenEmpty_IsEmptyReturnsTrue()
        {
            // Arrange
            var tube = new Tube
            {
                Id = 1,
                Position = 0,
                Balls = new List<Ball>()
            };

            // Act & Assert
            Assert.True(tube.IsEmpty);
        }

        [Fact]
        public void Tube_WithBalls_IsEmptyReturnsFalse()
        {
            // Arrange
            var tube = new Tube
            {
                Id = 1,
                Position = 0,
                Balls = new List<Ball>
                {
                    new Ball { Id = 1, Color = "#ff0000", Position = 0 }
                }
            };

            // Act & Assert
            Assert.False(tube.IsEmpty);
        }

        [Fact]
        public void Tube_WithSameColorBalls_IsCompleteReturnsTrue()
        {
            // Arrange
            var tube = new Tube
            {
                Id = 1,
                Position = 0,
                Balls = new List<Ball>
                {
                    new Ball { Id = 1, Color = "#ff0000", Position = 0 },
                    new Ball { Id = 2, Color = "#ff0000", Position = 1 },
                    new Ball { Id = 3, Color = "#ff0000", Position = 2 }
                }
            };

            // Act & Assert
            Assert.True(tube.IsComplete);
        }

        [Fact]
        public void Tube_WithDifferentColorBalls_IsCompleteReturnsFalse()
        {
            // Arrange
            var tube = new Tube
            {
                Id = 1,
                Position = 0,
                Balls = new List<Ball>
                {
                    new Ball { Id = 1, Color = "#ff0000", Position = 0 },
                    new Ball { Id = 2, Color = "#00ff00", Position = 1 }
                }
            };

            // Act & Assert
            Assert.False(tube.IsComplete);
        }

        [Fact]
        public void GetTopBall_WithBalls_ReturnsTopBall()
        {
            // Arrange
            var topBall = new Ball { Id = 2, Color = "#ff0000", Position = 1 };
            var tube = new Tube
            {
                Id = 1,
                Position = 0,
                Balls = new List<Ball>
                {
                    new Ball { Id = 1, Color = "#ff0000", Position = 0 },
                    topBall
                }
            };

            // Act
            var result = tube.GetTopBall();

            // Assert
            Assert.Equal(topBall, result);
        }

        [Fact]
        public void GetTopBall_EmptyTube_ReturnsNull()
        {
            // Arrange
            var tube = new Tube
            {
                Id = 1,
                Position = 0,
                Balls = new List<Ball>()
            };

            // Act
            var result = tube.GetTopBall();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void CanReceiveBall_EmptyTube_ReturnsTrue()
        {
            // Arrange
            var tube = new Tube
            {
                Id = 1,
                Position = 0,
                Balls = new List<Ball>()
            };
            var ball = new Ball { Id = 1, Color = "#ff0000", Position = 0 };

            // Act
            var result = tube.CanReceiveBall(ball);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanReceiveBall_SameColorAsTop_ReturnsTrue()
        {
            // Arrange
            var tube = new Tube
            {
                Id = 1,
                Position = 0,
                Balls = new List<Ball>
                {
                    new Ball { Id = 1, Color = "#ff0000", Position = 0 }
                }
            };
            var ball = new Ball { Id = 2, Color = "#ff0000", Position = 0 };

            // Act
            var result = tube.CanReceiveBall(ball);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanReceiveBall_DifferentColorFromTop_ReturnsFalse()
        {
            // Arrange
            var tube = new Tube
            {
                Id = 1,
                Position = 0,
                Balls = new List<Ball>
                {
                    new Ball { Id = 1, Color = "#ff0000", Position = 0 }
                }
            };
            var ball = new Ball { Id = 2, Color = "#00ff00", Position = 0 };

            // Act
            var result = tube.CanReceiveBall(ball);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanReceiveBall_FullTube_ReturnsFalse()
        {
            // Arrange
            var tube = new Tube
            {
                Id = 1,
                Position = 0,
                Capacity = 2, // Assuming capacity is 2
                Balls = new List<Ball>
                {
                    new Ball { Id = 1, Color = "#ff0000", Position = 0 },
                    new Ball { Id = 2, Color = "#ff0000", Position = 1 }
                }
            };
            var ball = new Ball { Id = 3, Color = "#ff0000", Position = 0 };

            // Act
            var result = tube.CanReceiveBall(ball);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsFull_WithCapacityReached_ReturnsTrue()
        {
            // Arrange
            var tube = new Tube
            {
                Id = 1,
                Position = 0,
                Capacity = 2,
                Balls = new List<Ball>
                {
                    new Ball { Id = 1, Color = "#ff0000", Position = 0 },
                    new Ball { Id = 2, Color = "#ff0000", Position = 1 }
                }
            };

            // Act & Assert
            Assert.True(tube.IsFull);
        }

        [Fact]
        public void IsFull_WithSpaceRemaining_ReturnsFalse()
        {
            // Arrange
            var tube = new Tube
            {
                Id = 1,
                Position = 0,
                Capacity = 3,
                Balls = new List<Ball>
                {
                    new Ball { Id = 1, Color = "#ff0000", Position = 0 }
                }
            };

            // Act & Assert
            Assert.False(tube.IsFull);
        }
    }
}