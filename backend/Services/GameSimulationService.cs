using System.Text.Json;
using UFAGameCast.Backend.Models;

namespace UFAGameCast.Backend.Services;

/// <summary>
/// Background service that simulates upstream API data and generates sample play events
/// This will be replaced with real upstream API integration later
/// </summary>
public class GameSimulationService : BackgroundService
{
    private readonly GameStateService _gameStateService;
    private readonly ILogger<GameSimulationService> _logger;
    private readonly Random _random = new();

    private readonly string[] _playDescriptions = new[]
    {
        "{0} throws {1}yds to {2}",
        "{0} catches {1}yd pass from {2}",
        "{0} scores a {1}yd goal",
        "{0} turns over to {2}",
        "{0} gets blocked by {2}",
        "{0} drops the disc",
        "{0} makes a diving catch from {2}"
    };

    public GameSimulationService(GameStateService gameStateService, ILogger<GameSimulationService> logger)
    {
        _gameStateService = gameStateService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Game Simulation Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Generate a random play event every 3-8 seconds
                await Task.Delay(Random.Shared.Next(3000, 8000), stoppingToken);

                var currentState = _gameStateService.GetCurrentGameState();
                var playEvent = GenerateRandomPlayEvent(currentState);

                _gameStateService.AddPlayEvent(playEvent);
                _logger.LogInformation("Play event generated: {description}", playEvent.Description);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in game simulation service");
            }
        }

        _logger.LogInformation("Game Simulation Service stopped");
    }

    private PlayEvent GenerateRandomPlayEvent(GameState gameState)
    {
        var players = gameState.AllPlayers.ToList();
        var initiator = players[_random.Next(players.Count)];
        var receiver = players[_random.Next(players.Count)];

        while (receiver.PlayerId == initiator.PlayerId)
        {
            receiver = players[_random.Next(players.Count)];
        }

        var eventType = (EventType)_random.Next(0, 7);
        var distance = _random.Next(5, 50);
        var descriptionTemplate = _playDescriptions[_random.Next(_playDescriptions.Length)];
        var description = string.Format(descriptionTemplate, initiator.PlayerName, distance, receiver.PlayerName);

        // Update player positions slightly for animation (field is 120x53 with endzones)
        initiator.Position = new FieldPosition
        {
            X = Math.Clamp(initiator.Position.X + _random.Next(-10, 10), 2, 118),
            Y = Math.Clamp(initiator.Position.Y + _random.Next(-5, 5), 2, 51)
        };

        receiver.Position = new FieldPosition
        {
            X = Math.Clamp(receiver.Position.X + _random.Next(-10, 10), 2, 118),
            Y = Math.Clamp(receiver.Position.Y + _random.Next(-5, 5), 2, 51)
        };

        // Move disc to receiver's position (simulating a catch)
        gameState.DiscPosition = new FieldPosition
        {
            X = receiver.Position.X,
            Y = receiver.Position.Y
        };

        // Persist the updated game state
        _gameStateService.UpdateGameState(gameState);

        return new PlayEvent
        {
            EventType = eventType,
            InitiatorPlayerId = initiator.PlayerId,
            InitiatorName = initiator.PlayerName,
            ReceiverPlayerId = receiver.PlayerId,
            ReceiverName = receiver.PlayerName,
            Distance = distance,
            Description = description,
            PlayersInvolved = new List<PlayerSnapshot> { initiator, receiver }
        };
    }
}
