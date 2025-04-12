using Backend.Poker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Application.Interfaces
{
    public interface IGameService
    {
        Task<Game> GetGameAsync();
        Task<Game?> GetGameByIdAsync(Guid gameId);
        Task<Game> StartNewGameAsync(int numOfBots, string playerName = "Player");
        Task StartNewHandAsync(Guid gameId);
        Task CardsDealingActionFinished(Guid gameId);
        Task<Game> ProcessActionAsync(Game game, Player player, PlayerAction action);
		//Task<Game> DealNextRound(Guid gameId);
		Task<Game> DealNextRound(Game game);
        Task<Game> SetGameActionShowOff(Game game);
        Task<List<Winner>> GetWinners(Guid handId);
    }
}
