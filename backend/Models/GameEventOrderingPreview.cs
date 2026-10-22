namespace UFAGameCast.Backend.Models;

/// <summary>Compares timestamp-ordered events with a historical ordering preview.</summary>
public sealed record GameEventOrderingPreviewResponse(
    string GameId,
    int MaxCrossFeedSkewSeconds,
    IReadOnlyList<GameEventOrderingEntry> TimestampOrderedEvents,
    IReadOnlyList<GameEventOrderingEntry> NormalizedEvents,
    IReadOnlyList<GameEventOrderingChange> Reorders,
    IReadOnlyList<GameEventOrderingWarning> UnresolvedConflicts);

/// <summary>An event and its position and source in an ordering preview.</summary>
public sealed record GameEventOrderingEntry(
    int Position,
    string Source,
    int SourceIndex,
    GameEvent Event);

/// <summary>A pair of events whose relative order changed in the preview.</summary>
public sealed record GameEventOrderingChange(
    int MovedEventId,
    EventType MovedEventType,
    string MovedSource,
    int OriginalPosition,
    int NormalizedPosition,
    int OvertakenEventId,
    EventType OvertakenEventType,
    string OvertakenSource,
    int? TimestampSkewSeconds,
    string Rule);

/// <summary>A sequence rule that could not be applied safely.</summary>
public sealed record GameEventOrderingWarning(
    int EarlierEventId,
    EventType EarlierEventType,
    string EarlierSource,
    int LaterEventId,
    EventType LaterEventType,
    string LaterSource,
    string Rule,
    string Reason,
    int? TimestampSkewSeconds);
