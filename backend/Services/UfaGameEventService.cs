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
    private readonly HashSet<string> _processedEvents = new();

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
        _gameId = configuration["UFA_GAME_ID"] ?? "2026-05-09-SLC-COL"; //TODO: Fix hardcode here
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_gameId))
        {
            _logger.LogWarning("UFA_GAME_ID is not configured. UFA game event polling is disabled.");
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        _logger.LogInformation("UFA game event service started for gameId={gameId}", _gameId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollGameEventsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling UFA game events");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("UFA game event service stopped");
    }

    private async Task PollGameEventsAsync(CancellationToken cancellationToken)
    {
        var eventsUrl = $"{_baseUrl}/gameEvents?gameID={Uri.EscapeDataString(_gameId)}";
        var gameUrl = $"{_baseUrl}/games?gameIDs={Uri.EscapeDataString(_gameId)}";
        var playerUrl = $"{_baseUrl}/playerGameStats?gameID={Uri.EscapeDataString(_gameId)}";

        var eventsTask = _httpClient.GetAsync(eventsUrl, cancellationToken);
        var gameTask = _httpClient.GetAsync(gameUrl, cancellationToken);
        var playerTask = _httpClient.GetAsync(playerUrl, cancellationToken);

        var eventsResponse = await eventsTask;
        var gameResponse = await gameTask;
        var playerResponse = await playerTask;

        if (!eventsResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("UFA game events request returned {statusCode}: {url}", eventsResponse.StatusCode, eventsUrl);
            return;
        }
        else if (!gameResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("UFA game request returned {statusCode}: {url}", gameResponse.StatusCode, gameUrl);
            return;
        }
        else if (!playerResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("UFA player request returned {statusCode}: {url}", playerResponse.StatusCode, playerUrl);
            return;
        }

        var gameObject = await gameResponse.Content.ReadFromJsonAsync<GameResponse>(_jsonOptions, cancellationToken);
        if (gameObject?.Data == null)
        {
            _logger.LogWarning("UFA game response did not contain data");
            return;
        }

        var gameData = gameObject.Data.FirstOrDefault();
        if (gameData == null)
        {
            _logger.LogWarning("UFA game data not found");
            return;
        }

        var updatedGameState = new GameState
        {
            HomeTeamName = gameData.HomeTeamID,
            AwayTeamName = gameData.AwayTeamID,
            HomeTeamScore = gameData.HomeScore,
            AwayTeamScore = gameData.AwayScore,
            GameStatus = gameData.Status,
            Week = gameData.Week,
            StreamingUrl = gameData.StreamingURL
        };

        var eventsObject = await eventsResponse.Content.ReadFromJsonAsync<GameEventsResponse>(_jsonOptions, cancellationToken);
        if (eventsObject?.Data == null)
        {
            _logger.LogWarning("UFA game events response did not contain data");
            return;
        }

        var mergedEvents = eventsObject.Data.HomeEvents
            .Select((evt, index) => new { Source = "home", Event = evt, Index = index })
            .Concat(eventsObject.Data.AwayEvents.Select((evt, index) => new { Source = "away", Event = evt, Index = index }))
            .OrderBy(pair => pair.Event.Timestamp)
            .ThenBy(pair => pair.Index)
            .ToList();

        foreach (var pair in mergedEvents)
        {
            var signature = BuildSignature(pair.Source, pair.Index, pair.Event);
            if (!_processedEvents.Add(signature))
            {
                continue;
            }

            var playEvent = CreatePlayEvent(pair.Event);
            if (playEvent == null)
            {
                continue;
            }

            _gameStateService.UpdateGameState(updatedGameState);
            _gameStateService.AddPlayEvent(playEvent);

            _logger.LogInformation("Processed UFA event type={eventType} description={description}", pair.Event.Type, playEvent.Description);
        }
    }

    private static string BuildSignature(string source, int index, GameEvent evt)
    {
        return string.Join('|',
            source,
            index,
            evt.Type,
            evt.Time?.ToString() ?? string.Empty,
            evt.Thrower ?? string.Empty,
            evt.Receiver ?? string.Empty,
            evt.Puller ?? string.Empty,
            evt.TurnoverX?.ToString() ?? string.Empty,
            evt.TurnoverY?.ToString() ?? string.Empty);
    }

    private static GameEventViewModel? CreatePlayEvent(GameEvent evt)
    {
        var description = BuildDescription(evt);

        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return new GameEventViewModel
        {
            EventType = evt.Type,
            InitiatorPlayerId = 0,
            InitiatorName = evt.Thrower ?? evt.Puller ?? evt.Defender ?? evt.Receiver ?? "Unknown",
            ReceiverPlayerId = 0,
            ReceiverName = evt.Receiver ?? evt.Defender ?? evt.Thrower,
            Distance = CalculateDistance(evt.ThrowerX, evt.ThrowerY, evt.ReceiverX, evt.ReceiverY),
            Description = description
        };
    }

    private static string BuildDescription(GameEvent evt)
    {
        return evt.Type switch
        {
            (EventType)7 => $"Pull inbounds by {evt.Puller ?? "unknown player"}",
            (EventType)18 => $"Pass from {evt.Thrower ?? "unknown"} to {evt.Receiver ?? "unknown"}",
            (EventType)19 => $"Goal from {evt.Thrower ?? "unknown"} to {evt.Receiver ?? "unknown"}",
            (EventType)20 => $"Drop by {evt.Receiver ?? evt.Thrower ?? "unknown"}",
            (EventType)22 => $"Throwaway by {evt.Thrower ?? "unknown"}",
            _ => $"Event type {evt.Type}"
        };
    }

    private static int? CalculateDistance(double? x1, double? y1, double? x2, double? y2)
    {
        if (!x1.HasValue || !y1.HasValue || !x2.HasValue || !y2.HasValue)
        {
            return null;
        }

        var dx = x2.Value - x1.Value;
        var dy = y2.Value - y1.Value;
        return (int)Math.Round(Math.Sqrt(dx * dx + dy * dy));
    }
}
