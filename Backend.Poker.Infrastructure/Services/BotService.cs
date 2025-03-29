using Backend.Poker.Application.Interfaces;
using Backend.Poker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Infrastructure.Services
{
    public class BotService : IBotService
    {


        public BotService(
            
            )
        { 
        }
        public async Task<PlayerAction> GenerateBotActionAsync(Guid botId)
        {
            return new PlayerAction(PlayerActionType.Call, null);
            // Enum összes értékének lekérése
            //var actionTypes = Enum.GetValues(typeof(PlayerActionType));
            //// Random példány létrehozása
            //var random = new Random();
            //// Véletlenszerű érték kiválasztása
            //var randomAction = (PlayerActionType)actionTypes.GetValue(random.Next(actionTypes.Length));
            //return await Task.FromResult(randomAction);
        }
    }
}
