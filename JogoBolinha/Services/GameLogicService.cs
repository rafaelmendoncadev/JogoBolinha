using JogoBolinha.Models.Game;
using JogoBolinha.Data;
using Microsoft.EntityFrameworkCore;

namespace JogoBolinha.Services
{
    public class GameLogicService
    {
        private readonly GameDbContext _context;
        
        public GameLogicService(GameDbContext context)
        {
            _context = context;
        }
        
        public async Task<bool> ValidateMoveAsync(int gameStateId, int fromTubeId, int toTubeId)
        {
            var gameState = await GetGameStateWithTubesAsync(gameStateId);
            if (gameState == null || gameState.Status != GameStatus.InProgress)
                return false;
            
            var fromTube = gameState.Tubes.FirstOrDefault(t => t.Id == fromTubeId);
            var toTube = gameState.Tubes.FirstOrDefault(t => t.Id == toTubeId);
            
            if (fromTube == null || toTube == null || fromTubeId == toTubeId)
                return false;
            
            var topBall = fromTube.GetTopBall();
            if (topBall == null)
                return false;
            
            return toTube.CanReceiveBall(topBall);
        }
        
        public async Task<GameMove?> ExecuteMoveAsync(int gameStateId, int fromTubeId, int toTubeId)
        {
            if (!await ValidateMoveAsync(gameStateId, fromTubeId, toTubeId))
                return null;
            
            var gameState = await GetGameStateWithTubesAsync(gameStateId);
            var fromTube = gameState!.Tubes.First(t => t.Id == fromTubeId);
            var toTube = gameState.Tubes.First(t => t.Id == toTubeId);
            
            var ball = fromTube.GetTopBall()!;
            
            // Move the ball
            ball.TubeId = toTubeId;
            ball.Position = toTube.Balls.Count;
            
            // Create move record
            var move = new GameMove
            {
                GameStateId = gameStateId,
                FromTubeId = fromTubeId,
                ToTubeId = toTubeId,
                BallColor = ball.Color,
                MoveNumber = gameState.MovesCount + 1
            };
            
            gameState.MovesCount++;
            gameState.Moves.Add(move);
            
            // Check for victory
            if (IsGameWon(gameState))
            {
                gameState.Status = GameStatus.Completed;
                gameState.EndTime = DateTime.UtcNow;
                gameState.Score = await CalculateScoreAsync(gameState);
            }
            
            _context.GameMoves.Add(move);
            await _context.SaveChangesAsync();
            
            return move;
        }
        
        public async Task<bool> UndoMoveAsync(int gameStateId)
        {
            var gameState = await GetGameStateWithTubesAsync(gameStateId);
            if (gameState == null || !gameState.CanUndo)
                return false;
            
            var lastMove = gameState.Moves
                .Where(m => !m.IsUndone)
                .OrderByDescending(m => m.MoveNumber)
                .FirstOrDefault();
            
            if (lastMove == null)
                return false;
            
            // Find the ball that was moved
            var ball = gameState.Balls.FirstOrDefault(b => 
                b.TubeId == lastMove.ToTubeId && 
                b.Color == lastMove.BallColor);
            
            if (ball == null)
                return false;
            
            var fromTube = gameState.Tubes.First(t => t.Id == lastMove.FromTubeId);
            var toTube = gameState.Tubes.First(t => t.Id == lastMove.ToTubeId);
            
            // Move ball back
            ball.TubeId = lastMove.FromTubeId;
            ball.Position = fromTube.Balls.Count;
            
            // Update move as undone
            lastMove.IsUndone = true;
            gameState.MovesCount--;
            
            // Reset game status if it was completed
            if (gameState.Status == GameStatus.Completed)
            {
                gameState.Status = GameStatus.InProgress;
                gameState.EndTime = null;
                gameState.Score = 0;
            }
            
            await _context.SaveChangesAsync();
            return true;
        }
        
