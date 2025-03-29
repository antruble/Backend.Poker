using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Domain.ValueObjects
{
    /// <summary>
    /// A showdown eredménye: a győztes(ek) azonosítói és a pot felosztása.
    /// </summary>
    public class PokerHandResult
    {
        public List<Guid> WinnerIds { get; set; } = new List<Guid>();
        public Dictionary<Guid, decimal> PotAllocations { get; set; } = new Dictionary<Guid, decimal>();
    }
}
