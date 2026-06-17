namespace UFAGameCast.Backend.Models;

public class GameState
{
    public string HomeTeamName { get; set; } = string.Empty;
    public string AwayTeamName { get; set; } = string.Empty;
    public int HomeTeamScore { get; set; }
    public int AwayTeamScore { get; set; }
    public string GameStatus { get; set; } = string.Empty;
    public bool IsActive => GameStatus != "Upcoming" && GameStatus != "Final" && GameStatus != "Postponed" && GameStatus != "Abandoned";
    public string StreamingUrl { get; set; } = string.Empty;
    public string Week { get; set; } = string.Empty;
    public FieldPosition DiscPosition { get; set; } = new FieldPosition();
}