using JogoBolinha.Models.Game;
using JogoBolinha.Data;
using Microsoft.EntityFrameworkCore;

namespace JogoBolinha.Services
{
    public enum GameEndReason
    {
        None,
        Victory,
        NoMovesLeft,
        TimeOut,
        UserQuit
    }

    public class GameStateResult
    {
        public bool IsGameOver { get; set; }
        public bool IsWon { get; set; }
        public GameEndReason EndReason { get; set; }
        public string Message { get; set; } = "";
        public int? NextLevelId { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    public class GameStateManager
    {
        private readonly GameDbContext _context;
        private readonly ScoreCalculationService _scoreService;
        private readonly Dictionary<int, (GameState gameState, DateTime cachedAt)> _gameStateCache;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromSeconds(30);

        public GameStateManager(GameDbContext context, ScoreCalculationService scoreService)
        {
            _context = context;
            _scoreService = scoreService;
            _gameStateCache = new Dictionary<int, (GameState, DateTime)>();
        }

        /// <summary>
        /// Comprehensive check for game state after each move
        /// </summary>
        public async Task<GameStateResult> CheckGameStateAsync(int gameStateId)
        {
            var stackTrace = new System.Diagnostics.StackTrace();
            var callingMethod = stackTrace.GetFrame(1)?.GetMethod()?.Name ?? "Unknown";
            var callingClass = stackTrace.GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
            
            Console.WriteLine($"[CHECK LOG] CheckGameStateAsync called from {callingClass}.{callingMethod} for gameStateId: {gameStateId}");
            
            var gameState = await GetGameStateAsync(gameStateId);
            if (gameState == null)
            {
                Console.WriteLine($"[ERROR] GameState {gameStateId} not found");
                return new GameStateResult 
                { 
                    IsGameOver = true, 
                    EndReason = GameEndReason.None,
                    Message = "Estado do jogo não encontrado"
                };
            }

            // Skip checks if game is already over
            if (gameState.Status == GameStatus.Completed || gameState.Status == GameStatus.Failed)
            {
                Console.WriteLine($"[SKIP] GameState {gameStateId} already finished with status: {gameState.Status}");
                return new GameStateResult 
                { 
                    IsGameOver = true,
                    IsWon = gameState.Status == GameStatus.Completed,
                    EndReason = gameState.Status == GameStatus.Completed ? GameEndReason.Victory : GameEndReason.NoMovesLeft
                };
            }

            Console.WriteLine($"[CHECKING] Verifying victory condition for gameStateId: {gameStateId}");
            // Priority 1: Check for victory
            var victoryCheck = CheckVictoryCondition(gameState);
            
            if (victoryCheck.IsWon)
            {
                Console.WriteLine($"[VICTORY] GameState {gameStateId} - Victory detected!");
                await HandleVictoryAsync(gameState);
                // Invalidate cache after state change
                InvalidateGameStateCache(gameStateId);
                return victoryCheck;
            }

            Console.WriteLine($"[CHECKING] Verifying defeat condition for gameStateId: {gameStateId}");
            // Priority 2: Check for defeat (no moves)
            var defeatCheck = CheckDefeatCondition(gameState);
            
            if (defeatCheck.IsGameOver)
            {
                Console.WriteLine($"[DEFEAT] GameState {gameStateId} - No valid moves remaining");
                await HandleDefeatAsync(gameState);
                // Invalidate cache after state change
                InvalidateGameStateCache(gameStateId);
                return defeatCheck;
            }

            Console.WriteLine($"[CONTINUE] Game continues for gameStateId: {gameStateId}");
            
            return new GameStateResult 
            { 
                IsGameOver = false,
                EndReason = GameEndReason.None
            };
        }

        /// <summary>
        /// Check if the game is won
        /// </summary>
        public GameStateResult CheckVictoryCondition(GameState gameState)
        {
            // Simple and reliable victory logic: All tubes must be either empty or complete
            bool isWon = gameState.Tubes.All(tube => tube.IsEmpty || tube.IsComplete);
            
            if (!isWon)
            {
                return new GameStateResult { IsGameOver = false };
            }

            // Count completed tubes and colors for the victory message
            int completedTubes = gameState.Tubes.Count(t => t.IsComplete);
            var completedColors = new HashSet<string>();
            int totalBalls = 0;
            
            foreach (var tube in gameState.Tubes)
            {
                if (tube.IsComplete && tube.Balls.Any())
                {
                    var color = tube.Balls.First().Color;
                    completedColors.Add(color);
                }
                
                totalBalls += tube.Balls.Count;
            }

            Console.WriteLine($"[VICTORY] GameState {gameState.Id} - {completedColors.Count} colors in {completedTubes} tubes, {gameState.MovesCount} moves");
            
            return new GameStateResult
            {
                IsGameOver = true,
                IsWon = true,
                EndReason = GameEndReason.Victory,
                Message = $"Parabéns! Você organizou {completedColors.Count} cores em {completedTubes} tubos!",
                AdditionalData = new Dictionary<string, object>
                {
                    { "CompletedTubes", completedTubes },
                    { "CompletedColors", completedColors.Count },
                    { "TotalBalls", totalBalls },
                    { "Moves", gameState.MovesCount }
                }
            };
        }

        /// <summary>
        /// Check if the game is lost (no valid moves available)
        /// </summary>
        public GameStateResult CheckDefeatCondition(GameState gameState)
        {
            var validMoves = FindAllValidMoves(gameState);
            
            if (!validMoves.Any())
            {
                // Additional check: Are there empty tubes that could be used?
                var emptyTubes = gameState.Tubes.Count(t => !t.Balls.Any());
                
                // If there are empty tubes but no moves, player made poor choices
                string message = emptyTubes > 0 
                    ? $"Sem movimentos válidos! Você tem {emptyTubes} tubos vazios não utilizados."
                    : "Sem movimentos válidos! Tente uma estratégia diferente.";

                return new GameStateResult
                {
                    IsGameOver = true,
                    IsWon = false,
                    EndReason = GameEndReason.NoMovesLeft,
                    Message = message,
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "EmptyTubes", emptyTubes },
                        { "MovesAtDefeat", gameState.MovesCount }
                    }
                };
            }

            return new GameStateResult 
            { 
                IsGameOver = false,
                AdditionalData = new Dictionary<string, object>
                {
                    { "ValidMovesCount", validMoves.Count }
                }
            };
        }

