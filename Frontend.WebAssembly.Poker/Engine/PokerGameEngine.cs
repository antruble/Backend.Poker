using Backend.Poker.Domain.Entities;
using Backend.Poker.Domain.ValueObjects;
using Frontend.WebAssembly.Poker.Components.Pages;
using Frontend.WebAssembly.Poker.Services;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using static System.Net.WebRequestMethods;

namespace Frontend.WebAssembly.Poker.Engine
{
    public class PokerGameEngine : IDisposable
    {
        #region Fields
        private readonly GameStateService _gameStateService;

        private CancellationTokenSource? _gameLoopCts;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _isDisposed = false;

        // Game-hez szükséges adattagok
        private Game _game { get; set; }
        private Guid UserId { get; set; }


        //private Guid CurrentPlayerId { get; set; }
        private TaskCompletionSource<PlayerAction>? _playerActionTcs;

        private readonly HttpClient _http;
        private readonly ILogger<PokerGameEngine> _logger;
        private readonly Action _stateHasChangedCallback;
        #endregion
        
        #region Ctor
        public PokerGameEngine(Game game, HttpClient http, ILogger<PokerGameEngine> logger, Action stateHasChangedCallback, GameStateService gameStateService)
        {
            _game = game;
            _http = http;
            _logger = logger;
            _stateHasChangedCallback = stateHasChangedCallback;
            _gameStateService = gameStateService;

            UserId = GetUserId();
        }
        #endregion

        #region Methods

        #region     Game loop methods
        public void Start()
        {
            if (_gameLoopCts is not null)
                throw new InvalidOperationException("Game loop már fut!");

            _logger.LogInformation($"Engine elindítva..");
            _gameLoopCts = new CancellationTokenSource();
            _ = GameLoopAsync(_gameLoopCts.Token); // Fire and forget (nem várunk rá)
        }
        public async Task StopAsync()
        {
            if (_gameLoopCts is null) return;
            _gameLoopCts.Cancel();

            try
            {
                await Task.Delay(500);
            }
            finally
            {
                _gameLoopCts.Dispose();
                _gameLoopCts = null;
            }
        }
        private async Task GameLoopAsync(CancellationToken token)
        {
            var interval = TimeSpan.FromSeconds(3); // 3 másodpercenként frissül
            
            while (!token.IsCancellationRequested)
            {
                var startTime = DateTime.UtcNow;
                _logger.LogInformation($"Új loop indult: {startTime}");
                try
                {
                    // Szemafor megakadályozza a párhuzamos futást
                    await _semaphore.WaitAsync(token);
                    _logger.LogInformation($"Új beengedett szál: {DateTime.UtcNow} gameaction: {_game.CurrentGameAction}");
                    // Frissítési logika: játék állapot, bot akciók, UI frissítés stb.
                    await UpdateGameStateAsync(token);
                }
                catch (OperationCanceledException)
                {
                    // Normális leállás (CancellationToken)
                    _logger.LogInformation("A játékloop megszakítva!");
                    break;
                }
                catch (Exception ex)
                {
                    // Egyéb kivételek kezelése
                    _logger.LogError($"Kivétel történt a _game loop-ban: {ex.Message}");
                }
                finally
                {
                    // Mindig engedd el a szemafort
                    if (_semaphore.CurrentCount == 0)
                        _semaphore.Release();
                }

                // Pontos időzítés: figyelj, hogy pontos legyen az időköz!
                var elapsed = DateTime.UtcNow - startTime;
                var delay = interval - elapsed;

                if (delay > TimeSpan.Zero)
                {
                    try
                    {
                        await Task.Delay(delay, token);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("A játékloop megszakítva várakozás alatt!");
                        break;
                    }
                }
            }

            _logger.LogInformation("Game loop leállt.");
        }
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _semaphore?.Dispose();
            _gameLoopCts?.Dispose();

            _isDisposed = true;
        }
        #endregion

