using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using UFAGameCast.Backend.Models;
using UFAGameCast.Backend.Services;

namespace UFAGameCast.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly GameStateService _gameStateService;
    private readonly ILogger<StatsController> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public StatsController(
        GameStateService gameStateService,
        ILogger<StatsController> logger)
    {
        _gameStateService = gameStateService;
        _logger = logger;
    }

    /// <summary>
    /// Initial load endpoint.
    /// Returns the current game state and complete play history.
    /// </summary>
    [HttpGet("snapshot")]
    public async Task<IActionResult> GetSnapshot([FromQuery] string gameId)
    {
        _gameStateService.SetCurrentGameId(gameId);
        var history = _gameStateService.GetAllPlayEvents();

        _ = Task.Run(async () =>
        {
            await _gameStateService.ReplayEventsSlowly(history, 5000);
        });

        return Ok(new
        {
            gameState = _gameStateService.GetCurrentGameState(),
            playHistory = _gameStateService.GetAllPlayEvents()
        });
    }

    [HttpGet("live")]
    public async Task GetLiveStats(CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        _logger.LogInformation("SSE client connected");

        // send initial state once
        await SendEvent(
            Response,
            "gamestate",
            _gameStateService.GetCurrentGameState(),
            cancellationToken);

        // stream live events
        await foreach (var play in _gameStateService
            .GetEventStream(cancellationToken)
            .WithCancellation(cancellationToken))
        {
            await SendEvent(
                Response,
                "playevent",
                play,
                cancellationToken);
        }
    }

    /// <summary>
    /// Polling fallback endpoint.
    /// </summary>
    [HttpGet("current")]
    public ActionResult<GameState> GetCurrentState()
    {
        return Ok(_gameStateService.GetCurrentGameState());
    }

    private async Task SendEvent<T>(
        HttpResponse response,
        string eventType,
        T data,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);

        await response.WriteAsync(
            $"event: {eventType}\n",
            cancellationToken);

        await response.WriteAsync(
            $"data: {json}\n\n",
            cancellationToken);

        await response.Body.FlushAsync(cancellationToken);
    }
}