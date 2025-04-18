using Backend.Poker.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Application.Interfaces
{
    public interface IOddsCalculator
    {
        /// <summary>
        /// Visszaadja, hogy az egyes játékosoknak jelenlegi lapjaik és a már megjelenített közös lapok ismeretében
        /// mennyi az esélye a hand megnyerésére.
        /// </summary>
        /// <param name="holeCards">Játékosonként a zárt lapok.</param>
        /// <param name="communityCards">Már kiosztott közös lapok.</param>
        /// <param name="numPlayers">Összes játékos száma.</param>
        /// <param name="iterations">Monte‑Carlo iterációk száma (opcionális).</param>
        /// <returns>PlayerId → win% map.</returns>
        Dictionary<Guid, double> CalculateWinProbabilities(
            Dictionary<Guid, IList<Card>> holeCards,
            IList<Card> communityCards,
            int iterations = 10_000);
    }
}
