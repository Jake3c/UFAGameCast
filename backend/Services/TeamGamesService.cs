using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using UFAGameCast.Backend.Models;

namespace UFAGameCast.Backend.Services;

public sealed class TeamGamesService : ITeamGamesService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public TeamGamesService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["UFA_API_BASE_URL"]?.TrimEnd('/')
            ?? "https://www.backend.ufastats.com/api/v1";
    }

    public async Task<IReadOnlyList<TeamGameResponse>> GetTeamGamesAsync(
        string teamName,
        CancellationToken cancellationToken)
    {
        var url = $"{_baseUrl}/games/?teamIDs={Uri.EscapeDataString(teamName)}&date=0000:";
        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                "The UFA games service returned an unsuccessful response.",
                null,
                response.StatusCode);
        }

        var games = await response.Content.ReadFromJsonAsync<GameResponse>(
            cancellationToken: cancellationToken);

        return (games?.Data ?? [])
            .Select(game => new TeamGameResponse(
                game.GameID,
                game.AwayTeamID,
                game.HomeTeamID,
                game.AwayScore,
                game.HomeScore,
                game.Status,
                DateTimeOffset.Parse(game.StartTimestamp, CultureInfo.InvariantCulture),
                game.StartTimezone,
                game.StreamingURL,
                game.Location))
            .ToList();
    }
}
