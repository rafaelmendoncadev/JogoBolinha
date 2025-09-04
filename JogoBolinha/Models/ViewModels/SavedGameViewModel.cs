namespace JogoBolinha.Models.ViewModels
{
    public class SavedGameViewModel
    {
        public int GameStateId { get; set; }
        public int LevelNumber { get; set; }
        public int MovesCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastActivity { get; set; }
        public int ProgressPercentage { get; set; }
        public int CompletedTubes { get; set; }
        public int TotalTubes { get; set; }
        public TimeSpan TimePlayed { get; set; }
        public int HintsUsed { get; set; }
    }
}