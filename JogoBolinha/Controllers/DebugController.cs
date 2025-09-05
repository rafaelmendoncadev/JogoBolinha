using JogoBolinha.Data;
using JogoBolinha.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JogoBolinha.Controllers
{
    public class DebugController : Controller
    {
        private readonly GameDbContext _context;
        private readonly LevelGeneratorServiceV2 _levelGenerator;

        public DebugController(GameDbContext context, LevelGeneratorServiceV2 levelGenerator)
        {
            _context = context;
            _levelGenerator = levelGenerator;
        }

        [HttpGet]
        public async Task<IActionResult> RegenerateLevel(int levelNumber)
        {
            try
            {
                // Delete existing level and related data
                var existingLevel = await _context.Levels.FirstOrDefaultAsync(l => l.Number == levelNumber);
                if (existingLevel != null)
                {
                    // Delete game states for this level
                    var gameStates = await _context.GameStates.Where(gs => gs.LevelId == existingLevel.Id).ToListAsync();
                    foreach (var gameState in gameStates)
                    {
                        // Delete related data
                        var moves = _context.GameMoves.Where(gm => gm.GameStateId == gameState.Id);
                        var balls = _context.Balls.Where(b => b.GameStateId == gameState.Id);
                        var tubes = _context.Tubes.Where(t => t.GameStateId == gameState.Id);
                        
                        _context.GameMoves.RemoveRange(moves);
                        _context.Balls.RemoveRange(balls);
                        _context.Tubes.RemoveRange(tubes);
                    }
                    _context.GameStates.RemoveRange(gameStates);
                    
                    // Delete the level itself
                    _context.Levels.Remove(existingLevel);
                    await _context.SaveChangesAsync();
                }

                // Generate new level with fixed logic
                var newLevel = _levelGenerator.GenerateLevel(levelNumber);
                _context.Levels.Add(newLevel);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Level {levelNumber} regenerated successfully",
                    levelData = new {
                        Number = newLevel.Number,
                        Colors = newLevel.Colors,
                        Tubes = newLevel.Tubes,
                        BallsPerColor = newLevel.BallsPerColor,
                        Difficulty = newLevel.Difficulty.ToString(),
                        IsSolvable = newLevel.Tubes >= newLevel.Colors + 2
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"Error regenerating level {levelNumber}: {ex.Message}" 
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> RegenerateEarlyLevels()
        {
            var results = new List<object>();
            
            for (int level = 1; level <= 10; level++)
            {
                try
                {
                    // Delete existing level
                    var existingLevel = await _context.Levels.FirstOrDefaultAsync(l => l.Number == level);
                    if (existingLevel != null)
                    {
                        var gameStates = await _context.GameStates.Where(gs => gs.LevelId == existingLevel.Id).ToListAsync();
                        foreach (var gameState in gameStates)
                        {
                            var moves = _context.GameMoves.Where(gm => gm.GameStateId == gameState.Id);
                            var balls = _context.Balls.Where(b => b.GameStateId == gameState.Id);
                            var tubes = _context.Tubes.Where(t => t.GameStateId == gameState.Id);
                            
                            _context.GameMoves.RemoveRange(moves);
                            _context.Balls.RemoveRange(balls);
                            _context.Tubes.RemoveRange(tubes);
                        }
                        _context.GameStates.RemoveRange(gameStates);
                        _context.Levels.Remove(existingLevel);
                    }

                    // Generate new level
                    var newLevel = _levelGenerator.GenerateLevel(level);
                    _context.Levels.Add(newLevel);
                    await _context.SaveChangesAsync();

                    results.Add(new {
                        Level = level,
                        Success = true,
                        Colors = newLevel.Colors,
                        Tubes = newLevel.Tubes,
                        BallsPerColor = newLevel.BallsPerColor,
                        IsSolvable = newLevel.Tubes >= newLevel.Colors + 2
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new {
                        Level = level,
                        Success = false,
                        Error = ex.Message
                    });
                }
            }

            return Json(new { 
                success = true, 
                message = "Early levels regenerated",
                results = results 
            });
        }

        [HttpGet]
        public async Task<IActionResult> CheckLevel(int levelNumber)
        {
            var level = await _context.Levels.FirstOrDefaultAsync(l => l.Number == levelNumber);
            if (level == null)
            {
                return Json(new { success = false, message = "Level not found" });
            }

            return Json(new {
                success = true,
                levelData = new {
                    Number = level.Number,
                    Colors = level.Colors,
                    Tubes = level.Tubes,
                    BallsPerColor = level.BallsPerColor,
                    Difficulty = level.Difficulty.ToString(),
                    IsSolvable = level.Tubes >= level.Colors + 2,
                    SolvabilityRule = $"tubes({level.Tubes}) >= colors({level.Colors}) + 2 = {level.Tubes >= level.Colors + 2}",
                    InitialState = level.InitialState
                }
            });
        }
    }
}