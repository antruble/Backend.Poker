using Backend.Poker.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Backend.Poker.Domain.Entities
{
    public class Player
    {
        public Guid Id { get; }
        public string Name { get; set; }
        public int Chips { get; private set; }
        public bool IsBot { get; private set; }
        public int Seat { get; set; }
        public List<Card> HoleCards { get; private set; }
        public BlindStatus BlindStatus { get; set; }
        public PlayerStatus PlayerStatus { get; set; }

        // AI training szempontjából érdemes logolni a játékos döntéseit
        public List<PlayerAction> ActionsHistory { get; private set; }

        protected Player() { }

        public Player(Guid id, string name, int chips, bool isBot, int seat)
        {
            Id = id;
            Name = name;
            Chips = chips;
            IsBot = isBot;
            HoleCards = new List<Card>();
            ActionsHistory = new List<PlayerAction>();
            Seat = seat;

            BlindStatus = BlindStatus.None;
            PlayerStatus = PlayerStatus.Waiting;
        }

        [JsonConstructor]
        public Player(Guid id, string name, int chips, bool isBot, int seat, List<Card> holeCards, List<PlayerAction> actionsHistory)
            => (Id, Name, Chips, IsBot, Seat, HoleCards, ActionsHistory) = (id, name, chips, isBot, seat, holeCards, actionsHistory);


        public void DeductChips(int amount) => Chips -= amount;
        public void AddChips(int amount) => Chips += amount;
        public void ResetHoleCards() => HoleCards = new List<Card>();
        public void HandleAction(PlayerAction action)
        {
            switch (action.ActionType)
            {
                case PlayerActionType.Fold:
                    PlayerStatus = PlayerStatus.Folded;
                    return;
                case PlayerActionType.Call: case PlayerActionType.Raise:
                    if (action.Amount is null || action.Amount <= 0)
                        throw new Exception("Hiba a Player.HandleAction metódusban, mivel az amount <= 0 vagy null");

                    DeductChips((int)action.Amount);
                    if (Chips < 0)
                    {
                        Chips = 0;
                        throw new Exception("Sok lesz ");
                    }
                    return;
                case PlayerActionType.Check:
                    return;
                default:
                    return;
            }
        }
    }

    public enum PlayerActionType
    {
        Fold,
        Call,
        Raise,
        Check
    }
    public enum PlayerStatus
    { 
        Lost,
        Folded,
        Waiting,
        PlayersTurn,
        AllIn
    }
    public enum BlindStatus
    {
        BigBlind,
        SmallBlind,
        None,
    }

    public class PlayerAction
    {
        public PlayerAction() { }

        public PlayerAction(PlayerActionType actionType, int? amount, DateTime timestamp)
        {
            ActionType = actionType;
            Amount = amount;
            Timestamp = timestamp;
        }
        public PlayerAction(PlayerActionType actionType, int? amount)
        {
            ActionType = actionType;
            Amount = amount;
            Timestamp = DateTime.UtcNow;
        }

        public PlayerActionType ActionType { get; set; }
        public int? Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }


}
