using System.Collections.Concurrent;
using UFAGameCast.Backend.Models;

namespace UFAGameCast.Backend.Services;

/// <summary>
/// Manages the current game state and maintains a queue of recent play events
/// </summary>
public class GameStateService
{
    private readonly ConcurrentQueue<GameEventViewModel> _gameEventQueue = new();
    private GameState _currentGameState;
    private int _gameEventId = 1;
    private readonly object _lockObject = new();

    public GameStateService()
    {
        _currentGameState = InitializeGameState();
    }

    public GameState GetCurrentGameState() => _currentGameState;

    public void UpdateGameState(GameState state)
    {
        lock (_lockObject)
        {
            _currentGameState = state;
        }
    }

    public void AddPlayEvent(GameEventViewModel gameEvent)
    {
        gameEvent.Id = _gameEventId++;
        gameEvent.Timestamp = DateTime.UtcNow;

        if (gameEvent.EventType == EventType.StartOPoint || gameEvent.EventType == EventType.StartDPoint)
        {
            var _random = new Random();
            var totalSeconds = _random.Next(0, 12 * 60 + 1);
            var time = $"{totalSeconds / 60:D2}:{totalSeconds % 60:D2}";
            gameEvent.Time = time;
        }

        _gameEventQueue.Enqueue(gameEvent);

        // Keep only the last 50 events in memory
        while (_gameEventQueue.Count > 50)
        {
            _gameEventQueue.TryDequeue(out _);
        }

        // Update the current game state with the latest event
        lock (_lockObject)
        {
            //_currentGameState.LastPlayEvent = gameEvent;
        }
    }

    public IEnumerable<GameEventViewModel> GetRecentPlayEvents(int count = 10)
    {
        return _gameEventQueue.TakeLast(count).Reverse();
    }

    private GameState InitializeGameState()
    {
        return new GameState
        {
            HomeTeamName = "Team1",
            AwayTeamName = "Team2",
            HomeTeamScore = 0,
            AwayTeamScore = 0,
            DiscPosition = new FieldPosition { X = 60, Y = 26.5f } // Center field
        };
    }
}
