using System.Text.Json;
using System.Globalization;
using UFAGameCast.Backend.Models;

namespace UFAGameCast.Backend.Services;

public class UfaGameEventService : BackgroundService
{
    private readonly GameStateService _gameStateService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<UfaGameEventService> _logger;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    // -------------------------
    // CACHES
    // -------------------------

    private readonly HashSet<string> _processedEvents = new();
    private readonly object _processedEventsLock = new();

    private readonly Dictionary<string, Team> _teamCache = new();
    private DateTime _teamCacheLastRefresh = DateTime.MinValue;
    private static readonly TimeSpan TeamCacheDuration = TimeSpan.FromHours(1);

    public UfaGameEventService(
        GameStateService gameStateService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<UfaGameEventService> logger)
    {
        Console.WriteLine("UFA CONSTRUCTOR HIT");

        _gameStateService = gameStateService;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _baseUrl = configuration["UFA_API_BASE_URL"]?.TrimEnd('/') ?? "https://www.backend.ufastats.com/api/v1";
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
        _logger.LogInformation("UFA service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var gameId = _gameStateService.GetCurrentGameId();

            if (string.IsNullOrWhiteSpace(gameId))
            {
                _logger.LogInformation("Waiting for gameId...");
                await Task.Delay(1000, stoppingToken);
                continue;
            }

            try
            {
                await EnsureTeamCache(stoppingToken);
                await PollGameCycle(gameId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Polling cycle error");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    // =========================================================
    // POLLING ORCHESTRATION
    // =========================================================

    private async Task PollGameCycle(string gameId, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(gameId))
        {
            _logger.LogWarning("No active game. Skipping poll cycle.");
            return;
        }

        var eventsUrl = $"{_baseUrl}/gameEvents?gameID={Uri.EscapeDataString(gameId)}";
        var gameUrl = $"{_baseUrl}/games?gameIDs={Uri.EscapeDataString(gameId)}";

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

        if (!_gameStateService.UpdateGameState(gameId, updatedGameState))
        {
            return;
        }

        if (!updatedGameState.IsActive)
        {
            return;
        }

        await ProcessEvents(gameId, eventsResponse, token);
    }

    // =========================================================
    // GAME DATA
    // =========================================================

    public async Task RefreshGame(string gameId)
    {
        await EnsureTeamCache(CancellationToken.None);
        await PollGameCycle(gameId, CancellationToken.None);
    }

    public async Task<GameState> GetGameStateAsync(string gameId, CancellationToken cancellationToken)
    {
        await EnsureTeamCache(cancellationToken);

        var url = $"{_baseUrl}/games?gameIDs={Uri.EscapeDataString(gameId)}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                "The UFA games API returned an unsuccessful response.",
                null,
                response.StatusCode);
        }

        var gameResponse = await response.Content.ReadFromJsonAsync<GameResponse>(
            _jsonOptions,
            cancellationToken);
        var gameData = gameResponse?.Data.FirstOrDefault(game =>
            string.Equals(game.GameID, gameId, StringComparison.OrdinalIgnoreCase));

        if (gameData is null)
        {
            throw new KeyNotFoundException($"No game was found with ID '{gameId}'.");
        }

        return BuildGameState(gameData);
    }

    public async Task<(
        GameState GameState,
        IReadOnlyList<GameEvent> GameEvents,
        IReadOnlyList<GameEventViewModel> PlayHistory,
        IReadOnlyDictionary<string, string> PlayerNames)> GetGameSnapshotAsync(
        string gameId,
        CancellationToken cancellationToken)
    {
        await EnsureTeamCache(cancellationToken);

        var gameUrl = $"{_baseUrl}/games?gameIDs={Uri.EscapeDataString(gameId)}";
        using var gameResponse = await _httpClient.GetAsync(gameUrl, cancellationToken);
        if (!gameResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                "The UFA games API returned an unsuccessful response.",
                null,
                gameResponse.StatusCode);
        }

        var gameResult = await gameResponse.Content.ReadFromJsonAsync<GameResponse>(
            _jsonOptions,
            cancellationToken);
        var gameData = gameResult?.Data.FirstOrDefault(game =>
            string.Equals(game.GameID, gameId, StringComparison.OrdinalIgnoreCase));

        if (gameData is null)
        {
            throw new KeyNotFoundException($"No game was found with ID '{gameId}'.");
        }

        var gameState = BuildGameState(gameData);
        var playerNames = await GetPlayerNamesAsync(gameData, cancellationToken);
        if (IsUpcomingOrUnplayed(gameState.GameStatus))
        {
            return (
                gameState,
                Array.Empty<GameEvent>(),
                Array.Empty<GameEventViewModel>(),
                playerNames);
        }

        var eventsUrl = $"{_baseUrl}/gameEvents?gameID={Uri.EscapeDataString(gameId)}";
        using var eventsResponse = await _httpClient.GetAsync(eventsUrl, cancellationToken);
        if (!eventsResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                "The UFA game events API returned an unsuccessful response.",
                null,
                eventsResponse.StatusCode);
        }

