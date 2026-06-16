/**
 * Frontend TypeScript types that mirror backend C# models
 * These ensure type safety and match the API contract
 */

export enum EventType {
  Pass = 'Pass',
  Goal = 'Goal',
  Turnover = 'Turnover',
  Block = 'Block',
  Catch = 'Catch',
  Drop = 'Drop',
  Other = 'Other',
}

export interface FieldPosition {
  x: number;
  y: number;
}

export interface PlayerSnapshot {
  playerId: number;
  playerName: string;
  team: string;
  position: FieldPosition;
  jerseyNumber: number;
}

export interface PlayEvent {
  id: number;
  timestamp: string;
  eventType: EventType;
  initiatorPlayerId: number;
  initiatorName: string;
  receiverPlayerId?: number;
  receiverName?: string;
  distance?: number;
  description: string;
  playersInvolved: PlayerSnapshot[];
}

export interface GameState {
  gameId: number;
  currentTime: string;
  team1Name: string;
  team2Name: string;
  team1Score: number;
  team2Score: number;
  allPlayers: PlayerSnapshot[];
  lastPlayEvent?: PlayEvent;
  discPosition: FieldPosition;
}

export interface StatsStreamEvent {
  type: 'gamestate' | 'playevent';
  data: GameState | PlayEvent;
}
