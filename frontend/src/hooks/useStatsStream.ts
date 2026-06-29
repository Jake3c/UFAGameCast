import { useEffect, useState, useCallback, useRef } from 'react';
import { GameState, PlayEvent, StatsStreamEvent } from '../types/api';
import { getStatsService } from '../services/StatsService';

const RECENT_PLAYS_LIMIT = 20;

export function useStatsStream() {
  const [gameState, setGameState] = useState<GameState | null>(null);
  const [recentPlays, setRecentPlays] = useState<PlayEvent[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const unsubscribeRef = useRef<(() => void) | null>(null);
  const statsService = getStatsService();

  const handleStreamEvent = useCallback((event: StatsStreamEvent) => {
    if (event.type === 'gamestate') {
      setGameState(event.data as GameState);
    }

    if (event.type === 'playevent') {
      const play = event.data as PlayEvent;

      setRecentPlays(prev => {
        if (prev.some(p => p.id === play.id)) return prev;
        return [play, ...prev].slice(0, RECENT_PLAYS_LIMIT);
      });
    }
  }, []);

  useEffect(() => {
    let mounted = true;

    const start = async () => {
      try {
        setError(null);

        await statsService.connect();

        if (!mounted) return;

        setIsConnected(true);

        const unsubscribe = statsService.subscribe(handleStreamEvent);
        unsubscribeRef.current = unsubscribe;
      } catch (e) {
        if (!mounted) return;

        setError(e instanceof Error ? e.message : 'connection failed');
        setIsConnected(false);
      }
    };

    start();

    return () => {
      mounted = false;

      unsubscribeRef.current?.();
      unsubscribeRef.current = null;

      // 🔥 CRITICAL FIX
      statsService.reset();

      setGameState(null);
      setRecentPlays([]);
      setIsConnected(false);
    };
  }, [statsService, handleStreamEvent]);

  return {
    gameState,
    recentPlays,
    isConnected,
    error,
  };
}