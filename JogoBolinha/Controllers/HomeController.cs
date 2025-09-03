using System.Diagnostics;
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
        // Get recent game state for continue option
        var recentGameState = await _context.GameStates
            .Include(gs => gs.Level)
            .Where(gs => gs.Status == Models.Game.GameStatus.InProgress)
            .OrderByDescending(gs => gs.StartTime)
            .FirstOrDefaultAsync();

        var viewModel = new HomeViewModel
        {
            HasContinueGame = recentGameState != null,
            ContinueGameState = recentGameState
        };

        return View(viewModel);
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
}
