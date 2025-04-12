using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Backend.Poker.Domain.Entities
{
    public class Pot
    {
        public int MainPot { get; private set; }
        public int CurrentRoundPot { get; private set; }
        public List<PlayerContribution> Contributions { get; private set; } = new ();
        public List<SidePot> SidePots { get; private set; } = new();


    [JsonConstructor]
        public Pot(int mainPot, int currentRoundPot, List<PlayerContribution> contributions, List<SidePot> sidePots)
        {
            MainPot = mainPot;
            CurrentRoundPot = currentRoundPot;
            Contributions = contributions;
            SidePots = sidePots;
        }

        // Ha szükséges egy parameterless konstruktor is:
        public Pot() { }
        public void AddContribution(Guid playerId, int amount)
        {
            int index = Contributions.FindIndex(pc => pc.PlayerId == playerId);
            if (index >= 0)
                Contributions[index] = Contributions[index].Add(amount);
            else
                Contributions.Add(new PlayerContribution(playerId, amount));
            CurrentRoundPot += amount;
        }
        public int GetCallAmountForPlayer(Guid playerId)
        {
            // Ha még nincs hozzájárulás, akkor 0-t veszünk alapul
            int playerContribution = Contributions.FirstOrDefault(pc => pc.PlayerId == playerId)?.Amount ?? 0;

            // Meghatározzuk a legnagyobb befizetést a jelenlegi roundben
            int maxContribution = Contributions.Any() ? Contributions.Max(pc => pc.Amount) : 0;

            // A call összeg a különbség a legmagasabb és az adott játékos által befizetett összeg között
            int callAmount = maxContribution - playerContribution;

            if (callAmount < 0)
                throw new Exception("A call értéke nem lehet kisebb mint 0");

            return callAmount;
        }
        public void CompleteRound()
        {
            MainPot += CurrentRoundPot;
            CurrentRoundPot = 0;
            // Itt lehet side pot logikát is beépíteni, ha all-in esetek merülnek fel
        }

        // Metódus a side pot hozzáadásához, módosításához, stb.
        public void AddSidePot(int amount, IEnumerable<Guid> eligiblePlayers)
        {
            SidePots.Add(new SidePot(amount, eligiblePlayers));
        }

        public void CreateSidePots()
        {
            // Rendezés a hozzájárulások szerint (növekvő)
            var sortedContributions = Contributions.OrderBy(pc => pc.Amount).ToList();
            if (sortedContributions.Count == 0)
                return;
            // A fő pot már a legalacsonyabb összeggel került kialakításra:
            int baseAmount = sortedContributions.First().Amount;

            // Minden játékosból levonjuk a baseAmount-et, így kapjuk meg a maradékot (extra tét)
            var extraContributions = sortedContributions
                .Select(pc => new { pc.PlayerId, Extra = pc.Amount - baseAmount })
                .ToList();

            // Az első side pot: mindenki, aki extra befizetett (Extra > 0)
            // A legalacsonyabb extra összeget vesszük alapul az extra tétekből.
            var eligibleForSidePot = extraContributions.Where(x => x.Extra > 0).ToList();
            if (!eligibleForSidePot.Any())
            {
                // Nincs side pot, mert senki nem fizetett extra tétet
                return;
            }

            int sidePotBase = eligibleForSidePot.Min(x => x.Extra);
            // Az első side pot összege:
            int sidePotAmount = sidePotBase * eligibleForSidePot.Count;
            // Hozzunk létre egy side potot, amelybe azok a játékosok kerülnek, akik extra tétet fizettek.
            AddSidePot(sidePotAmount, eligibleForSidePot.Select(x => x.PlayerId));

            // Most csökkentsük az extra tétet minden érintett játékosnál:
            for (int i = 0; i < eligibleForSidePot.Count; i++)
            {
                eligibleForSidePot[i] = new
                {
                    eligibleForSidePot[i].PlayerId,
                    Extra = eligibleForSidePot[i].Extra - sidePotBase
                };
            }

            // Ismételjük, ha maradt olyan extra, amelyből új side pot keletkezhet.
            while (eligibleForSidePot.Any(x => x.Extra > 0))
            {
                var currentEligible = eligibleForSidePot.Where(x => x.Extra > 0).ToList();
                int currentBase = currentEligible.Min(x => x.Extra);
                int currentSidePot = currentBase * currentEligible.Count;
                AddSidePot(currentSidePot, currentEligible.Select(x => x.PlayerId));
                // Frissítjük a listát
                eligibleForSidePot = currentEligible
                    .Select(x => new { x.PlayerId, Extra = x.Extra - currentBase })
                    .ToList();
            }
        }
    }

    public record PlayerContribution(Guid PlayerId, int Amount)
    {
        public PlayerContribution Add(int additionalAmount) => new(PlayerId, Amount + additionalAmount);
    }

    public record SidePot(int Amount, IEnumerable<Guid> EligiblePlayerIds);
}
