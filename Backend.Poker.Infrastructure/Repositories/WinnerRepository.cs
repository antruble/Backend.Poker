using Backend.Poker.Domain.Entities;
using Backend.Poker.Domain.IRepositories;
using Backend.Poker.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Infrastructure.Repositories
{
    public class WinnerRepository(ApplicationDbContext context) : GenericsRepository<Winner>(context), IWinnerRepository
    {
    }
}
