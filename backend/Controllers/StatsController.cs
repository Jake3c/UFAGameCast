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
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public StatsController(GameStateService gameStateService, ILogger<StatsController> logger)
    {
        _gameStateService = gameStateService;
        _logger = logger;
    }

    /// <summary>
    /// Server-Sent Events endpoint for streaming live game state and play events
    /// Client establishes a persistent HTTP connection to receive real-time updates
    /// </summary>
    [HttpGet("live")]
    public async Task GetLiveStats(CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        _logger.LogInformation("SSE client connected");

        try
        {
            // Send initial game state
            var initialState = _gameStateService.GetCurrentGameState();
            await SendEvent(Response, "gamestate", initialState, cancellationToken);

            // Track the last event ID we sent to avoid duplicate sends
            var lastSentEventId = initialState.LastPlayEvent?.Id ?? 0;
            var lastSentTime = DateTime.UtcNow;

            // Keep sending updates every 2 seconds or when new events occur
            while (!cancellationToken.IsCancellationRequested)
            {
                var currentState = _gameStateService.GetCurrentGameState();

                // Send updated game state periodically
                if (DateTime.UtcNow - lastSentTime > TimeSpan.FromSeconds(2))
                {
                    await SendEvent(Response, "gamestate", currentState, cancellationToken);
                    lastSentTime = DateTime.UtcNow;
                }

                // Check for new play events
                if (currentState.LastPlayEvent?.Id > lastSentEventId)
                {
                    await SendEvent(Response, "playevent", currentState.LastPlayEvent, cancellationToken);
                    lastSentEventId = currentState.LastPlayEvent.Id;
                }

                await Task.Delay(500, cancellationToken); // Poll interval
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
    /// Get the current game state (polling alternative to SSE)
    /// </summary>
    [HttpGet("current")]
    public ActionResult<GameState> GetCurrentState()
    {
        return Ok(_gameStateService.GetCurrentGameState());
    }

    /// <summary>
    /// Get the last N play events
    /// </summary>
    [HttpGet("recent-plays")]
    public ActionResult<IEnumerable<GameEventViewModel>> GetRecentPlays([FromQuery] int count = 10)
    {
        if (count < 1 || count > 100)
            return BadRequest("Count must be between 1 and 100");

        return Ok(_gameStateService.GetRecentPlayEvents(count));
    }

    private async Task SendEvent<T>(HttpResponse response, string eventType, T data, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            var sseMessage = $"event: {eventType}\ndata: {json}\n\n";
            await response.WriteAsync(sseMessage, cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SSE event");
        }
    }
}
