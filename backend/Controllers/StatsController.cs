using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using UFAGameCast.Backend.Models;
using UFAGameCast.Backend.Services;

namespace UFAGameCast.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly GameStateService _gameStateService;
    private readonly UfaGameEventService _ufaGameEventService;
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
        UfaGameEventService ufaGameEventService,
        ILogger<StatsController> logger)
    {
        _gameStateService = gameStateService;
        _ufaGameEventService = ufaGameEventService;
        _logger = logger;
    }

    /// <summary>
    /// Initial load endpoint.
    /// Returns the current game state and complete play history.
    /// </summary>
    [HttpGet("snapshot")]
    public async Task<IActionResult> GetSnapshot(
        [FromQuery, Required] string gameId,
        CancellationToken cancellationToken)
    {
        _gameStateService.SetCurrentGameId(gameId);

        try
        {
            var gameState = await _ufaGameEventService.GetGameStateAsync(gameId, cancellationToken);
            if (!_gameStateService.UpdateGameState(gameId, gameState))
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Game selection changed",
                    Detail = "The requested game was superseded by another game request."
                });
            }

            return Ok(new
            {
                gameState,
                playHistory = _gameStateService.GetAllPlayEvents()
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Game not found",
                Detail = $"No game was found with ID '{gameId}'."
            });
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Unable to retrieve game {GameId}", gameId);
            return Problem(
                title: "Unable to retrieve game",
                detail: "The upstream games service could not complete the request.",
                statusCode: StatusCodes.Status502BadGateway);
        }
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