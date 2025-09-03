using JogoBolinha.Models.Game;

namespace JogoBolinha.Models.ViewModels
{
    public class GameViewModel
    {
        public GameState GameState { get; set; } = null!;
        public Level Level { get; set; } = null!;
        public List<Tube> Tubes { get; set; } = new List<Tube>();
        public bool CanUndo { get; set; }
        public bool IsCompleted { get; set; }
        public int RemainingHints { get; set; }
        public int RemainingAdvancedHints { get; set; }
        public string FormattedDuration
        {
            get
            {
                if (GameState.Duration.HasValue)
                {
                    var duration = GameState.Duration.Value;
                    return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
                }
                var currentDuration = DateTime.UtcNow - GameState.StartTime;
                return $"{currentDuration.Minutes:D2}:{currentDuration.Seconds:D2}";
            }
        }
    }
}