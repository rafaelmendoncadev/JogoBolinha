using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JogoBolinha.Data;
using JogoBolinha.Models.Game;
using JogoBolinha.Services;
using JogoBolinha.Models.ViewModels;
using System.Text.Json;
using System.Security.Claims;

namespace JogoBolinha.Controllers
{
    public class GameController : Controller
    {
        private readonly GameDbContext _context;
        private readonly GameLogicService _gameLogicService;
        private readonly LevelGeneratorService _levelGeneratorService;
        private readonly ScoreCalculationService _scoreCalculationService;
        private readonly GameSessionService _gameSessionService;
        private readonly HintService _hintService;
        private readonly GameStateManager _gameStateManager;

        public GameController(GameDbContext context, GameLogicService gameLogicService, 
            LevelGeneratorService levelGeneratorService, ScoreCalculationService scoreCalculationService,
            GameSessionService gameSessionService, HintService hintService, GameStateManager gameStateManager)
        {
            _context = context;
            _gameLogicService = gameLogicService;
            _levelGeneratorService = levelGeneratorService;
            _scoreCalculationService = scoreCalculationService;
            _gameSessionService = gameSessionService;
            _hintService = hintService;
            _gameStateManager = gameStateManager;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                Console.WriteLine($"[DEBUG] Index action started");
                
                await EnsureLevelsExistAsync();
                Console.WriteLine($"[DEBUG] EnsureLevelsExistAsync completed");
                
                var levels = await _context.Levels
                    .OrderBy(l => l.Number)
                    .Take(10)
                    .ToListAsync();
                    
                Console.WriteLine($"[DEBUG] Found {levels.Count} levels");
                
                return View(levels);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in Index action: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<IActionResult> Play(int levelNumber = 1)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Play action started for level {levelNumber}");
                
                await EnsureLevelsExistAsync();
                Console.WriteLine($"[DEBUG] EnsureLevelsExistAsync completed");
                
                var level = await GetOrCreateLevelAsync(levelNumber);
                Console.WriteLine($"[DEBUG] Level obtained: {level?.Id} - {level?.Number}");
                
                var playerId = GetCurrentPlayerId();
                Console.WriteLine($"[DEBUG] PlayerId: {playerId}");
                
                var gameState = await CreateNewGameStateAsync(level, playerId);
                Console.WriteLine($"[DEBUG] GameState created: {gameState?.Id}");
                
                var viewModel = CreateGameViewModel(gameState);
                Console.WriteLine($"[DEBUG] ViewModel created, returning view");
                
                return View("Game", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in Play action: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        
        public async Task<IActionResult> Continue(int gameStateId)
        {
            var playerId = GetCurrentPlayerId();
            var gameState = await _context.GameStates
                .Include(gs => gs.Level)
                .Include(gs => gs.Tubes).ThenInclude(t => t.Balls)
                .Include(gs => gs.Moves)
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId && (playerId == null || gs.PlayerId == playerId));
                
            if (gameState == null)
                return NotFound();
                
            var viewModel = CreateGameViewModel(gameState);
            return View("Game", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> MakeMove(int gameStateId, int fromTubeId, int toTubeId)
        {
            var playerId = GetCurrentPlayerId();
            var gameState = await _context.GameStates
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId && (playerId == null || gs.PlayerId == playerId));
            
            if (gameState == null)
                return Json(new { success = false, message = "Jogo não encontrado" });

            var move = await _gameLogicService.ExecuteMoveAsync(gameStateId, fromTubeId, toTubeId);
            
            if (move == null)
            {
                return Json(new { success = false, message = "Movimento inválido" });
            }

            // Invalidate cache after move
            _gameStateManager.InvalidateGameStateCache(gameStateId);

            // Check game state after move
            var stateCheck = await _gameStateManager.CheckGameStateAsync(gameStateId);
            
            // Get updated game state
            var updatedGameState = await _context.GameStates
                .Include(gs => gs.Level)
                .Include(gs => gs.Tubes).ThenInclude(t => t.Balls)
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId);

            // If game ended, complete the session
            if (stateCheck.IsGameOver && stateCheck.IsWon)
            {
                await _gameSessionService.CompleteGameSessionAsync(gameStateId, true);
                
                // Refresh game state to get updated score
                updatedGameState = await _context.GameStates
                    .Include(gs => gs.Level)
                    .Include(gs => gs.Tubes).ThenInclude(t => t.Balls)
                    .FirstOrDefaultAsync(gs => gs.Id == gameStateId);
            }
            
            var result = new
            {
                success = true,
                move = new
                {
                    id = move.Id,
                    fromTubeId = move.FromTubeId,
                    toTubeId = move.ToTubeId,
                    ballColor = move.BallColor
                },
                gameState = new
                {
                    movesCount = updatedGameState!.MovesCount,
                    score = updatedGameState.Score,
                    status = updatedGameState.Status.ToString(),
                    isWon = stateCheck.IsWon,
                    isGameOver = stateCheck.IsGameOver,
                    endReason = stateCheck.EndReason.ToString(),
                    message = stateCheck.Message
                },
                tubes = updatedGameState.Tubes.Select(t => new
                {
                    id = t.Id,
                    position = t.Position,
                    balls = t.Balls.OrderBy(b => b.Position).Select(b => new
                    {
                        id = b.Id,
                        color = b.Color,
                        position = b.Position
                    })
                })
            };

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> UndoMove(int gameStateId)
        {
            var playerId = GetCurrentPlayerId();
            var gameStateExists = await _context.GameStates
                .AnyAsync(gs => gs.Id == gameStateId && (playerId == null || gs.PlayerId == playerId));
            
            if (!gameStateExists)
                return Json(new { success = false, message = "Jogo não encontrado" });

            var success = await _gameLogicService.UndoMoveAsync(gameStateId);
            
            if (!success)
            {
                return Json(new { success = false, message = "Não é possível desfazer o movimento" });
            }

            // Invalidate cache after undo
            _gameStateManager.InvalidateGameStateCache(gameStateId);

            var gameState = await _context.GameStates
                .Include(gs => gs.Level)
                .Include(gs => gs.Tubes).ThenInclude(t => t.Balls)
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId);

            var result = new
            {
                success = true,
                gameState = new
                {
                    movesCount = gameState!.MovesCount,
                    score = gameState.Score,
                    status = gameState.Status.ToString()
                },
                tubes = gameState.Tubes.Select(t => new
                {
                    id = t.Id,
                    position = t.Position,
                    balls = t.Balls.OrderBy(b => b.Position).Select(b => new
                    {
                        id = b.Id,
                        color = b.Color,
                        position = b.Position
                    })
                })
            };

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> GetHint(int gameStateId, string hintType = "simple")
        {
            var playerId = GetCurrentPlayerId();
            var gameStateExists = await _context.GameStates
                .AnyAsync(gs => gs.Id == gameStateId && (playerId == null || gs.PlayerId == playerId));
            
            if (!gameStateExists)
                return Json(new { success = false, message = "Jogo não encontrado" });

            var type = hintType.ToLower() switch
            {
                "advanced" => HintType.Advanced,
                "strategic" => HintType.Strategic,
                "tutorial" => HintType.Tutorial,
                _ => HintType.Simple
            };

            var hint = await _hintService.GetHintAsync(gameStateId, type);
            
            if (hint == null || !hint.TubeIds.Any())
            {
                return Json(new { success = false, message = "Nenhuma dica disponível" });
            }

            return Json(new 
            { 
                success = true, 
                hint = hint.TubeIds, 
                explanation = hint.Explanation,
                score = hint.Score,
                type = hint.Type.ToString()
            });
        }
        
        [HttpPost]
        public async Task<IActionResult> AutoSave(int gameStateId)
        {
            var playerId = GetCurrentPlayerId();
            var gameState = await _context.GameStates
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId && 
                                        (playerId == null || gs.PlayerId == playerId));
            
            if (gameState != null)
            {
                gameState.LastModified = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Json(new { success = true, timestamp = gameState.LastModified });
            }
            
            return Json(new { success = false, message = "Jogo não encontrado" });
        }
        
        [HttpPost]
        public async Task<IActionResult> UndoMultipleMoves(int gameStateId, int movesToUndo = 1)
        {
            var playerId = GetCurrentPlayerId();
            var gameStateExists = await _context.GameStates
                .AnyAsync(gs => gs.Id == gameStateId && (playerId == null || gs.PlayerId == playerId));
            
            if (!gameStateExists)
                return Json(new { success = false, message = "Jogo não encontrado" });

            var success = await _gameLogicService.UndoMultipleMovesAsync(gameStateId, movesToUndo);
            
            if (!success)
            {
                return Json(new { success = false, message = "Não é possível desfazer os movimentos" });
            }

            // Invalidate cache after multiple undo
            _gameStateManager.InvalidateGameStateCache(gameStateId);

            var gameState = await _context.GameStates
                .Include(gs => gs.Level)
                .Include(gs => gs.Tubes).ThenInclude(t => t.Balls)
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId);

            var result = new
            {
                success = true,
                gameState = new
                {
                    movesCount = gameState!.MovesCount,
                    score = gameState.Score,
                    status = gameState.Status.ToString()
                },
                tubes = gameState.Tubes.Select(t => new
                {
                    id = t.Id,
                    position = t.Position,
                    balls = t.Balls.OrderBy(b => b.Position).Select(b => new
                    {
                        id = b.Id,
                        color = b.Color,
                        position = b.Position
                    })
                })
            };

            return Json(result);
        }
        
