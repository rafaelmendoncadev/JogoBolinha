using JogoBolinha.Models.Game;

namespace JogoBolinha.Models.ViewModels
{
    public class HomeViewModel
    {
        public bool HasContinueGame { get; set; }
        public GameState? ContinueGameState { get; set; }
    }
}