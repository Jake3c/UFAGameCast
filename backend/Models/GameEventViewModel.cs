namespace UFAGameCast.Backend.Models;

public class GameEventViewModel
{
    public int Id { get; set; }
    public EventType EventType { get; set; }

    public int InitiatorPlayerId { get; set; }
    public string InitiatorName { get; set; } = string.Empty;

    public int ReceiverPlayerId { get; set; }
    public string? ReceiverName { get; set; }

    public int? Distance { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}