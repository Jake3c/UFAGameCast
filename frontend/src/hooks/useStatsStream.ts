import { useEffect, useState, useCallback, useRef } from 'react';
import { GameState, PlayEvent, StatsStreamEvent } from '../types/api';
import { getStatsService } from '../services/StatsService';

interface UseStatsStreamReturn {
  gameState: GameState | null;
  recentPlays: PlayEvent[];
  isConnected: boolean;
  error: string | null;
}

const RECENT_PLAYS_LIMIT = 20;

/**
 * Custom React hook for managing the SSE connection and game state
 * Automatically connects/disconnects based on component lifecycle
 */
export function useStatsStream(): UseStatsStreamReturn {
  const [gameState, setGameState] = useState<GameState | null>(null);
  const [recentPlays, setRecentPlays] = useState<PlayEvent[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const unsubscribeRef = useRef<(() => void) | null>(null);
  const statsService = getStatsService();

  /**
   * Handle incoming stream events
   */
  const handleStreamEvent = useCallback((event: StatsStreamEvent) => {
    if (event.type === 'gamestate') {
      setGameState(event.data as GameState);
    } else if (event.type === 'playevent') {
      const playEvent = event.data as PlayEvent;
      setRecentPlays((prev) => {
        // Check if this play already exists in the list (avoid duplicates)
        const alreadyExists = prev.some((p) => p.id === playEvent.id);
        if (alreadyExists) {
          return prev;
        }
        // Add new play and keep only the most recent plays
        const updated = [playEvent, ...prev];
        return updated.slice(0, RECENT_PLAYS_LIMIT);
      });
    }
  }, []);

  /**
   * Connect to the stats stream on mount and subscribe to events
   */
  useEffect(() => {
    const connect = async () => {
      try {
        setError(null);
        await statsService.connect();
        setIsConnected(true);

        // Subscribe to stream events
        const unsubscribe = statsService.subscribe(handleStreamEvent);
        unsubscribeRef.current = unsubscribe;
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Failed to connect to stats stream';
        setError(errorMessage);
        setIsConnected(false);
      }
    };

    connect();

    // Cleanup on unmount
    return () => {
      if (unsubscribeRef.current) {
        unsubscribeRef.current();
      }
      statsService.disconnect();
    };
  }, [statsService, handleStreamEvent]);

  return {
    gameState,
    recentPlays,
    isConnected,
    error,
  };
}
