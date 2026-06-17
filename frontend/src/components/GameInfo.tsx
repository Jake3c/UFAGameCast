import React from 'react';
import { GameState } from '../types/api';
import { getTeamLogo } from '../utils/teamLogos';
import { getTeamColor } from '../utils/teamColors';

interface GameInfoProps {
  gameState: GameState | null;
  isConnected: boolean;
  error: string | null;
}

/**
 * Displays current game state (score, teams, connection status)
 */
export const GameInfo: React.FC<GameInfoProps> = ({ gameState, isConnected, error }) => {
  const toTitleCase = (str: string) =>
  str
    .toLowerCase()
    .split(/\s+/)
    .map(word => word.charAt(0).toUpperCase() + word.slice(1))
    .join(" ");

  return (
    <div>
      <div className="flex justify-between items-center pb-4">
        <div className="px-2 bg-[radial-gradient(circle_at_top_left,var(--team-color),white_60%)]" style={{ "--team-color": getTeamColor(gameState?.awayTeamName || "") } as React.CSSProperties}>
          <div className="w-16 h-16 flex items-center justify-center">
            <img src={getTeamLogo(gameState?.awayTeamName || "")} alt={gameState?.awayTeamName} className="max-w-full max-h-full" />
          </div>
          <div className="font-semibold text-gray-700">
            {gameState ? toTitleCase(gameState.awayTeamName) : "Away"}
          </div>
          <div className="text-gray-400 text-xs">
            (1-4)
          </div>
        </div>

        <div className='w-full px-2 flex justify-between'>
          <div className="text-3xl font-bold">
            {gameState ? gameState.awayTeamScore : "0"}
          </div>
          <div className="flex-grow">
            <div className="text-center">
              {gameState?.isActive && (
                <div className="text-lg font-bold">12:00</div>
              )}
              <div className="text-sm text-gray-400 font-semibold">{gameState?.gameStatus}</div>
              {gameState?.streamingUrl && (
                <a
                  href={gameState?.streamingUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="
                    inline-flex items-center justify-center
                    px-2 py-1 translate-y-4
                    rounded-md
                    bg-red-600
                    text-white
                    text-xs
                    font-semibold
                    shadow-sm
                    hover:bg-red-700
                    hover:shadow-md
                    active:scale-95
                    transition-all
                    duration-150
                  "
                >
                  {gameState?.isActive ? "Watch LIVE" : "Watch Replay"}
                </a>
              )}
            </div>
          </div>
          <div className="text-3xl font-bold">
            {gameState ? gameState.homeTeamScore : "0"}
          </div>
        </div>

        <div className="text-right px-2 bg-[radial-gradient(circle_at_top_right,var(--team-color),white_60%)]" style={{ "--team-color": getTeamColor(gameState?.homeTeamName || "")  } as React.CSSProperties}>
          <div className="w-16 h-16 flex items-center justify-center">
            <img src={getTeamLogo(gameState?.homeTeamName || "")} alt={gameState?.homeTeamName} className="max-w-full max-h-full" />
          </div>
          <div className="font-semibold text-gray-700">
            {gameState ? toTitleCase(gameState.homeTeamName) : "Home"}
          </div>
          <div className="text-gray-400 text-xs">
            (3-2)
          </div>
        </div>
      </div>
    </div>
  );
};
