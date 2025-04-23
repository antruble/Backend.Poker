using Backend.Poker.Domain.Entities;

namespace Frontend.WebAssembly.Poker.Services
{
    public class GameStateService
    {
        private readonly ILogger<GameStateService> _logger;
        public GameStateService(ILogger<GameStateService> logger)
        {
            _logger = logger;
        }
        // Az OnChange esemény értesíti a komponenseket a frissítésről.
        public event Action? OnChange;

        // Tárolja az aktuális játék állapotát.
        private Game? _currentGame;
        private ICollection<Winner>? _winners;

        public Game? CurrentGame
        {
            get => _currentGame;
            private set
            {
                _currentGame = value;
                NotifyStateChanged();
            }
        }
        public ICollection<Winner>? Winners
        {
            get => _winners;
            private set
            {
                _winners = value;
                NotifyStateChanged();
            }
        }

        public void SetWinners(ICollection<Winner> winners)
        {
            Winners = winners;
        }

        public void ResetWinners()
        {
            Winners = [];
        }
        // Az állapot frissítését végző metódus.
        //public void UpdateGame(Game newGame) => CurrentGame = newGame;
        public void UpdateGame(Game newGame) => CurrentGame = newGame;

        // Az esemény kiváltása, ha az állapot változik.
        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
