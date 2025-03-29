using Backend.Poker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Domain.Events
{
    public record PlayerActionMadeEvent(Guid HandId, Guid PlayerId, PlayerAction Action);
    public record HandCompletedEvent(Guid HandId, decimal Pot, Guid? WinnerPlayerId);
}