        [HttpPost]
        public async Task<IActionResult> RedoMove(int gameStateId, int movesToRedo = 1)
        {
            var playerId = GetCurrentPlayerId();
            var gameStateExists = await _context.GameStates
                .AnyAsync(gs => gs.Id == gameStateId && (playerId == null || gs.PlayerId == playerId));
            
            if (!gameStateExists)
                return Json(new { success = false, message = "Jogo não encontrado" });

            var success = await _gameLogicService.RedoMoveAsync(gameStateId, movesToRedo);
            
            if (!success)
            {
                return Json(new { success = false, message = "Não é possível refazer os movimentos" });
            }

            var gameState = await _context.GameStates
                .Include(gs => gs.Level)
                .Include(gs => gs.Tubes).ThenInclude(t => t.Balls)
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId);

            // Check game state after redo
            var stateCheck = await _gameStateManager.CheckGameStateAsync(gameStateId);
            
            var result = new
            {
                success = true,
                gameState = new
                {
                    movesCount = gameState!.MovesCount,
                    score = gameState.Score,
                    status = gameState.Status.ToString(),
                    isWon = stateCheck.IsWon,
                    isGameOver = stateCheck.IsGameOver
                },
                tubes = gameState.Tubes.Select(t => new
                {
                    id = t.Id,
                    position = t.Position,
                    balls = t.Balls.OrderBy(b => b.Position).Select(b => new
                    {
                        id = b.Id,
                        color = b.Color,
                        position = b.Position
                    })
                })
            };

