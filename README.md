# UFAGameCast

A real-time web application for visualizing Ultimate Frisbee games with live field tracking, player animations, and play history.

## 📋 Overview

UFAGameCast provides a bird's-eye view of the field with animated player positions, driven by real-time stats data. The application features:

- **Live Field Visualization** — Animated player positions on an interactive SVG field
- **Play History** — Scrollable list of recent plays with timestamps
- **Real-time Streaming** — Server-Sent Events (SSE) for efficient data delivery
- **Smooth Animations** — Framer Motion for fluid player movement and UI transitions
- **Responsive Design** — Works seamlessly on desktop and tablet devices

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Upstream Stats API                     │
│                  (External Service)                      │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
        ┌─────────────────────────────┐
        │   C# ASP.NET Core BFF       │
        │  (Backend-for-Frontend)     │
        ├─────────────────────────────┤
        │ • Consumes upstream API     │
        │ • Cleans & transforms data  │
        │ • Streams via SSE           │
        │ • Manages game state        │
        └──────────────┬──────────────┘
                       │ SSE: /api/stats/live
                       ▼
        ┌─────────────────────────────┐
        │    React + TypeScript       │
        │      Frontend               │
        ├─────────────────────────────┤
        │ • Field visualization (SVG) │
        │ • Play history list         │
        │ • Game info panel           │
        │ • Framer Motion animations  │
        └─────────────────────────────┘
```

## 📁 Project Structure

```
UFAGameCast/
├── backend/                      # C# ASP.NET Core BFF
│   ├── Models/                   # Data contracts
│   ├── Controllers/              # API endpoints
│   ├── Services/                 # Business logic
│   ├── Properties/               # Configuration
│   ├── UFAGameCast.Backend.csproj
│   ├── Program.cs
│   ├── .gitignore
│   └── README.md
│
├── frontend/                     # React + TypeScript
│   ├── src/
│   │   ├── components/           # React components
│   │   ├── hooks/                # Custom React hooks
│   │   ├── services/             # API clients
│   │   ├── types/                # TypeScript definitions
│   │   ├── App.tsx
│   │   ├── main.tsx
│   │   └── index.css
│   ├── public/                   # Static assets
│   ├── index.html
│   ├── package.json
│   ├── tsconfig.json
│   ├── vite.config.ts
│   ├── .gitignore
│   └── README.md
│
├── docs/
│   └── API_CONTRACT.md           # API specification
│
├── .git/
├── .gitignore
└── README.md
```

## 🚀 Quick Start

### Prerequisites

- **.NET 8.0 SDK** or later (for backend)
- **Node.js 16+** and npm/yarn (for frontend)

### Setup

#### 1. Backend Setup

```bash
cd backend
dotnet restore
dotnet run
```

The backend API will start on **http://localhost:5000**

#### 2. Frontend Setup

In a new terminal:

```bash
cd frontend
npm install
npm run dev
```

The frontend will start on **http://localhost:3000** (or next available port)

#### 3. Verify Connection

1. Open **http://localhost:3000** in your browser
2. Check the connection status indicator in the top-left
3. Watch the field visualization and play history update in real-time

## 📊 API Contract

The frontend consumes data via **Server-Sent Events (SSE)** from the backend:

### SSE Endpoint

**GET `/api/stats/live`**

Streams two event types:

- `gamestate` — Complete game state snapshot (score, players, current time)
- `playevent` — Individual play events (pass, goal, turnover, etc.)

**Example Event:**

```
event: playevent
data: {
  "id": 5,
  "timestamp": "2026-06-15T14:30:40.000Z",
  "eventType": "Pass",
  "initiatorName": "Carrico",
  "receiverName": "Coniff",
  "distance": 24,
  "description": "Carrico throws 24yds to Coniff"
}
```

For detailed API specification, see [API_CONTRACT.md](docs/API_CONTRACT.md)

## 🔧 Development

### Run Both Services

Use separate terminals:

**Terminal 1 — Backend:**

```bash
cd backend
dotnet run
```

**Terminal 2 — Frontend:**

```bash
cd frontend
npm run dev
```

### Backend Structure

- **Models/** — DTOs that match frontend TypeScript types
- **Controllers/StatsController.cs** — SSE endpoint + helper endpoints
- **Services/GameStateService.cs** — Manages game state and event queue
- **Services/GameSimulationService.cs** — Simulates upstream API data (to be replaced)

### Frontend Structure

- **components/FieldVisualization.tsx** — SVG field with animated players
- **components/PlayHistory.tsx** — Scrollable play list with animations
- **components/GameInfo.tsx** — Score board and connection status
- **components/GameDashboard.tsx** — Main orchestration component
- **hooks/useStatsStream.ts** — Custom hook for SSE connection + state
- **services/StatsService.ts** — SSE client with reconnection logic

## 📱 Features

### ✅ Implemented

- [x] Real-time field visualization with animated player positions
- [x] SSE connection with automatic reconnection
- [x] Play history with timestamps and event types
- [x] Game info panel (score, team names, connection status)
- [x] Responsive design (desktop + tablet)
- [x] Sample data generation for testing
- [x] Full TypeScript support
- [x] Type-safe API contracts

### 🔮 Future Enhancements

- [ ] Replace sample data with real upstream API integration
- [ ] Add authentication/authorization
- [ ] Zoom/pan controls for field visualization
- [ ] Player roles and team roster display
- [ ] Game statistics panel (completion %, yards, etc.)
- [ ] Animated play replay (show ball trajectory)
- [ ] Fullscreen mode for field
- [ ] Audio alerts for goals/turnovers
- [ ] Historical game data and archives
- [ ] WebSocket support for bidirectional communication

## 🔌 Integration Notes

### Upstream API Integration

The backend currently **generates sample play events** for testing. To integrate with a real upstream API:

1. Replace `GameSimulationService` with an actual API client
2. Update DTOs in `Models/` if needed to match upstream schema
3. Keep the SSE streaming layer (`StatsController.cs`) unchanged

### Deployment

Each service can be deployed independently:

- **Backend**: Docker container with ASP.NET Core runtime
- **Frontend**: Static SPA (output to `dist/` folder)

For production deployment, consider:

- CORS policy adjustment for your domain
- Authentication/authorization layer
- Nginx/reverse proxy for frontend
- Container orchestration (Docker Compose, Kubernetes)

## 📝 Documentation

- [Backend README](backend/README.md) — Backend-specific setup and development
- [Frontend README](frontend/README.md) — Frontend-specific setup and development
- [API Contract](docs/API_CONTRACT.md) — Detailed API specification

## 🛠️ Troubleshooting

### Frontend won't connect to backend

- Check backend is running on `http://localhost:5000`
- Verify CORS is configured correctly in `Program.cs`
- Check browser DevTools → Network tab for SSE connection
- Look at browser console for error messages

### No plays appearing

- Verify `GameSimulationService` is running in backend (check terminal output)
- Check `/api/stats/current` in browser to see current game state
- Monitor browser console for SSE event parsing errors

### TypeScript errors

- Run `npm install` in `frontend/` to ensure all dependencies are present
- Delete `node_modules/` and `.npm/` cache if issues persist
- Ensure Node.js version is 16+

## 📄 License

(Add appropriate license information)

## 👤 Author

Jake (Jake3c) — [GitHub](https://github.com/Jake3c)

---

**Ready to dive in?** Start with the [Quick Start](#-quick-start) section above!
