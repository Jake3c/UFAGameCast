import React, { useEffect, useState } from 'react';
import { FieldVisualization } from './FieldVisualization';
import { PlayHistory } from './PlayHistory';
import { GameInfo } from './GameInfo';
import { TeamTile } from './TeamTile';
import { TeamGamesPage } from './TeamGamesPage';
import UfaLogo from '../resources/UFAIcon.png';
import { getStatsService } from '../services/StatsService';
import { GameEvent, GameState } from '../types/api';
import { getTeamColor, getTeamNames } from '../utils/teamColors';
import { getTeamLogo } from '../utils/teamLogos';

const teamNames = getTeamNames();

function getTeamFromPath(): string | null {
  const match = window.location.pathname.match(/^\/team=([^/]+)$/);
  const teamName = match?.[1].toLowerCase();

  return teamName && teamNames.includes(teamName) ? teamName : null;
}

function getGameFromPath(): string | null {
  const match = window.location.pathname.match(/^\/game=([^/]+)$/);
  if (!match?.[1]) return null;

  try {
    return decodeURIComponent(match[1]);
  } catch {
    return null;
  }
}

function isEndOfPeriodEvent(eventType: string) {
  return [
    'endQ1',
    'halftime',
    'endQ3',
    'endRegulation',
    'endOT1',
    'endOT2',
  ].includes(eventType);
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
  const [selectedGameId, setSelectedGameId] = useState<string | null>(getGameFromPath);
  const [gameState, setGameState] = useState<GameState | null>(null);
  const [gameEvents, setGameEvents] = useState<GameEvent[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const syncRouteFromPath = () => {
      setSelectedTeamName(getTeamFromPath());
      setSelectedGameId(getGameFromPath());
    };
    window.addEventListener('popstate', syncRouteFromPath);
    return () => window.removeEventListener('popstate', syncRouteFromPath);
  }, []);

  const navigateToTeam = (teamName: string | null) => {
    const path = teamName ? `/team=${encodeURIComponent(teamName)}` : '/';
    window.history.pushState(null, '', path);
    setSelectedTeamName(teamName);
    setSelectedGameId(null);
  };

  const navigateToGame = (gameId: string) => {
    window.history.pushState(null, '', `/game=${encodeURIComponent(gameId)}`);
    setSelectedGameId(gameId);
  };

  // =========================================================
  // BOOTSTRAP SNAPSHOT + CONNECT SSE
  // =========================================================
  useEffect(() => {
    if (!selectedGameId) return;

    let unsubscribe: (() => void) | null = null;
    let cancelled = false;

    const init = async () => {
      try {
        stats.disconnect();
        setIsConnected(false);
        setGameState(null);
        setGameEvents([]);
        setError(null);
        stats.setGameId(selectedGameId);

        const snapshot = await stats.getSnapshot();
        if (cancelled) return;

        setGameState(snapshot.gameState);
        setGameEvents(snapshot.gameEvents);

        if (snapshot.gameState.isActive) {
          unsubscribe = stats.subscribe((event) => {
            if (event.type === 'gamestate') {
              setGameState(event.data);
            }

            if (event.type === 'playevent') {
              const sourceEvent = event.data.sourceEvent;
              if (!sourceEvent) {
                setError('Live event did not include its source game event.');
                return;
              }

              const incomingEvent = { ...sourceEvent, id: event.data.id };
              setGameEvents((prev) => {
                if (!isEndOfPeriodEvent(incomingEvent.type)) {
                  return [...prev, incomingEvent];
                }

                const existingIndex = prev.findIndex(
                  (play) => play.type === incomingEvent.type,
                );
                if (existingIndex === -1) {
                  return [...prev, incomingEvent];
                }

                const existingTimestamp = prev[existingIndex].timestamp;
                const incomingTimestamp = incomingEvent.timestamp;
                if (
                  incomingTimestamp === null ||
                  (existingTimestamp !== null && incomingTimestamp <= existingTimestamp)
                ) {
                  return prev;
                }

                return prev.map((play, index) =>
                  index === existingIndex ? incomingEvent : play,
                );
              });
            }

          });

          await stats.connect();
          if (!cancelled) {
            setIsConnected(true);
          }
        }
      } catch (err: unknown) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load game data');
        }
      }
    };

    init();

    return () => {
      cancelled = true;
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
          onOpenGame={navigateToGame}
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
          setGameEvents([]);
          navigateToTeam(selectedTeamName);
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
        {gameState?.isActive && (
          <div className="field-section">
            <FieldVisualization homeTeam={gameState.homeTeamName} discPosition={gameState.discPosition} />
          </div>
        )}

        <div className="plays-section">
          <PlayHistory
            plays={gameEvents}
            homeTeam={gameState?.homeTeamName ?? ''}
            awayTeam={gameState?.awayTeamName ?? ''}
            emptyMessage={
              gameState?.gameStatus.toLowerCase() === 'upcoming'
                ? 'No plays yet.'
                : gameState?.gameStatus.toLowerCase() === 'final'
                  ? 'No play history available.'
                  : 'Waiting for plays...'
            }
          />
        </div>
      </div>
    </div>
  );
};