        /// <summary>
        /// Find all valid moves in current game state
        /// </summary>
        private List<(int fromTubeId, int toTubeId)> FindAllValidMoves(GameState gameState)
        {
            var validMoves = new List<(int, int)>();

            foreach (var fromTube in gameState.Tubes.Where(t => t.Balls.Any()))
            {
                var topBall = fromTube.Balls.OrderByDescending(b => b.Position).FirstOrDefault();
                if (topBall == null) continue;

                foreach (var toTube in gameState.Tubes.Where(t => t.Id != fromTube.Id))
                {
                    if (CanMoveBall(fromTube, toTube, topBall))
                    {
                        validMoves.Add((fromTube.Id, toTube.Id));
                    }
                }
            }

            return validMoves;
        }

        /// <summary>
        /// Check if a ball can be moved from one tube to another
        /// </summary>
        private bool CanMoveBall(Tube fromTube, Tube toTube, Ball ball)
        {
            // Can't move to full tube
            if (toTube.Balls.Count >= toTube.Capacity)
                return false;

            // Can always move to empty tube
            if (!toTube.Balls.Any())
                return true;

            // Can only move to tube with same color on top
            var topBallInTarget = toTube.Balls.OrderByDescending(b => b.Position).FirstOrDefault();
            return topBallInTarget?.Color == ball.Color;
        }

