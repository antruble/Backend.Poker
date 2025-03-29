using Backend.Poker.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Backend.Poker.Domain.Entities
{
    public class Game
    {
        public Guid Id { get; set; }
        public IList<Player> Players { get; set; } = new List<Player>();
        public Hand? CurrentHand { get; private set; }
        public GameStatus Status { get; private set; }
        public DateTime CreatedOnUtc { get; set; }
        public GameActions CurrentGameAction { get; set; }

        public Game() { }

        public Game(IList<Player> players)
        {
            Id = Guid.NewGuid();
            Players = [.. players.OrderBy(p => p.Seat)];
            Status = GameStatus.NotStarted;
            CreatedOnUtc = DateTime.UtcNow;
            CurrentGameAction = GameActions.Waiting;
        }

        [JsonConstructor]
        public Game(Guid id, IList<Player> players, Hand? currentHand, GameStatus status, DateTime createdOnUtc, GameActions currentGameAction)
        {
            Id = id;
            Players = [.. players.OrderBy(p => p.Seat)];
            CurrentHand = currentHand;
            Status = status;
            CreatedOnUtc = createdOnUtc;
            CurrentGameAction = currentGameAction;
        }

        public Hand StartNewHand()
        {
            Players.Where(p => p.PlayerStatus != PlayerStatus.Lost).ToList().ForEach(p => p.PlayerStatus = PlayerStatus.Waiting);
            Players = Players.OrderBy(p => p.Seat).ToList();



            var lastSmallBlindPlayer = Players.FirstOrDefault(p => p.BlindStatus == BlindStatus.SmallBlind); // A legutolsó kisvak

            if (lastSmallBlindPlayer is not null)
                lastSmallBlindPlayer.BlindStatus = BlindStatus.None;

            // Ha a lastSmallBlindPlayer null, akkor ez az első kör -> kiválasztjuk az első játékost kisvaknak, máskülönben a következő játékos
            Player nextSmallBlindPlayer = lastSmallBlindPlayer is null ?
                                                Players.Where(p => p.PlayerStatus != PlayerStatus.Lost).First()
                                                : GetNextPlayer(lastSmallBlindPlayer.Id);

            Player nextBigBlindPlayer = GetNextPlayer(nextSmallBlindPlayer.Id);

            nextBigBlindPlayer.BlindStatus = BlindStatus.BigBlind;
            nextSmallBlindPlayer.BlindStatus = BlindStatus.SmallBlind;

            var currentPlayer = GetNextPlayer(nextBigBlindPlayer.Id)!;

            currentPlayer.PlayerStatus = PlayerStatus.PlayersTurn;

            CurrentHand = new Hand(currentPlayer);
            CurrentHand.DealHoleCards(Players);

            Status = GameStatus.InProgress;
            CurrentGameAction = GameActions.DealingCards;

            return CurrentHand;
        }

        public void SwitchToTheNextPlayer(Player lastPlayer)
        {
            //Players.Where(p => p.PlayerStatus == PlayerStatus.PlayersTurn).ToList().ForEach(p => p.PlayerStatus = PlayerStatus.Waiting);
            var player = GetNextPlayer(lastPlayer.Id);
            if (lastPlayer.PlayerStatus != PlayerStatus.Folded && lastPlayer.PlayerStatus != PlayerStatus.AllIn)
                lastPlayer.PlayerStatus = PlayerStatus.Waiting;

            player.PlayerStatus = PlayerStatus.PlayersTurn;
        }

        public void DealHoleCards()
        {
            if (CurrentHand != null)
            {
                CurrentHand.DealHoleCards(Players);
            }
        }
        
        public Player GetNextPlayer(Guid lastPlayerId, bool includeAllIn = false)
        {
            // Rendezzük a játékosokat seat szerint növekvő sorrendbe
            var sortedPlayers = Players.OrderBy(p => p.Seat).ToList();

            // Keressük meg a lastPlayer-t a rendezett listában
            var lastPlayer = sortedPlayers.FirstOrDefault(p => p.Id == lastPlayerId)
                             ?? throw new ArgumentException("A megadott játékos nincs benne a listában.", nameof(lastPlayerId));

            List<PlayerStatus> helper = new List<PlayerStatus> { PlayerStatus.Waiting };

            if (includeAllIn)
                helper.Add(PlayerStatus.AllIn);

            // Keressük meg az első olyan játékost, akinek a seat értéke nagyobb, mint a lastPlayeré, és aki Waiting státuszú
            var nextPlayer = sortedPlayers.FirstOrDefault(p => p.Seat > lastPlayer.Seat && helper.Contains(p.PlayerStatus));

            // Ha nincs olyan, akkor vegyük az első aktív játékost a lista elejéről (körkörös asztal)
            if (nextPlayer == null)
                nextPlayer = sortedPlayers.FirstOrDefault(p => helper.Contains(p.PlayerStatus));

            if (nextPlayer == null)
                throw new InvalidOperationException("Nincs olyan játékos, akinek a státusza 'Waiting'.");

            return nextPlayer; ;
        }

        public void SetCurrentPlayerToPivot()
        {
            var currentPlayersId = GetCurrentPlayersId();
            CurrentHand!.PivotPlayerId = currentPlayersId;
        }
        public bool IsNextPlayerPivot()
        {
            var currentPlayersId = GetCurrentPlayersId();
            return GetNextPlayer(currentPlayersId).Id == CurrentHand!.PivotPlayerId; 
        }
        public void SetRoundsFirstPlayerToCurrent()
        {
            Players.Where(p => p.PlayerStatus == PlayerStatus.PlayersTurn).ToList().ForEach(p => p.PlayerStatus = PlayerStatus.Waiting);

            var firstPlayer = Players.FirstOrDefault(p => p.Id == CurrentHand!.FirstPlayerId);
            while (firstPlayer!.PlayerStatus == PlayerStatus.Folded)
            {
                firstPlayer = GetNextPlayer(firstPlayer.Id);
            }
            //CurrentHand!.SetFirstCurrentPlayerId(firstPlayer.Id);
            Players.First(p => p.Id == firstPlayer.Id).PlayerStatus = PlayerStatus.PlayersTurn;
        }
        public Guid GetCurrentPlayersId()
        {
            var player = Players.Where(p => p.PlayerStatus == PlayerStatus.PlayersTurn).ToList();
            if (player.Count == 1)
                return player[0].Id;

            throw new Exception($"Hiba, mivel {player.Count} játékos PlayersTurn státuszú.");

        }
    }

    public enum GameStatus
    {
        NotStarted,
        InProgress,
        Completed
    }
    public enum GameActions 
    { 
        Waiting,
        DealingCards,
        PlayerAction,
        ShowOff,
    }
}
