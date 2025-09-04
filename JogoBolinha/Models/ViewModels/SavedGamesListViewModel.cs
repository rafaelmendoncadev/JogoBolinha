using JogoBolinha.Models.Game;

namespace JogoBolinha.Models.ViewModels
{
    public class SavedGamesListViewModel
    {
        public List<GameState> Games { get; set; } = new List<GameState>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string Filter { get; set; } = "all";
        public int TotalGames { get; set; }
    }
}