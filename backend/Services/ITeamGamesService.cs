using UFAGameCast.Backend.Models;

namespace UFAGameCast.Backend.Services;

public interface ITeamGamesService
{
    Task<IReadOnlyList<TeamGameResponse>> GetTeamGamesAsync(
        string teamName,
        CancellationToken cancellationToken);
}
