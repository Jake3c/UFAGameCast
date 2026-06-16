namespace UFAGameCast.Backend.Models;

public class PlayerSnapshot
{
    public int PlayerId { get; set; }

    public string PlayerName { get; set; } = string.Empty;

    public string Team { get; set; } = string.Empty;

    public int JerseyNumber { get; set; }

    public FieldPosition Position { get; set; } = new();
}