using JogoBolinha.Models.Game;
using System.Text.Json;

namespace JogoBolinha.Services
{
    public class LevelGeneratorService
    {
        private static readonly string[] ColorPalette = {
            "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7",
            "#DDA0DD", "#98D8C8", "#F7DC6F", "#BB8FCE", "#85C1E9",
            "#F8C471", "#82E0AA", "#F1948A", "#85C1E9", "#D7BDE2"
        };
        
        public Level GenerateLevel(int levelNumber)
        {
            var difficulty = DetermineDifficulty(levelNumber);
            var (colors, tubes, ballsPerColor) = GetLevelParameters(difficulty);
            
            var level = new Level
            {
                Number = levelNumber,
                Difficulty = difficulty,
                Colors = colors,
                Tubes = tubes,
                BallsPerColor = ballsPerColor,
                InitialState = GenerateInitialState(colors, tubes, ballsPerColor),
                MinimumMoves = EstimateMinimumMoves(colors, tubes, ballsPerColor)
            };
            
            return level;
        }
        
        private Difficulty DetermineDifficulty(int levelNumber)
        {
            return levelNumber switch
            {
                <= 10 => Difficulty.Easy,
                <= 30 => Difficulty.Medium,
                <= 50 => Difficulty.Hard,
                _ => Difficulty.Expert
            };
        }
        
        private (int colors, int tubes, int ballsPerColor) GetLevelParameters(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Easy => (2 + Random.Shared.Next(0, 2), 3 + Random.Shared.Next(0, 2), 2 + Random.Shared.Next(0, 3)),
                Difficulty.Medium => (3 + Random.Shared.Next(0, 2), 4 + Random.Shared.Next(0, 2), 3 + Random.Shared.Next(0, 3)),
                Difficulty.Hard => (4 + Random.Shared.Next(0, 2), 5 + Random.Shared.Next(0, 2), 4 + Random.Shared.Next(0, 3)),
                Difficulty.Expert => (5 + Random.Shared.Next(0, 2), 6 + Random.Shared.Next(0, 2), 5 + Random.Shared.Next(0, 4)),
                _ => (3, 4, 3)
            };
        }
        
        private string GenerateInitialState(int colorCount, int tubeCount, int ballsPerColor)
        {
            var colors = ColorPalette.Take(colorCount).ToList();
            var tubes = new List<List<string>>();
            
            // Initialize tubes
            for (int i = 0; i < tubeCount; i++)
            {
                tubes.Add(new List<string>());
            }
            
            // Create balls for each color
            var allBalls = new List<string>();
            foreach (var color in colors)
            {
                for (int i = 0; i < ballsPerColor; i++)
                {
                    allBalls.Add(color);
                }
            }
            
            // Shuffle balls randomly
            var random = new Random();
            for (int i = allBalls.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (allBalls[i], allBalls[j]) = (allBalls[j], allBalls[i]);
            }
            
            // Distribute balls among tubes, ensuring solvability
            int tubeCapacity = (int)Math.Ceiling((double)(allBalls.Count) / (tubeCount - 1)); // Leave at least one tube empty
            int ballIndex = 0;
            
            for (int tubeIndex = 0; tubeIndex < tubeCount - 1 && ballIndex < allBalls.Count; tubeIndex++)
            {
                int ballsInThisTube = Math.Min(tubeCapacity, allBalls.Count - ballIndex);
                
                for (int i = 0; i < ballsInThisTube; i++)
                {
                    tubes[tubeIndex].Add(allBalls[ballIndex++]);
                }
            }
            
            // Ensure the puzzle is not already solved
            while (IsPuzzleSolved(tubes, colors))
            {
                ShuffleTubes(tubes);
            }
            
            var initialState = new
            {
                Tubes = tubes.Select((tube, index) => new
                {
                    Id = index,
                    Balls = tube.Select((color, position) => new
                    {
                        Color = color,
                        Position = position
                    }).ToList()
                }).ToList()
            };
            
            return JsonSerializer.Serialize(initialState);
        }
        
        private bool IsPuzzleSolved(List<List<string>> tubes, List<string> colors)
        {
            foreach (var color in colors)
            {
                bool colorSolved = tubes.Any(tube => 
                    tube.Count > 0 && 
                    tube.All(ball => ball == color) && 
                    tube.Count == tube.Count(ball => ball == color));
                
                if (!colorSolved) return false;
            }
            
            return true;
        }
        
        private void ShuffleTubes(List<List<string>> tubes)
        {
            var random = new Random();
            var allBalls = tubes.SelectMany(tube => tube).ToList();
            
            // Clear all tubes
            foreach (var tube in tubes)
            {
                tube.Clear();
            }
            
            // Redistribute randomly
            foreach (var ball in allBalls.OrderBy(_ => random.Next()))
            {
                var availableTubes = tubes.Where(t => t.Count < 4).ToList();
                if (availableTubes.Any())
                {
                    var randomTube = availableTubes[random.Next(availableTubes.Count)];
                    randomTube.Add(ball);
                }
            }
        }
        
        private int EstimateMinimumMoves(int colorCount, int tubeCount, int ballsPerColor)
        {
            // Simple heuristic: each color needs to be sorted, considering tube capacity constraints
            int totalBalls = colorCount * ballsPerColor;
            int averageMovesPerBall = 2; // Rough estimate
            
            return Math.Max(colorCount * ballsPerColor, totalBalls * averageMovesPerBall / 3);
        }
        
        public GameState CreateGameStateFromLevel(Level level, int? playerId = null)
        {
            var initialStateData = JsonSerializer.Deserialize<dynamic>(level.InitialState);
            
            var gameState = new GameState
            {
                LevelId = level.Id,
                Level = level,
                PlayerId = playerId,
                Status = GameStatus.InProgress,
                StartTime = DateTime.UtcNow
            };
            
            return gameState;
        }
    }
}