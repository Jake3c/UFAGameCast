namespace UFAGameCast.Backend.Models;

public class GameState
{
    public int GameId { get; set; }

    public string Time { get; set; } = string.Empty;

    public string Team1Name { get; set; } = string.Empty;

    public string Team2Name { get; set; } = string.Empty;

    public int Team1Score { get; set; }

    public int Team2Score { get; set; }

    public List<PlayerSnapshot> AllPlayers { get; set; } = [];

    public FieldPosition DiscPosition { get; set; } = new();

    public GameEventViewModel? LastPlayEvent { get; set; }
}