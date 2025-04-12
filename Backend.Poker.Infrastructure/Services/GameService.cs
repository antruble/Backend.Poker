using Backend.Poker.Application.Interfaces;
using Backend.Poker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backend.Poker.Domain.IRepositories;
using Backend.Poker.Domain.Events;
using Backend.Poker.Domain.Services;
using Backend.Poker.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Backend.Poker.Infrastructure.Services
{
    public class GameService : IGameService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPlayerService _playerService;
        private readonly IPokerHandEvaluator _handEvaluator;
        private readonly ILogger<GameService> _logger;
        //private readonly IDomainEventPublisherService _eventPublisherService;

        private const string DeckMemoryCacheKey = "DECK-CACHE-KEY-";
        private readonly IMemoryCache _cache;

        public GameService(
            IUnitOfWork unitOfWork,
            IPlayerService playerService,
            IPokerHandEvaluator handEvaluator,
            ILogger<GameService> logger,

            IMemoryCache cache
            //IDomainEventPublisherService eventPublisherService
            )
        {
            _unitOfWork = unitOfWork;
            _playerService = playerService;
            _handEvaluator = handEvaluator;
            _logger = logger;

            _cache = cache;
            //_eventPublisherService = eventPublisherService;
        }

        public async Task<Game> GetGameAsync()
        {
            try
            {
                var games = await _unitOfWork.Games.GetAllAsync(filter: g => g.Status != GameStatus.Completed, includeProperties: "Players,CurrentHand");

                var game =
                    games.ToList().OrderBy(g => g.Id).FirstOrDefault()
                    ?? await StartNewGameAsync(5);

                game.Players = [.. game.Players.OrderBy(p => p.Seat)];

                return game;
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }
        public async Task<Game?> GetGameByIdAsync(Guid gameId)
        {
            var game = await _unitOfWork.Games.GetByIdAsync(gameId);
            if (game is not null)
                game.Players = game.Players.OrderBy(p => p.Seat).ToList();

            return game;
        }
        public async Task<Game> StartNewGameAsync(int numOfBots, string playerName = "Player")
        {
            var players = await _playerService.GetPlayersAsync(numOfBots, playerName);
            Game game = new(players);
            game.StartNewHand();

            await _unitOfWork.Games.AddAsync(game);
            await _unitOfWork.SaveChangesAsync();

            return game;
        }
        public async Task CardsDealingActionFinished(Guid gameId)
        {
            var game = await _unitOfWork.Games.GetByIdAsync(gameId)
                       ?? throw new Exception($"Nem található játék az alábbi azonosítóval: {gameId}");
            if (!game.CurrentHand!.SkipActions)
                game.CurrentGameAction = GameActions.PlayerAction;
            else
                game.CurrentGameAction = GameActions.DealNextRound;
            await _unitOfWork.SaveChangesAsync();
        }
        public async Task<Game> ProcessActionAsync(Game game, Player player, PlayerAction action)
        {
            _logger.LogInformation($"LEFUTOTT A ProcessActionAsync");
            if (game.GetCurrentPlayersId() != player.Id)
                throw new Exception("Error.ProcessActionAsync: nem a soron következő játékos hívta meg a függvényt. függvényt meghívó játékos: {player.Id} jelenlegi játékos: {game.GetCurrentPlayersId()} ");
            if (game.CurrentHand!.SkipActions)
                throw new Exception($"Error.ProcessActionAsync: a skip actions nem kéne, hogy true legyen");
            
            await HandleActionAsync(game, player, action);
            if (game.CurrentGameAction != GameActions.ShowOff)
            {
                
                // bezárölt kör -> vagy új kör, vagy vége a hand
                if (game.IsNextPlayerPivot())
                {
                    // River utolsó embere a hand vége
                    if (game.CurrentHand!.HandStatus == HandStatus.River)
                    {
                        game.CurrentGameAction = GameActions.DealNextRound;
                        //await ProcessFinishedHandAsync(game);
                    }
                    else // Még nem zárult be a hand, csak egy kör
                    {
                        _logger.LogInformation($"A következő játékos a pivot játékos, de még nincs river, ezért a körnek vége");
                        game.CurrentHand.Pot.CompleteRound();
                        game.Players
                            .Where(p => p.PlayerStatus == PlayerStatus.AllIn)
                            .ToList()
                            .ForEach(p => p.HasToRevealCards = true);

                        game.CurrentGameAction = GameActions.DealingCards;
                        game = await DealNextRound(game);
                    }
                }
                else // még nem zárult be a kör
                {
                    game.CurrentGameAction = GameActions.PlayerAction;
                    game.SwitchToTheNextPlayer(lastPlayer: player);
                }
                _logger.LogInformation($"Átváltva a következő játékosra (ha nem hand vége volt)");
            }

            // Mentjük a változtatásokat
            await _unitOfWork.SaveChangesAsync();
            return game;
        }
        public async Task<Game> DealNextRound(Game game)
        {
            if (game.CurrentHand!.HandStatus == HandStatus.River)
            {
                await ProcessFinishedHandAsync(game);
            }
            else
            {
                if (game.Players.Count(p => p.PlayerStatus == PlayerStatus.Waiting) < 2)
                    game.CurrentHand!.SkipActions = true;
                _logger.LogInformation($"Bezárult az előző kör, új kört osztunk. Előző kör: {game.CurrentHand!.HandStatus}");

                var deck = GetCurrentDeck(game);

                if (!game.CurrentHand.SkipActions)
                {
                    var playerId = game.SetRoundsFirstPlayerToCurrent();
                    game.SetCurrentPlayerToPivot(playerId);
                }

                game.CurrentHand.DealNextRound(deck!);
                game.CurrentGameAction = GameActions.DealingCards;
            }
            await _unitOfWork.Games.UpdateAsync(game);
            await _unitOfWork.Games.SaveChangesAsync();

            return game;

        }
        public async Task ProcessFinishedHandAsync(Game game)
        {
            _logger.LogInformation($"A következő játékos a pivot játékos, és river van, ezért a handnek vége");
            game.CurrentHand!.Pot.CreateSidePots();

            await CompleteHandAndSaveWinnersAsync(game);
            await EliminateChiplessPlayersAsync(game);

            game.CurrentGameAction = GameActions.ShowOff;
        }
        public async Task<Game> SetGameActionShowOff(Game game)
        {
            game.CurrentGameAction = GameActions.ShowOff;
            await _unitOfWork.Games.UpdateAsync(game);
            await _unitOfWork.Games.SaveChangesAsync();

            return game;
        }
        private async Task CompleteHandAndSaveWinnersAsync(Game game)
        {
            try
            {
                var winners = game.CurrentHand!.CompleteHand(_handEvaluator, game.Players);
                winners
                    .Join(game.Players,
                          winner => winner.PlayerId,
                          player => player.Id,
                          (winner, player) => new { Winner = winner, Player = player })
                    .ToList()
                    .ForEach(x => x.Player.AddChips(x.Winner.Pot));

                _logger.LogInformation($"Winners: {winners.ToString()}");
                _logger.LogInformation($"WinnersCount: {winners.Count}");
                await _unitOfWork.Winners.AddRangeAsync(winners.ToList());
                await _unitOfWork.Winners.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }

        private async Task EliminateChiplessPlayersAsync(Game game)
        {
            // Nem-bot játékosok esetében, ha chipje <= 0, akkor visszaállítjuk 1000-re
            foreach (var player in game.Players.Where(p => !p.IsBot && p.Chips <= 0).ToList())
                player.AddChips(1000);

            game.Players = game.Players.Where(p => p.Chips > 0).ToList();

            await _unitOfWork.Players.SaveChangesAsync();
        }

        public async Task StartNewHandAsync(Guid gameId)
        {
            var game = await _unitOfWork.Games.GetByIdAsync(gameId)
                        ?? throw new NullReferenceException($"Nem található game a megadott game ID-val. Game ID: {gameId}");

            var hand = game.StartNewHand();

            // Új hand-et a kártyák kiosztásával kezdjük
            game.CurrentGameAction = GameActions.DealingCards;

            await _unitOfWork.Hands.AddAsync(hand);
            await _unitOfWork.Games.SaveChangesAsync();
        }
        public async Task<List<Winner>> GetWinners(Guid handId)
        {
            var winners = await _unitOfWork.Winners.GetAllAsync(filter: w => w.HandId == handId);

            // Iterálunk a Winner objektumokon, és feltöltjük a Player property-t
            foreach (var winner in winners)
            {
                if (winner is null)
                    continue;

                // Feltételezzük, hogy a _unitOfWork.Players.GetByIdAsync(winner.PlayerId) visszaadja a teljes Player objektumot
                winner.Player = await _unitOfWork.Players.GetByIdAsync(winner.PlayerId) 
                                    ?? throw new NullReferenceException($"Nem találtam a playert");
            }

            return winners.ToList();
        }
        private Deck GetCurrentDeck(Game game)
        {
            if (!_cache.TryGetValue($"{DeckMemoryCacheKey}{game.CurrentHand!.Id}", out Deck? deck))
            {
                var drawnCards = game.Players.SelectMany(p => p.HoleCards).ToList();
                deck = game.CurrentHand.RestoreDeck(drawnCards) ?? throw new Exception("Nem lehet null ez itt");

                var cacheOptions = new MemoryCacheEntryOptions()
                                        .SetSlidingExpiration(TimeSpan.FromSeconds(120))
                                        .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                _cache.Set($"{DeckMemoryCacheKey}{game.CurrentHand.Id}", deck, cacheOptions);
            }
            return deck;
        }

        private async Task HandleActionAsync(Game game, Player player, PlayerAction action)
        {
            switch (action.ActionType)
            {
                case PlayerActionType.Fold:
                    player.Fold(); //TODO: ha fold, és emiatt hand vége
                    var activePlayerCount = game.Players.Count(p => p.PlayerStatus == PlayerStatus.Waiting);
                    if (activePlayerCount < 2)
                    {
                        var nonFoldedPlayerCount = game.Players.Count(p => p.PlayerStatus == PlayerStatus.Waiting || p.PlayerStatus == PlayerStatus.AllIn);
                        if (nonFoldedPlayerCount == 1)
                        {
                            await CompleteHandAndSaveWinnersAsync(game);
                            game.CurrentGameAction = GameActions.ShowOff;
                        }
                        else if (nonFoldedPlayerCount > 1)
                        {
                            game.CurrentHand!.SkipActions = true;
                        }
                    }
                    break;
                case PlayerActionType.Call:
                    HandleCallAction(game, player);
                    break;
                case PlayerActionType.Raise:
                    if (action.Amount is null || action.Amount <= 0)
                        throw new Exception("Raise volt null, vagy kisebb egyenlő 0 értékű amounttal");

                    HandleRaiseAction(game, player, (int)action.Amount);
                    break;
            }
        }
        private void HandleCallAction(Game game, Player player)
        { 
            var amount = game.CurrentHand!.Pot.GetCallAmountForPlayer(player.Id);

            if (amount == 0)
                return;

            HandleChipsDeduction(game, player, amount);

        }
        private void HandleRaiseAction(Game game, Player player, int amount)
        {
            HandleChipsDeduction(game, player, amount);
            game.SetCurrentPlayerToPivot(player.Id);
        }

        private void HandleChipsDeduction(Game game, Player player, int amount)
        {

            if (amount >= player.Chips)
            {
                amount = player.Chips;
                player.PlayerStatus = PlayerStatus.AllIn;
            }
            player.DeductChips(amount);
            RegisterContribution(game, player, amount);
        }

        private void RegisterContribution(Game game, Player player, int amount) => 
            game.CurrentHand!.Pot.AddContribution(player.Id, amount);

        private async Task<PlayerAction> ValidateAndFixActionAsync(Game game, Player player, PlayerAction action)
        {
            switch (action.ActionType)
            {
                case PlayerActionType.Fold:
                    
                    break;
                case PlayerActionType.Call:
                    action.Amount = game.CurrentHand!.Pot.GetCallAmountForPlayer(player.Id);

                    // Ha nincs pot amit ki lehetne fizetni, akkor a call az egy check
                    if (action.Amount == 0)
                        action.ActionType = PlayerActionType.Check;
                    // Ha több a tét, mint a teljes vagyon, akkor a vagyon a tét (all in)
                    else if (action.Amount >= player.Chips)
                        action.Amount = player.Chips;

                    game.CurrentHand!.Pot.AddContribution(player.Id, (int)action.Amount);
                    break;
                case PlayerActionType.Raise:
                    if (action.Amount is null || action.Amount <= 0)
                        throw new Exception("Nem kéne itt bajnak");

                    game.CurrentHand!.Pot.AddContribution(player.Id, (int)action.Amount);
                    game.SetCurrentPlayerToPivot(player.Id);
                    break;
            }

            return action;
        }
    }
}
