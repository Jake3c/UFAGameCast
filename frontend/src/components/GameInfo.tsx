import React from 'react';
import { GameState } from '../types/api';
import LogoTeamMAD from '../resources/logo-team-MAD.png'
import LogoTeamIND from '../resources/logo-team-IND.png'

interface GameInfoProps {
  gameState: GameState | null;
  isConnected: boolean;
  error: string | null;
}

/**
 * Displays current game state (score, teams, connection status)
 */
export const GameInfo: React.FC<GameInfoProps> = ({ gameState, isConnected, error }) => {
  return (
    <div>
      <div className="flex justify-between items-center pb-4">
        <div className="px-2 bg-[radial-gradient(circle_at_top_left,var(--team-color),white_60%)]" style={{ "--team-color": "#27A332" } as React.CSSProperties}>
          <div className="w-16 h-16 flex items-center justify-center">
            <img src={LogoTeamIND} alt="IND" className="max-w-full max-h-full" />
          </div>
          <div className="font-semibold text-gray-700">
            {gameState ? gameState.team1Name : "IND"}
          </div>
          <div className="text-gray-400 text-xs">
            (1-4)
          </div>
        </div>

        <div className='w-full px-2 flex justify-between'>
          <div className="text-3xl font-bold">
            {gameState ? gameState.team1Score : "0"}
          </div>
          <div className="flex-grow">
            <div className="text-center">
              {/* TODO: Fix time so that it goes off game time, not last play event time */}
              <div className="text-lg font-bold">{gameState ? gameState.lastPlayEvent?.time : "12:00"}</div>
              <div className="text-sm text-gray-400 font-semibold">Q3</div>
            </div>
          </div>
          <div className="text-3xl font-bold">
            {gameState ? gameState.team2Score : "0"}
          </div>
        </div>

        <div className="text-right px-2 bg-[radial-gradient(circle_at_top_right,var(--team-color),white_60%)]" style={{ "--team-color": "#cbb82e"  } as React.CSSProperties}>
          <div className="w-16 h-16 flex items-center justify-center">
            <img src={LogoTeamMAD} alt="MAD" className="max-w-full max-h-full" />
          </div>
          <div className="font-semibold text-gray-700">
            {gameState ? gameState.team2Name : "MAD"}
          </div>
          <div className="text-gray-400 text-xs">
            (3-2)
          </div>
        </div>
      </div>
    </div>
  );
};