            return Json(result);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetScoreBreakdown(int gameStateId)
        {
            var breakdown = await _gameSessionService.GetScoreBreakdownAsync(gameStateId);
            
            return Json(new 
            { 
                success = true, 
                breakdown = new
                {
                    baseScore = breakdown.BaseScore,
                    efficiencyBonus = breakdown.EfficiencyBonus,
                    speedBonus = breakdown.SpeedBonus,
                    difficultyMultiplier = breakdown.DifficultyMultiplier,
                    hintPenalty = breakdown.HintPenalty,
                    perfectGameBonus = breakdown.PerfectGameBonus,
                    totalScore = breakdown.TotalScore,
                    breakdownText = breakdown.GetBreakdownText()
                }
            });
        }
        
        [HttpPost]
        public async Task<IActionResult> CheckGameState(int gameStateId)
        {
            var stateCheck = await _gameStateManager.CheckGameStateAsync(gameStateId);
            var progress = await _gameStateManager.GetGameProgressAsync(gameStateId);
            
            return Json(new 
            { 
                success = true,
                isGameOver = stateCheck.IsGameOver,
                isWon = stateCheck.IsWon,
                endReason = stateCheck.EndReason.ToString(),
                message = stateCheck.Message,
                progress = progress,
                additionalData = stateCheck.AdditionalData
            });
        }
        
