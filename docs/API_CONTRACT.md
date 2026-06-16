# UFAGameCast API Contract

## Overview

This document defines the API contract between the C# backend and React frontend for real-time game state and play event streaming.

## Server-Sent Events (SSE) Endpoint

### GET `/api/stats/live`

Establishes a persistent Server-Sent Events connection for streaming real-time game data.

**Response Headers:**

```
Content-Type: text/event-stream
Cache-Control: no-cache
X-Accel-Buffering: no
```

**Connection:** HTTP/1.1 persistent connection (keeps open as long as client is connected)

**Events:** Two event types are emitted via this stream

---

## Event Types

### 1. `gamestate`

Emitted periodically (every ~2 seconds) or when the game state changes.

**Event Format:**

```
event: gamestate
data: {JSON payload}

```

**JSON Payload Schema:**

```typescript
interface GameState {
  gameId: number;
  currentTime: string; // ISO 8601 UTC timestamp
  team1Name: string; // e.g., "Hawks"
  team2Name: string; // e.g., "Eagles"
  team1Score: number;
  team2Score: number;
  allPlayers: PlayerSnapshot[]; // All players on field
  lastPlayEvent?: PlayEvent; // Most recent play
}

interface PlayerSnapshot {
  playerId: number;
  playerName: string;
  team: string;
  position: FieldPosition;
  jerseyNumber: number;
}

interface FieldPosition {
  x: number; // 0-100 (percentage of field width)
  y: number; // 0-100 (percentage of field height)
}
```

**Example:**

```json
{
  "gameId": 1,
  "currentTime": "2026-06-15T14:30:45.123Z",
  "team1Name": "Hawks",
  "team2Name": "Eagles",
  "team1Score": 2,
  "team2Score": 1,
  "allPlayers": [
    {
      "playerId": 1,
      "playerName": "Carrico",
      "team": "Hawks",
      "position": { "x": 45.5, "y": 30.0 },
      "jerseyNumber": 1
    }
    // ... more players
  ],
  "lastPlayEvent": {
    "id": 5,
    "timestamp": "2026-06-15T14:30:40.000Z",
    "eventType": "Pass",
    "initiatorPlayerId": 1,
    "initiatorName": "Carrico",
    "receiverPlayerId": 2,
    "receiverName": "Coniff",
    "distance": 24,
    "description": "Carrico throws 24yds to Coniff",
    "playersInvolved": [
      // PlayerSnapshot array
    ]
  }
}
```

---

### 2. `playevent`

Emitted each time a new play occurs.

**Event Format:**

```
event: playevent
data: {JSON payload}

```

**JSON Payload Schema:**

```typescript
interface PlayEvent {
  id: number;
  timestamp: string; // ISO 8601 UTC timestamp
  eventType: EventType; // One of: Pass, Goal, Turnover, Block, Catch, Drop, Other
  initiatorPlayerId: number;
  initiatorName: string;
  receiverPlayerId?: number; // Optional, depending on event type
  receiverName?: string;
  distance?: number; // Yards (optional)
  description: string; // Human-readable summary
  playersInvolved: PlayerSnapshot[]; // Players whose positions changed in this play
}

enum EventType {
  Pass = "Pass",
  Goal = "Goal",
  Turnover = "Turnover",
  Block = "Block",
  Catch = "Catch",
  Drop = "Drop",
  Other = "Other",
}
```

**Example:**

```json
{
  "id": 6,
  "timestamp": "2026-06-15T14:30:47.500Z",
  "eventType": "Goal",
  "initiatorPlayerId": 2,
  "initiatorName": "Coniff",
  "receiverPlayerId": 3,
  "receiverName": "Marks",
  "distance": 10,
  "description": "Coniff throws 10yd goal to Marks",
  "playersInvolved": [
    {
      "playerId": 2,
      "playerName": "Coniff",
      "team": "Hawks",
      "position": { "x": 50.0, "y": 50.0 },
      "jerseyNumber": 2
    },
    {
      "playerId": 3,
      "playerName": "Marks",
      "team": "Hawks",
      "position": { "x": 60.0, "y": 55.0 },
      "jerseyNumber": 3
    }
  ]
}
```

---

## Additional Endpoints

### GET `/api/stats/current`

Returns the current game state (polling alternative to SSE).

**Response:**

```json
{
  "gameId": 1,
  "currentTime": "...",
  "team1Name": "Hawks",
  "team2Name": "Eagles",
  "team1Score": 2,
  "team2Score": 1,
  "allPlayers": [...],
  "lastPlayEvent": {...}
}
```

---

### GET `/api/stats/recent-plays?count=10`

Returns the last N play events.

**Query Parameters:**

- `count` (optional, default=10, max=100) — Number of recent plays to return

**Response:**

```json
[
  { "id": 5, "timestamp": "...", ... },
  { "id": 4, "timestamp": "...", ... },
  { "id": 3, "timestamp": "...", ... }
]
```

---

## Field Coordinate System

The field uses a **percentage-based coordinate system** for scalability across different screen sizes:

```
(0, 0) ──────────────────────── (100, 0)
│                                     │
│                                     │
│          Field (100x60)             │
│                                     │
(0, 60) ──────────────────────── (100, 60)

- X: 0-100 (0 = left sideline, 100 = right sideline)
- Y: 0-60 (0 = bottom endline, 60 = top endline)
```

**Why Percentages?**

- Responsive to container size
- No need to adjust coordinates for different resolutions
- Players render at correct proportions on any screen

---

## Implementation Notes for Frontend

1. **SSE Connection:**
   - Use native `EventSource` API to connect to `/api/stats/live`
   - Listen for `gamestate` and `playevent` event types
   - Implement reconnection logic with exponential backoff on connection failure

2. **State Management:**
   - Store `GameState` in React state and update on `gamestate` events
   - Maintain a queue of recent `PlayEvent` items (latest ~20 plays)
   - Trigger Framer Motion animations on player position changes and new play events

3. **Error Handling:**
   - Handle `EventSource` `onerror` callback for connection failures
   - Display connection status indicator in UI
   - Provide user feedback for network issues

4. **Performance:**
   - Use React `useMemo` / `useCallback` to prevent unnecessary re-renders
   - Animate only affected players using Framer Motion's `layoutId` or key-based reconciliation
   - Debounce or throttle state updates if needed for high-frequency events

---

## Future Enhancements

- [ ] Add authentication/authorization to SSE endpoint
- [ ] Add historical game data endpoints
- [ ] Add WebSocket support for bidirectional communication (e.g., user interactions)
- [ ] Add filtering/subscription to specific event types
- [ ] Add replay/scrubbing functionality for historical plays
- [ ] Add real player roster and team metadata endpoints
