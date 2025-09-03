using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JogoBolinha.Data;
using JogoBolinha.Models.Game;
using JogoBolinha.Services;
using JogoBolinha.Models.ViewModels;
using System.Text.Json;

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
            await EnsureLevelsExistAsync();
            
            var levels = await _context.Levels
                .OrderBy(l => l.Number)
                .Take(10)
                .ToListAsync();
                
            return View(levels);
        }

        public async Task<IActionResult> Play(int levelNumber = 1)
        {
            var level = await GetOrCreateLevelAsync(levelNumber);
            var gameState = await CreateNewGameStateAsync(level);
            
            var viewModel = await CreateGameViewModelAsync(gameState);
            return View("Game", viewModel);
        }
        
        public async Task<IActionResult> Continue(int gameStateId)
        {
            var gameState = await _context.GameStates
                .Include(gs => gs.Level)
                .Include(gs => gs.Tubes).ThenInclude(t => t.Balls)
                .Include(gs => gs.Moves)
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId);
                
            if (gameState == null)
                return RedirectToAction("Index");
                
            var viewModel = await CreateGameViewModelAsync(gameState);
            return View("Game", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> MakeMove(int gameStateId, int fromTubeId, int toTubeId)
        {
            var move = await _gameLogicService.ExecuteMoveAsync(gameStateId, fromTubeId, toTubeId);
            
            if (move == null)
            {
                return Json(new { success = false, message = "Movimento inválido" });
            }

            // Check game state after move
            var stateCheck = await _gameStateManager.CheckGameStateAsync(gameStateId);
            
            // Get updated game state
            var gameState = await _context.GameStates
                .Include(gs => gs.Level)
                .Include(gs => gs.Tubes).ThenInclude(t => t.Balls)
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId);

            // If game ended, complete the session
            if (stateCheck.IsGameOver && stateCheck.IsWon && gameState!.Score == 0)
            {
                await _gameSessionService.CompleteGameSessionAsync(gameStateId, true);
                
                // Refresh game state to get updated score
                gameState = await _context.GameStates
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
                    movesCount = gameState!.MovesCount,
                    score = gameState.Score,
                    status = gameState.Status.ToString(),
                    isWon = stateCheck.IsWon,
                    isGameOver = stateCheck.IsGameOver,
                    endReason = stateCheck.EndReason.ToString(),
                    message = stateCheck.Message
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
        public async Task<IActionResult> UndoMove(int gameStateId)
        {
            var success = await _gameLogicService.UndoMoveAsync(gameStateId);
            
            if (!success)
            {
                return Json(new { success = false, message = "Não é possível desfazer o movimento" });
            }

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
            var type = hintType.ToLower() switch
            {
                "advanced" => HintType.Advanced,
                "strategic" => HintType.Strategic,
                "tutorial" => HintType.Tutorial,
                _ => HintType.Simple
            };

            var hintResult = await _hintService.GetHintAsync(gameStateId, type);
            
            if (hintResult == null || !hintResult.TubeIds.Any())
            {
                return Json(new { success = false, message = "Nenhuma dica disponível" });
            }

            return Json(new 
            { 
                success = true, 
                hint = hintResult.TubeIds, 
                explanation = hintResult.Explanation,
                score = hintResult.Score,
                type = hintResult.Type.ToString()
            });
        }
        
        [HttpPost]
        public async Task<IActionResult> UndoMultipleMoves(int gameStateId, int movesToUndo = 1)
        {
            var success = await _gameLogicService.UndoMultipleMovesAsync(gameStateId, movesToUndo);
            
            if (!success)
            {
                return Json(new { success = false, message = "Não é possível desfazer os movimentos" });
            }

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
            var gameState = await _context.GameStates
                .Include(gs => gs.Level)
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId);
                
            if (gameState == null)
                return Json(new { success = false, message = "Jogo não encontrado" });

            var newGameState = await CreateNewGameStateAsync(gameState.Level);
            
            return Json(new { success = true, newGameStateId = newGameState.Id });
        }

        private async Task<GameViewModel> CreateGameViewModelAsync(GameState gameState)
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

        private async Task<GameState> CreateNewGameStateAsync(Level level)
        {
            var gameState = new GameState
            {
                LevelId = level.Id,
                Level = level,
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
            return await _context.GameStates
                .Include(gs => gs.Level)
                .Include(gs => gs.Tubes).ThenInclude(t => t.Balls)
                .FirstAsync(gs => gs.Id == gameState.Id);
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