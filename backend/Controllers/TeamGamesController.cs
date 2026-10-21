using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using UFAGameCast.Backend.Models;
using UFAGameCast.Backend.Services;

namespace UFAGameCast.Backend.Controllers;

[ApiController]
[Route("api/stats")]
public sealed class TeamGamesController : ControllerBase
{
    private static readonly HashSet<string> SupportedTeams = new(StringComparer.Ordinal)
    {
        "alleycats", "apex", "bighorns", "breeze", "cascades", "empire",
        "flyers", "glory", "growlers", "havoc", "hustle", "phoenix",
        "radicals", "royale", "rush", "shred", "sol", "spiders", "steel",
        "thunderbirds", "union", "windchill"
    };

    private readonly ITeamGamesService _teamGamesService;
    private readonly ILogger<TeamGamesController> _logger;

    public TeamGamesController(
        ITeamGamesService teamGamesService,
        ILogger<TeamGamesController> logger)
    {
        _teamGamesService = teamGamesService;
        _logger = logger;
    }

    /// <summary>Gets the game history for a team.</summary>
    /// <param name="teamName">Lowercase team ID, such as radicals.</param>
    /// <param name="cancellationToken">Token for cancelling the upstream request.</param>
    /// <returns>Games returned by the UFA stats API.</returns>
    [HttpGet("GetTeamGames", Name = "GetTeamGames")]
    [ProducesResponseType(typeof(IReadOnlyList<TeamGameResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<TeamGameResponse>>> GetTeamGames(
        [FromQuery(Name = "TeamName"), Required, RegularExpression("^[a-z]+$")] string teamName,
        CancellationToken cancellationToken)
    {
        if (!SupportedTeams.Contains(teamName))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid team name",
                Detail = "TeamName must be a supported lowercase team ID."
            });
        }

        try
        {
            var games = await _teamGamesService.GetTeamGamesAsync(
                teamName,
                cancellationToken);
            return Ok(games);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Unable to retrieve game history for {TeamName}", teamName);
            return Problem(
                title: "Unable to retrieve team games",
                detail: "The upstream games service could not complete the request.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
