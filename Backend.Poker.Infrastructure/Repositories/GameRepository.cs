using Backend.Poker.Domain.Entities;
using Backend.Poker.Domain.IRepositories;
using Backend.Poker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Infrastructure.Repositories
{
    public class GameRepository(ApplicationDbContext context) : GenericsRepository<Game>(context), IGameRepository
    {
        public override async Task<Game?> GetByIdAsync(object id)
        {
            // Feltételezzük, hogy a Game.Id típusa Guid.
            Guid gameId = (Guid)id;
            return await base._dbSet
                .Include(g => g.Players)
                .Include(g => g.CurrentHand)
                    // Ha az aktuális kézhez tartoznak például a CommunityCards:
                    .ThenInclude(hand => hand.CommunityCards)
                .FirstOrDefaultAsync(g => g.Id == gameId);
        }
    }
}
