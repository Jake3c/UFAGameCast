namespace UFAGameCast.Backend.Models;

public class TeamsResponse
{
    public string Object { get; set; } = string.Empty;

    public List<Team> Data { get; set; } = new List<Team>();
}

public class Team
{
    public string TeamID { get; set; } = string.Empty;
    public int Year { get; set; }
    public Division Division { get; set; } = new Division();
    public string City { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Abbrev { get; set; } = string.Empty;
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ties { get; set; }
    public int Standing { get; set; }
}

public class Division
{
    public string DivisionID { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}