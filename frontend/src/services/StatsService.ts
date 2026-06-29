import { GameState, PlayEvent, StatsStreamEvent } from '../types/api';

export class StatsService {
  private gameId = '';
  private eventSource: EventSource | null = null;

  private listeners: Set<(event: StatsStreamEvent) => void> = new Set();

  private reconnectAttempts = 0;
  private maxReconnectAttempts = 10;
  private reconnectDelay = 1000;
  private reconnectTimeout: number | null = null;

  private readonly apiUrl: string;

  constructor(apiUrl: string = import.meta.env.VITE_API_URL || 'http://localhost:5000') {
    this.apiUrl = apiUrl;
  }

  setGameId(gameId: string) {
    this.gameId = gameId;
  }

  // =========================
  // SNAPSHOT
  // =========================

  async getSnapshot(): Promise<{
    gameState: GameState;
    playHistory: PlayEvent[];
  }> {
    const res = await fetch(
      `${this.apiUrl}/api/stats/snapshot?gameId=${encodeURIComponent(this.gameId)}`
    );

    if (!res.ok) throw new Error(`Failed snapshot: ${res.status}`);

    return await res.json();
  }

  async initialize() {
    const snapshot = await this.getSnapshot();
    await this.connect();
    return snapshot;
  }

  // =========================
  // SSE
  // =========================

  connect(): Promise<void> {
    // prevent double connections
    if (this.eventSource) {
      this.disconnect();
    }

    return new Promise((resolve, reject) => {
      const url = `${this.apiUrl}/api/stats/live`;

      this.eventSource = new EventSource(url);

      this.eventSource.onopen = () => {
        this.reconnectAttempts = 0;
        console.log('SSE connected');
        resolve();
      };

      this.eventSource.addEventListener('gamestate', (event: MessageEvent) => {
        this.emit({
          type: 'gamestate',
          data: JSON.parse(event.data),
        });
      });

      this.eventSource.addEventListener('playevent', (event: MessageEvent) => {
        this.emit({
          type: 'playevent',
          data: JSON.parse(event.data),
        });
      });

      this.eventSource.onerror = () => {
        console.error('SSE error');
        this.handleReconnect(reject);
      };
    });
  }

  // =========================
  // SUBSCRIPTIONS
  // =========================

  subscribe(listener: (event: StatsStreamEvent) => void): () => void {
    this.listeners.add(listener);

    return () => {
      this.listeners.delete(listener);
    };
  }

  private emit(event: StatsStreamEvent) {
    for (const listener of this.listeners) {
      try {
        listener(event);
      } catch (e) {
        console.error('listener error', e);
      }
    }
  }

  // =========================
  // CLEANUP (THIS FIXES YOUR ISSUE)
  // =========================

  disconnect() {
    if (this.eventSource) {
      console.log('SSE disconnected');
      this.eventSource.close();
      this.eventSource = null;
    }

    if (this.reconnectTimeout) {
      clearTimeout(this.reconnectTimeout);
      this.reconnectTimeout = null;
    }

    this.reconnectAttempts = 0;
  }

  reset() {
    this.disconnect();
    this.listeners.clear(); // 🔥 THIS fixes duplicate listeners
  }

  // =========================
  // RECONNECT
  // =========================

  private handleReconnect(reject?: (reason?: any) => void) {
    this.disconnect();

    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      reject?.(new Error('SSE failed'));
      return;
    }

    this.reconnectAttempts++;

    const delay = this.reconnectDelay * Math.pow(1.5, this.reconnectAttempts - 1);

    this.reconnectTimeout = window.setTimeout(() => {
      this.connect().catch(err => {
        console.error('Reconnect failed', err);
        this.handleReconnect(reject);
      });
    }, delay);
  }
}

// singleton
let statsService: StatsService | null = null;

export function getStatsService() {
  if (!statsService) {
    statsService = new StatsService();
  }
  return statsService;
}