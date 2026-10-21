import React from 'react';
import { TeamGame } from '../services/TeamGamesService';

interface GameRowTileProps {
  game: TeamGame;
  teamName: string;
  onOpenGame: (gameId: string) => void;
}

function formatGameDate(startTime: string): string {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
  }).format(new Date(startTime));
}

function getGameTimeParts(startTime: string): { time: string; period: string } {
  const parts = new Intl.DateTimeFormat('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  }).formatToParts(new Date(startTime));

  return {
    time: `${parts.find((part) => part.type === 'hour')?.value}:${parts.find((part) => part.type === 'minute')?.value}`,
    period: parts.find((part) => part.type === 'dayPeriod')?.value ?? '',
  };
}

function toTitleCase(value: string): string {
  return value.charAt(0).toUpperCase() + value.slice(1);
}

export const GameRowTile: React.FC<GameRowTileProps> = ({ game, teamName, onOpenGame }) => {
  const isHome = game.homeTeam.toLowerCase() === teamName.toLowerCase();
  const opponent = isHome ? game.awayTeam : game.homeTeam;
  const teamScore = isHome ? game.homeScore : game.awayScore;
  const opponentScore = isHome ? game.awayScore : game.homeScore;
  const didWin = teamScore > opponentScore;
  const gameTime = getGameTimeParts(game.startTime);

  return (
    <article className="game-row" title={game.gameId}>
      <a
        className="game-row-navigation"
        href={`/game=${encodeURIComponent(game.gameId)}`}
        aria-label={`Open ${toTitleCase(teamName)} vs ${toTitleCase(opponent)} gamecast`}
        onClick={(event) => {
          event.preventDefault();
          onOpenGame(game.gameId);
        }}
      />
      <time className="game-date" dateTime={game.startTime}>
        {formatGameDate(game.startTime)}
        <span className="game-time-details">
          <span className="game-time">
            {gameTime.time}
            <span className="game-time-period">{gameTime.period}</span>
          </span>
          <span className="game-time-zone">{game.startTimeZone}</span>
        </span>
      </time>
      <div className="game-opponent">
        <span className="game-location">{isHome ? 'vs' : '@'}</span>
        <span>
          {toTitleCase(opponent)}
          <span className="game-location-name">{game.location || 'Location TBD'}</span>
        </span>
      </div>
      <div className="game-result">
        <span className="game-status">{game.status}</span>
        <span className={`game-outcome ${didWin ? 'is-win' : 'is-loss'}`}>
          {didWin ? 'W' : 'L'}
        </span>
        <span>{teamScore}–{opponentScore}</span>
        {game.streamingUrl ? (
          <a
            href={game.streamingUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="game-watch-link"
            aria-label={`Watch ${toTitleCase(teamName)} vs ${toTitleCase(opponent)}`}
            title="Watch game"
            onClick={(event) => event.stopPropagation()}
          >
            <svg
              aria-hidden="true"
              viewBox="0 0 24 24"
              className="game-watch-icon"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.8"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <rect x="3" y="6" width="18" height="13" rx="2" />
              <path d="M8 3l4 3 4-3M8 22h8M12 19v3" />
              <path d="m10 10 5 2.5-5 2.5z" fill="currentColor" stroke="none" />
            </svg>
          </a>
        ) : (
          <span className="game-watch-placeholder" aria-hidden="true" />
        )}
      </div>
    </article>
  );
};
