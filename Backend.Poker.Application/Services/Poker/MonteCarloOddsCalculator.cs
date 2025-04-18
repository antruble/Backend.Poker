using Backend.Poker.Application.Interfaces;
using Backend.Poker.Domain.Services;
using Backend.Poker.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Application.Services.Poker
{
    public class MonteCarloOddsCalculator : IOddsCalculator
    {
        private readonly IHandRankEvaluator _rankEvaluator;

        public MonteCarloOddsCalculator(IHandRankEvaluator rankEvaluator)
        {
            _rankEvaluator = rankEvaluator;
        }

        public Dictionary<Guid, double> CalculateWinProbabilities(
            Dictionary<Guid, IList<Card>> holeCards,
            IList<Card> communityCards,
            int iterations = 10_000)
        {
            // Inicializáljuk a számlálókat
            var wins = holeCards.Keys.ToDictionary(id => id, id => 0.0);

            // Mindig ugyanazzal a seed-del is kísérletezhetsz, ha reprodukálhatóságot akarsz
            for (int i = 0; i < iterations; i++)
            {
                // 1) Deck a még nem kiosztott kártyákkal
                var drawn = holeCards.SelectMany(kv => kv.Value).Concat(communityCards).ToList();
                var deck = new Deck(drawn);

                // 2) Pótlás véletlenszerűen, amíg 5 közös lap nincs
                var simCommunity = new List<Card>(communityCards);
                while (simCommunity.Count < 5)
                    simCommunity.Add(deck.Draw());

                // 3) Minden játékos legjobb rangja
                var playerRanks = holeCards
                    .ToDictionary(
                        kv => kv.Key,
                        kv => _rankEvaluator.EvaluateRank(kv.Value, simCommunity)
                    );

                // 4) Legnagyobb rang megtalálása
                var bestRank = playerRanks.Values.Max();
                var winners = playerRanks
                    .Where(kv => kv.Value.CompareTo(bestRank) == 0)
                    .Select(kv => kv.Key)
                    .ToList();

                // 5) Osztott győzelem kezelése
                foreach (var pid in winners)
                    wins[pid] += 1.0 / winners.Count;
            }

            // Visszaadjuk százalékban
            return wins.ToDictionary(
                kv => kv.Key,
                kv => kv.Value * 100.0 / iterations
            );
        }
    }
}
