﻿using Backend.Poker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Application.Interfaces
{
    public interface IPlayerService
    {
        Task<IList<Player>> GetPlayersAsync(int numOfBots, string playerName = "Player");
        Task<Guid> GetUserIdAsync(Guid gameId);
        Task<Player> GetPlayerByIdAsync(Guid playerId);
        //Task SetBlindsAsync(IList<Player> players);

    }
}
