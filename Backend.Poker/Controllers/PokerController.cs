using Backend.Poker.Application.Interfaces;
using Backend.Poker.Domain.Entities;
using Backend.Poker.DTOs;
using Microsoft.AspNetCore.Mvc;
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

        //public async Task<IActionResult> Index()
        //{
        //    var game = await _gameService.StartNewGameAsync(4);
        //    return Ok(game);
        //}

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
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("processaction")]
        public async Task<IActionResult> ProcessActionAsync(Guid gameId, Guid playerId, [FromBody] PlayerAction action)
        {
            try
            {
                var game = await _gameService.GetGameByIdAsync(gameId) 
                    ?? throw new InvalidOperationException($"Nem található game {gameId} ID-val");

                if (game.CurrentGameAction != GameActions.PlayerAction)
                    throw new InvalidOperationException($"A game action-ja nem ProcessAction, hanem {game.CurrentGameAction}");

                var player = await _playerService.GetPlayerByIdAsync(playerId)
                        ?? throw new InvalidOperationException($"Nem található player {playerId} ID-val");

                await _gameService.ProcessActionAsync(game, player, action);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("cardshasdealed")]
        public async Task<IActionResult> CardsHasDealedAsync(Guid gameId)
        {
            try
            {
                _logger.LogInformation($"Kártyák kiosztva, gameaction továbbléptetése");
                await _gameService.CardsDealingActionFinished(gameId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("startnewhand")]
        public async Task<IActionResult> StartNewHandAsync(Guid gameId)
        {
            try
            {
                await _gameService.StartNewHandAsync(gameId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("dealnextround")]
        public async Task<IActionResult> DealNextRound(Guid gameId)
        {
            var game = await _gameService.GetGameByIdAsync(gameId) ?? throw new Exception($"Nem található game {gameId} ID-val!");
            if (game.CurrentHand!.HandStatus == HandStatus.Shutdown)
                game = await _gameService.SetGameActionShowOff(game);
            else
                game = await _gameService.DealNextRound(game);
            return Ok(game);
        }

        [HttpGet("getwinners")]
        public async Task<IActionResult> GetWinners(Guid handId)
        {
            var winners = await _gameService.GetWinners(handId);
            return Ok(winners);
        }

    }
}
