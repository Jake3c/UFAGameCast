namespace UFAGameCast.Backend.Models;

/// <summary>A game played by a requested team.</summary>
public sealed record TeamGameResponse(
    string GameId,
    string AwayTeam,
    string HomeTeam,
    int AwayScore,
    int HomeScore,
    string Status,
    DateTimeOffset StartTime,
    string StartTimeZone,
    string? StreamingUrl,
    string? Location);
