using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Domain.ValueObjects
{
    public enum HandCategory
    {
        HighCard = 1,
        OnePair = 2,
        TwoPair = 3,
        ThreeOfAKind = 4,
        Straight = 5,
        Flush = 6,
        FullHouse = 7,
        FourOfAKind = 8,
        StraightFlush = 9,
        RoyalFlush = 10
    }
    public class PokerHandRank : IComparable<PokerHandRank>
    {
        public HandCategory Category { get; set; }
        public List<int> Kickers { get; set; } = []; // Csökkenő sorrendben

        public int CompareTo(PokerHandRank? other)
        {
            if (other == null)
                return 1;

            // Először a kategória numerikus értékét hasonlítjuk össze.
            int categoryComparison = this.Category.CompareTo(other.Category);
            // Mivel a magasabb érték erősebb kéz, ha this.Category nagyobb, akkor this a jobb kéz.
            // (A CompareTo metódus így a normális növekvő sorrendet adja.)
            if (categoryComparison != 0)
                return categoryComparison;

            // Ha a kategóriák egyenlőek, akkor a kickerek összehasonlítása következik.
            int minCount = Math.Min(this.Kickers.Count, other.Kickers.Count);
            for (int i = 0; i < minCount; i++)
            {
                int kickerComparison = this.Kickers[i].CompareTo(other.Kickers[i]);
                if (kickerComparison != 0)
                    return kickerComparison;
            }

            // Ha a kickerek megegyeznek az összehasonlított részen, a hosszabb kicker lista nyerhet,
            // vagy ha azonos hosszúságú, akkor az egyenlő.
            return this.Kickers.Count.CompareTo(other.Kickers.Count);
        }
    }

}
