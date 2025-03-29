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
    public class PlayerRepository : GenericsRepository<Player>, IPlayerRepository
    {
        public PlayerRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Player?> FindPlayerByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            name = name.Trim().ToLower();

            return await _context.Players
                .FirstOrDefaultAsync(p => p.Name.ToLower().Trim() == name);
        }

    }
}