        #region     Game engine methods
        private async Task UpdateGameStateAsync(CancellationToken token)
        {
            if (_game.CurrentHand is null)
                throw new Exception("Hiba az UpdateGameStateAsync metódusban: a _game.CurrentHand null!");

            switch (_game.CurrentGameAction)
            {
                case GameActions.Waiting:
                    _logger.LogInformation($"GameAction is Waiting: ...");
                    break;
                case GameActions.DealingCards:
                    _logger.LogInformation($"GameAction is DealingCards: várunk 2 mp-t");
                    await Task.Delay(2000, token);
                    await SendCardsHasDealedStatusAsync(token);
                    break;
                case GameActions.PlayerAction:
                    _logger.LogInformation($"GameAction is PlayerAction: játékos action generálása és küldése..");
                    await HandlePlayerActionGameActionAsync( token);
                    break;
                case GameActions.ShowOff:
                    _logger.LogInformation($"GameAction is ShowOff: várunk 3 mp-t");
                    await Task.Delay(3000, token);

                    var winners = await _http.GetFromJsonAsync<ICollection<Winner>>($"getwinners?handid={_game.CurrentHand.Id}") ?? throw new Exception();
                    _gameStateService.SetWinners(winners);
                    _logger.LogInformation($"Vége a handnek, a nyertesek: {string.Join(", ", winners!.Select(p => p.Player.Name))}");
                    await Task.Delay(4000, token);
                    await _http.PostAsync($"startnewhand?gameId={_game.Id}", null, token);
                    break;
                default:
                    break;
            }

            // Játék frissítése
            await UpdateGameAsync(token);

        }
        private async Task SendCardsHasDealedStatusAsync(CancellationToken token)
        {
            await _http.PostAsync($"cardshasdealed?gameId={_game.Id}", null, token);
        }
        private async Task HandlePlayerActionGameActionAsync(CancellationToken token)
        {
            var currentPlayer = _game.Players?.First(p => p.PlayerStatus == PlayerStatus.PlayersTurn);

            if (currentPlayer is null)
            {
                _logger.LogWarning($"Nem jön senki");
                return;
            }

            if (currentPlayer.PlayerStatus == PlayerStatus.Folded || currentPlayer.PlayerStatus == PlayerStatus.AllIn)
                return;

            _gameStateService.UpdateCurrentPlayerId(_game.Players.First(p => p.PlayerStatus == PlayerStatus.PlayersTurn).Id);

            _logger.LogInformation($"PlayerAction: a soron következő játékos: {currentPlayer.Name}");
            if (currentPlayer.IsBot)
            {
                _logger.LogInformation($"PlayerAction: mivel a soron következő játékos bot, ezért várunk 2mp-t..");
                await Task.Delay(2000, token);
            }

            if (currentPlayer.IsBot)
                await HandleBotActionsAsync(currentPlayer, token);
            else
                await HandleUserActionsAsync(token);

        }
        private async Task HandleUserActionsAsync(CancellationToken token)
        {
            try
            {
                if (_playerActionTcs == null)
                    _playerActionTcs = new TaskCompletionSource<PlayerAction>();

                _logger.LogInformation($"PlayerAction.HandlePlayerActionGameActionAsync: játékos action-re várás..");

                // Timeout beépítése: ha 30 másodperc alatt nem érkezik akció, defaultként Fold-ot választ
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), token);
                var completedTask = await Task.WhenAny(_playerActionTcs.Task, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    _logger.LogWarning("Player action timed out. Defaulting to Fold.");
                    _playerActionTcs.TrySetResult(new PlayerAction(PlayerActionType.Fold, null, DateTime.UtcNow));
                }

                var action = await _playerActionTcs.Task;

                _logger.LogInformation($"PlayerAction.HandlePlayerActionGameActionAsync: a játékos action-je: {action.ActionType} érték: {action.Amount}");
                await _http.PostAsJsonAsync($"processaction?gameId={_game.Id}&playerId={UserId}", action, token);
                _playerActionTcs = null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error.PlayerAction.HandleUserActionsAsync: {ex.Message}");
                return;
            }
        }
        private async Task HandleBotActionsAsync(Player bot, CancellationToken token)
        {
            try
            {
                _logger.LogInformation($"PlayerAction.HandlePlayerActionGameActionAsync: {bot.Name} bot action generálása elkezdődőtt..");
                var action = await _http.GetFromJsonAsync<PlayerAction>($"generatebotaction?gameId={_game.Id}&botId={bot.Id}", token)
                        ?? throw new NullReferenceException("Error.PlayerAction.HandlePlayerActionGameActionAsync: a generált action null");

                _logger.LogInformation($"PlayerAction.HandlePlayerActionGameActionAsync: a generált action: {action.ActionType} érték: {action.Amount}. Action küldése a szervernek feldolgozásra..");

                await _http.PostAsJsonAsync($"processaction?gameId={_game.Id}&playerId={bot.Id}", action, token);
                _logger.LogInformation($"PlayerAction.HandlePlayerActionGameActionAsync: generált action feldolgozva");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error.PlayerAction.HandleBotActionsAsync: {ex.Message}");
                return;
            }
        }
        public void RecordPlayerAction(PlayerAction action) => _playerActionTcs?.TrySetResult(action);
        private async Task UpdateGameAsync(CancellationToken token)
        {
            _game = await _http.GetFromJsonAsync<Game>($"getgame?gameId={_game.Id}", token)
                   ?? throw new NullReferenceException($"Null a game a UpdateGameStateAsync-ban");

            _gameStateService.UpdateGame(_game);
            _stateHasChangedCallback?.Invoke();
        }
        #endregion

        #region     Utilities
        public Guid GetCurrentPlayersId() =>
                _game.Players.First(p => p.PlayerStatus == PlayerStatus.PlayersTurn).Id;

        public Guid GetUserId() { return _game.Players.First(p => !p.IsBot).Id; }

        #endregion



        #endregion
    }
}
