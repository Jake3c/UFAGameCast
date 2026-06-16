# UFAGameCast Frontend

React + TypeScript frontend for the UFAGameCast field visualization system.

## Overview

A real-time web application that displays:

- **Field Visualization** — Bird's-eye view of the field with animated player positions
- **Play History** — Scrollable list of recent plays with timestamps
- **Game Info** — Current score, team names, and connection status

## Getting Started

### Prerequisites

- Node.js 16+ and npm/yarn

### Setup

1. Navigate to the frontend directory:

   ```bash
   cd frontend
   ```

2. Install dependencies:

   ```bash
   npm install
   ```

3. Create a `.env.local` file (optional, defaults to `http://localhost:5000`):

   ```
   VITE_API_URL=http://localhost:5000
   ```

4. Start the development server:

   ```bash
   npm run dev
   ```

   The app will run on `http://localhost:3000` (or next available port)

5. Open your browser and navigate to the development URL shown in terminal

## Build for Production

```bash
npm run build
```

Output goes to the `dist/` directory.

## Project Structure

- **src/components/** — React components
  - `GameDashboard.tsx` — Main app container
  - `FieldVisualization.tsx` — SVG field with animated player markers
  - `PlayHistory.tsx` — Scrollable play list
  - `GameInfo.tsx` — Score, team names, connection status
- **src/hooks/** — Custom React hooks
  - `useStatsStream.ts` — Manages SSE connection and game state
- **src/services/** — API clients
  - `StatsService.ts` — Server-Sent Events connection handler
- **src/types/** — TypeScript type definitions
  - `api.ts` — Mirrors backend C# models

## Features

- **Real-time Updates** — Server-Sent Events (SSE) stream from backend
- **Smooth Animations** — Framer Motion for player movement and play history
- **Responsive Design** — Works on desktop and tablet screens
- **Type Safety** — Full TypeScript support with type contracts matching backend
- **Automatic Reconnection** — Graceful handling of connection errors with exponential backoff
- **Live Connection Status** — Visual indicator of streaming connection state

## Environment Variables

- `VITE_API_URL` — Backend API base URL (defaults to `http://localhost:5000`)

## Development Notes

- **Hot Module Replacement (HMR)** — Changes are reflected instantly during development
- **Framer Motion** — Used for smooth animations of player positions and play history items
- **SVG Field** — Uses percentage-based coordinates (0-100) for scalability to any screen size
- **Responsive Grid** — Dashboard adapts from 2-column to 1-column layout on smaller screens

## Integration with Backend

The frontend expects the backend SSE endpoint at:

```
GET /api/stats/live
```

With events of type:

- `gamestate` — Complete game state snapshot
- `playevent` — Individual play events

See [API_CONTRACT.md](../docs/API_CONTRACT.md) for detailed event schemas.

## TODO

- [ ] Add keyboard/gamepad controls for camera pan/zoom
- [ ] Display player roles/positions on field
- [ ] Add animated play replay (show ball movement trajectory)
- [ ] Add team roster view with player stats
- [ ] Add game statistics panel (completion %, yards, etc.)
- [ ] Add fullscreen mode for field visualization
- [ ] Add audio alerts for goals/turnovers
