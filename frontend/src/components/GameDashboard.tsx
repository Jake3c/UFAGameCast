import React from 'react';
import { useStatsStream } from '../hooks/useStatsStream';
import { FieldVisualization } from './FieldVisualization';
import { PlayHistory } from './PlayHistory';
import { GameInfo } from './GameInfo';
import UfaLogo from '../resources/UFAIcon.png';

/**
 * Main dashboard component that orchestrates the entire field visualization
 * and play history display using real-time data from the backend
 */
export const GameDashboard: React.FC = () => {
  const { gameState, recentPlays, isConnected, error } = useStatsStream();

  return (
    <div>
      <div className="text-center font-semibold py-1 bg-slate-800">
        <img
            src={UfaLogo}
            alt="UFA Gamecast Logo"
            className="block mx-auto pt-1"
            style={{ width: '60px', height: 'auto' }}
        />
        </div>

      <GameInfo gameState={gameState} isConnected={isConnected} error={error} />

      <div className="dashboard-content">
        <div className="field-section">
            <FieldVisualization discPosition={gameState?.discPosition} />
        </div>

        <div className="plays-section">
          <PlayHistory plays={recentPlays} maxVisible={5} />
        </div>
      </div>
    </div>
  );
};
