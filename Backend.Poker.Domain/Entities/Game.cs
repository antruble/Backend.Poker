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
            Players.Where(p => p.PlayerStatus != PlayerStatus.Lost).ToList().ForEach(p => { p.PlayerStatus = PlayerStatus.Waiting; p.HasToRevealCards = false; });
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

            var currentPlayer = GetNextPlayer(nextBigBlindPlayer.Id);

            CurrentHand = new Hand(currentPlayer);
            CurrentHand.CurrentPlayerId = currentPlayer.Id;
            CurrentHand.DealHoleCards(Players);

            Status = GameStatus.InProgress;
            CurrentGameAction = GameActions.DealingCards;

            return CurrentHand;
        }

        public void SwitchToTheNextPlayer(Player lastPlayer)
        {
            //Players.Where(p => p.PlayerStatus == PlayerStatus.PlayersTurn).ToList().ForEach(p => p.PlayerStatus = PlayerStatus.Waiting);
            var player = GetNextPlayer(lastPlayer.Id);

            CurrentHand!.CurrentPlayerId = player.Id;
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

        public void SetCurrentPlayerToPivot(Guid playerId)
        {
            CurrentHand!.PivotPlayerId = playerId;
        }
        public void SetPreviousPlayerToPivot(Guid playerId)
        {
            // Csak azokat a játékosokat vesszük figyelembe, akik waiting státuszban vannak,
            // és rendezzük őket a Seat érték szerint növekvő sorrendbe.
            var waitingPlayers = Players
                .Where(p => p.PlayerStatus == PlayerStatus.Waiting)
                .OrderBy(p => p.Seat)
                .ToList();

            if (waitingPlayers.Count < 2)
                throw new Exception("Nem sikerült az előző játékost beállítani, ugyanis már csak max 1db player van waiting státuszban.");

            // Keressük meg a jelenlegi játékos seatjét
            var player = Players.First(p => p.Id == playerId) ?? throw new Exception("A megadott játékos nincs waiting státuszban.");
            int index = player.Seat;

            // Körkörös módon, az előző játékos indexe: ha az index 0, akkor a legutolsó lesz, különben index - 1
            int prevIndex = (index - 1 + waitingPlayers.Count) % waitingPlayers.Count;
            var prevPlayer = waitingPlayers[prevIndex];

            // A hand pivot játékosának beállítása
            CurrentHand!.PivotPlayerId = prevPlayer.Id;
        }

        public int GetActivePlayersCount() => Players.Count(p => p.PlayerStatus != PlayerStatus.Lost && p.PlayerStatus != PlayerStatus.Folded);
        public int GetWaitingPlayersCount() => Players.Count(p => p.PlayerStatus == PlayerStatus.Waiting);
        //public bool IsNextPlayerPivot()
        //{
        //    try
        //    {
        //        var sortedPlayers = Players.OrderBy(p => p.Seat).ToList();

        //        // Keressük meg a lastPlayer-t a rendezett listában
        //        var lastPlayer = sortedPlayers.FirstOrDefault(p => p.Id == CurrentHand!.CurrentPlayerId)
        //                         ?? throw new ArgumentException("A soron lévő játékos nincsen a playerek között");

        //        Player? nextPlayer;

        //        var maxSeat = sortedPlayers.Last().Seat;
        //        for (int i = lastPlayer.Seat; i <= maxSeat; i++)
        //        {
        //            var nextSeat = (i + 1) % (maxSeat + 1);
        //            nextPlayer = sortedPlayers.FirstOrDefault(p => p.Seat == nextSeat);
        //            if (nextPlayer is null)
        //                continue;
        //            if (nextPlayer.Id == CurrentHand!.PivotPlayerId)
        //                return true;
        //            if (nextPlayer.PlayerStatus == PlayerStatus.Waiting)
        //                return false;
        //        }
        //        throw new Exception("Nincs pivot játékos");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        throw new Exception(ex.Message);
        //    }
        //}
        public bool IsNextPlayerPivot()
        {
            try
            {
                var sortedPlayers = Players.OrderBy(p => p.Seat).ToList();
                int currentIndex = sortedPlayers.FindIndex(p => p.Id == CurrentHand!.CurrentPlayerId);
                if (currentIndex == -1)
                    throw new ArgumentException("A soron lévő játékos nincsen a playerek között");

                // Iteráljunk körkörösen a listában az aktuális játékost követő elemtől
                for (int offset = 1; offset < sortedPlayers.Count; offset++)
                {
                    int nextIndex = (currentIndex + offset) % sortedPlayers.Count;
                    var candidate = sortedPlayers[nextIndex];
                    if (candidate.Id == CurrentHand!.PivotPlayerId)
                        return true;
                    if (candidate.PlayerStatus == PlayerStatus.Waiting)
                        return false;
                }
                throw new Exception("Nincs pivot játékos");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new Exception(ex.Message);
            }
        }
        public Guid SetRoundsFirstPlayerToCurrent()
        {
            var firstPlayer = Players.FirstOrDefault(p => p.Id == CurrentHand!.FirstPlayerId);
            while (firstPlayer!.PlayerStatus != PlayerStatus.Waiting)
            {
                firstPlayer = GetNextPlayer(firstPlayer.Id);
            }
            CurrentHand!.CurrentPlayerId = firstPlayer.Id;

            return firstPlayer.Id;
        }
        public Guid GetCurrentPlayersId()
        {
            return CurrentHand!.CurrentPlayerId;

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
        DealNextRound,
        ShowOff,
    }
}
