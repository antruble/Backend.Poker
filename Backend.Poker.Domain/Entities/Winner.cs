using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Domain.Entities
{
    public class Winner
    {
        // Idegen kulcsok
        public Guid HandId { get; set; }
        public Guid PlayerId { get; set; }
        public required Player Player { get; set; }
        public int Pot { get; set; } = 0;

    }
}
