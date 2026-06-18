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
  time: string;
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
  homeTeamName: string;
  awayTeamName: string;
  homeTeamScore: number;
  awayTeamScore: number;
  homeTeamWins: number;
  awayTeamWins: number;
  homeTeamLosses: number;
  awayTeamLosses: number;
  homeTeamDivisionStanding: number;
  awayTeamDivisionStanding: number;
  gameStatus: string;
  isActive: boolean;
  week: string;
  discPosition: FieldPosition;
  streamingUrl: string;
}

export type StatsStreamEvent =
  | { type: 'gamestate'; data: GameState }
  | { type: 'playevent'; data: PlayEvent };
