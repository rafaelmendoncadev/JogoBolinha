using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JogoBolinha.Data;
using JogoBolinha.Services;
using JogoBolinha.Models.Game;
using Microsoft.AspNetCore.Authorization;

namespace JogoBolinha.Controllers
{
    [Authorize] // Proteger endpoints administrativos
    public class AdminController : Controller
    {
        private readonly GameDbContext _context;
        private readonly LevelGeneratorServiceV2 _levelGeneratorV2;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            GameDbContext context, 
            LevelGeneratorServiceV2 levelGeneratorV2,
            ILogger<AdminController> logger)
        {
            _context = context;
            _levelGeneratorV2 = levelGeneratorV2;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> LevelManagement()
        {
            // Verificar se o usuário é admin (simplificado - você pode melhorar isso)
            if (!User.Identity?.Name?.Contains("admin") == true)
            {
                return Forbid();
            }

            var levels = await _context.Levels
                .OrderBy(l => l.Number)
                .Select(l => new
                {
                    l.Id,
                    l.Number,
                    l.Colors,
                    l.Tubes,
                    l.BallsPerColor,
                    l.Difficulty,
                    EmptyTubes = l.Tubes - l.Colors, // Estimativa de tubos vazios
                    IsSolvable = l.Tubes >= l.Colors + 2, // Verificação básica de solvabilidade
                    ActiveGames = _context.GameStates.Count(gs => gs.LevelId == l.Id && gs.Status == GameStatus.InProgress)
                })
                .ToListAsync();

            ViewBag.Levels = levels;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegenerateLevel(int levelNumber)
        {
            // Verificar se o usuário é admin
            if (!User.Identity?.Name?.Contains("admin") == true)
            {
                return Forbid();
            }

            try
            {
                var existingLevel = await _context.Levels
                    .FirstOrDefaultAsync(l => l.Number == levelNumber);

                if (existingLevel == null)
                {
                    return Json(new { success = false, message = "Nível não encontrado" });
                }

                // Deletar game states relacionados
                var gameStates = await _context.GameStates
                    .Where(gs => gs.LevelId == existingLevel.Id)
                    .ToListAsync();

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

                // Remover nível antigo
                _context.Levels.Remove(existingLevel);

                // Gerar novo nível com V2
                var newLevel = _levelGeneratorV2.GenerateLevel(levelNumber);
                _context.Levels.Add(newLevel);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Nível {Number} regenerado com sucesso pelo admin {User}", 
                    levelNumber, User.Identity?.Name);

                return Json(new 
                { 
                    success = true, 
                    message = $"Nível {levelNumber} regenerado com sucesso!",
                    level = new 
                    {
                        newLevel.Number,
                        newLevel.Colors,
                        newLevel.Tubes,
                        newLevel.BallsPerColor,
                        EmptyTubes = newLevel.Tubes - newLevel.Colors
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao regenerar nível {Number}", levelNumber);
                return Json(new { success = false, message = $"Erro: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RegenerateAllLevels()
        {
            // Verificar se o usuário é admin
            if (!User.Identity?.Name?.Contains("admin") == true)
            {
                return Forbid();
            }

            try
            {
                _logger.LogWarning("Iniciando regeneração completa de níveis pelo admin {User}", User.Identity?.Name);

                // Deletar todos os game states
                var allGameStates = await _context.GameStates.ToListAsync();
                foreach (var gameState in allGameStates)
                {
                    var moves = _context.GameMoves.Where(gm => gm.GameStateId == gameState.Id);
                    var balls = _context.Balls.Where(b => b.GameStateId == gameState.Id);
                    var tubes = _context.Tubes.Where(t => t.GameStateId == gameState.Id);

                    _context.GameMoves.RemoveRange(moves);
                    _context.Balls.RemoveRange(balls);
                    _context.Tubes.RemoveRange(tubes);
                }
                _context.GameStates.RemoveRange(allGameStates);

                // Deletar todos os níveis
                _context.Levels.RemoveRange(_context.Levels);
                await _context.SaveChangesAsync();

                // Regenerar 50 níveis
                for (int i = 1; i <= 50; i++)
                {
                    var level = _levelGeneratorV2.GenerateLevel(i);
                    _context.Levels.Add(level);
                }

                await _context.SaveChangesAsync();

                _logger.LogWarning("50 níveis regenerados com sucesso pelo admin {User}", User.Identity?.Name);

                return Json(new 
                { 
                    success = true, 
                    message = "Todos os 50 níveis foram regenerados com sucesso!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao regenerar todos os níveis");
                return Json(new { success = false, message = $"Erro: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RegenerateProblematicLevels()
        {
            // Verificar se o usuário é admin
            if (!User.Identity?.Name?.Contains("admin") == true)
            {
                return Forbid();
            }

            try
            {
                // Identificar níveis problemáticos
                var problematicLevels = await _context.Levels
                    .Where(l => 
                        l.Tubes < l.Colors + 2 || // Regra de solvabilidade
                        (l.Number <= 10 && l.Colors > 4) || // Níveis fáceis não devem ter mais de 4 cores
                        (l.Number == 4 && l.Tubes < 5) || // Nível 4 específico
                        (l.Number <= 3 && l.Colors > 2) // Níveis 1-3 devem ter no máximo 2 cores
                    )
                    .ToListAsync();

                if (!problematicLevels.Any())
                {
                    return Json(new { success = true, message = "Nenhum nível problemático encontrado!" });
                }

                _logger.LogWarning("Regenerando {Count} níveis problemáticos", problematicLevels.Count);

                foreach (var level in problematicLevels)
                {
                    // Deletar game states relacionados
                    var gameStates = await _context.GameStates
                        .Where(gs => gs.LevelId == level.Id)
                        .ToListAsync();

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

                    // Remover nível antigo
                    _context.Levels.Remove(level);

                    // Gerar novo nível
                    var newLevel = _levelGeneratorV2.GenerateLevel(level.Number);
                    _context.Levels.Add(newLevel);
                }

                await _context.SaveChangesAsync();

                return Json(new 
                { 
                    success = true, 
                    message = $"{problematicLevels.Count} níveis problemáticos foram regenerados!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao regenerar níveis problemáticos");
                return Json(new { success = false, message = $"Erro: {ex.Message}" });
            }
        }
    }
}
