using System.Text.Json.Serialization;

namespace UFAGameCast.Backend.Models;

/// <summary>
/// Server-Sent Events message wrapper for streaming to clients
/// </summary>
public class GameEventsResponse
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public GameEventsData Data { get; set; } = new();
}

public class GameEventsData
{
    [JsonPropertyName("homeEvents")]
    public List<GameEvent> HomeEvents { get; set; } = [];

    [JsonPropertyName("awayEvents")]
    public List<GameEvent> AwayEvents { get; set; } = [];
}