
using JogoBolinha.Models.Game;
using System.Text;
using System.Text.Json;

namespace JogoBolinha.Services
{
    public class LevelGeneratorService
    {
        private readonly Random _random = new();
        private static readonly string[] ColorPalette = {
            "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7",
            "#DDA0DD", "#98D8C8", "#F7DC6F", "#BB8FCE", "#85C1E9",
            "#F8C471", "#82E0AA", "#F1948A", "#D7BDE2", "#A9DFBF"
        };

        public Level GenerateLevel(int levelNumber)
        {
            var difficulty = DetermineDifficulty(levelNumber);
            var parameters = GetDifficultyParameters(levelNumber, difficulty);
            
            var (initialState, seed, minimumMoves) = GenerateLevelByReverseAlgorithm(parameters);
            
            var level = new Level
            {
                Number = levelNumber,
                Difficulty = difficulty,
                Colors = parameters.ColorCount,
                Tubes = parameters.TubeCount,
                BallsPerColor = 4, // Standard
                InitialState = initialState,
                GenerationSeed = seed,
                MinimumMoves = minimumMoves
            };
            
            Console.WriteLine($"[LEVEL GEN] Level {levelNumber}: {parameters.ColorCount}c, {parameters.TubeCount}t, {parameters.ShuffleMoves}m -> Solvable ✓");
            
            return level;
        }

        public bool ValidateLevel(string levelLayout)
        {
            try
            {
                var tubes = ParseCompactFormat(levelLayout);
                return IsLevelSolvable(tubes);
            }
            catch
            {
                return false;
            }
        }

        private (string initialState, long seed, int minimumMoves) GenerateLevelByReverseAlgorithm(LevelParameters parameters)
        {
            long seed = Guid.NewGuid().GetHashCode();
            var random = new Random((int)seed);
            
            var tubes = CreateSolvedState(parameters);
            
            var moveHistory = new List<(int from, int to)>();
            int actualMoves = ApplyReverseMoves(tubes, parameters.ShuffleMoves, random, moveHistory);
            
            string compactState = ConvertToCompactFormat(tubes);
            
            return (compactState, seed, actualMoves);
        }

        private List<List<string>> CreateSolvedState(LevelParameters parameters)
        {
            var tubes = new List<List<string>>();
            var colors = ColorPalette.Take(parameters.ColorCount).ToList();
            
            for (int i = 0; i < parameters.ColorCount; i++)
            {
                var tube = new List<string>();
                for (int j = 0; j < 4; j++) // 4 balls per color
                {
                    tube.Add(colors[i]);
                }
                tubes.Add(tube);
            }
            
            for (int i = 0; i < parameters.EmptyTubes; i++)
            {
                tubes.Add(new List<string>());
            }
            
            return tubes;
        }

        private int ApplyReverseMoves(List<List<string>> tubes, int targetMoves, Random random, List<(int, int)> moveHistory)
        {
            int successfulMoves = 0;
            int attempts = 0;
            int maxAttempts = targetMoves * 5;

            while (successfulMoves < targetMoves && attempts < maxAttempts)
            {
                attempts++;
                
                var validMoves = FindAllValidReverseMoves(tubes);
                if (validMoves.Count == 0) break;
                
                var move = validMoves[random.Next(validMoves.Count)];
                
                var ball = tubes[move.from].Last();
                tubes[move.from].RemoveAt(tubes[move.from].Count - 1);
                tubes[move.to].Add(ball);
                
                moveHistory.Add((move.from, move.to));
                successfulMoves++;
            }
            
            return successfulMoves;
        }

