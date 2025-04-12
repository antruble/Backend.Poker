using Backend.Poker.Application.Interfaces;
using Backend.Poker.Domain.Entities;
using Backend.Poker.Domain.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Infrastructure.Services
{
    public class BotService : IBotService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BotService(
            IUnitOfWork unitOfWork
            )
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<PlayerAction> GenerateBotActionAsync(Guid botId, int amount)
        {
            var bot = await _unitOfWork.Players.GetByIdAsync(botId) 
                ?? throw new Exception("Bot nem található.");

            var random = new Random();
            double chance = random.NextDouble(); // 0.0 és 1.0 közötti érték

            var action = new PlayerAction
            {
                Timestamp = DateTime.UtcNow
            };

            if (amount > 0)
                chance = 0.0;

            if (chance < 0.70)
            {
                // 70% esetben Call
                action.ActionType = PlayerActionType.Call;
                action.Amount = null; // Call esetén nincs szükség tétre
            }
            else if (chance < 0.95)
            {
                // 25% esetben Raise 5%-kal a bot chipjeiből
                action.ActionType = PlayerActionType.Raise;
                int raiseAmount = (int)(bot.Chips * 0.05);
                // Minimum tét: ha a számolt érték nulla, tegyük 1-re
                if (raiseAmount < 1) raiseAmount = 1;
                action.Amount = raiseAmount;
            }
            else
            {
                // 5% esetben all-in Raise (az összes chip)
                action.ActionType = PlayerActionType.Raise;
                action.Amount = bot.Chips;
            }

            return action;
        }

    }
}
