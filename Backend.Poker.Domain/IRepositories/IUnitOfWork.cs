using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Domain.IRepositories
{
    public interface IUnitOfWork : IDisposable
    {
        IGameRepository Games { get; }
        IPlayerRepository Players { get; }
        IWinnerRepository Winners { get; }
        IHandRepository Hands { get; }
        Task SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
