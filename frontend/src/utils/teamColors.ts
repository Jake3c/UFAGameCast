export function getTeamColor(teamName: string): string {
    const teamColors: Record<string, string> = {
        "hustle": "#333366",
        "sol": "#2B3283",
        "glory": "#E1B87F",
        "union": "#41B6E6",
        "apex": "#9FA8F0",
        "breese": "#0A3751",
        "havoc": "#FFF349",
        "alleycats": "#27A332",
        "bighorns": "#A25F3F",
        "radicals": "#cbb82e",
        "windchill": "#6F7F98",
        "royale": "#f96b07",
        "empire": "#69bd45",
        "spiders": "#FEBD25",
        "steel": "#00BBF2",
        "phoenix": "#F04E23",
        "thunderbirds": "#FDBC11",
        "flyers": "#003049",
        "growlers": "#EC1C24",
        "rush": "#C52033",
        "cascades": "#195187",
        "shred": "#00477B"
    };

    return teamColors[teamName.toLowerCase()] || "#ffffff";
}