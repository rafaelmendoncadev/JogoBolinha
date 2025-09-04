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
            gameState.LastModified = DateTime.UtcNow;
            
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
            gameState.LastModified = DateTime.UtcNow;
            
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
        
        public async Task<bool> UndoMultipleMovesAsync(int gameStateId, int movesToUndo = 1)
        {
            var gameState = await GetGameStateWithTubesAsync(gameStateId);
            if (gameState == null || !gameState.CanUndo)
                return false;
            
            // Limit undo to maximum of 3 moves as per PRD
            movesToUndo = Math.Min(movesToUndo, 3);
            movesToUndo = Math.Min(movesToUndo, gameState.Moves.Count(m => !m.IsUndone));
            
            for (int i = 0; i < movesToUndo; i++)
            {
                var lastMove = gameState.Moves
                    .Where(m => !m.IsUndone)
                    .OrderByDescending(m => m.MoveNumber)
                    .FirstOrDefault();
                
                if (lastMove == null)
                    break;
                
                // Find the ball that was moved (get the top ball in the destination tube)
                var toTube = gameState.Tubes.First(t => t.Id == lastMove.ToTubeId);
                var ball = toTube.Balls
                    .Where(b => b.Color == lastMove.BallColor)
                    .OrderByDescending(b => b.Position)
                    .FirstOrDefault();
                
                if (ball == null)
                    continue;
                
                var fromTube = gameState.Tubes.First(t => t.Id == lastMove.FromTubeId);
                
                // Move ball back
                ball.TubeId = lastMove.FromTubeId;
                
                // Recalculate positions in both tubes
                RecalculateTubePositions(fromTube, gameState.Balls);
                RecalculateTubePositions(toTube, gameState.Balls);
                
                // Update move as undone
                lastMove.IsUndone = true;
                gameState.MovesCount--;
            }
            
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
        
        public async Task<bool> RedoMoveAsync(int gameStateId, int movesToRedo = 1)
        {
            var gameState = await GetGameStateWithTubesAsync(gameStateId);
            if (gameState == null)
                return false;
            
            var undoneMoves = gameState.Moves
                .Where(m => m.IsUndone)
                .OrderBy(m => m.MoveNumber)
                .Take(movesToRedo)
                .ToList();
            
            if (!undoneMoves.Any())
                return false;
            
            foreach (var move in undoneMoves)
            {
                // Validate the move is still possible
                if (!await ValidateMoveAsync(gameStateId, move.FromTubeId, move.ToTubeId))
                    continue;
                
                var fromTube = gameState.Tubes.First(t => t.Id == move.FromTubeId);
                var toTube = gameState.Tubes.First(t => t.Id == move.ToTubeId);
                var ball = fromTube.Balls
                    .Where(b => b.Color == move.BallColor)
                    .OrderByDescending(b => b.Position)
                    .FirstOrDefault();
                
                if (ball == null)
                    continue;
                
                // Move the ball
                ball.TubeId = move.ToTubeId;
                
                // Recalculate positions
                RecalculateTubePositions(fromTube, gameState.Balls);
                RecalculateTubePositions(toTube, gameState.Balls);
                
                // Update move as not undone
                move.IsUndone = false;
                gameState.MovesCount++;
            }
            
            // Check for victory after redo
            if (IsGameWon(gameState))
            {
                gameState.Status = GameStatus.Completed;
                gameState.EndTime = DateTime.UtcNow;
                gameState.Score = await CalculateScoreAsync(gameState);
            }
            
            await _context.SaveChangesAsync();
            return true;
        }
        
        private void RecalculateTubePositions(Tube tube, ICollection<Ball> allBalls)
        {
            var tubeBalls = allBalls
                .Where(b => b.TubeId == tube.Id)
                .OrderBy(b => b.Position)
                .ToList();
            
            for (int i = 0; i < tubeBalls.Count; i++)
            {
                tubeBalls[i].Position = i;
            }
        }

        public bool IsGameWon(GameState gameState)
        {
            return gameState.Tubes.All(tube => tube.IsEmpty || tube.IsComplete);
        }
        
        public bool IsGameLost(GameState gameState)
        {
            // A game is lost if no valid moves are possible
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
                        return false; // Found a valid move
                    }
                }
            }
            
            return true; // No valid moves found
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
        
        public async Task<List<int>?> GetSmartHintAsync(int gameStateId, bool isAdvanced = false)
        {
            var gameState = await GetGameStateWithTubesAsync(gameStateId);
            if (gameState == null || gameState.Status != GameStatus.InProgress)
                return null;
            
            var hint = GetPriorityMove(gameState, isAdvanced);
            
            if (hint != null)
            {
                // Increment hints used counter
                gameState.HintsUsed++;
                await _context.SaveChangesAsync();
            }
            
            return hint;
        }
        
        private List<int>? GetPriorityMove(GameState gameState, bool isAdvanced)
        {
            // Priority 1: Complete a tube (move to same color stack)
            var completionMove = FindCompletionMove(gameState);
            if (completionMove != null) return completionMove;
            
            // Priority 2: Free up buried balls of the same color
            var freeingMove = FindFreeingMove(gameState);
            if (freeingMove != null) return freeingMove;
            
            // Priority 3: Create homogeneous stacks
            var stackingMove = FindStackingMove(gameState);
            if (stackingMove != null) return stackingMove;
            
            // Priority 4: Move to empty tube to create space
            var emptyTubeMove = FindEmptyTubeMove(gameState);
            if (emptyTubeMove != null) return emptyTubeMove;
            
            // Fallback: Any valid move
            return FindAnyValidMove(gameState, isAdvanced);
        }
        
        private List<int>? FindCompletionMove(GameState gameState)
        {
            foreach (var fromTube in gameState.Tubes.Where(t => !t.IsEmpty))
            {
                var topBall = fromTube.GetTopBall();
                if (topBall == null) continue;
                
                // Look for a tube that has the same color and can be completed
                var targetTube = gameState.Tubes.FirstOrDefault(t =>
                    t.Id != fromTube.Id &&
                    !t.IsFull &&
                    !t.IsEmpty &&
                    t.GetTopBallColor() == topBall.Color &&
                    t.Balls.All(b => b.Color == topBall.Color));
                
                if (targetTube != null)
                {
                    return new List<int> { fromTube.Id, targetTube.Id };
                }
            }
            
            return null;
        }
        
        private List<int>? FindFreeingMove(GameState gameState)
        {
            foreach (var fromTube in gameState.Tubes.Where(t => !t.IsEmpty))
            {
                var topBall = fromTube.GetTopBall();
                if (topBall == null) continue;
                
                // Check if there are balls of the same color buried in this tube
                var buriedSameColorBalls = fromTube.Balls
                    .Where(b => b.Color == topBall.Color && b.Position < fromTube.Balls.Max(x => x.Position))
                    .Any();
                
                if (buriedSameColorBalls)
                {
                    // Look for a place to move the top ball
                    var targetTube = gameState.Tubes.FirstOrDefault(t =>
                        t.Id != fromTube.Id &&
                        t.CanReceiveBall(topBall) &&
                        (t.IsEmpty || t.GetTopBallColor() == topBall.Color));
                    
                    if (targetTube != null)
                    {
                        return new List<int> { fromTube.Id, targetTube.Id };
                    }
                }
            }
            
            return null;
        }
        
        private List<int>? FindStackingMove(GameState gameState)
        {
            foreach (var fromTube in gameState.Tubes.Where(t => !t.IsEmpty))
            {
                var topBall = fromTube.GetTopBall();
                if (topBall == null) continue;
                
                // Look for a tube with the same color on top
                var targetTube = gameState.Tubes.FirstOrDefault(t =>
                    t.Id != fromTube.Id &&
                    !t.IsFull &&
                    !t.IsEmpty &&
                    t.GetTopBallColor() == topBall.Color);
                
                if (targetTube != null)
                {
                    return new List<int> { fromTube.Id, targetTube.Id };
                }
            }
            
            return null;
        }
        
        private List<int>? FindEmptyTubeMove(GameState gameState)
        {
            var emptyTube = gameState.Tubes.FirstOrDefault(t => t.IsEmpty);
            if (emptyTube == null) return null;
            
            // Find a tube with mixed colors (not homogeneous)
            var mixedTube = gameState.Tubes.FirstOrDefault(t =>
                !t.IsEmpty &&
                !t.IsComplete &&
                t.Balls.Select(b => b.Color).Distinct().Count() > 1);
            
            if (mixedTube != null)
            {
                return new List<int> { mixedTube.Id, emptyTube.Id };
            }
            
            return null;
        }
        
        private List<int>? FindAnyValidMove(GameState gameState, bool isAdvanced)
        {
            // Simple fallback: find any valid move
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
                        
                        // For advanced hint, try to find a sequence
                        if (isAdvanced)
                        {
                            var nextMove = FindNextLogicalMove(gameState, fromTube.Id, toTube.Id);
                            if (nextMove.HasValue)
                            {
                                hint.Add(nextMove.Value.fromTubeId);
                                hint.Add(nextMove.Value.toTubeId);
                            }
                        }
                        
                        return hint;
                    }
                }
            }
            
            return null;
        }
        
        private (int fromTubeId, int toTubeId)? FindNextLogicalMove(GameState gameState, int currentFromId, int currentToId)
        {
            // After the current move, find the next logical move
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