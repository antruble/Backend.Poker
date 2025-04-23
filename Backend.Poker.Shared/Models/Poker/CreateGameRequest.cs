using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Shared.Models.Poker
{
    public class CreateGameRequest
    {
        public int NumOfBots { get; set; }
        public string PlayerName { get; set; } = default!;
    }
}
