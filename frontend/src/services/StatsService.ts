import { GameState, PlayEvent, StatsStreamEvent } from '../types/api';

/**
 * Manages Server-Sent Events (SSE) connection to the backend
 * Handles reconnection logic and event parsing
 */
export class StatsService {
  private eventSource: EventSource | null = null;
  private listeners: Array<(event: StatsStreamEvent) => void> = [];
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 10;
  private reconnectDelay = 1000; // ms
  private readonly apiUrl: string;

  constructor(apiUrl: string = import.meta.env.VITE_API_URL || 'http://localhost:5000') {
    this.apiUrl = apiUrl;
  }

  /**
   * Connect to the SSE stream
   */
  connect(): Promise<void> {
    return new Promise((resolve, reject) => {
      try {
        const url = `${this.apiUrl}/api/stats/live`;
        this.eventSource = new EventSource(url);

        this.eventSource.addEventListener('gamestate', (event: Event) => {
          const messageEvent = event as MessageEvent;
          try {
            const gameState: GameState = JSON.parse(messageEvent.data);
            this.notifyListeners({ type: 'gamestate', data: gameState });
          } catch (err) {
            console.error('Failed to parse gamestate event:', err);
          }
        });

        this.eventSource.addEventListener('playevent', (event: Event) => {
          const messageEvent = event as MessageEvent;
          try {
            const playEvent: PlayEvent = JSON.parse(messageEvent.data);
            this.notifyListeners({ type: 'playevent', data: playEvent });
          } catch (err) {
            console.error('Failed to parse playevent:', err);
          }
        });

        this.eventSource.onerror = () => {
          console.error('SSE connection error');
          this.handleConnectionError(reject);
        };

        this.reconnectAttempts = 0;
        console.log('Connected to stats stream:', url);
        resolve();
      } catch (err) {
        reject(err);
      }
    });
  }

  /**
   * Subscribe to stream events
   */
  subscribe(listener: (event: StatsStreamEvent) => void): () => void {
    this.listeners.push(listener);

    // Return unsubscribe function
    return () => {
      const index = this.listeners.indexOf(listener);
      if (index > -1) {
        this.listeners.splice(index, 1);
      }
    };
  }

  /**
   * Disconnect from the SSE stream
   */
  disconnect(): void {
    if (this.eventSource) {
      this.eventSource.close();
      this.eventSource = null;
      console.log('Disconnected from stats stream');
    }
  }

  /**
   * Check if connected to SSE stream
   */
  isConnected(): boolean {
    return this.eventSource !== null && this.eventSource.readyState === EventSource.OPEN;
  }

  /**
   * Notify all listeners of a new event
   */
  private notifyListeners(event: StatsStreamEvent): void {
    this.listeners.forEach((listener) => {
      try {
        listener(event);
      } catch (err) {
        console.error('Error in event listener:', err);
      }
    });
  }

  /**
   * Handle connection errors and attempt reconnection
   */
  private handleConnectionError(reject?: (reason?: any) => void): void {
    this.disconnect();

    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++;
      const delay = this.reconnectDelay * Math.pow(1.5, this.reconnectAttempts - 1);
      console.log(`Attempting to reconnect (${this.reconnectAttempts}/${this.maxReconnectAttempts}) in ${delay}ms`);

      setTimeout(() => {
        this.connect().catch((err) => {
          console.error('Reconnection failed:', err);
          if (this.reconnectAttempts >= this.maxReconnectAttempts) {
            console.error('Max reconnection attempts reached');
            reject?.(err);
          } else {
            this.handleConnectionError(reject);
          }
        });
      }, delay);
    } else {
      console.error('Failed to connect after max attempts');
      reject?.(new Error('Failed to connect to stats stream'));
    }
  }
}

// Singleton instance
let statsService: StatsService | null = null;

export function getStatsService(): StatsService {
  if (!statsService) {
    statsService = new StatsService();
  }
  return statsService;
}
