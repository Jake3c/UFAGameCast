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

  const [gameId, setGameId] = useState<string>('2026-06-27-MAD-IND');
  const [selectedGameId, setSelectedGameId] = useState<string | null>(null);
  const [gameState, setGameState] = useState<GameState | null>(null);
  const [recentPlays, setRecentPlays] = useState<PlayEvent[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // =========================================================
  // BOOTSTRAP SNAPSHOT + CONNECT SSE
  // =========================================================
  useEffect(() => {
    if (!selectedGameId) return;

    let unsubscribe: (() => void) | null = null;

    const init = async () => {
        try {
        stats.disconnect();

        stats.setGameId(selectedGameId);

        const snapshot = await stats.getSnapshot();

        setGameState(snapshot.gameState);
        setRecentPlays(snapshot.playHistory);

        await stats.connect();
        setIsConnected(true);

        unsubscribe = stats.subscribe((event) => {
            if (event.type === 'gamestate') {
            setGameState(event.data);
            }

            if (event.type === 'playevent') {
            setRecentPlays((prev) =>
                [...prev, event.data].slice(-50)
            );
            }
        });
        } catch (err: any) {
        setError(err.message ?? 'Failed to load game data');
        }
    };

    init();

    return () => {
        unsubscribe?.();
        stats.disconnect();
    };
    }, [selectedGameId]);

  // =========================================================
  // RENDER
  // =========================================================

    if (!selectedGameId) {
    return (
        <div className="min-h-screen flex items-center justify-center">
        <div className="w-full max-w-md p-6">
            <h1 className="text-2xl font-bold mb-4 text-center">
            UFA GameCast
            </h1>

            <input
            type="text"
            value={gameId}
            onChange={(e) => setGameId(e.target.value)}
            className="w-full border rounded px-3 py-2 mb-3"
            />

            <button
            className="w-full bg-blue-600 text-white rounded px-3 py-2"
            onClick={() => {
                if (gameId.trim()) {
                setSelectedGameId(gameId.trim());
                }
            }}
            >
            Load Game
            </button>
        </div>
        </div>
    );
    }

  return (
    <div>
      {/* Header */}
      <div className="text-center font-semibold py-1 bg-slate-800 relative">
        <button
            className="text-white left-2 top-1/2 -translate-y-1/2 absolute"
            onClick={() => {
                stats.disconnect();
                setSelectedGameId(null);
                setGameState(null);
                setRecentPlays([]);
            }}
            >
            Back
        </button>
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
          <FieldVisualization homeTeam={gameState?.homeTeamName ??  ''} discPosition={gameState?.discPosition} />
        </div>

        <div className="plays-section">
          <PlayHistory plays={recentPlays} maxVisible={550} />
        </div>
      </div>
    </div>
  );
};