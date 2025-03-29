namespace Backend.Poker.DTOs
{
    public class CreateGameRequest
    {
        public string PlayerName { get; set; }
        public int NumOfBots { get; set; }
    }
}
