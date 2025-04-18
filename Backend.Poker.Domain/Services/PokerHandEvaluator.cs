using Backend.Poker.Domain.Entities;
using Backend.Poker.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Domain.Services
{
    /// <summary>
    /// Interfész a Texas Hold’em showdown értékeléséhez.
    /// </summary>
    public interface IPokerHandEvaluator
    {
        /// <summary>
        /// Kiértékeli a megadott kezet, és meghatározza a győztes(ek) azonosítóját, valamint a pot felosztását.
        /// </summary>
        /// <param name="hand">A showdown-hoz szükséges kéz, ahol a közös lapoknak már legalább 5-nek kell lenniük</param>
        /// <returns>A showdown eredménye</returns>
        IList<Winner> Evaluate(Hand hand, IList<Player> players);
    }
    /// <summary>
    /// Kiszámolja egy adott játékos hole + community kártyáiból
    /// a legjobb 5‑kártyás kéz rangját (PokerHandRank).
    /// </summary>
    public interface IHandRankEvaluator
    {
        PokerHandRank EvaluateRank(IEnumerable<Card> holeCards, IEnumerable<Card> communityCards);
    }
    /// <summary>
    /// simplesített implementáció a Texas Hold’em showdown értékeléséhez.
    /// Megjegyzés: A valós értékelési logika sokkal összetettebb,
    /// ez csak egy struktúrális példa.
    /// </summary>
    public class PokerHandEvaluator : IPokerHandEvaluator, IHandRankEvaluator
    {
        public IList<Winner> Evaluate(Hand hand, IList<Player> players)
        {
            // Először szűrjük ki az aktív, nem falt játékosokat (például a showdown előtt)
            var eligiblePlayers = players.Where(p => p.PlayerStatus != PlayerStatus.Folded && p.PlayerStatus != PlayerStatus.Lost).ToList();
            var winners = new List<Winner>();

            if (eligiblePlayers.Count == 1)
            {
                var player = eligiblePlayers[0];
                var winner = new Winner { HandId = hand.Id, Player = player, PlayerId = player.Id, Pot = hand.Pot.MainPot };
                return [winner];
            }
            // 1. Fő pot feldolgozása
            // Feltételezzük, hogy a MainPot értékét a hand.Pot.MainPot tartalmazza.
            if (eligiblePlayers.Any())
            {
                // Számoljuk ki minden játékos legjobb kezeit (ugyanúgy, mint eddig)
                Dictionary<Guid, PokerHandRank> bestRanksMain = new Dictionary<Guid, PokerHandRank>();
                foreach (var player in eligiblePlayers)
                {
                    var allCards = player.HoleCards.Concat(hand.CommunityCards).ToList();
                    var combinations = GetAll5CardCombinations(allCards);
                    PokerHandRank? bestRankForPlayer = null;
                    foreach (var combo in combinations)
                    {
                        var rank = EvaluateCombo(combo);
                        if (bestRankForPlayer == null || rank.CompareTo(bestRankForPlayer) > 0)
                            bestRankForPlayer = rank;
                    }
                    bestRanksMain[player.Id] = bestRankForPlayer!;
                }
                var bestOverallMain = bestRanksMain.Values.Max();
                var mainPotWinners = bestRanksMain
                    .Where(kvp => kvp.Value.CompareTo(bestOverallMain) == 0)
                    .Select(kvp => kvp.Key)
                    .ToList();

                int mainAllocation = hand.Pot.MainPot / mainPotWinners.Count;
                foreach (var playerId in mainPotWinners)
                {
                    winners.Add(new Winner
                    {
                        HandId = hand.Id,
                        PlayerId = playerId,
                        Player = eligiblePlayers.First(p => p.Id == playerId),
                        Pot = mainAllocation
                    });
                }
            }

            // 2. Side pot(ok) feldolgozása
            foreach (var sidePot in hand.Pot.SidePots)
            {
                // Szűrjük azokat a játékosokat, akik jogosultak az adott side potra
                var eligibleForSide = eligiblePlayers
                    .Where(p => sidePot.EligiblePlayerIds.Contains(p.Id))
                    .ToList();

                if (!eligibleForSide.Any())
                    continue; // Ha nincs jogosult, kihagyjuk

                // Számoljuk ki az adott side potban a játékosok legjobb kezeit
                Dictionary<Guid, PokerHandRank> bestRanksSide = new Dictionary<Guid, PokerHandRank>();
                foreach (var player in eligibleForSide)
                {
                    var allCards = player.HoleCards.Concat(hand.CommunityCards).ToList();
                    var combinations = GetAll5CardCombinations(allCards);
                    PokerHandRank? bestRankForPlayer = null;
                    foreach (var combo in combinations)
                    {
                        var rank = EvaluateCombo(combo);
                        if (bestRankForPlayer == null || rank.CompareTo(bestRankForPlayer) > 0)
                            bestRankForPlayer = rank;
                    }
                    bestRanksSide[player.Id] = bestRankForPlayer;
                }
                var bestOverallSide = bestRanksSide.Values.Max();
                var sidePotWinners = bestRanksSide
                    .Where(kvp => kvp.Value.CompareTo(bestOverallSide) == 0)
                    .Select(kvp => kvp.Key)
                    .ToList();

                int sideAllocation = sidePot.Amount / sidePotWinners.Count;
                foreach (var playerId in sidePotWinners)
                {
                    // Ha már van Winner bejegyzés az adott játékosra a fő potból, akkor az összeget összeadhatjuk
                    var existingWinner = winners.FirstOrDefault(w => w.PlayerId == playerId);
                    if (existingWinner != null)
                    {
                        existingWinner.Pot += sideAllocation;
                    }
                    else
                    {
                        winners.Add(new Winner
                        {
                            HandId = hand.Id,
                            PlayerId = playerId,
                            Player = eligiblePlayers.First(p => p.Id == playerId),
                            Pot = sideAllocation
                        });
                    }
                }
            }

            return winners;
        }
        public PokerHandRank EvaluateRank(IEnumerable<Card> holeCards, IEnumerable<Card> communityCards)
        {
            var allCards = holeCards.Concat(communityCards).ToList();
            var combos = GetAll5CardCombinations(allCards);
            PokerHandRank best = null!;
            foreach (var c in combos)
            {
                var rank = EvaluateCombo(c);
                if (best == null || rank.CompareTo(best) > 0)
                    best = rank;
            }
            return best;
        }
        private static IEnumerable<List<Card>> GetAll5CardCombinations(List<Card> cards)
        {
            return GetCombinations(cards, 5);
        }
        private static IEnumerable<List<Card>> GetCombinations(List<Card> cards, int k)
        {
            // Rekurzív kombinációgenerátor:
            if (k == 0)
            {
                yield return new List<Card>();
                yield break;
            }
            if (cards.Count < k)
                yield break;

            for (int i = 0; i <= cards.Count - k; i++)
            {
                // A maradék elemekből generáljuk a k-1 kombinációkat
                foreach (var tail in GetCombinations(cards.Skip(i + 1).ToList(), k - 1))
                {
                    var combination = new List<Card> { cards[i] };
                    combination.AddRange(tail);
                    yield return combination;
                }
            }
        }
        private PokerHandRank EvaluateCombo(List<Card> combo)
        {
            if (combo.Count != 5)
                throw new Exception("nem 5");

            // Rendezés csökkenő sorrendben
            var sorted = combo.OrderByDescending(c => (int)c.Rank).ToList();

            // Ellenőrizzük, hogy flush-e (minden lap azonos színű-e)
            bool isFlush = combo.All(c => c.Suit == combo[0].Suit);

            // Ellenőrizzük a straight-et: a lapoknek egymást követőnek kell lenniük.
            bool isStraight = IsStraight(sorted, out int highStraightRank);

            // Csoportosítjuk a lapokat Rank szerint
            var rankGroups = combo.GroupBy(c => c.Rank)
                                  .OrderByDescending(g => g.Count())
                                  .ThenByDescending(g => (int)g.Key)
                                  .ToList();
            int maxGroupCount = rankGroups.First().Count();

            var rank = new PokerHandRank();

            if (isFlush && isStraight)
            {
                if (highStraightRank == (int)Rank.Ace)
                {
                    rank.Category = HandCategory.RoyalFlush;
                    rank.Kickers = new List<int> { highStraightRank };
                }
                else
                {
                    rank.Category = HandCategory.StraightFlush;
                    rank.Kickers = new List<int> { highStraightRank };
                }
            }
            else if (maxGroupCount == 4)
            {
                rank.Category = HandCategory.FourOfAKind;
                int fourRank = (int)rankGroups.First().Key;
                int kicker = sorted.Where(c => (int)c.Rank != fourRank).Max(c => (int)c.Rank);
                rank.Kickers = new List<int> { fourRank, kicker };
            }
            else if (maxGroupCount == 3 && rankGroups.Count >= 2 && rankGroups[1].Count() >= 2)
            {
                rank.Category = HandCategory.FullHouse;
                int threeRank = (int)rankGroups.First().Key;
                int pairRank = (int)rankGroups[1].Key;
                rank.Kickers = new List<int> { threeRank, pairRank };
            }
            else if (isFlush)
            {
                rank.Category = HandCategory.Flush;
                rank.Kickers = sorted.Select(c => (int)c.Rank).ToList();
            }
            else if (isStraight)
            {
                rank.Category = HandCategory.Straight;
                rank.Kickers = new List<int> { highStraightRank };
            }
            else if (maxGroupCount == 3)
            {
                rank.Category = HandCategory.ThreeOfAKind;
                int threeRank = (int)rankGroups.First().Key;
                var kickers = sorted.Where(c => (int)c.Rank != threeRank)
                                    .Select(c => (int)c.Rank)
                                    .Distinct()
                                    .Take(2)
                                    .ToList();
                rank.Kickers = new List<int> { threeRank };
                rank.Kickers.AddRange(kickers);
            }
            else if (maxGroupCount == 2)
            {
                var pairCount = rankGroups.Count(g => g.Count() == 2);
                if (pairCount >= 2)
                {
                    rank.Category = HandCategory.TwoPair;
                    var pairs = rankGroups.Where(g => g.Count() == 2)
                                          .Select(g => (int)g.Key)
                                          .OrderByDescending(x => x)
                                          .ToList();
                    int kicker = sorted.Where(c => !pairs.Contains((int)c.Rank)).Max(c => (int)c.Rank);
                    // A Kickers első két eleme a két pár, harmadik a kicker
                    rank.Kickers = new List<int> { pairs[0], pairs[1], kicker };
                }
                else
                {
                    rank.Category = HandCategory.OnePair;
                    int pairRank = (int)rankGroups.First().Key;
                    var kickers = sorted.Where(c => (int)c.Rank != pairRank)
                                        .Select(c => (int)c.Rank)
                                        .Distinct()
                                        .Take(3)
                                        .ToList();
                    rank.Kickers = new List<int> { pairRank };
                    rank.Kickers.AddRange(kickers);
                }
            }
            else
            {
                rank.Category = HandCategory.HighCard;
                rank.Kickers = sorted.Select(c => (int)c.Rank).ToList();
            }

            return rank;
        }
        private bool IsStraight(List<Card> sortedCards, out int highStraightRank)
        {
            // Feltételezzük, hogy a sortedCards csökkenő sorrendben van.
            var distinctRanks = sortedCards.Select(c => (int)c.Rank).Distinct().ToList();
            highStraightRank = 0;
            if (distinctRanks.Count < 5) return false;

            for (int i = 0; i <= distinctRanks.Count - 5; i++)
            {
                bool isSeq = true;
                for (int j = 1; j < 5; j++)
                {
                    if (distinctRanks[i] - j != distinctRanks[i + j])
                    {
                        isSeq = false;
                        break;
                    }
                }
                if (isSeq)
                {
                    highStraightRank = distinctRanks[i];
                    return true;
                }
            }

            // Special case: Ace-alacsony straight (Ace, 2, 3, 4, 5)
            if (distinctRanks.Contains((int)Rank.Ace) &&
                distinctRanks.Contains(2) &&
                distinctRanks.Contains(3) &&
                distinctRanks.Contains(4) &&
                distinctRanks.Contains(5))
            {
                highStraightRank = 5;
                return true;
            }
            return false;
        }
    }
}
