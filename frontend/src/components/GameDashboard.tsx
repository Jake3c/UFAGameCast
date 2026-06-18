import React, { useEffect, useState } from 'react';
import { FieldVisualization } from './FieldVisualization';
import { PlayHistory } from './PlayHistory';
import { GameInfo } from './GameInfo';
import UfaLogo from '../resources/UFAIcon.png';
import { getStatsService } from '../services/StatsService';
import { GameState, PlayEvent } from '../types/api';

/**
 * Main dashboard component
 */
export const GameDashboard: React.FC = () => {
  const stats = getStatsService();

  const [gameState, setGameState] = useState<GameState | null>(null);
  const [recentPlays, setRecentPlays] = useState<PlayEvent[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // =========================================================
  // BOOTSTRAP SNAPSHOT + CONNECT SSE
  // =========================================================
  useEffect(() => {
    let unsubscribe: (() => void) | null = null;

    const init = async () => {
      try {
        // 1. Load snapshot (INITIAL STATE)
        const snapshot = await stats.getSnapshot();

        setGameState(snapshot.gameState);
        setRecentPlays(snapshot.playHistory);

        // 2. Connect SSE stream
        await stats.connect();
        setIsConnected(true);

        // 3. Subscribe to live updates
        unsubscribe = stats.subscribe((event) => {
          if (event.type === 'gamestate') {
            setGameState(event.data);
          }

          if (event.type === 'playevent') {
            setRecentPlays((prev) => {
              const updated = [...prev, event.data];

              // keep only last 50 in UI
              return updated.slice(-50);
            });
          }
        });
      } catch (err: any) {
        console.error(err);
        setError(err.message ?? 'Failed to load game data');
      }
    };

    init();

    return () => {
      unsubscribe?.();
      stats.disconnect();
    };
  }, []);

  // =========================================================
  // RENDER
  // =========================================================

  return (
    <div>
      {/* Header */}
      <div className="text-center font-semibold py-1 bg-slate-800">
        <img
          src={UfaLogo}
          alt="UFA Gamecast Logo"
          className="block mx-auto pt-1"
          style={{ width: '60px', height: 'auto' }}
        />
      </div>

      {/* Game Info */}
      <GameInfo
        gameState={gameState}
        isConnected={isConnected}
        error={error}
      />

      {/* Main Layout */}
      <div className="dashboard-content">
        <div className="field-section">
          <FieldVisualization discPosition={gameState?.discPosition} />
        </div>

        <div className="plays-section">
          <PlayHistory plays={recentPlays} maxVisible={550} />
        </div>
      </div>
    </div>
  );
};