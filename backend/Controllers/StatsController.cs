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
    public IActionResult GetSnapshot()
    {
        return Ok(new
        {
            gameState = _gameStateService.GetCurrentGameState(),
            playHistory = _gameStateService.GetAllPlayEvents()
        });
    }

    /// <summary>
    /// SSE endpoint for live updates.
    /// Sends game state updates and newly-added play events.
    /// </summary>
    [HttpGet("live")]
    public async Task GetLiveStats(CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        _logger.LogInformation("SSE client connected");

        try
        {
            // Send current state immediately
            await SendEvent(
                Response,
                "gamestate",
                _gameStateService.GetCurrentGameState(),
                cancellationToken);

            // Don't replay history.
            // Client gets history from /snapshot.
            var lastPlayCount = _gameStateService
                .GetAllPlayEvents()
                .Count;

            var lastGameStatePush = DateTime.UtcNow;

            while (!cancellationToken.IsCancellationRequested)
            {
                // Push game state every 2 seconds
                if (DateTime.UtcNow - lastGameStatePush >= TimeSpan.FromSeconds(2))
                {
                    await SendEvent(
                        Response,
                        "gamestate",
                        _gameStateService.GetCurrentGameState(),
                        cancellationToken);

                    lastGameStatePush = DateTime.UtcNow;
                }

                // Push any new play events
                var plays = _gameStateService.GetAllPlayEvents();

                if (plays.Count > lastPlayCount)
                {
                    foreach (var play in plays.Skip(lastPlayCount))
                    {
                        await SendEvent(
                            Response,
                            "playevent",
                            play,
                            cancellationToken);
                    }

                    lastPlayCount = plays.Count;
                }

                await Task.Delay(500, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE client disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE stream");
        }
        finally
        {
            await Response.CompleteAsync();
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