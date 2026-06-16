namespace UFAGameCast.Backend.Models;

public enum EventType
{
    Pass,
    Goal,
    Turnover,
    Block,
    Catch,
    Drop,
    Other
}

public class FieldPosition
{
    public float X { get; set; }
    public float Y { get; set; }
}

public class PlayerSnapshot
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public FieldPosition Position { get; set; } = new();
    public int JerseyNumber { get; set; }
}

public class PlayEvent
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public EventType EventType { get; set; }
    public int InitiatorPlayerId { get; set; }
    public string InitiatorName { get; set; } = string.Empty;
    public int? ReceiverPlayerId { get; set; }
    public string? ReceiverName { get; set; }
    public int? Distance { get; set; } // in yards
    public string Description { get; set; } = string.Empty;
    public List<PlayerSnapshot> PlayersInvolved { get; set; } = new();
}

public class GameState
{
    public int GameId { get; set; }
    public DateTime CurrentTime { get; set; }
    public string Team1Name { get; set; } = "Team A";
    public string Team2Name { get; set; } = "Team B";
    public int Team1Score { get; set; }
    public int Team2Score { get; set; }
    public List<PlayerSnapshot> AllPlayers { get; set; } = new();
    public PlayEvent? LastPlayEvent { get; set; }
    public FieldPosition DiscPosition { get; set; } = new();
}
