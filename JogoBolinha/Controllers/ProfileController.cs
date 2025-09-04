using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JogoBolinha.Data;
using JogoBolinha.Models.Game;
using JogoBolinha.Models.ViewModels;

namespace JogoBolinha.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly GameDbContext _context;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(GameDbContext context, ILogger<ProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> SavedGames(int page = 1, string filter = "all")
        {
            var playerId = GetCurrentPlayerId();
            if (!playerId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var query = _context.GameStates
                .Include(gs => gs.Level)
                .Where(gs => gs.PlayerId == playerId);

            // Apply filters
            query = filter switch
            {
                "inprogress" => query.Where(gs => gs.Status == GameStatus.InProgress),
                "completed" => query.Where(gs => gs.Status == GameStatus.Completed),
                _ => query
            };

            const int pageSize = 20;
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            // Ensure page is within valid range
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var games = await query
                .OrderByDescending(gs => gs.LastModified ?? gs.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new SavedGamesListViewModel
            {
                Games = games,
                CurrentPage = page,
                TotalPages = totalPages,
                Filter = filter,
                TotalGames = totalItems
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSavedGame(int gameStateId)
        {
            var playerId = GetCurrentPlayerId();
            if (!playerId.HasValue)
            {
                return Json(new { success = false, message = "Usuário não autenticado" });
            }

            var gameState = await _context.GameStates
                .FirstOrDefaultAsync(gs => gs.Id == gameStateId && gs.PlayerId == playerId);

            if (gameState != null)
            {
                _context.GameStates.Remove(gameState);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Jogo não encontrado" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMultipleSavedGames([FromBody] List<int> gameStateIds)
        {
            var playerId = GetCurrentPlayerId();
            if (!playerId.HasValue)
            {
                return Json(new { success = false, message = "Usuário não autenticado" });
            }

            var gamesToDelete = await _context.GameStates
                .Where(gs => gameStateIds.Contains(gs.Id) && gs.PlayerId == playerId)
                .ToListAsync();

            if (gamesToDelete.Any())
            {
                _context.GameStates.RemoveRange(gamesToDelete);
                await _context.SaveChangesAsync();
                return Json(new { success = true, deleted = gamesToDelete.Count });
            }

            return Json(new { success = false, message = "Nenhum jogo encontrado" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOldSavedGames()
        {
            var playerId = GetCurrentPlayerId();
            if (!playerId.HasValue)
            {
                return Json(new { success = false, message = "Usuário não autenticado" });
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            var oldGames = await _context.GameStates
                .Where(gs => gs.PlayerId == playerId && 
                            gs.Status == GameStatus.InProgress &&
                            (gs.LastModified ?? gs.StartTime) < cutoffDate)
                .ToListAsync();

            if (oldGames.Any())
            {
                _context.GameStates.RemoveRange(oldGames);
                await _context.SaveChangesAsync();
                return Json(new { success = true, deleted = oldGames.Count });
            }

            return Json(new { success = false, message = "Nenhum jogo antigo encontrado" });
        }

        private int? GetCurrentPlayerId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int playerId))
            {
                return playerId;
            }
            return null;
        }
    }
}