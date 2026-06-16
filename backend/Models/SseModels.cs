namespace UFAGameCast.Backend.Models;

/// <summary>
/// Server-Sent Events message wrapper for streaming to clients
/// </summary>
public class SseEvent
{
    public string EventType { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}
