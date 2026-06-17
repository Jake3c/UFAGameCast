namespace UFAGameCast.Backend.Models;

public class GameResponse
{
    public string Object { get; set; } = string.Empty;
    public List<GameData> Data { get; set; } = new();
}

public class GameData
{
    public string GameID { get; set; } = string.Empty;
    public string AwayTeamID { get; set; } = string.Empty;
    public string HomeTeamID { get; set; } = string.Empty;
    public int AwayScore { get; set; }
    public int HomeScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StartTimestamp { get; set; } = string.Empty;
    public string StartTimezone { get; set; } = string.Empty;
    public string StreamingURL { get; set; } = string.Empty;
    public string UpdateTimestamp { get; set; } = string.Empty;
    public string Week { get; set; } = string.Empty;
}

public enum GameStatus
{
    Upcoming,
    Delayed,
    FirstQuarter,
    EndOfQ1,
    SecondQuarter,
    EndOfHalf,
    ThirdQuarter,
    EndOfQ3,
    FourthQuarter,
    EndOfQ4,
    FirstOT,
    EndOfOT,
    SecondOT,
    Postponed,
    Final
}