        /// <summary>
        /// Handle victory state
        /// </summary>
        private async Task HandleVictoryAsync(GameState gameState)
        {
            if (gameState.Status != GameStatus.Completed)
            {
                gameState.Status = GameStatus.Completed;
                gameState.EndTime = DateTime.UtcNow;
                
                // Calculate final score
                gameState.Score = _scoreService.CalculateGameScore(gameState);
                
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Handle defeat state
        /// </summary>
        private async Task HandleDefeatAsync(GameState gameState)
        {
            if (gameState.Status != GameStatus.Failed)
            {
                gameState.Status = GameStatus.Failed;
                gameState.EndTime = DateTime.UtcNow;
                gameState.Score = 0; // No score for failed games
                
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Get detailed game progress
        /// </summary>
        public async Task<Dictionary<string, object>> GetGameProgressAsync(int gameStateId)
        {
            var gameState = await GetGameStateAsync(gameStateId);
            if (gameState == null)
                return new Dictionary<string, object>();

            var completedTubes = 0;
            var mixedTubes = 0;
            var emptyTubes = 0;
            var totalColors = new HashSet<string>();

            foreach (var tube in gameState.Tubes)
            {
                var balls = tube.Balls.ToList();
                
                if (!balls.Any())
                {
                    emptyTubes++;
                }
                else
                {
                    var colors = balls.Select(b => b.Color).Distinct().ToList();
                    colors.ForEach(c => totalColors.Add(c));
                    
                    if (colors.Count == 1 && balls.Count == tube.Capacity)
                    {
                        completedTubes++;
                    }
                    else if (colors.Count > 1)
                    {
                        mixedTubes++;
                    }
                }
            }

            var validMoves = FindAllValidMoves(gameState);
            var progressPercentage = (completedTubes * 100) / Math.Max(1, totalColors.Count);

            return new Dictionary<string, object>
            {
                { "CompletedTubes", completedTubes },
                { "MixedTubes", mixedTubes },
                { "EmptyTubes", emptyTubes },
                { "TotalColors", totalColors.Count },
                { "ValidMovesCount", validMoves.Count },
                { "MovesUsed", gameState.MovesCount },
                { "HintsUsed", gameState.HintsUsed },
                { "ProgressPercentage", progressPercentage },
                { "Status", gameState.Status.ToString() }
            };
        }

        /// <summary>
        /// Check if current move leads to a dead end
        /// </summary>
        public bool IsDeadEndMove(GameState gameState, int fromTubeId, int toTubeId)
        {
            // Simulate the move
            var simulatedState = SimulateMove(gameState, fromTubeId, toTubeId);
            if (simulatedState == null) return true;

            // Check if this leads to no valid moves
            var validMovesAfter = FindAllValidMoves(simulatedState);
            return !validMovesAfter.Any();
        }

        private GameState? SimulateMove(GameState original, int fromTubeId, int toTubeId)
        {
            // Create a deep copy and simulate the move
            // This is a simplified version - in production you'd want proper cloning
            try
            {
                var fromTube = original.Tubes.First(t => t.Id == fromTubeId);
                var toTube = original.Tubes.First(t => t.Id == toTubeId);
                var ball = fromTube.Balls.OrderByDescending(b => b.Position).FirstOrDefault();

                if (ball == null || !CanMoveBall(fromTube, toTube, ball))
                    return null;

                // Return the original state (we're just checking, not modifying)
                return original;
            }
            catch
            {
                return null;
            }
        }

        public async Task<GameState?> GetGameStateAsync(int gameStateId)
        {
            var stackTrace = new System.Diagnostics.StackTrace();
            var callingMethod = stackTrace.GetFrame(1)?.GetMethod()?.Name ?? "Unknown";
            var callingClass = stackTrace.GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
            
            Console.WriteLine($"[QUERY LOG] GetGameStateAsync called from {callingClass}.{callingMethod} for gameStateId: {gameStateId}");
            
            // Check cache first
            if (_gameStateCache.TryGetValue(gameStateId, out var cached))
            {
                if (DateTime.UtcNow - cached.cachedAt < _cacheExpiry)
                {
                    Console.WriteLine($"[CACHE HIT] Returning cached game state for gameStateId: {gameStateId}");
                    return cached.gameState;
                }
                else
                {
                    // Remove expired cache entry
                    _gameStateCache.Remove(gameStateId);
                    Console.WriteLine($"[CACHE EXPIRED] Removed expired cache for gameStateId: {gameStateId}");
                }
            }

            Console.WriteLine($"[DB QUERY] Executing database query for gameStateId: {gameStateId}");
            var gameState = await _context.GameStates
                .Include(gs => gs.Tubes)
                    .ThenInclude(t => t.Balls)
                .Include(gs => gs.Level)
                .AsNoTracking() // Optimize for read-only operations
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId);

            // Cache the result if found
            if (gameState != null)
            {
                _gameStateCache[gameStateId] = (gameState, DateTime.UtcNow);
                Console.WriteLine($"[CACHE STORED] Cached game state for gameStateId: {gameStateId}");
            }
            
            return gameState;
        }
        
        /// <summary>
        /// Gets a game state and verifies that the specified player has access to it
        /// </summary>
        /// <param name="gameStateId">The ID of the game state to retrieve</param>
        /// <param name="playerId">The ID of the player requesting access</param>
        /// <returns>The game state if authorized, null otherwise</returns>
        public async Task<GameState?> GetAuthorizedGameStateAsync(int gameStateId, int? playerId)
        {
            Console.WriteLine($"[AUTH CHECK] Verifying authorization for gameStateId: {gameStateId}, playerId: {playerId}");
            
            var gameState = await GetGameStateAsync(gameStateId);
            
            // If game state doesn't exist, return null
            if (gameState == null)
            {
                Console.WriteLine($"[AUTH FAIL] Game state {gameStateId} not found");
                return null;
            }
            
            // If no player ID is provided (anonymous user) and the game state has no owner, allow access
            if (playerId == null && !gameState.PlayerId.HasValue)
            {
                Console.WriteLine($"[AUTH SUCCESS] Anonymous access granted to unowned game state {gameStateId}");
                return gameState;
            }
            
            // If player ID matches the game state owner, allow access
            if (playerId.HasValue && playerId.Value == gameState.PlayerId)
            {
                Console.WriteLine($"[AUTH SUCCESS] Access granted to owner for game state {gameStateId}");
                return gameState;
            }
            
            // In all other cases, deny access
             Console.WriteLine($"[AUTH FAIL] Access denied for playerId: {playerId} to game state {gameStateId}");
             return null;
        }

        /// <summary>
        /// Invalidate cache for a specific game state (call after moves/changes)
        /// </summary>
        public void InvalidateGameStateCache(int gameStateId)
        {
            _gameStateCache.Remove(gameStateId);
            Console.WriteLine($"[CACHE INVALIDATED] GameStateId: {gameStateId}");
        }

        /// <summary>
        /// Clear all cached game states
        /// </summary>
        public void ClearCache()
        {
            _gameStateCache.Clear();
            Console.WriteLine($"[CACHE CLEARED] All game states removed from cache");
        }
    }
}