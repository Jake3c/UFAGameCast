import React, { useEffect, useState } from 'react';
import { fetchTeamGames, TeamGame } from '../services/TeamGamesService';
import { GameRowTile } from './GameRowTile';

interface TeamGamesPageProps {
  teamName: string;
  teamColor: string;
  teamLogo: string;
}

export const TeamGamesPage: React.FC<TeamGamesPageProps> = ({
  teamName,
  teamColor,
  teamLogo,
}) => {
  const [games, setGames] = useState<TeamGame[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    setIsLoading(true);
    setError(null);

    fetchTeamGames(teamName)
      .then((result) => {
        if (active) {
          setGames(result);
          setIsLoading(false);
        }
      })
      .catch((reason: unknown) => {
        if (active) {
          setError(reason instanceof Error ? reason.message : 'Unable to load games.');
          setIsLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [teamName]);

  const gamesByYear = games.reduce<Record<string, TeamGame[]>>((grouped, game) => {
    const year = new Date(game.startTime).getFullYear().toString();
    grouped[year] ??= [];
    grouped[year].push(game);
    return grouped;
  }, {});

  return (
    <main className="team-games-scroll">
      <div className="team-games-content">
        <section
          className="team-games-heading"
          style={{ '--team-color': teamColor } as React.CSSProperties}
        >
          <img src={teamLogo} alt="" className="team-games-logo" />
          <div>
            <p className="team-games-eyebrow">Team schedule</p>
            <h1>{teamName}</h1>
          </div>
        </section>

        {isLoading && <p className="team-games-message">Loading games...</p>}
        {error && <p className="team-games-message team-games-error">{error}</p>}
        {!isLoading && !error && Object.entries(gamesByYear)
          .sort(([firstYear], [secondYear]) => secondYear.localeCompare(firstYear))
          .map(([year, yearGames]) => (
          <section className="season-section" key={year}>
            <h2>{year}</h2>
            <div className="season-games">
              {[...yearGames]
                .sort((first, second) => second.startTime.localeCompare(first.startTime))
                .map((game) => (
                  <GameRowTile key={game.gameId} game={game} teamName={teamName} />
                ))}
            </div>
          </section>
        ))}
      </div>
    </main>
  );
};
