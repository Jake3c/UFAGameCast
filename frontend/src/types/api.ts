/**
 * Frontend TypeScript types that mirror backend C# models
 * These ensure type safety and match the API contract
 */

export interface FieldPosition {
  x: number;
  y: number;
}

export interface GameEvent {
  id: number;
  isHomeTeam: boolean;
  timestamp: number | null;
  type: string;
  line: string[] | null;
  time: number | null;
  thrower: string | null;
  throwerX: number | null;
  throwerY: number | null;
  receiver: string | null;
  receiverX: number | null;
  receiverY: number | null;
  defender: string | null;
  turnoverX: number | null;
  turnoverY: number | null;
  puller: string | null;
  pullX: number | null;
  pullY: number | null;
  pullMs: number | null;
  player: string | null;
}

export type SourceGameEvent = Omit<GameEvent, 'id'>;

export interface PlayEvent {
  id: number;
  time: string;
  timestamp: number | null;
  eventType: string;
  initiatorPlayerId: number | null;
  initiatorName: string;
  receiverPlayerId: number | null;
  receiverName: string | null;
  distance: number | null;
  description: string;
  sourceEvent: SourceGameEvent | null;
}

export interface GameState {
  gameId: string;
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
