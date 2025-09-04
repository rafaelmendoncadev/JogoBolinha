using JogoBolinha.Models.Game;

namespace JogoBolinha.Models.ViewModels
{
    public class HomeViewModel
    {
        public bool HasContinueGame { get; set; }
        public GameState? ContinueGameState { get; set; }
        
        public List<SavedGameViewModel> SavedGames { get; set; } = new List<SavedGameViewModel>();
        public bool HasSavedGames { get; set; }
        public SavedGameViewModel? MostRecentGame { get; set; }
    }
}