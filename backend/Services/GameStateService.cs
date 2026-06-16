using System.Collections.Concurrent;
using UFAGameCast.Backend.Models;

namespace UFAGameCast.Backend.Services;

/// <summary>
/// Manages the current game state and maintains a queue of recent play events
/// </summary>
public class GameStateService
{
    private readonly ConcurrentQueue<PlayEvent> _playEventQueue = new();
    private GameState _currentGameState;
    private int _playEventId = 1;
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

    public void AddPlayEvent(PlayEvent playEvent)
    {
        playEvent.Id = _playEventId++;
        playEvent.Timestamp = DateTime.UtcNow;
        _playEventQueue.Enqueue(playEvent);

        // Keep only the last 50 events in memory
        while (_playEventQueue.Count > 50)
        {
            _playEventQueue.TryDequeue(out _);
        }

        // Update the current game state with the latest event
        lock (_lockObject)
        {
            _currentGameState.LastPlayEvent = playEvent;
        }
    }

    public IEnumerable<PlayEvent> GetRecentPlayEvents(int count = 10)
    {
        return _playEventQueue.TakeLast(count).Reverse();
    }

    private GameState InitializeGameState()
    {
        var players = GenerateSamplePlayers();

        return new GameState
        {
            GameId = 1,
            CurrentTime = DateTime.UtcNow,
            Team1Name = "Hawks",
            Team2Name = "Eagles",
            Team1Score = 0,
            Team2Score = 0,
            AllPlayers = players,
            DiscPosition = new FieldPosition { X = 60, Y = 26.5f } // Center field
        };
    }

    private List<PlayerSnapshot> GenerateSamplePlayers()
    {
        var players = new List<PlayerSnapshot>();

        // Team 1 (Hawks)
        var team1Players = new[]
        {
            (1, "Carrico"), (2, "Coniff"), (3, "Marks"), (4, "Smith"),
            (5, "Johnson"), (6, "Williams"), (7, "Brown")
        };

        foreach (var (id, name) in team1Players)
        {
            players.Add(new PlayerSnapshot
            {
                PlayerId = id,
                PlayerName = name,
                Team = "Hawks",
                JerseyNumber = id,
                Position = new FieldPosition { X = Random.Shared.Next(10, 90), Y = Random.Shared.Next(10, 90) }
            });
        }

        // Team 2 (Eagles)
        var team2Players = new[]
        {
            (8, "Davis"), (9, "Miller"), (10, "Taylor"), (11, "Anderson"),
            (12, "Thomas"), (13, "Jackson"), (14, "White")
        };

        foreach (var (id, name) in team2Players)
        {
            players.Add(new PlayerSnapshot
            {
                PlayerId = id,
                PlayerName = name,
                Team = "Eagles",
                JerseyNumber = id,
                Position = new FieldPosition { X = Random.Shared.Next(10, 90), Y = Random.Shared.Next(10, 90) }
            });
        }

        return players;
    }
}
