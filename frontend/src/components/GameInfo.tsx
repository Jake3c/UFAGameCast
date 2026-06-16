import React from 'react';
import { GameState } from '../types/api';
import './GameInfo.css';

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
    <div className="game-info-container">
      <div className="status-bar">
        <div className={`connection-status ${isConnected ? 'connected' : 'disconnected'}`}>
          <div className="status-indicator"></div>
          <span>{isConnected ? 'Live' : 'Offline'}</span>
        </div>
        {error && <div className="error-message">{error}</div>}
      </div>

      {gameState && (
        <div className="scoreboard">
          <div className="team-score">
            <h3 className="team-name">{gameState.team1Name}</h3>
            <div className="score">{gameState.team1Score}</div>
          </div>

          <div className="vs">VS</div>

          <div className="team-score">
            <h3 className="team-name">{gameState.team2Name}</h3>
            <div className="score">{gameState.team2Score}</div>
          </div>
        </div>
      )}

      {gameState?.lastPlayEvent && (
        <div className="last-play">
          <p className="last-play-label">Last Play:</p>
          <p className="last-play-description">{gameState.lastPlayEvent.description}</p>
        </div>
      )}
    </div>
  );
};
