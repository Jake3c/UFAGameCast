namespace UFAGameCast.Backend.Models;

public class GameEvent
{
    public long Timestamp { get; set; }

    public EventType Type { get; set; }

    public List<string>? Line { get; set; }

    public int? Time { get; set; }

    public string? Thrower { get; set; }
    public double? ThrowerX { get; set; }
    public double? ThrowerY { get; set; }

    public string? Receiver { get; set; }
    public double? ReceiverX { get; set; }
    public double? ReceiverY { get; set; }

    public string? Defender { get; set; }

    public double? TurnoverX { get; set; }
    public double? TurnoverY { get; set; }

    public string? Puller { get; set; }
    public double? PullX { get; set; }
    public double? PullY { get; set; }
    public int? PullMs { get; set; }

    public string? Player { get; set; }
}

public enum EventType
{
    StartDPoint = 1,
    StartOPoint = 2,
    MidpointTimeoutRecording = 3,
    BetweenPointTimeoutRecording = 4,
    MidpointTimeoutOpponent = 5,
    BetweenPointTimeoutOpponent = 6,
    PullInbounds = 7,
    PullOutOfBounds = 8,
    OffsidesRecording = 9,
    OffsidesOpponent = 10,
    Block = 11,
    CallahanThrownByOpponent = 12,
    ThrowawayByOpponent = 13,
    StallAgainstOpponent = 14,
    ScoreByOpponent = 15,
    PenaltyRecording = 16,
    PenaltyOpponent = 17,
    Pass = 18,
    Goal = 19,
    Drop = 20,
    DroppedPull = 21,
    ThrowawayByRecording = 22,
    CallahanThrownByRecording = 23,
    StallAgainstRecording = 24,
    Injury = 25,
    PlayerMisconductFoul = 26,
    PlayerEjected = 27,
    EndQ1 = 28,
    Halftime = 29,
    EndQ3 = 30,
    EndRegulation = 31,
    EndOT1 = 32,
    EndOT2 = 33,
    Delayed = 34
}