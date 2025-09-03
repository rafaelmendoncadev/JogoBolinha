using JogoBolinha.Models.Game;
using JogoBolinha.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace JogoBolinha.Services
{
    public enum HintType
    {
        Simple,      // Shows one move
        Advanced,    // Shows sequence of 2-3 moves
        Strategic,   // Shows optimal path to solution
        Tutorial     // Explains why the move is good
    }

    public class HintResult
    {
        public List<int> TubeIds { get; set; } = new List<int>();
        public string? Explanation { get; set; }
        public int Score { get; set; } // How good is this move (0-100)
        public HintType Type { get; set; }
    }

    public class HintService
    {
        private readonly GameDbContext _context;
        private readonly GameLogicService _gameLogicService;

        public HintService(GameDbContext context, GameLogicService gameLogicService)
        {
            _context = context;
            _gameLogicService = gameLogicService;
        }

        public async Task<HintResult?> GetHintAsync(int gameStateId, HintType hintType = HintType.Simple)
        {
            var gameState = await GetGameStateWithTubesAsync(gameStateId);
            if (gameState == null || gameState.Status != GameStatus.InProgress)
                return null;

            HintResult? hint = hintType switch
            {
                HintType.Simple => GetSimpleHint(gameState),
                HintType.Advanced => GetAdvancedHint(gameState),
                HintType.Strategic => GetStrategicHint(gameState),
                HintType.Tutorial => GetTutorialHint(gameState),
                _ => GetSimpleHint(gameState)
            };

            if (hint != null)
            {
                // Track hint usage
                gameState.HintsUsed++;
                await _context.SaveChangesAsync();
            }

            return hint;
        }

        private HintResult? GetSimpleHint(GameState gameState)
        {
            // Evaluate all possible moves and pick the best one
            var allMoves = EvaluateAllMoves(gameState);
            var bestMove = allMoves.OrderByDescending(m => m.Score).FirstOrDefault();
            
            return bestMove;
        }

        private HintResult? GetAdvancedHint(GameState gameState)
        {
            var firstMove = GetSimpleHint(gameState);
            if (firstMove == null) return null;

            // Simulate the first move and find the next best moves
            var simulatedState = SimulateMove(gameState, firstMove.TubeIds[0], firstMove.TubeIds[1]);
            if (simulatedState != null)
            {
                var secondMove = GetSimpleHint(simulatedState);
                if (secondMove != null)
                {
                    firstMove.TubeIds.AddRange(secondMove.TubeIds);
                    firstMove.Type = HintType.Advanced;
                }
            }

            return firstMove;
        }

        private HintResult? GetStrategicHint(GameState gameState)
        {
            // Use A* or similar algorithm to find optimal solution path
            var solution = FindOptimalSolution(gameState, maxDepth: 10);
            if (solution == null || !solution.Any()) return null;

            var hint = new HintResult
            {
                TubeIds = solution.Take(6).ToList(), // Show up to 3 moves
                Type = HintType.Strategic,
                Score = 100,
                Explanation = $"Sequência ótima de {solution.Count / 2} movimentos para completar o nível"
            };

            return hint;
        }

        private HintResult? GetTutorialHint(GameState gameState)
        {
            var hint = GetSimpleHint(gameState);
            if (hint == null) return null;

            hint.Type = HintType.Tutorial;
            
            // Add educational explanation
            var fromTube = gameState.Tubes.First(t => t.Id == hint.TubeIds[0]);
            var toTube = gameState.Tubes.First(t => t.Id == hint.TubeIds[1]);
            var ball = fromTube.GetTopBall();

            if (toTube.IsEmpty)
            {
                hint.Explanation = "Mover para tubo vazio cria espaço para organizar melhor as cores";
            }
            else if (toTube.Balls.All(b => b.Color == ball?.Color))
            {
                hint.Explanation = $"Agrupar bolas da mesma cor ({ball?.Color}) aproxima você da vitória";
            }
            else if (IsMoveUnblocking(gameState, hint.TubeIds[0], hint.TubeIds[1]))
            {
                hint.Explanation = "Este movimento libera bolas importantes bloqueadas embaixo";
            }
            else
            {
                hint.Explanation = "Movimento estratégico para reorganizar as peças";
            }

            return hint;
        }

        private List<HintResult> EvaluateAllMoves(GameState gameState)
        {
            var moves = new List<HintResult>();

            foreach (var fromTube in gameState.Tubes.Where(t => !t.IsEmpty))
            {
                var topBall = fromTube.GetTopBall();
                if (topBall == null) continue;

                foreach (var toTube in gameState.Tubes.Where(t => t.Id != fromTube.Id))
                {
                    if (!toTube.CanReceiveBall(topBall)) continue;

                    var score = CalculateMoveScore(gameState, fromTube, toTube, topBall);
                    
                    moves.Add(new HintResult
                    {
                        TubeIds = new List<int> { fromTube.Id, toTube.Id },
                        Score = score,
                        Type = HintType.Simple
                    });
                }
            }

            return moves;
        }

        private int CalculateMoveScore(GameState gameState, Tube fromTube, Tube toTube, Ball ball)
        {
            int score = 0;

            // Priority 1: Completing a tube (highest priority)
            if (WillCompleteTube(toTube, ball))
            {
                score += 100;
            }

            // Priority 2: Moving to same color stack
            if (!toTube.IsEmpty && toTube.Balls.All(b => b.Color == ball.Color))
            {
                score += 80;
                score += toTube.Balls.Count * 10; // Prefer larger stacks
            }

            // Priority 3: Freeing up buried balls
            if (HasBuriedBallsOfSameColor(fromTube, ball))
            {
                score += 60;
            }

            // Priority 4: Moving from mixed tubes
            var fromColors = fromTube.Balls.Select(b => b.Color).Distinct().Count();
            if (fromColors > 1)
            {
                score += 40 + (fromColors * 5);
            }

            // Priority 5: Moving to empty tube (strategic value)
            if (toTube.IsEmpty)
            {
                score += 30;
                // But penalize if we have few empty tubes
                var emptyCount = gameState.Tubes.Count(t => t.IsEmpty);
                if (emptyCount <= 1) score -= 10;
            }

            // Penalty: Breaking a good stack
            if (fromTube.Balls.All(b => b.Color == ball.Color) && fromTube.Balls.Count > 1)
            {
                score -= 20;
            }

            // Penalty: Moving to a mixed tube (unless necessary)
            if (!toTube.IsEmpty && !toTube.Balls.All(b => b.Color == ball.Color))
            {
                score -= 15;
            }

            return Math.Max(0, score);
        }

        private bool WillCompleteTube(Tube tube, Ball ball)
        {
            if (tube.IsEmpty) return false;
            
            return tube.Balls.Count == tube.Capacity - 1 && 
                   tube.Balls.All(b => b.Color == ball.Color);
        }

        private bool HasBuriedBallsOfSameColor(Tube tube, Ball topBall)
        {
            return tube.Balls.Any(b => 
                b.Color == topBall.Color && 
                b.Position < topBall.Position);
        }

        private bool IsMoveUnblocking(GameState gameState, int fromTubeId, int toTubeId)
        {
            var fromTube = gameState.Tubes.First(t => t.Id == fromTubeId);
            var topBall = fromTube.GetTopBall();
            
            if (topBall == null) return false;

            // Check if moving this ball unblocks important balls
            var ballsBelow = fromTube.Balls.Where(b => b.Position < topBall.Position);
            
            foreach (var ball in ballsBelow)
            {
                // Check if any tube needs this ball to complete
                var needingTube = gameState.Tubes.FirstOrDefault(t =>
                    t.Id != fromTubeId &&
                    !t.IsEmpty &&
                    t.Balls.All(b => b.Color == ball.Color) &&
                    t.Balls.Count < t.Capacity);
                
                if (needingTube != null) return true;
            }

            return false;
        }

        private GameState? SimulateMove(GameState gameState, int fromTubeId, int toTubeId)
        {
            // Create a deep copy of the game state
            var simulatedState = new GameState
            {
                Id = gameState.Id,
                LevelId = gameState.LevelId,
                Level = gameState.Level,
                Status = gameState.Status,
                MovesCount = gameState.MovesCount + 1,
                HintsUsed = gameState.HintsUsed,
                Tubes = new List<Tube>()
            };

            // Deep copy tubes and balls
            foreach (var tube in gameState.Tubes)
            {
                var newTube = new Tube
                {
                    Id = tube.Id,
                    Capacity = tube.Capacity,
                    Position = tube.Position,
                    Balls = new List<Ball>()
                };

                foreach (var ball in tube.Balls)
                {
                    newTube.Balls.Add(new Ball
                    {
                        Id = ball.Id,
                        Color = ball.Color,
                        Position = ball.Position,
                        TubeId = ball.TubeId
                    });
                }

                simulatedState.Tubes.Add(newTube);
            }

            // Simulate the move
            var fromTube = simulatedState.Tubes.FirstOrDefault(t => t.Id == fromTubeId);
            var toTube = simulatedState.Tubes.FirstOrDefault(t => t.Id == toTubeId);
            
            if (fromTube == null || toTube == null) return null;

            var ballToMove = fromTube.Balls.OrderByDescending(b => b.Position).FirstOrDefault();
            if (ballToMove == null || !toTube.CanReceiveBall(ballToMove)) return null;

            // Move the ball
            fromTube.Balls.Remove(ballToMove);
            ballToMove.TubeId = toTubeId;
            ballToMove.Position = toTube.Balls.Count;
            toTube.Balls.Add(ballToMove);

            return simulatedState;
        }

        private List<int>? FindOptimalSolution(GameState gameState, int maxDepth)
        {
            // Simplified BFS to find solution
            var queue = new Queue<(GameState state, List<int> moves, int depth)>();
            var visited = new HashSet<string>();
            
            queue.Enqueue((gameState, new List<int>(), 0));
            visited.Add(GetStateHash(gameState));

            while (queue.Count > 0)
            {
                var (currentState, moves, depth) = queue.Dequeue();
                
                if (depth > maxDepth) continue;
                
                if (_gameLogicService.IsGameWon(currentState))
                {
                    return moves;
                }

                var possibleMoves = EvaluateAllMoves(currentState);
                
                foreach (var move in possibleMoves.OrderByDescending(m => m.Score).Take(3))
                {
                    var nextState = SimulateMove(currentState, move.TubeIds[0], move.TubeIds[1]);
                    if (nextState == null) continue;
                    
                    var stateHash = GetStateHash(nextState);
                    if (visited.Contains(stateHash)) continue;
                    
                    visited.Add(stateHash);
                    var newMoves = new List<int>(moves);
                    newMoves.AddRange(move.TubeIds);
                    
                    queue.Enqueue((nextState, newMoves, depth + 1));
                }
            }

            return null;
        }

        private string GetStateHash(GameState gameState)
        {
            // Create a unique hash for the game state
            var tubeStrings = gameState.Tubes.OrderBy(t => t.Position).Select(t =>
            {
                var balls = t.Balls.OrderBy(b => b.Position).Select(b => b.Color);
                return string.Join(",", balls);
            });
            
            return string.Join("|", tubeStrings);
        }

        private async Task<GameState?> GetGameStateWithTubesAsync(int gameStateId)
        {
            return await _context.GameStates
                .Include(gs => gs.Tubes)
                    .ThenInclude(t => t.Balls)
                .Include(gs => gs.Level)
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId);
        }
    }
}