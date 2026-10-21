import React, { useEffect, useState } from 'react';
import { FieldVisualization } from './FieldVisualization';
import { PlayHistory } from './PlayHistory';
import { GameInfo } from './GameInfo';
import { TeamTile } from './TeamTile';
import { TeamGamesPage } from './TeamGamesPage';
import UfaLogo from '../resources/UFAIcon.png';
import { getStatsService } from '../services/StatsService';
import { GameState, PlayEvent } from '../types/api';
import { getTeamColor, getTeamNames } from '../utils/teamColors';
import { getTeamLogo } from '../utils/teamLogos';

const teamNames = getTeamNames();

function getTeamFromPath(): string | null {
  const match = window.location.pathname.match(/^\/team=([^/]+)$/);
  const teamName = match?.[1].toLowerCase();

  return teamName && teamNames.includes(teamName) ? teamName : null;
}

function toTitleCase(value: string) {
  return value.charAt(0).toUpperCase() + value.slice(1);
}

interface AppBannerProps {
  onBack?: () => void;
}

const AppBanner: React.FC<AppBannerProps> = ({ onBack }) => (
  <header className="app-banner">
    {onBack && (
      <button className="app-banner-back" onClick={onBack}>
        Back
      </button>
    )}
    <img src={UfaLogo} alt="UFA" className="team-picker-logo" />
  </header>
);

/**
 * Main dashboard component
 */
export const GameDashboard: React.FC = () => {
  const stats = getStatsService();

  const [selectedTeamName, setSelectedTeamName] = useState<string | null>(getTeamFromPath);
  const [selectedGameId, setSelectedGameId] = useState<string | null>(null);
  const [gameState, setGameState] = useState<GameState | null>(null);
  const [recentPlays, setRecentPlays] = useState<PlayEvent[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const syncTeamFromPath = () => setSelectedTeamName(getTeamFromPath());
    window.addEventListener('popstate', syncTeamFromPath);
    return () => window.removeEventListener('popstate', syncTeamFromPath);
  }, []);

  const navigateToTeam = (teamName: string | null) => {
    const path = teamName ? `/team=${encodeURIComponent(teamName)}` : '/';
    window.history.pushState(null, '', path);
    setSelectedTeamName(teamName);
  };

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

    if (!selectedGameId && selectedTeamName) {
      return (
        <div className="team-picker">
          <AppBanner onBack={() => navigateToTeam(null)} />
          <TeamGamesPage
            teamName={toTitleCase(selectedTeamName)}
            teamColor={getTeamColor(selectedTeamName)}
            teamLogo={getTeamLogo(selectedTeamName)}
          />
        </div>
      );
    }

    if (!selectedGameId) {
    return (
        <div className="team-picker">
          <AppBanner />
          <div className="team-grid-scroll">
            <div className="team-grid">
              <h1 className="team-picker-title">GameCast</h1>
              {teamNames.map((teamName) => (
                <TeamTile
                  key={teamName}
                  name={toTitleCase(teamName)}
                  teamColor={getTeamColor(teamName)}
                  teamLogo={getTeamLogo(teamName)}
                  onClick={() => navigateToTeam(teamName)}
                />
              ))}
            </div>
          </div>
        </div>
    );
    }

  return (
    <div>
      <AppBanner
        onBack={() => {
          stats.disconnect();
          setSelectedGameId(null);
          setGameState(null);
          setRecentPlays([]);
        }}
      />

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