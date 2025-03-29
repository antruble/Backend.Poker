using Backend.Poker.Application.Interfaces;
using Backend.Poker.Domain.Entities;
using Backend.Poker.DTOs;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("getgame")]
        public async Task<IActionResult> GetGameById(Guid gameId)
        {
            var game = await _gameService.GetGameByIdAsync(gameId);
            if (game is null)
            {
                return BadRequest($"Nem található game az adott game id-vel. Game ID: {gameId}");
            }
            return Ok(game);
        }
        
        [HttpGet("games")]
        public async Task<IActionResult> Games()
        {
            var games = await _gameService.GetGamesAsync();
            return Ok(games);
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
                var action = await _botService.GenerateBotActionAsync(botId);
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
                _logger.LogInformation($"ProcessActionAsync meghívva..");
                _ = await _gameService.ProcessActionAsync(gameId, playerId, action);
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

        //[HttpGet("dealnext")]
        //public async Task<IActionResult> DealNextRound(Guid gameId)
        //{
        //    var game = await _gameService.DealNextRound(gameId);
        //    return Ok(game);
        //}

        [HttpGet("getwinners")]
        public async Task<IActionResult> GetWinners(Guid handId)
        {
            var winners = await _gameService.GetWinners(handId);
            return Ok(winners);
        }

    }
}
