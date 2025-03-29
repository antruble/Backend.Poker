using Backend.Poker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Application.Interfaces
{
    public interface IBotService
    {
        Task<PlayerAction> GenerateBotActionAsync(Guid botId);
    }
}
