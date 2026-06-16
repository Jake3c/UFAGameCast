import React from 'react';
import { useStatsStream } from '../hooks/useStatsStream';
import { FieldVisualization } from './FieldVisualization';
import { PlayHistory } from './PlayHistory';
import { GameInfo } from './GameInfo';
import './GameDashboard.css';

/**
 * Main dashboard component that orchestrates the entire field visualization
 * and play history display using real-time data from the backend
 */
export const GameDashboard: React.FC = () => {
  const { gameState, recentPlays, isConnected, error } = useStatsStream();

  return (
    <div className="game-dashboard">
      <header className="dashboard-header">
        <h1>UFAGameCast - Live Field Visualization</h1>
        <p className="subtitle">Real-time game statistics and player tracking</p>
      </header>

      <GameInfo gameState={gameState} isConnected={isConnected} error={error} />

      <div className="dashboard-content">
        <div className="field-section">
          {gameState?.allPlayers ? (
            <FieldVisualization discPosition={gameState.discPosition} width={900} height={540} />
          ) : (
            <div className="loading-placeholder">Loading field data...</div>
          )}
        </div>

        <div className="plays-section">
          <PlayHistory plays={recentPlays} maxVisible={5} />
        </div>
      </div>
    </div>
  );
};