        [HttpGet]
        public async Task<IActionResult> GetGameProgress(int gameStateId)
        {
            var progress = await _gameStateManager.GetGameProgressAsync(gameStateId);
            
            return Json(new 
            { 
                success = true,
                progress = progress
            });
        }

        [HttpPost]
        public async Task<IActionResult> RestartLevel(int gameStateId)
        {
            var playerId = GetCurrentPlayerId();
            var gameState = await _context.GameStates
                .Include(gs => gs.Level)
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId && (playerId == null || gs.PlayerId == playerId));
                
            if (gameState == null)
                return Json(new { success = false, message = "Jogo não encontrado" });

            // Invalidate cache for old game state
            _gameStateManager.InvalidateGameStateCache(gameStateId);
            
            var newGameState = await CreateNewGameStateAsync(gameState.Level, playerId);
            
            return Json(new { success = true, newGameStateId = newGameState.Id });
        }

        private GameViewModel CreateGameViewModel(GameState gameState)
        {
            return new GameViewModel
            {
                GameState = gameState,
                Tubes = gameState.Tubes.OrderBy(t => t.Position).ToList(),
                Level = gameState.Level,
                CanUndo = gameState.CanUndo,
                IsCompleted = gameState.IsCompleted,
                RemainingHints = 3 - gameState.HintsUsed,
                RemainingAdvancedHints = 1 - (gameState.HintsUsed > 3 ? gameState.HintsUsed - 3 : 0)
            };
        }

        private async Task<GameState> CreateNewGameStateAsync(Level level, int? playerId = null)
        {
            var gameState = new GameState
            {
                LevelId = level.Id,
                Level = level,
                PlayerId = playerId,
                Status = GameStatus.InProgress,
                StartTime = DateTime.UtcNow
            };

            _context.GameStates.Add(gameState);
            await _context.SaveChangesAsync();

            // Parse initial state and create tubes/balls
            var initialState = JsonSerializer.Deserialize<JsonElement>(level.InitialState);
            var tubesData = initialState.GetProperty("Tubes").EnumerateArray();

            foreach (var tubeData in tubesData)
            {
                var tubeId = tubeData.GetProperty("Id").GetInt32();
                var tube = new Tube
                {
                    GameStateId = gameState.Id,
                    Position = tubeId,
                    Capacity = 4
                };
                _context.Tubes.Add(tube);
                await _context.SaveChangesAsync();

                var ballsData = tubeData.GetProperty("Balls").EnumerateArray();
                foreach (var ballData in ballsData)
                {
                    var ball = new Ball
                    {
                        GameStateId = gameState.Id,
                        TubeId = tube.Id,
                        Color = ballData.GetProperty("Color").GetString()!,
                        Position = ballData.GetProperty("Position").GetInt32()
                    };
                    _context.Balls.Add(ball);
                }
            }

            await _context.SaveChangesAsync();

            // Reload with relations
            var reloadedGameState = await _context.GameStates
                .Include(gs => gs.Level)
                .Include(gs => gs.Tubes).ThenInclude(t => t.Balls)
                .FirstAsync(gs => gs.Id == gameState.Id);
                
            // Invalidate any existing cache for this new game state
            _gameStateManager.InvalidateGameStateCache(gameState.Id);
            
            return reloadedGameState;
        }

        private async Task<Level> GetOrCreateLevelAsync(int levelNumber)
        {
            var level = await _context.Levels.FirstOrDefaultAsync(l => l.Number == levelNumber);
            
            if (level == null)
            {
                level = _levelGeneratorService.GenerateLevel(levelNumber);
                _context.Levels.Add(level);
                await _context.SaveChangesAsync();
            }
            
            return level;
        }

        private int? GetCurrentPlayerId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }

        private async Task EnsureLevelsExistAsync()
        {
            var existingLevelsCount = await _context.Levels.CountAsync();
            
            if (existingLevelsCount < 10)
            {
                for (int i = existingLevelsCount + 1; i <= 10; i++)
                {
                    var level = _levelGeneratorService.GenerateLevel(i);
                    _context.Levels.Add(level);
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}