        public bool IsGameWon(GameState gameState)
        {
            return gameState.Tubes.All(tube => tube.IsEmpty || tube.IsComplete);
        }
        
        public async Task<int> CalculateScoreAsync(GameState gameState)
        {
            const int baseScore = 100;
            const int efficiencyBonus = 10;
            const int speedBonusPoints = 50;
            const int hintPenalty = 20;
            
            int score = baseScore;
            
            // Efficiency bonus (unused moves)
            var minimumMoves = gameState.Level.MinimumMoves;
            var unusedMoves = Math.Max(0, minimumMoves + 10 - gameState.MovesCount); // Allow 10 extra moves
            score += unusedMoves * efficiencyBonus;
            
            // Speed bonus (completed in less than 2 minutes)
            if (gameState.Duration.HasValue && gameState.Duration.Value.TotalMinutes < 2)
            {
                score += speedBonusPoints;
            }
            
            // Hint penalty
            score -= gameState.HintsUsed * hintPenalty;
            
            return Math.Max(0, score); // Ensure score is never negative
        }
        
        public async Task<List<int>?> GetHintAsync(int gameStateId, bool isAdvanced = false)
        {
            var gameState = await GetGameStateWithTubesAsync(gameStateId);
            if (gameState == null || gameState.Status != GameStatus.InProgress)
                return null;
            
            // Simple hint: find any valid move
            for (int fromIndex = 0; fromIndex < gameState.Tubes.Count; fromIndex++)
            {
                var fromTube = gameState.Tubes.ElementAt(fromIndex);
                if (fromTube.IsEmpty) continue;
                
                for (int toIndex = 0; toIndex < gameState.Tubes.Count; toIndex++)
                {
                    if (fromIndex == toIndex) continue;
                    
                    var toTube = gameState.Tubes.ElementAt(toIndex);
                    var topBall = fromTube.GetTopBall();
                    
                    if (topBall != null && toTube.CanReceiveBall(topBall))
                    {
                        var hint = new List<int> { fromTube.Id, toTube.Id };
                        
                        // For advanced hint, try to find a sequence that leads to completion
                        if (isAdvanced)
                        {
                            var nextMove = FindNextLogicalMove(gameState, fromTube.Id, toTube.Id);
                            if (nextMove.HasValue)
                            {
                                hint.Add(nextMove.Value.fromTubeId);
                                hint.Add(nextMove.Value.toTubeId);
                            }
                        }
                        
                        // Increment hints used counter
                        gameState.HintsUsed++;
                        await _context.SaveChangesAsync();
                        
                        return hint;
                    }
                }
            }
            
            return null;
        }
        
        private (int fromTubeId, int toTubeId)? FindNextLogicalMove(GameState gameState, int currentFromId, int currentToId)
        {
            // Simple logic: look for moves that complete tubes or free up space
            foreach (var tube in gameState.Tubes.Where(t => !t.IsEmpty && t.Id != currentFromId && t.Id != currentToId))
            {
                var topBall = tube.GetTopBall();
                if (topBall == null) continue;
                
                // Look for a tube where this ball can create a complete color stack
                var targetTube = gameState.Tubes.FirstOrDefault(t => 
                    t.Id != tube.Id && 
                    !t.IsFull && 
                    (t.IsEmpty || (t.GetTopBallColor() == topBall.Color && t.Balls.All(b => b.Color == topBall.Color))));
                
                if (targetTube != null)
                {
                    return (tube.Id, targetTube.Id);
                }
            }
            
            return null;
        }
        
        private async Task<GameState?> GetGameStateWithTubesAsync(int gameStateId)
        {
            return await _context.GameStates
                .Include(gs => gs.Tubes)
                    .ThenInclude(t => t.Balls)
                .Include(gs => gs.Level)
                .Include(gs => gs.Moves)
                .Include(gs => gs.Balls)
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId);
        }
    }
}