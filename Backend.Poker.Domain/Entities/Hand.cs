using Backend.Poker.Domain.Entities;
using Backend.Poker.Domain.Services;
using Backend.Poker.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Backend.Poker.Domain.Entities
{
    public enum HandStatus
    {
        Preflop,
        Flop,
        Turn,
        River,
        Shutdown,
    }
    public class Hand
    {
        public Guid Id { get; }
        public List<Card> CommunityCards { get; set; }
        public HandStatus HandStatus { get; private set; }
        public Pot Pot { get; private set; }
        //public Guid CurrentPlayerId { get; set; }
        public Guid FirstPlayerId { get; set; }
        public Guid PivotPlayerId { get; set; }
        public Guid CurrentPlayerId { get; set; }
        public bool SkipActions { get; set; } = false;

        [NotMapped]
        public Dictionary<Guid, double> Odds { get; set; }

        [NotMapped]
        [JsonIgnore]
        public Deck Deck { get; private set; }

        protected Hand() { }
        public Hand(Player currentPlayer)
        {
            Id = Guid.NewGuid();
            Deck = new Deck();
            Pot = new();
            FirstPlayerId = currentPlayer.Id;
            PivotPlayerId = currentPlayer.Id;
            HandStatus = HandStatus.Preflop;
            CommunityCards = [];
        }

        [JsonConstructor]
        public Hand(Guid id, List<Card> communityCards, HandStatus handStatus, Pot pot, bool skipActions, Dictionary<Guid, double> odds)
        {
            (Id, CommunityCards, HandStatus, Pot, SkipActions, Odds) = (id, communityCards, handStatus, pot, skipActions, odds);
        }

        public void DealHoleCards(IList<Player> players)
        {
            if (Deck is null)
            {
                throw new NullReferenceException("Miért null a deck");
            }
            foreach (var player in players)
            {
                if (player.HoleCards.Count > 0)
                    player.ResetHoleCards();

                if (player.PlayerStatus != PlayerStatus.Lost)
                {
                    player.HoleCards.Add(Deck.Draw());
                    player.HoleCards.Add(Deck.Draw());
                }
            }
        }

        /// <summary>
        /// Váltás a következő fázisra a kézben.
        /// A logika:
        /// - Preflop fázisból: 3 közös lap (Flop) osztása, majd állapot Frissítése.
        /// - Flop fázisból: 1 közös lap (Turn) osztása, majd állapot frissítése.
        /// - Turn fázisból: 1 közös lap (River) osztása, majd állapot frissítése.
        /// - River fázisból: a kéz befejeződik (Shutdown).
        /// </summary>
        public void DealNextRound(Deck deck)
        {
            Deck = deck;

            switch (HandStatus)
            {
                case HandStatus.Preflop:
                    // Flop: 3 lap
                    for (int i = 0; i < 3; i++)
                    {
                        AddCommunityCard();
                    }
                    HandStatus = HandStatus.Flop;
                    break;
                case HandStatus.Flop:
                    // Turn: 1 lap
                    AddCommunityCard();
                    HandStatus = HandStatus.Turn;
                    break;
                case HandStatus.Turn:
                    // River: 1 lap
                    AddCommunityCard();
                    HandStatus = HandStatus.River;
                    break;
                case HandStatus.River:
                    // A kéz lezárul
                    HandStatus = HandStatus.Shutdown;
                    break;
                default:
                    throw new InvalidOperationException($"A kéz már lezárult, nem lehet tovább deal-elni. {HandStatus}");
            }
        }
        
        public void AddCommunityCard() => CommunityCards.Add(Deck.Draw());

        /// <summary>
        /// Lezárja a kezet és meghívja a showdown logikát a győztes(ek) meghatározásához.
        /// </summary>
        /// <param name="evaluator">Az értékelő, amely implementálja az IPokerHandEvaluator interfészt</param>
        /// <returns>A kéz showdown eredménye, ami tartalmazza a győztes(ek) azonosítóit és a pot felosztását</returns>
        public ICollection<Winner> CompleteHand(IPokerHandEvaluator evaluator, IList<Player> players)
        {
            if (evaluator == null)
                throw new ArgumentNullException(nameof(evaluator));

            return evaluator.Evaluate(this, players).ToList();
        }

        public Deck RestoreDeck(IList<Card> drawnCards)
        {
            return new Deck(drawnCards.Concat(this.CommunityCards));
        }


        #region Pot methods
        public void AddBet(Guid playerId, int amount)
        {
            Pot.AddContribution(playerId, amount);
        }
        public void CompleteRound()
        {
            Pot.CompleteRound();
        }
        #endregion
    }
}
