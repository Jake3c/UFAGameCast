using System.Text.Json;
using UFAGameCast.Backend.Models;

namespace UFAGameCast.Backend.Services;

public class UfaGameEventService : BackgroundService
{
    private readonly GameStateService _gameStateService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<UfaGameEventService> _logger;
    private readonly string _gameId;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    // -------------------------
    // CACHES
    // -------------------------

    private readonly HashSet<string> _processedEvents = new();

    private readonly Dictionary<string, Team> _teamCache = new();
    private DateTime _teamCacheLastRefresh = DateTime.MinValue;
    private static readonly TimeSpan TeamCacheDuration = TimeSpan.FromHours(1);

    public UfaGameEventService(
        GameStateService gameStateService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<UfaGameEventService> logger)
    {
        _gameStateService = gameStateService;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _baseUrl = configuration["UFA_API_BASE_URL"]?.TrimEnd('/') ?? "https://www.backend.ufastats.com/api/v1";

        _gameId = configuration["UFA_GAME_ID"] ?? "2026-05-09-SLC-COL";
        //_gameId = configuration["UFA_GAME_ID"] ?? "2026-06-19-TOR-ORE";

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    // =========================================================
    // MAIN LOOP
    // =========================================================

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_gameId))
        {
            _logger.LogWarning("UFA_GAME_ID not configured. Service disabled.");
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        _logger.LogInformation("UFA service started for gameId={gameId}", _gameId);
        var a = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnsureTeamCache(stoppingToken);
                await PollGameCycle(stoppingToken);
                Console.WriteLine(a++);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Polling cycle error");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("UFA service stopped");
    }

    // =========================================================
    // POLLING ORCHESTRATION
    // =========================================================

    private async Task PollGameCycle(CancellationToken token)
    {
        var eventsUrl = $"{_baseUrl}/gameEvents?gameID={Uri.EscapeDataString(_gameId)}";
        var gameUrl = $"{_baseUrl}/games?gameIDs={Uri.EscapeDataString(_gameId)}";

        var eventsTask = _httpClient.GetAsync(eventsUrl, token);
        var gameTask = _httpClient.GetAsync(gameUrl, token);

        await Task.WhenAll(eventsTask, gameTask);

        var eventsResponse = await eventsTask;
        var gameResponse = await gameTask;

        if (!eventsResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Events failed: {status}", eventsResponse.StatusCode);
            return;
        }

        if (!gameResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Game failed: {status}", gameResponse.StatusCode);
            return;
        }

        var gameData = await ExtractGameData(gameResponse, token);

        var updatedGameState = BuildGameState(gameData);

        _gameStateService.UpdateGameState(updatedGameState);

        await ProcessEvents(eventsResponse, updatedGameState, token);
    }

    // =========================================================
    // GAME DATA
    // =========================================================

    private async Task<GameData> ExtractGameData(HttpResponseMessage response, CancellationToken token)
    {
        var gameObject = await response.Content.ReadFromJsonAsync<GameResponse>(_jsonOptions, token);
        var gameData = gameObject.Data?.FirstOrDefault();

        return gameData;
    }

    private GameState BuildGameState(GameData gameData)
    {
        _teamCache.TryGetValue(gameData.HomeTeamID, out var homeTeam);
        _teamCache.TryGetValue(gameData.AwayTeamID, out var awayTeam);

        return new GameState
        {
            HomeTeamName = gameData.HomeTeamID,
            AwayTeamName = gameData.AwayTeamID,
            HomeTeamScore = gameData.HomeScore,
            AwayTeamScore = gameData.AwayScore,
            GameStatus = gameData.Status,
            Week = gameData.Week,
            StreamingUrl = gameData.StreamingURL,

            HomeTeamWins = homeTeam?.Wins ?? 0,
            AwayTeamWins = awayTeam?.Wins ?? 0,
            HomeTeamLosses = homeTeam?.Losses ?? 0,
            AwayTeamLosses = awayTeam?.Losses ?? 0,
            HomeTeamDivisionStanding = homeTeam?.Standing ?? 0,
            AwayTeamDivisionStanding = awayTeam?.Standing ?? 0
        };
    }

    // =========================================================
    // TEAM CACHE
    // =========================================================

    private async Task EnsureTeamCache(CancellationToken token)
    {
        if (_teamCache.Count > 0 &&
            DateTime.UtcNow - _teamCacheLastRefresh < TeamCacheDuration)
        {
            return;
        }

        _logger.LogInformation("Refreshing team cache...");

        var url = $"{_baseUrl}/teams?years={DateTime.UtcNow.Year}";
        var response = await _httpClient.GetAsync(url, token);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Team cache refresh failed: {status}", response.StatusCode);
            return;
        }

        var data = await response.Content.ReadFromJsonAsync<TeamsResponse>(_jsonOptions, token);

        _teamCache.Clear();

        foreach (var team in data?.Data ?? [])
        {
            _teamCache[team.TeamID] = team;
        }

        _teamCacheLastRefresh = DateTime.UtcNow;
    }

    // =========================================================
    // EVENT PROCESSING
    // =========================================================

    private async Task ProcessEvents(HttpResponseMessage response, GameState state, CancellationToken token)
    {
        var eventsObject = await response.Content.ReadFromJsonAsync<GameEventsResponse>(_jsonOptions, token);

        if (eventsObject?.Data == null)
            return;

        var excludedEventTypes = new HashSet<EventType>
        {
            EventType.StartDPoint,
            EventType.StartOPoint,
            EventType.ThrowawayByOpponent,
            EventType.StallAgainstOpponent,
            EventType.ScoreByOpponent,
            EventType.PenaltyOpponent,
            EventType.CallahanThrownByRecording,
            EventType.BetweenPointTimeoutOpponent
        };
        var mergedEvents = eventsObject.Data.HomeEvents
            .Where(evt => !excludedEventTypes.Contains((EventType)evt.Type))
            .Select((evt, i) => ("home", evt, i))
            .Concat(
                eventsObject.Data.AwayEvents
                    .Where(evt => !excludedEventTypes.Contains((EventType)evt.Type))
                    .Select((evt, i) => ("away", evt, i))
            )
            .OrderBy(x => x.Item2.Timestamp)
            .ThenBy(x => x.Item3)
            .ToList();

        var playHistory = new List<GameEventViewModel>();

        foreach (var (source, evt, index) in mergedEvents)
        {
            var playEvent = CreatePlayEvent(evt);

            if (playEvent == null)
                continue;

            playHistory.Add(playEvent);

            //
            // Still track newly-seen events for logging/debugging
            //
            var signature = BuildSignature(source, index, evt);

            if (_processedEvents.Add(signature))
            {
                _logger.LogInformation(
                    "Processed NEW event {type}: {desc}",
                    evt.Type,
                    playEvent.Description);
            }
        }

        //
        // Replace the entire history every poll
        //
        _gameStateService.SetPlayHistory(playHistory);
    }

    // =========================================================
    // HELPERS (UNCHANGED LOGIC)
    // =========================================================

    private static string BuildSignature(string source, int index, GameEvent evt)
        => string.Join('|', source, index, evt.Type, evt.Time?.ToString() ?? "");

    private static GameEventViewModel? CreatePlayEvent(GameEvent evt)
    {
        var distance = CalculateDistance(evt.ThrowerX, evt.ThrowerY, evt.ReceiverX, evt.ReceiverY);
        var description = BuildDescription(evt, distance);
        if (string.IsNullOrWhiteSpace(description)) return null;

        return new GameEventViewModel
        {
            EventType = evt.Type,
            Description = description,
            Time = evt.Time.ToString() ?? ""
        };
    }

    private static string BuildDescription(GameEvent evt, int? distance) => evt.Type switch
    {
        //Fill in descs here
        EventType.StartDPoint => $"Start of D-Point by {evt.Thrower ?? "unknown"}",
        EventType.StartOPoint => $"Start of O-Point by {evt.Thrower ?? "unknown"}",
        EventType.MidpointTimeoutRecording => $"Midpoint timeout for {evt.Thrower ?? "unknown"}",
        EventType.BetweenPointTimeoutRecording => $"Between point timeout for {evt.Thrower ?? "unknown"}",
        //EventType.MidpointTimeoutOpponent => $"Midpoint timeout for opponent {evt.Thrower ?? "unknown"}",
        //EventType.BetweenPointTimeoutOpponent => $"Between point timeout for opponent {evt.Thrower ?? "unknown"}",
        EventType.PullInbounds => $"Pull inbounds by {evt.Puller ?? "unknown"}",
        EventType.PullOutOfBounds => $"Pull out of bounds by {evt.Puller ?? "unknown"}",
        EventType.OffsidesRecording => $"Offside for {evt.Thrower ?? "unknown"}",
        //EventType.OffsidesOpponent => $"Offside for opponent {evt.Thrower ?? "unknown"}",
        EventType.Block => $"Block by {evt.Defender ?? "unknown"}",
        EventType.CallahanThrownByOpponent => $"Callahan by {evt.Receiver ?? "unknown"}",
        EventType.Pass => $"{distance} yard pass from {evt.Thrower} to {evt.Receiver}",
        EventType.Goal => $"{distance} yard goal by {evt.Thrower}",
        EventType.Drop => $"Drop by {evt.Thrower}",
        EventType.DroppedPull => $"Dropped pull by {evt.Puller}",
        EventType.ThrowawayByRecording => $"Throwaway by {evt.Thrower}",
        //EventType.CallahanThrownByRecording => $"Callahan thrown by {evt.Thrower}",
        EventType.StallAgainstRecording => $"{evt.Defender} stalled",
        EventType.Injury => $"{evt.Player} injured on the play",
        EventType.PlayerMisconductFoul => $"{evt.Player} foul",
        EventType.PlayerEjected => $"{evt.Player} ejected from game",
        EventType.EndQ1 => "End of Q1",
        EventType.Halftime => "Halftime",
        EventType.EndQ3 => "End of Q3",
        EventType.EndRegulation => "End of Regulation Time",
        EventType.EndOT1 => "End of OT1",
        EventType.EndOT2 => "End of OT2",
        _ => $"Unknown event: {evt.Type}"
    };

    private static int? CalculateDistance(double? x1, double? y1, double? x2, double? y2)
    {
        if (!x1.HasValue || !y1.HasValue || !x2.HasValue || !y2.HasValue)
            return null;

        var dx = x2.Value - x1.Value;
        var dy = y2.Value - y1.Value;

        return (int)Math.Sqrt(dx * dx + dy * dy);
    }
}