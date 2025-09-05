using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JogoBolinha.Models;
using JogoBolinha.Data;
using JogoBolinha.Models.ViewModels;

namespace JogoBolinha.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly GameDbContext _context;

    public HomeController(ILogger<HomeController> logger, GameDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new HomeViewModel();
        
        if (User.Identity?.IsAuthenticated == true)
        {
            var playerId = GetCurrentPlayerId();
            
            if (playerId.HasValue)
            {
                // Get recent game state for continue option
                var recentGameState = await _context.GameStates
                    .Include(gs => gs.Level)
                    .Where(gs => gs.Status == Models.Game.GameStatus.InProgress && gs.PlayerId == playerId)
                    .OrderByDescending(gs => gs.LastModified ?? gs.StartTime)
                    .FirstOrDefaultAsync();
                
                model.HasContinueGame = recentGameState != null;
                model.ContinueGameState = recentGameState;
                
                // Get saved games list
                var savedGames = await _context.GameStates
                    .Include(gs => gs.Level)
                    .Include(gs => gs.Tubes)
                        .ThenInclude(t => t.Balls)
                    .Where(gs => gs.PlayerId == playerId && gs.Status == Models.Game.GameStatus.InProgress)
                    .OrderByDescending(gs => gs.LastModified ?? gs.StartTime)
                    .Take(5)
                    .ToListAsync();
                
                model.SavedGames = savedGames.Select(gs => new SavedGameViewModel
                {
                    GameStateId = gs.Id,
                    LevelNumber = gs.Level.Number,
                    MovesCount = gs.MovesCount,
                    StartTime = gs.StartTime,
                    LastActivity = gs.LastModified ?? gs.StartTime,
                    ProgressPercentage = CalculateProgress(gs),
                    CompletedTubes = CountCompletedTubes(gs),
                    TotalTubes = gs.Tubes.Count,
                    TimePlayed = (gs.LastModified ?? DateTime.UtcNow) - gs.StartTime,
                    HintsUsed = gs.HintsUsed
                }).ToList();
                
                model.HasSavedGames = model.SavedGames.Any();
                model.MostRecentGame = model.SavedGames.FirstOrDefault();
            }
        }
        
        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
    
    private int CalculateProgress(Models.Game.GameState gameState)
    {
        if (gameState.Tubes == null || !gameState.Tubes.Any())
            return 0;
        
        int completedTubes = CountCompletedTubes(gameState);
        int totalTubes = gameState.Tubes.Count(t => t.Balls.Any());
        
        if (totalTubes == 0)
            return 0;
        
        return (completedTubes * 100) / totalTubes;
    }
    
    private int CountCompletedTubes(Models.Game.GameState gameState)
    {
        if (gameState.Tubes == null)
            return 0;
        
        return gameState.Tubes.Count(tube => 
        {
            if (!tube.Balls.Any())
                return false;
            
            var balls = tube.Balls.OrderBy(b => b.Position).ToList();
            if (balls.Count != tube.Capacity)
                return false;
            
            return balls.All(b => b.Color == balls[0].Color);
        });
    }
}
