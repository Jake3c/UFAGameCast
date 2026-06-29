using System.Collections.Concurrent;
using System.Threading.Channels;
using UFAGameCast.Backend.Models;

namespace UFAGameCast.Backend.Services;

/// <summary>
/// Manages the current game state and maintains both:
/// - Full game history
/// - Recent event queue
/// </summary>
public class GameStateService
{
    private readonly ConcurrentQueue<GameEventViewModel> _recentGameEventQueue = new();
    private readonly Channel<GameEventViewModel> _eventChannel = Channel.CreateUnbounded<GameEventViewModel>();
    private readonly List<GameEventViewModel> _allGameEvents = new();
    private readonly ILogger<GameStateService> _logger;

    private string? _currentGameId;
    private GameState _currentGameState;
    private int _gameEventId = 1;

    private readonly object _lockObject = new();

    public GameStateService(ILogger<GameStateService> logger)
    {
        _logger = logger;

        _logger.LogInformation("GameStateService CREATED");

        _currentGameState = InitializeGameState();
    }

    public void SetCurrentGameId(string gameId)
    {
        lock (_lockObject)
        {
            _currentGameId = gameId;
        }
    }

    public string? GetCurrentGameId()
    {
        lock (_lockObject)
        {
            return _currentGameId;
        }
    }

    public GameState GetCurrentGameState()
    {
        lock (_lockObject)
        {
            return _currentGameState;
        }
    }

    public void UpdateGameState(GameState state)
    {
        lock (_lockObject)
        {
            _currentGameState = state;
        }
    }

    public void PublishPlayEvent(GameEventViewModel playEvent)
    {
        _logger.LogInformation("Publishing event: {type}", playEvent.EventType);

        AddPlayEvent(playEvent);
        _eventChannel.Writer.TryWrite(playEvent);
    }

    public IAsyncEnumerable<GameEventViewModel> GetEventStream(CancellationToken token)
    {
        return _eventChannel.Reader.ReadAllAsync(token);
    }


    public async Task ReplayEventsSlowly(IEnumerable<GameEventViewModel> events, int delayMs = 5000, CancellationToken token = default)
    {
        foreach (var evt in events)
        {
            if (token.IsCancellationRequested)
                break;

            PublishPlayEvent(evt);

            await Task.Delay(delayMs, token);
        }
    }

    /// <summary>
    /// Adds a newly processed event.
    /// Used by UfaGameEventService when new events arrive.
    /// </summary>
    public void AddPlayEvent(GameEventViewModel gameEvent)
    {
        gameEvent.Id = _gameEventId++;

        // Preserve existing timestamp if already supplied
        if (gameEvent.Timestamp == default)
        {
            gameEvent.Timestamp = DateTime.UtcNow;
        }

        if (gameEvent.EventType == EventType.StartOPoint ||
            gameEvent.EventType == EventType.StartDPoint)
        {
            var random = new Random();

            var totalSeconds = random.Next(0, 12 * 60 + 1);

            gameEvent.Time =
                $"{totalSeconds / 60:D2}:{totalSeconds % 60:D2}";
        }

        lock (_lockObject)
        {
            _allGameEvents.Add(gameEvent);
        }

        _recentGameEventQueue.Enqueue(gameEvent);

        // Keep only the most recent 50 events in the queue
        while (_recentGameEventQueue.Count > 50)
        {
            _recentGameEventQueue.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Replaces the entire play history.
    /// Useful when rebuilding from a complete UFA event feed.
    /// </summary>
    public void SetPlayHistory(IEnumerable<GameEventViewModel> events)
    {
        lock (_lockObject)
        {
            _allGameEvents.Clear();
            _recentGameEventQueue.Clear();

            foreach (var evt in events)
            {
                if (evt.Id == 0)
                {
                    evt.Id = _gameEventId++;
                }

                if (evt.Timestamp == default)
                {
                    evt.Timestamp = DateTime.UtcNow;
                }

                _allGameEvents.Add(evt);

                _recentGameEventQueue.Enqueue(evt);

                while (_recentGameEventQueue.Count > 50)
                {
                    _recentGameEventQueue.TryDequeue(out _);
                }
            }
        }
    }

    /// <summary>
    /// Returns the full game history in chronological order.
    /// </summary>
    public IReadOnlyList<GameEventViewModel> GetAllPlayEvents()
    {
        lock (_lockObject)
        {
            return _allGameEvents.ToList();
        }
    }

    /// <summary>
    /// Returns only the most recent events.
    /// Default = 10.
    /// </summary>
    public IEnumerable<GameEventViewModel> GetRecentPlayEvents(int count = 10)
    {
        return _recentGameEventQueue
            .TakeLast(count)
            .Reverse();
    }

    private static GameState InitializeGameState()
    {
        return new GameState
        {
            HomeTeamName = "Team1",
            AwayTeamName = "Team2",
            HomeTeamScore = 0,
            AwayTeamScore = 0,
            DiscPosition = new FieldPosition
            {
                X = 60,
                Y = 26.5f
            }
        };
    }
}