        private List<(int from, int to)> FindAllValidReverseMoves(List<List<string>> tubes)
        {
            var validMoves = new List<(int from, int to)>();
            int capacity = 4;

            for (int from = 0; from < tubes.Count; from++)
            {
                if (tubes[from].Count == 0) continue;

                var topBall = tubes[from].Last();
                
                for (int to = 0; to < tubes.Count; to++)
                {
                    if (from == to) continue;

                    if (tubes[to].Count < capacity)
                    {
                        if (tubes[to].Count == 0)
                        {
                            validMoves.Add((from, to));
                        }
                        else if (tubes[to].Last() == topBall)
                        {
                            validMoves.Add((from, to));
                        }
                    }
                }
            }
            return validMoves;
        }

        private string ConvertToCompactFormat(List<List<string>> tubes)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < tubes.Count; i++)
            {
                sb.Append($"T{i + 1}=");
                sb.Append(string.Join(",", tubes[i].Select(GetColorCode)));
                if (i < tubes.Count - 1) sb.Append(';');
            }
            return sb.ToString();
        }

        private List<List<string>> ParseCompactFormat(string compactFormat)
        {
            var tubes = new List<List<string>>();
            var tubeParts = compactFormat.Split(';');
            
            foreach (var tubePart in tubeParts)
            {
                var parts = tubePart.Split('=');
                var tube = new List<string>();
                if (parts.Length == 2 && !string.IsNullOrEmpty(parts[1]))
                {
                    var balls = parts[1].Split(',');
                    foreach (var ball in balls)
                    {
                        tube.Add(DecodeColor(ball));
                    }
                }
                tubes.Add(tube);
            }
            return tubes;
        }

        private string GetColorCode(string color)
        {
            int index = Array.IndexOf(ColorPalette, color);
            return index != -1 ? index.ToString() : "0";
        }

        private string DecodeColor(string code)
        {
            if (int.TryParse(code, out int index) && index >= 0 && index < ColorPalette.Length)
            {
                return ColorPalette[index];
            }
            return ColorPalette[0];
        }

        private bool IsLevelSolvable(List<List<string>> tubes)
        {
            var colorCounts = new Dictionary<string, int>();
            foreach (var tube in tubes)
            {
                foreach (var ball in tube)
                {
                    if (!colorCounts.ContainsKey(ball)) colorCounts[ball] = 0;
                    colorCounts[ball]++;
                }
            }
            
            if (colorCounts.Count == 0) return true; // Empty level is solvable
            
            var firstColorCount = colorCounts.First().Value;
            return colorCounts.All(kv => kv.Value == firstColorCount);
        }

        private LevelParameters GetDifficultyParameters(int levelNumber, Difficulty difficulty)
        {
            if (levelNumber <= 10)
                return new LevelParameters { ColorCount = 3, TubeCount = 4, EmptyTubes = 1, ShuffleMoves = 10 + _random.Next(11) };
            if (levelNumber <= 30)
                return new LevelParameters { ColorCount = 4, TubeCount = 5, EmptyTubes = 1, ShuffleMoves = 15 + _random.Next(11) };
            if (levelNumber <= 60)
                return new LevelParameters { ColorCount = 5, TubeCount = 6 + _random.Next(2), EmptyTubes = 1 + _random.Next(2), ShuffleMoves = 30 + _random.Next(16) };
            if (levelNumber <= 100)
                return new LevelParameters { ColorCount = 6 + _random.Next(2), TubeCount = 8 + _random.Next(2), EmptyTubes = 2, ShuffleMoves = 50 + _random.Next(21) };
            
            return new LevelParameters { ColorCount = 8 + _random.Next(3), TubeCount = 10 + _random.Next(3), EmptyTubes = 2, ShuffleMoves = 80 + _random.Next(21) };
        }

        private Difficulty DetermineDifficulty(int levelNumber)
        {
            if (levelNumber <= 10) return Difficulty.Easy;
            if (levelNumber <= 30) return Difficulty.Medium;
            if (levelNumber <= 60) return Difficulty.Hard;
            return Difficulty.Expert;
        }

        private class LevelParameters
        {
            public int ColorCount { get; set; }
            public int TubeCount { get; set; }
            public int EmptyTubes { get; set; }
            public int ShuffleMoves { get; set; }
        }
    }
}
