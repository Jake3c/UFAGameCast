import { GameState, PlayEvent, StatsStreamEvent } from '../types/api';

/**
 * Manages:
 * 1. Snapshot load (initial full game state + history)
 * 2. SSE live updates (incremental updates only)
 */
export class StatsService {
  private gameId = '';
  private eventSource: EventSource | null = null;

  private listeners: Array<(event: StatsStreamEvent) => void> = [];

  private reconnectAttempts = 0;
  private maxReconnectAttempts = 10;
  private reconnectDelay = 1000;

  private readonly apiUrl: string;

  setGameId(gameId: string){
    this.gameId = gameId;
  }

  constructor(apiUrl: string = import.meta.env.VITE_API_URL || 'http://localhost:5000') {
    this.apiUrl = apiUrl;
  }

  // =========================================================
  // SNAPSHOT (INITIAL LOAD)
  // =========================================================

  async getSnapshot(): Promise<{
    gameState: GameState;
    playHistory: PlayEvent[];
  }> {
    const res = await fetch(`${this.apiUrl}/api/stats/snapshot?gameId=${encodeURIComponent(this.gameId)}`);

    if (!res.ok) {
      throw new Error(`Failed to load snapshot: ${res.status}`);
    }

    return await res.json();
  }

  /**
   * Optional convenience method:
   * snapshot + connect in one call
   */
  async initialize(): Promise<{
    gameState: GameState;
    playHistory: PlayEvent[];
  }> {
    const snapshot = await this.getSnapshot();
    await this.connect();
    return snapshot;
  }

  // =========================================================
  // SSE CONNECTION
  // =========================================================

  connect(): Promise<void> {
    return new Promise((resolve, reject) => {
      try {
        const url = `${this.apiUrl}/api/stats/live`;
        this.eventSource = new EventSource(url);

        this.eventSource.onopen = () => {
          this.reconnectAttempts = 0;
          console.log('Connected to stats stream:', url);
          resolve();
        };

        this.eventSource.addEventListener('gamestate', (event: MessageEvent) => {
          this.safeNotify('gamestate', event.data, this.parseGameState);
        });

        this.eventSource.addEventListener('playevent', (event: MessageEvent) => {
          this.safeNotify('playevent', event.data, this.parsePlayEvent);
        });

        this.eventSource.onerror = () => {
          console.error('SSE connection error');
          this.handleConnectionError(reject);
        };
      } catch (err) {
        reject(err);
      }
    });
  }

  // =========================================================
  // SUBSCRIPTIONS
  // =========================================================

  subscribe(listener: (event: StatsStreamEvent) => void): () => void {
    this.listeners.push(listener);

    return () => {
      const index = this.listeners.indexOf(listener);
      if (index > -1) {
        this.listeners.splice(index, 1);
      }
    };
  }

  // =========================================================
  // CONNECTION MANAGEMENT
  // =========================================================

  disconnect(): void {
    if (this.eventSource) {
      this.eventSource.close();
      this.eventSource = null;
      console.log('Disconnected from stats stream');
    }
  }

  isConnected(): boolean {
    return (
      this.eventSource !== null &&
      this.eventSource.readyState === EventSource.OPEN
    );
  }

  private handleConnectionError(reject?: (reason?: any) => void): void {
    this.disconnect();

    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++;

      const delay =
        this.reconnectDelay * Math.pow(1.5, this.reconnectAttempts - 1);

      console.log(
        `Reconnecting (${this.reconnectAttempts}/${this.maxReconnectAttempts}) in ${delay}ms`
      );

      setTimeout(() => {
        this.connect().catch((err) => {
          console.error('Reconnection failed:', err);

          if (this.reconnectAttempts >= this.maxReconnectAttempts) {
            reject?.(err);
          } else {
            this.handleConnectionError(reject);
          }
        });
      }, delay);
    } else {
      reject?.(new Error('Failed to connect to stats stream'));
    }
  }

  // =========================================================
  // EVENT DISPATCH
  // =========================================================

  private notifyListeners(event: StatsStreamEvent): void {
    this.listeners.forEach((listener) => {
      try {
        listener(event);
      } catch (err) {
        console.error('Listener error:', err);
      }
    });
  }

  private safeNotify<T>(
    type: StatsStreamEvent['type'],
    data: string,
    parser: (d: string) => T
  ) {
    try {
      const parsed = parser(data);

      this.notifyListeners({
        type,
        data: parsed,
      } as StatsStreamEvent);
    } catch (err) {
      console.error(`Failed to parse ${type}:`, err);
    }
  }

  // =========================================================
  // PARSERS
  // =========================================================

  private parseGameState(data: string): GameState {
    return JSON.parse(data);
  }

  private parsePlayEvent(data: string): PlayEvent {
    return JSON.parse(data);
  }
}

// =========================================================
// SINGLETON
// =========================================================

let statsService: StatsService | null = null;

export function getStatsService(): StatsService {
  if (!statsService) {
    statsService = new StatsService();
  }
  return statsService;
}