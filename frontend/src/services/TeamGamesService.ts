export interface TeamGame {
  gameId: string;
  awayTeam: string;
  homeTeam: string;
  awayScore: number;
  homeScore: number;
  status: string;
  startTime: string;
  startTimeZone: string;
  streamingUrl: string | null;
  location: string | null;
}

export async function fetchTeamGames(teamName: string): Promise<TeamGame[]> {
  const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5000';
  const response = await fetch(
    `${apiUrl}/api/stats/GetTeamGames?TeamName=${encodeURIComponent(teamName.toLowerCase())}`,
  );

  if (!response.ok) {
    throw new Error(`Failed to load team games: ${response.status}`);
  }

  return response.json() as Promise<TeamGame[]>;
}
