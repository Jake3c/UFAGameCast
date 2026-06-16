using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        _gameId = configuration["UFA_GAME_ID"] ?? string.Empty;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
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
        var url = $"{_baseUrl}/gameEvents?gameID={Uri.EscapeDataString(_gameId)}";
        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("UFA game events request returned {statusCode}: {url}", response.StatusCode, url);
            return;
        }

        var payload = await response.Content.ReadFromJsonAsync<UfaGameEventsResponse>(_jsonOptions, cancellationToken);
        if (payload?.Data == null)
        {
            _logger.LogWarning("UFA game events response did not contain data");
            return;
        }

        var mergedEvents = payload.Data.HomeEvents
            .Select((evt, index) => new { Source = "home", Event = evt, Index = index })
            .Concat(payload.Data.AwayEvents.Select((evt, index) => new { Source = "away", Event = evt, Index = index }))
            .OrderBy(pair => pair.Event.Time ?? int.MaxValue)
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

            var gameState = _gameStateService.GetCurrentGameState();
            var updatedState = new GameState
            {
                GameId = gameState.GameId,
                CurrentTime = DateTime.UtcNow,
                Team1Name = gameState.Team1Name,
                Team2Name = gameState.Team2Name,
                Team1Score = gameState.Team1Score,
                Team2Score = gameState.Team2Score,
                AllPlayers = gameState.AllPlayers,
                LastPlayEvent = playEvent,
                DiscPosition = gameState.DiscPosition
            };

            if (TryResolveDiscPosition(pair.Event, out var discPosition))
            {
                updatedState.DiscPosition = discPosition;
            }

            _gameStateService.UpdateGameState(updatedState);
            _gameStateService.AddPlayEvent(playEvent);

            _logger.LogInformation("Processed UFA event type={eventType} description={description}", pair.Event.Type, playEvent.Description);
        }
    }

    private static string BuildSignature(string source, int index, UfaGameEvent evt)
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

    private static PlayEvent? CreatePlayEvent(UfaGameEvent evt)
    {
        var eventType = MapEventType(evt.Type);
        var description = BuildDescription(evt);

        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return new PlayEvent
        {
            EventType = eventType,
            InitiatorPlayerId = 0,
            InitiatorName = evt.Thrower ?? evt.Puller ?? evt.Defender ?? evt.Receiver ?? "Unknown",
            ReceiverPlayerId = 0,
            ReceiverName = evt.Receiver ?? evt.Defender ?? evt.Thrower,
            Distance = CalculateDistance(evt.ThrowerX, evt.ThrowerY, evt.ReceiverX, evt.ReceiverY),
            Description = description,
            PlayersInvolved = new List<PlayerSnapshot>()
        };
    }

    private static EventType MapEventType(int type) => type switch
    {
        18 => EventType.Pass,
        19 => EventType.Goal,
        20 => EventType.Drop,
        22 => EventType.Turnover,
        23 => EventType.Turnover,
        11 => EventType.Turnover,
        12 => EventType.Turnover,
        13 => EventType.Turnover,
        24 => EventType.Drop,
        _ => EventType.Other,
    };

    private static string BuildDescription(UfaGameEvent evt)
    {
        return evt.Type switch
        {
            7 => $"Pull inbounds by {evt.Puller ?? "unknown player"}",
            18 => $"Pass from {evt.Thrower ?? "unknown"} to {evt.Receiver ?? "unknown"}",
            19 => $"Goal by {evt.Thrower ?? "unknown"} to {evt.Receiver ?? "unknown"}",
            20 => $"Drop by {evt.Thrower ?? "unknown"}",
            21 => $"Dropped pull by {evt.Receiver ?? "unknown"}",
            22 => $"Throwaway by {evt.Thrower ?? "unknown"}",
            23 => $"Callahan by {evt.Thrower ?? "unknown"}",
            24 => $"Stall on {evt.Thrower ?? "unknown"}",
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

    private static bool TryResolveDiscPosition(UfaGameEvent evt, out FieldPosition discPosition)
    {
        discPosition = new FieldPosition();

        if (evt.ReceiverX.HasValue && evt.ReceiverY.HasValue)
        {
            discPosition.X = (float)evt.ReceiverX.Value;
            discPosition.Y = (float)evt.ReceiverY.Value;
            return true;
        }

        if (evt.ThrowerX.HasValue && evt.ThrowerY.HasValue)
        {
            discPosition.X = (float)evt.ThrowerX.Value;
            discPosition.Y = (float)evt.ThrowerY.Value;
            return true;
        }

        if (evt.PullX.HasValue && evt.PullY.HasValue)
        {
            discPosition.X = (float)evt.PullX.Value;
            discPosition.Y = (float)evt.PullY.Value;
            return true;
        }

        discPosition = new FieldPosition { X = 60, Y = 26.5f };
        return false;
    }

    private class UfaGameEventsResponse
    {
        public string? Object { get; set; }
        public UfaGameEventsData? Data { get; set; }
    }

    private class UfaGameEventsData
    {
        public List<UfaGameEvent> HomeEvents { get; set; } = new();
        public List<UfaGameEvent> AwayEvents { get; set; } = new();
    }

    private class UfaGameEvent
    {
        public int Type { get; set; }
        public int? Time { get; set; }
        public string? Line { get; set; }
        public string? Puller { get; set; }
        public double? PullX { get; set; }
        public double? PullY { get; set; }
        public int? PullMs { get; set; }
        public string? Thrower { get; set; }
        public double? ThrowerX { get; set; }
        public double? ThrowerY { get; set; }
        public string? Receiver { get; set; }
        public double? ReceiverX { get; set; }
        public double? ReceiverY { get; set; }
        public string? Defender { get; set; }
        public double? TurnoverX { get; set; }
        public double? TurnoverY { get; set; }
    }
}