        var eventsResult = await eventsResponse.Content.ReadFromJsonAsync<GameEventsResponse>(
            _jsonOptions,
            cancellationToken);
        var timestampOrderedEvents = MergeEventsByTimestamp(eventsResult?.Data);
        var orderingPreview = HistoricalGameEventOrderer.CreatePreview(gameId, timestampOrderedEvents);
        var gameEvents = orderingPreview.NormalizedEvents
            .Select(entry => new TimedGameEvent(entry.Event, entry.Source, entry.SourceIndex, false))
            .ToList();
        var mappedEvents = gameEvents
            .Select((item, index) =>
            {
                item.Event.Id = index + 1;
                return CreatePlayEvent(item.Event, index + 1);
            })
            .ToList();

        lock (_processedEventsLock)
        {
            foreach (var item in gameEvents)
            {
                _processedEvents.Add(BuildSignature(gameId, item.Source, item.SourceIndex, item.Event));
            }
        }

        return (
            gameState,
            gameEvents.Select(item => item.Event).ToList(),
            mappedEvents,
            playerNames);
    }

    private async Task<IReadOnlyDictionary<string, string>> GetPlayerNamesAsync(
        GameData gameData,
        CancellationToken cancellationToken)
    {
        if (!DateTimeOffset.TryParse(
                gameData.StartTimestamp,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var startTimestamp))
        {
            throw new InvalidDataException(
                $"The start timestamp for game '{gameData.GameID}' could not be parsed.");
        }

        var year = startTimestamp.Year;
        var homePlayersTask = GetTeamPlayersAsync(gameData.HomeTeamID, year, cancellationToken);
        var awayPlayersTask = GetTeamPlayersAsync(gameData.AwayTeamID, year, cancellationToken);
        await Task.WhenAll(homePlayersTask, awayPlayersTask);

        return homePlayersTask.Result
            .Concat(awayPlayersTask.Result)
            .GroupBy(player => player.PlayerID, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => $"{group.First().FirstName} {group.First().LastName}".Trim(),
                StringComparer.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlyList<PlayerData>> GetTeamPlayersAsync(
        string teamId,
        int year,
        CancellationToken cancellationToken)
    {
        var url = $"{_baseUrl}/players?teamIDs={Uri.EscapeDataString(teamId)}&years={year}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"The UFA players API returned an unsuccessful response for team '{teamId}'.",
                null,
                response.StatusCode);
        }

        var playersResponse = await response.Content.ReadFromJsonAsync<PlayersResponse>(
            _jsonOptions,
            cancellationToken);
        if (playersResponse is null)
        {
            throw new InvalidDataException(
                $"The UFA players API returned an empty response for team '{teamId}'.");
        }

        return playersResponse.Data;
    }

    public async Task<GameEventOrderingPreviewResponse> GetHistoricalOrderingPreviewAsync(
        string gameId,
        CancellationToken cancellationToken)
    {
        var eventsUrl = $"{_baseUrl}/gameEvents?gameID={Uri.EscapeDataString(gameId)}";
        using var response = await _httpClient.GetAsync(eventsUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                "The UFA game events API returned an unsuccessful response.",
                null,
                response.StatusCode);
        }

        var eventsResult = await response.Content.ReadFromJsonAsync<GameEventsResponse>(
            _jsonOptions,
            cancellationToken);
        var timestampOrderedEvents = MergeEventsByTimestamp(eventsResult?.Data);
        return HistoricalGameEventOrderer.CreatePreview(gameId, timestampOrderedEvents);
    }

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
            GameId = gameData.GameID,
            HomeTeamName = gameData.HomeTeamID,
            AwayTeamName = gameData.AwayTeamID,
            HomeTeamScore = gameData.HomeScore,
            AwayTeamScore = gameData.AwayScore,
            GameStatus = gameData.Status,
            Week = gameData.Week,
            StreamingUrl = gameData.StreamingURL ?? string.Empty,

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

    private async Task ProcessEvents(string gameId, HttpResponseMessage response, CancellationToken token)
    {
        var eventsObject = await response.Content.ReadFromJsonAsync<GameEventsResponse>(_jsonOptions, token);

        if (eventsObject?.Data == null)
            return;

        foreach (var item in MergeEventsByTimestamp(eventsObject.Data))
        {
            var signature = BuildSignature(gameId, item.Source, item.SourceIndex, item.Event);
            bool isNew;
            lock (_processedEventsLock)
            {
                isNew = _processedEvents.Add(signature);
            }

            if (!isNew)
            {
                continue;
            }

            var play = CreatePlayEvent(item.Event, 0);
            if (play is not null)
            {
                _gameStateService.PublishPlayEvent(gameId, play);
            }
        }
    }

    // =========================================================
    // EVENT MAPPING AND ORDERING
    // =========================================================

    private static string BuildSignature(string gameId, string source, int index, GameEvent evt)
        => string.Join('|', gameId, source, index, evt.Type, evt.Time?.ToString() ?? "");

    private static bool IsUpcomingOrUnplayed(string status) =>
        string.Equals(status, "Upcoming", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "Postponed", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "Abandoned", StringComparison.OrdinalIgnoreCase);

    private static List<TimedGameEvent> MergeEventsByTimestamp(GameEventsData? data)
    {
        if (data is null)
        {
            return [];
        }

        // Put events without a timestamp last and use source/index to make ties deterministic.
        var events = data.HomeEvents
            .Select((gameEvent, index) => CreateTimedGameEvent(gameEvent, "home", index, true))
            .Concat(data.AwayEvents.Select((gameEvent, index) => CreateTimedGameEvent(gameEvent, "away", index, false)))
            .Where(item => item.Event.Type is not (
                EventType.ScoreByOpponent or
                EventType.ThrowawayByOpponent or
                EventType.MidpointTimeoutOpponent or
                EventType.BetweenPointTimeoutOpponent))
            .ToList();

        events = RemoveDuplicatePenaltyReports(events);

        var latestEndOfPeriodEvents = events
            .Where(item => IsEndOfPeriodEvent(item.Event.Type))
            .GroupBy(item => item.Event.Type)
            .Select(group => group
                .OrderBy(item => item.Event.Timestamp.HasValue ? 0 : 1)
                .ThenByDescending(item => item.Event.Timestamp)
                .ThenBy(item => item.Source, StringComparer.Ordinal)
                .ThenBy(item => item.SourceIndex)
                .First());

        return events
            .Where(item => !IsEndOfPeriodEvent(item.Event.Type))
            .Concat(latestEndOfPeriodEvents)
            .OrderBy(item => item.Event.Timestamp.HasValue ? 0 : 1)
            .ThenBy(item => item.Event.Timestamp)
            .ThenBy(item => item.Source, StringComparer.Ordinal)
            .ThenBy(item => item.SourceIndex)
            .ToList();
    }

    private static bool IsEndOfPeriodEvent(EventType eventType) =>
        eventType is
            EventType.EndQ1 or
            EventType.Halftime or
            EventType.EndQ3 or
            EventType.EndRegulation or
            EventType.EndOT1 or
            EventType.EndOT2;

    private static TimedGameEvent CreateTimedGameEvent(
        GameEvent gameEvent,
        string source,
        int sourceIndex,
        bool isHomeTeam)
    {
        var wasPenaltyOpponent = gameEvent.Type == EventType.PenaltyOpponent;
        NormalizeEvent(gameEvent, isHomeTeam);
        return new TimedGameEvent(gameEvent, source, sourceIndex, wasPenaltyOpponent);
    }

    private static List<TimedGameEvent> RemoveDuplicatePenaltyReports(
        IReadOnlyList<TimedGameEvent> events)
    {
        var matches = new List<(int ConvertedIndex, int RecordingIndex, decimal Skew)>();
        for (var convertedIndex = 0; convertedIndex < events.Count; convertedIndex++)
        {
            var converted = events[convertedIndex];
            if (!converted.WasPenaltyOpponent || converted.Event.Timestamp is not long convertedTimestamp)
            {
                continue;
            }

            for (var recordingIndex = 0; recordingIndex < events.Count; recordingIndex++)
            {
                var recording = events[recordingIndex];
                if (recording.WasPenaltyOpponent ||
                    recording.Event.Type != EventType.PenaltyRecording ||
                    recording.Source == converted.Source ||
                    recording.Event.IsHomeTeam != converted.Event.IsHomeTeam ||
                    recording.Event.Timestamp is not long recordingTimestamp)
                {
                    continue;
                }

                var skew = Math.Abs((decimal)convertedTimestamp - recordingTimestamp);
                if (skew <= HistoricalGameEventOrderer.MaxTimestampSkewSeconds)
                {
                    matches.Add((convertedIndex, recordingIndex, skew));
                }
            }
        }

        var matchedConverted = new HashSet<int>();
        var matchedRecording = new HashSet<int>();
        var duplicateIndices = new HashSet<int>();
        foreach (var match in matches
            .OrderBy(match => match.Skew)
            .ThenBy(match => events[match.ConvertedIndex].Event.Timestamp)
            .ThenBy(match => events[match.ConvertedIndex].Source, StringComparer.Ordinal)
            .ThenBy(match => events[match.ConvertedIndex].SourceIndex))
        {
            if (matchedConverted.Contains(match.ConvertedIndex) ||
                matchedRecording.Contains(match.RecordingIndex))
            {
                continue;
            }

            matchedConverted.Add(match.ConvertedIndex);
            matchedRecording.Add(match.RecordingIndex);
            duplicateIndices.Add(match.ConvertedIndex);
        }

        return events
            .Where((_, index) => !duplicateIndices.Contains(index))
            .ToList();
    }

    private static GameEvent NormalizeEvent(GameEvent gameEvent, bool isHomeTeam)
    {
        gameEvent.IsHomeTeam = isHomeTeam;

        if (gameEvent.Type == EventType.PenaltyOpponent)
        {
            gameEvent.IsHomeTeam = !gameEvent.IsHomeTeam;
            gameEvent.Type = EventType.PenaltyRecording;
        }

        return gameEvent;
    }

    private static GameEventViewModel CreatePlayEvent(GameEvent evt, int id)
    {
        var distance = CalculateDistance(evt.ThrowerX, evt.ThrowerY, evt.ReceiverX, evt.ReceiverY);
        var description = BuildDescription(evt, distance);

        return new GameEventViewModel
        {
            Id = id,
            EventType = evt.Type,
            Time = evt.Time?.ToString() ?? string.Empty,
            Timestamp = evt.Timestamp,
            InitiatorName = evt.Thrower ?? string.Empty,
            ReceiverName = evt.Receiver,
            Distance = distance,
            Description = description,
            SourceEvent = evt
        };
    }

    private static string BuildDescription(GameEvent evt, int? distance) => evt.Type switch
    {
        //Fill in descs here
        EventType.StartDPoint => $"Start of D-Point by {FormatPlayerName(evt.Thrower)}",
        EventType.StartOPoint => $"Start of O-Point by {FormatPlayerName(evt.Thrower)}",
        EventType.MidpointTimeoutRecording => $"Midpoint timeout for {FormatPlayerName(evt.Thrower)}",
        EventType.BetweenPointTimeoutRecording => $"Between point timeout for {FormatPlayerName(evt.Thrower)}",
        //EventType.MidpointTimeoutOpponent => $"Midpoint timeout for opponent {FormatPlayerName(evt.Thrower)}",
        //EventType.BetweenPointTimeoutOpponent => $"Between point timeout for opponent {FormatPlayerName(evt.Thrower)}",
        EventType.PullInbounds => $"Pull inbounds by {FormatPlayerName(evt.Puller)}",
        EventType.PullOutOfBounds => $"Pull out of bounds by {FormatPlayerName(evt.Puller)}",
        EventType.OffsidesRecording => $"Offside for {FormatPlayerName(evt.Thrower)}",
        //EventType.OffsidesOpponent => $"Offside for opponent {FormatPlayerName(evt.Thrower)}",
        EventType.Block => $"Block by {FormatPlayerName(evt.Defender)}",
        EventType.CallahanThrownByOpponent => $"Callahan by {FormatPlayerName(evt.Receiver)}",
        EventType.Pass => $"{distance} yard pass from {FormatPlayerName(evt.Thrower)} to {FormatPlayerName(evt.Receiver)}",
        EventType.Goal => $"{distance} yard GOAL from {FormatPlayerName(evt.Thrower)} to {FormatPlayerName(evt.Receiver)}",
        EventType.Drop => $"Drop by {FormatPlayerName(evt.Thrower)}",
        EventType.DroppedPull => $"Dropped pull by {FormatPlayerName(evt.Puller)}",
        EventType.ThrowawayByRecording => $"Throwaway by {FormatPlayerName(evt.Thrower)}",
        EventType.PenaltyRecording => $"Foul by {FormatPlayerName(evt.Player)}",
        //EventType.CallahanThrownByRecording => $"Callahan thrown by {evt.Thrower}",
        EventType.StallAgainstRecording => $"{FormatPlayerName(evt.Defender)} stalled",
        EventType.Injury => $"{FormatPlayerName(evt.Player)} injured on the play",
        EventType.PlayerMisconductFoul => $"Technical foul on {FormatPlayerName(evt.Player)}",
        EventType.PlayerEjected => $"{FormatPlayerName(evt.Player)} ejected from game",
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

    private static string FormatPlayerName(string? playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName) || playerName.Length < 2)
        {
            return "unknown";
        }

        var name = playerName.Substring(1);

        return char.ToUpper(name[0]) + name.Substring(1);
    }
}