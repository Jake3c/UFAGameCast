namespace UFAGameCast.Backend.Models;

public sealed class PlayersResponse
{
    public string Object { get; set; } = string.Empty;
    public List<PlayerData> Data { get; set; } = [];
}

public sealed class PlayerData
{
    public string PlayerID { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
