using Backend.Poker.Application.Interfaces;
using Backend.Poker.Domain.Entities;
using Backend.Poker.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Numerics;

namespace Backend.Poker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PokerController : Controller
    {
        private readonly IGameService _gameService;
        private readonly IPlayerService _playerService;
        private readonly IBotService _botService;
        private readonly ILogger<PokerController> _logger;

        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _gameLocks = new();

        public PokerController(
            IGameService gameService,
            IPlayerService playerService,
            IBotService botService,
            ILogger<PokerController> logger
            )
        { 
            _gameService = gameService;
            _playerService = playerService;
            _botService = botService;
            _logger = logger;
        }

        [HttpPost("newgame")]
        public async Task<IActionResult> CreateGame([FromBody] CreateGameRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request is null.");
            }

            var game = await _gameService.StartNewGameAsync(request.NumOfBots, request.PlayerName);
            return Ok(game);
        }

        [HttpGet("getgamebyid")]
        public async Task<IActionResult> GetGameById(Guid gameId)
        {
            var game = await _gameService.GetGameByIdAsync(gameId);
            if (game is null)
            {
                return BadRequest($"Nem található game az adott game id-vel. Game ID: {gameId}");
            }
            return Ok(game);
        }
        
        [HttpGet("getgame")]
        public async Task<IActionResult> GetGame()
        {
            var game = await _gameService.GetGameAsync();

            if (game is null)
                return NoContent();

            return Ok(game);
        }
        [HttpGet("getuserid")]
        public async Task<IActionResult> GetUserId(Guid gameId)
        {
            var playerId = await _playerService.GetUserIdAsync(gameId);
            return Ok(playerId);
        }
        [HttpGet("generatebotaction")]
        public async Task<IActionResult> GenerateBotAction(Guid gameId, Guid botId)
        {
            try
            {
                var game = await _gameService.GetGameByIdAsync(gameId)
                    ?? throw new InvalidOperationException($"Nem található game {gameId} ID-val");
                var callAmount = game.CurrentHand!.Pot.GetCallAmountForPlayer(botId);
                var action = await _botService.GenerateBotActionAsync(botId, callAmount);
                return Ok(action);
            }
            catch (Exception ex)
            {
                _logger.LogError($"GenerateBotAction.Error: {ex.Message}", ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("processaction")]
        public async Task<IActionResult> ProcessActionAsync(Guid gameId, Guid playerId, [FromBody] PlayerAction action)
        {
           
            try
            {
                await ProcessGameRequestAsync(gameId, async () =>
                {
                    _logger.LogInformation($"Új szál beengedve a ProcessActionAsync belsejébe. ThreadID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                    var game = await _gameService.GetGameByIdAsync(gameId)
                        ?? throw new InvalidOperationException($"Nem található game {gameId} ID-val");

                    if (game.CurrentGameAction != GameActions.PlayerAction)
                        throw new InvalidOperationException($"A game action-ja nem ProcessAction, hanem {game.CurrentGameAction}");

                    var player = await _playerService.GetPlayerByIdAsync(playerId)
                        ?? throw new InvalidOperationException($"Nem található player {playerId} ID-val");

                    await _gameService.ProcessActionAsync(game, player, action);
                });
               
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"ProcessActionAsync.Error: {ex.Message}", ex);
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("cardshasdealed")]
        public async Task<IActionResult> CardsHasDealedAsync(Guid gameId)
        {
            try
            {
                await ProcessGameRequestAsync(gameId, async () => 
                {
                    _logger.LogInformation($"Kártyák kiosztva, gameaction továbbléptetése");
                    await _gameService.CardsDealingActionFinished(gameId);
                });
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"CardsHasDealedAsync.Error: {ex.Message}", ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("startnewhand")]
        public async Task<IActionResult> StartNewHandAsync(Guid gameId)
        {
            try
            {
                await ProcessGameRequestAsync(gameId, async () =>
                {
                    await _gameService.StartNewHandAsync(gameId);
                });
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"StartNewHandAsync.Error: {ex.Message}", ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("dealnextround")]
        public async Task<IActionResult> DealNextRound(Guid gameId)
        {
            await ProcessGameRequestAsync(gameId, async () =>
            {
                var game = await _gameService.GetGameByIdAsync(gameId) ?? throw new Exception($"Nem található game {gameId} ID-val!");
                if (game.CurrentHand!.HandStatus == HandStatus.Shutdown)
                    game = await _gameService.SetGameActionShowOff(game);
                else
                    game = await _gameService.DealNextRound(game);
            });
            var updatedGame = await _gameService.GetGameByIdAsync(gameId);
            return Ok(updatedGame);
        }

        [HttpGet("getwinners")]
        public async Task<IActionResult> GetWinners(Guid handId)
        {
            var winners = await _gameService.GetWinners(handId);
            return Ok(winners);
        }


        #region Gamelocks
        private SemaphoreSlim GetGameLock(Guid gameId)
        {
            return _gameLocks.GetOrAdd(gameId, _ => new SemaphoreSlim(1, 1));
        }

        public async Task ProcessGameRequestAsync(Guid gameId, Func<Task> action)
        {
            var gameLock = GetGameLock(gameId);
            await gameLock.WaitAsync();
            try
            {
                _logger.LogWarning($"ThreadID ami most lett ELINDÍTVA: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                await action();
            }
            finally
            {
                _logger.LogWarning($"ThreadID ami fog LEZÁRULNI: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                gameLock.Release();
            }
        }
        #endregion
    }
}
