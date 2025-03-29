using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Domain.ValueObjects
{
    public enum Suit { Clubs, Diamonds, Hearts, Spades }
    public enum Rank
    {
        Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten,
        Jack, Queen, King, Ace
    }

    //public record Card(Rank Rank, Suit Suit);
    public class Card : IEquatable<Card>
    {
        public Rank Rank { get; }
        public Suit Suit { get; }
        public string DisplayValue => GetDisplayValue();

        public Card(Rank rank, Suit suit)
        {
            (Rank, Suit) = (rank, suit);

        }

        private string GetDisplayValue()
        {
            int numericValue = (int)Rank;
            if (numericValue <= 10)
                return numericValue.ToString();
            else
            {
                // Például: Jack -> "J", Queen -> "Q", King -> "K", Ace -> "A"
                string rankStr = Rank.ToString();
                return rankStr.Substring(0, 1);
            }
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            if (obj is null || obj.GetType() != GetType())
                return false;
            return Equals((Card)obj);
        }

        public bool Equals(Card? other)
        {
            if (other is null)
                return false;
            return Rank == other.Rank && Suit == other.Suit;
        }

        public override int GetHashCode()
        {
            unchecked // Túlcsordulás engedélyezése
            {
                int hash = 17;
                hash = hash * 23 + Rank.GetHashCode();
                hash = hash * 23 + Suit.GetHashCode();
                return hash;
            }
        }

    }
    public class Deck
    {
        private readonly List<Card> _cards;

        public Deck()
        {
            _cards = Enum.GetValues(typeof(Suit))
                .Cast<Suit>()
                .SelectMany(suit => Enum.GetValues(typeof(Rank))
                    .Cast<Rank>()
                    .Select(rank => new Card(rank, suit)))
                .ToList();

            Shuffle();
        }
        public Deck(IEnumerable<Card> drawnCards)
        {
            if (drawnCards == null)
            {
                throw new ArgumentNullException(nameof(drawnCards));
            }

            // Létrehozzuk a teljes paklit, majd kiszűrjük a drawnCards-ban szereplő lapokat.
            _cards = Enum.GetValues(typeof(Suit))
                .Cast<Suit>()
                .SelectMany(suit => Enum.GetValues(typeof(Rank))
                    .Cast<Rank>()
                    .Select(rank => new Card(rank, suit)))
                .Except(drawnCards)
                .ToList();

            Shuffle();
        }

        public void Shuffle() // FisherYatesShuffle
        {
            Random rnd = new Random();

            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int index = rnd.Next(i+1);
                (_cards[i], _cards[index]) = (_cards[index], _cards[i]); // swap
            }
        }

        public Card Draw()
        {
            if (_cards.Count == 0)
                throw new InvalidOperationException("Nincs több kártya a pakliban.");
            var card = _cards.First();
            _cards.RemoveAt(0);
            return card;
        }
    }
}
