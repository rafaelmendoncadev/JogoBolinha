using JogoBolinha.Models.Game;
using System.Text.Json;

namespace JogoBolinha.Services
{
    public class LevelGeneratorServiceV2
    {
        private static readonly string[] ColorPalette = {
            "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7",
            "#DDA0DD", "#98D8C8", "#F7DC6F", "#BB8FCE", "#85C1E9",
            "#F8C471", "#82E0AA", "#F1948A", "#85C1E9", "#D7BDE2"
        };
        
        // Configuração determinística para garantir progressão adequada
        private static readonly Dictionary<int, LevelConfig> LevelConfigs = new()
        {
            // Níveis tutoriais (1-5) - Muito fáceis para aprender a mecânica
            { 1, new LevelConfig { Colors = 2, Tubes = 4, BallsPerColor = 2, EmptyTubes = 2 } }, // 2 cores, 4 tubos (2 vazios)
            { 2, new LevelConfig { Colors = 2, Tubes = 4, BallsPerColor = 3, EmptyTubes = 2 } }, // 2 cores, mais bolas
            { 3, new LevelConfig { Colors = 2, Tubes = 5, BallsPerColor = 3, EmptyTubes = 3 } }, // 2 cores, mais espaço
            { 4, new LevelConfig { Colors = 3, Tubes = 5, BallsPerColor = 3, EmptyTubes = 2 } }, // 3 cores introduzidas
            { 5, new LevelConfig { Colors = 3, Tubes = 6, BallsPerColor = 3, EmptyTubes = 3 } }, // 3 cores, mais espaço
            
            // Níveis fáceis (6-10) - Progressão suave
            { 6, new LevelConfig { Colors = 3, Tubes = 5, BallsPerColor = 4, EmptyTubes = 2 } },
            { 7, new LevelConfig { Colors = 3, Tubes = 6, BallsPerColor = 4, EmptyTubes = 3 } },
            { 8, new LevelConfig { Colors = 4, Tubes = 6, BallsPerColor = 3, EmptyTubes = 2 } },
            { 9, new LevelConfig { Colors = 4, Tubes = 7, BallsPerColor = 3, EmptyTubes = 3 } },
            { 10, new LevelConfig { Colors = 4, Tubes = 6, BallsPerColor = 4, EmptyTubes = 2 } },
            
            // Níveis médios (11-20)
            { 11, new LevelConfig { Colors = 4, Tubes = 7, BallsPerColor = 4, EmptyTubes = 3 } },
            { 12, new LevelConfig { Colors = 5, Tubes = 7, BallsPerColor = 3, EmptyTubes = 2 } },
            { 13, new LevelConfig { Colors = 5, Tubes = 8, BallsPerColor = 3, EmptyTubes = 3 } },
            { 14, new LevelConfig { Colors = 5, Tubes = 7, BallsPerColor = 4, EmptyTubes = 2 } },
            { 15, new LevelConfig { Colors = 5, Tubes = 8, BallsPerColor = 4, EmptyTubes = 3 } },
            { 16, new LevelConfig { Colors = 6, Tubes = 8, BallsPerColor = 3, EmptyTubes = 2 } },
            { 17, new LevelConfig { Colors = 6, Tubes = 9, BallsPerColor = 3, EmptyTubes = 3 } },
            { 18, new LevelConfig { Colors = 6, Tubes = 8, BallsPerColor = 4, EmptyTubes = 2 } },
            { 19, new LevelConfig { Colors = 6, Tubes = 9, BallsPerColor = 4, EmptyTubes = 3 } },
            { 20, new LevelConfig { Colors = 7, Tubes = 9, BallsPerColor = 4, EmptyTubes = 2 } },
        };
        
        private class LevelConfig
        {
            public int Colors { get; set; }
            public int Tubes { get; set; }
            public int BallsPerColor { get; set; }
            public int EmptyTubes { get; set; }
        }
        
