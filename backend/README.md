# UFAGameCast Backend

C# ASP.NET Core BFF (Backend-for-Frontend) for the UFAGameCast field visualization system.

## Overview

This is the middle layer that:

- Consumes the upstream stats API (to be integrated)
- Cleans and transforms data for the frontend
- Streams real-time game state and play events via Server-Sent Events (SSE)

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later

### Setup

1. Navigate to the backend directory:

   ```bash
   cd backend
   ```

2. Restore dependencies:

   ```bash
   dotnet restore
   ```

3. Run the project:

   ```bash
   dotnet run
   ```

   The API will start on `http://localhost:5000`

### API Endpoints

- **GET `/api/stats/live`** — Server-Sent Events stream for real-time game state and play events
  - Content-Type: `text/event-stream`
  - Events: `gamestate`, `playevent`

- **GET `/api/stats/current`** — Get the current game state (polling alternative)

- **GET `/api/stats/recent-plays?count=10`** — Get the last N play events

## Project Structure

- **Models/** — Data contracts (GameState, PlayEvent, FieldPosition, etc.)
- **Controllers/** — API endpoints
- **Services/** — Business logic
  - `GameStateService` — Manages current game state and event queue
  - `GameSimulationService` — Background service simulating upstream API data

## Development Notes

- **Current Implementation:** Uses sample data generation via `GameSimulationService` (generates random play events)
- **Next Phase:** Replace sample data with real upstream API integration
- **CORS:** Configured to allow requests from `http://localhost:3000` and `http://localhost:5173`

## Integration Points

- **Upstream Stats API:** Currently simulated; replace `GameSimulationService` with actual API client
- **Frontend:** React app on `http://localhost:3000` consumes SSE stream from `/api/stats/live`

## TODO

- [ ] Replace sample data generation with real upstream API integration
- [ ] Add authentication/authorization for `/api/stats/live` endpoint
- [ ] Add historical data endpoints (game history, player stats, etc.)
- [ ] Add WebSocket support if real-time bidirectional communication becomes needed
- [ ] Add proper error logging and monitoring