        public Level GenerateLevel(int levelNumber)
        {
            // Nível 2: estado tutorial fixo e muito fácil
            if (levelNumber == 2)
            {
                // Create tutorial state manually with 2 colors and plenty of space
                // T1 has colors 0,1 (red, blue), T2 has colors 1,0 (blue, red), T3,T4,T5 are empty
                var tutorialState = new
                {
                    Tubes = new object[]
                    {
                        new { Id = 0, Balls = new[] { new { Color = ColorPalette[0], Position = 0 }, new { Color = ColorPalette[1], Position = 1 } }, Capacity = 3 },
                        new { Id = 1, Balls = new[] { new { Color = ColorPalette[1], Position = 0 }, new { Color = ColorPalette[0], Position = 1 } }, Capacity = 3 },
                        new { Id = 2, Balls = new object[0], Capacity = 3 },
                        new { Id = 3, Balls = new object[0], Capacity = 3 },
                        new { Id = 4, Balls = new object[0], Capacity = 3 }
                    }
                };
                
                return new Level
                {
                    Number = 2,
                    Difficulty = Difficulty.Easy,
                    Colors = 2,
                    Tubes = 5,
                    BallsPerColor = 2,
                    InitialState = JsonSerializer.Serialize(tutorialState),
                    MinimumMoves = 2,
                    GenerationSeed = 2
                };
            }

            var config = GetLevelConfig(levelNumber);
            
            // Validação crítica de solvabilidade
            if (config.Tubes < config.Colors + config.EmptyTubes)
            {
                throw new InvalidOperationException($"Level {levelNumber}: Configuração impossível! Tubos insuficientes.");
            }
            
            var level = new Level
            {
                Number = levelNumber,
                Difficulty = DetermineDifficulty(levelNumber),
                Colors = config.Colors,
                Tubes = config.Tubes,
                BallsPerColor = config.BallsPerColor,
                InitialState = GenerateSolvableInitialState(config),
                MinimumMoves = EstimateMinimumMoves(config)
            };
            
            Console.WriteLine($"[LEVEL GEN V2] Nível {levelNumber}: {config.Colors} cores, {config.Tubes} tubos ({config.EmptyTubes} vazios), {config.BallsPerColor} bolas/cor");
            
            return level;
        }
        
        private LevelConfig GetLevelConfig(int levelNumber)
        {
            // Para níveis pré-definidos (1-20), usar configuração específica
            if (LevelConfigs.TryGetValue(levelNumber, out var config))
            {
                // Overrides para corrigir progressão dos níveis iniciais
                if (levelNumber == 2)
                {
                    // Tornar o nível 2 visivelmente mais fácil que o 3
                    return new LevelConfig { Colors = 2, Tubes = 5, BallsPerColor = 2, EmptyTubes = 3 };
                }
                if (levelNumber == 3)
                {
                    // Nível 3 um pouco mais desafiador que o 2
                    return new LevelConfig { Colors = 2, Tubes = 4, BallsPerColor = 3, EmptyTubes = 2 };
                }
                if (levelNumber == 4)
                {
                    return new LevelConfig { Colors = 3, Tubes = 6, BallsPerColor = 3, EmptyTubes = 3 };
                }
                return config;
            }
            
            // Para níveis posteriores, gerar baseado em fórmula progressiva
            if (levelNumber <= 30) // Médio-difícil
            {
                int colors = 5 + (levelNumber - 20) / 3;
                int emptyTubes = (levelNumber % 3 == 0) ? 3 : 2;
                return new LevelConfig 
                { 
                    Colors = Math.Min(colors, 8), 
                    Tubes = Math.Min(colors + emptyTubes, 10),
                    BallsPerColor = 4,
                    EmptyTubes = emptyTubes
                };
            }
            else if (levelNumber <= 50) // Difícil
            {
                int colors = 7 + (levelNumber - 30) / 5;
                int emptyTubes = (levelNumber % 3 == 0) ? 3 : 2;
                return new LevelConfig 
                { 
                    Colors = Math.Min(colors, 10), 
                    Tubes = Math.Min(colors + emptyTubes, 12),
                    BallsPerColor = 4 + (levelNumber > 40 ? 1 : 0),
                    EmptyTubes = emptyTubes
                };
            }
            else // Expert
            {
                int colors = 8 + (levelNumber - 50) / 10;
                return new LevelConfig 
                { 
                    Colors = Math.Min(colors, 12), 
                    Tubes = Math.Min(colors + 2, 14),
                    BallsPerColor = 5,
                    EmptyTubes = 2
                };
            }
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
        
        private string GenerateSolvableInitialState(LevelConfig config)
        {
            var colors = ColorPalette.Take(config.Colors).ToList();
            var tubes = new List<List<string>>();
            var random = new Random(config.Colors * 1000 + config.Tubes * 100 + config.BallsPerColor); // Seed baseado na config para consistência
            
            // Inicializar tubos vazios
            for (int i = 0; i < config.Tubes; i++)
            {
                tubes.Add(new List<string>());
            }
            
            // Criar todas as bolas
            var allBalls = new List<string>();
            foreach (var color in colors)
            {
                for (int i = 0; i < config.BallsPerColor; i++)
                {
                    allBalls.Add(color);
                }
            }
            
            // Embaralhar bolas de forma controlada
            ShuffleBalls(allBalls, random);
            
            // Distribuir bolas garantindo solvabilidade
            DistributeBallsSolvable(tubes, allBalls, config, random);
            
            // Validar e ajustar se necessário
            int attempts = 0;
            while (!IsValidAndSolvable(tubes, colors, config) && attempts < 100)
            {
                RedistributeBalls(tubes, allBalls, config, random);
                attempts++;
            }
            
            if (attempts >= 100)
            {
                // Fallback: criar configuração simples garantidamente solvável
                CreateSimpleSolvableConfiguration(tubes, colors, config);
            }
            
            // Serializar estado inicial
            var initialState = new
            {
                Tubes = tubes.Select((tube, index) => new
                {
                    Id = index,
                    Balls = tube.Select((color, position) => new
                    {
                        Color = color,
                        Position = position
                    }).ToList(),
                    Capacity = config.BallsPerColor + 1 // Capacidade um pouco maior que o necessário
                }).ToList()
            };
            
            return JsonSerializer.Serialize(initialState);
        }
        
        private void ShuffleBalls(List<string> balls, Random random)
        {
            // Fisher-Yates shuffle
            for (int i = balls.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (balls[i], balls[j]) = (balls[j], balls[i]);
            }
        }
        
        private void DistributeBallsSolvable(List<List<string>> tubes, List<string> allBalls, LevelConfig config, Random random)
        {
            int filledTubes = config.Tubes - config.EmptyTubes;
            int ballIndex = 0;
            
            // Distribuir bolas apenas nos tubos que devem ser preenchidos
            for (int tubeIndex = 0; tubeIndex < filledTubes && ballIndex < allBalls.Count; tubeIndex++)
            {
                // Calcular quantas bolas colocar neste tubo
                int remainingBalls = allBalls.Count - ballIndex;
                int remainingTubes = filledTubes - tubeIndex;
                int targetBalls = remainingBalls / remainingTubes;
                
                // Adicionar alguma variação, mas manter dentro dos limites
                int variation = random.Next(-1, 2); // -1, 0 ou 1
                int ballsInThisTube = Math.Max(1, Math.Min(config.BallsPerColor, targetBalls + variation));
                
                // Ajustar para não exceder o total
                ballsInThisTube = Math.Min(ballsInThisTube, remainingBalls);
                
                for (int i = 0; i < ballsInThisTube && ballIndex < allBalls.Count; i++)
                {
                    tubes[tubeIndex].Add(allBalls[ballIndex++]);
                }
            }
            
            // Garantir que todas as bolas foram distribuídas
            while (ballIndex < allBalls.Count)
            {
                for (int tubeIndex = 0; tubeIndex < filledTubes && ballIndex < allBalls.Count; tubeIndex++)
                {
                    if (tubes[tubeIndex].Count < config.BallsPerColor)
                    {
                        tubes[tubeIndex].Add(allBalls[ballIndex++]);
                    }
                }
            }
        }
        
        private void RedistributeBalls(List<List<string>> tubes, List<string> allBalls, LevelConfig config, Random random)
        {
            // Coletar todas as bolas novamente
            var collectedBalls = new List<string>();
            foreach (var tube in tubes)
            {
                collectedBalls.AddRange(tube);
                tube.Clear();
            }
            
            // Embaralhar novamente
            ShuffleBalls(collectedBalls, random);
            
            // Redistribuir
            DistributeBallsSolvable(tubes, collectedBalls, config, random);
        }
        
        private bool IsValidAndSolvable(List<List<string>> tubes, List<string> colors, LevelConfig config)
        {
            // Verificar se há tubos vazios suficientes
            int emptyTubes = tubes.Count(t => t.Count == 0);
            if (emptyTubes < config.EmptyTubes) return false;
            
            // Verificar se cada cor tem o número correto de bolas
            var colorCounts = colors.ToDictionary(c => c, c => 0);
            foreach (var tube in tubes)
            {
                foreach (var ball in tube)
                {
                    if (colorCounts.ContainsKey(ball))
                        colorCounts[ball]++;
                }
            }
            
            if (!colorCounts.All(kv => kv.Value == config.BallsPerColor))
                return false;
            
            // Verificar se não está já resolvido
            if (IsPuzzleSolved(tubes, colors)) return false;
            
            // Verificação básica de solvabilidade
            // Para níveis fáceis (1-10), garantir que não há situação de deadlock óbvia
            if (config.Colors <= 3 && config.EmptyTubes >= 2)
            {
                // Com 2-3 cores e 2+ tubos vazios, quase sempre é solvável
                return !HasObviousDeadlock(tubes, colors);
            }
            
            return true;
        }
        
        private bool HasObviousDeadlock(List<List<string>> tubes, List<string> colors)
        {
            // Verificar situações de deadlock óbvias
            // Por exemplo: todos os tubos cheios têm cores misturadas no topo
            var filledTubes = tubes.Where(t => t.Count > 0).ToList();
            
            // Se todos os tubos cheios têm cores diferentes no topo e na segunda posição
            bool allMixed = filledTubes.All(tube =>
            {
                if (tube.Count < 2) return false;
                return tube[tube.Count - 1] != tube[tube.Count - 2];
            });
            
            // Se todos estão misturados e não há espaço suficiente para manobras
            if (allMixed && tubes.Count(t => t.Count == 0) < 2)
                return true;
            
            return false;
        }
        
        private bool IsPuzzleSolved(List<List<string>> tubes, List<string> colors)
        {
            foreach (var color in colors)
            {
                bool colorSolved = tubes.Any(tube => 
                    tube.Count > 0 && 
                    tube.All(ball => ball == color));
                
                if (!colorSolved) return false;
            }
            
            return true;
        }
        
        private void CreateSimpleSolvableConfiguration(List<List<string>> tubes, List<string> colors, LevelConfig config)
        {
            // Limpar todos os tubos
            foreach (var tube in tubes)
            {
                tube.Clear();
            }
            
            // Criar configuração simples mas não trivial
            int tubeIndex = 0;
            foreach (var color in colors)
            {
                var colorBalls = new List<string>();
                for (int i = 0; i < config.BallsPerColor; i++)
                {
                    colorBalls.Add(color);
                }
                
                // Distribuir as bolas da cor em 2 tubos diferentes
                int halfPoint = config.BallsPerColor / 2;
                for (int i = 0; i < halfPoint; i++)
                {
                    tubes[tubeIndex].Add(colorBalls[i]);
                }
                for (int i = halfPoint; i < colorBalls.Count; i++)
                {
                    tubes[tubeIndex + 1].Add(colorBalls[i]);
                }
                
                tubeIndex++;
                if (tubeIndex >= config.Tubes - config.EmptyTubes - 1)
                    tubeIndex = 0;
            }
            
            // Misturar um pouco os topos para não ser trivial
            var random = new Random();
            for (int i = 0; i < config.Tubes - config.EmptyTubes; i++)
            {
                if (tubes[i].Count > 1 && random.Next(2) == 0)
                {
                    // Trocar o topo com outro tubo
                    int otherIndex = (i + 1) % (config.Tubes - config.EmptyTubes);
                    if (tubes[otherIndex].Count > 0)
                    {
                        var temp = tubes[i][tubes[i].Count - 1];
                        tubes[i][tubes[i].Count - 1] = tubes[otherIndex][tubes[otherIndex].Count - 1];
                        tubes[otherIndex][tubes[otherIndex].Count - 1] = temp;
                    }
                }
            }
        }
        
        private int EstimateMinimumMoves(LevelConfig config)
        {
            // Estimativa mais realista baseada na configuração
            // Níveis fáceis: menos movimentos
            // Níveis difíceis: mais movimentos
            
            int baseMove = config.Colors * config.BallsPerColor;
            
            if (config.Colors <= 3)
                return baseMove / 2; // Níveis fáceis
            else if (config.Colors <= 5)
                return baseMove * 2 / 3; // Níveis médios
            else
                return baseMove; // Níveis difíceis
        }
        
        public GameState CreateGameStateFromLevel(Level level, int? playerId = null)
        {
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
