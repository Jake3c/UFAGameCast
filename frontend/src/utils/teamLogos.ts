import TeamLogo_ATL from "../resources/logo-team-ATL.png";
import TeamLogo_AUS from "../resources/logo-team-AUS.png";
import TeamLogo_BOS from "../resources/logo-team-BOS.png";
import TeamLogo_CHI from "../resources/logo-team-CHI.png";
import TeamLogo_COL from "../resources/logo-team-COL.png";
import TeamLogo_DC from "../resources/logo-team-DC.png";
import TeamLogo_HOU from "../resources/logo-team-HOU.png";
import TeamLogo_IND from "../resources/logo-team-IND.png";
import TeamLogo_LV from "../resources/logo-team-LV.png";
import TeamLogo_MAD from "../resources/logo-team-MAD.png";
import TeamLogo_MIN from "../resources/logo-team-MIN.png";
import TeamLogo_MTL from "../resources/logo-team-MTL.png";
import TeamLogo_NY from "../resources/logo-team-NY.png";
import TeamLogo_OAK from "../resources/logo-team-OAK.png";
import TeamLogo_ORE from "../resources/logo-team-ORE.png";
import TeamLogo_PHI from "../resources/logo-team-PHI.png";
import TeamLogo_PIT from "../resources/logo-team-PIT.png";
import TeamLogo_RAL from "../resources/logo-team-RAL.png";
import TeamLogo_SD from "../resources/logo-team-SD.png";
import TeamLogo_SEA from "../resources/logo-team-SEA.png";
import TeamLogo_SLC from "../resources/logo-team-SLC.png";
import TeamLogo_TOR from "../resources/logo-team-TOR.png";

export function getTeamLogo(teamName: string): string {
  return teamLogos[teamName.toLowerCase()] || "";
}

const teamLogos: Record<string, string> = {
  "hustle": TeamLogo_ATL,
  "sol": TeamLogo_AUS,
  "glory": TeamLogo_BOS,
  "union": TeamLogo_CHI,
  "apex": TeamLogo_COL,
  "breese": TeamLogo_DC,
  "havoc": TeamLogo_HOU,
  "alleycats": TeamLogo_IND,
  "bighorns": TeamLogo_LV,
  "radicals": TeamLogo_MAD,
  "windchill": TeamLogo_MIN,
  "royale": TeamLogo_MTL,
  "empire": TeamLogo_NY,
  "spiders": TeamLogo_OAK,
  "steel": TeamLogo_ORE,
  "phoenix": TeamLogo_PHI,
  "thunderbirds": TeamLogo_PIT,
  "flyers": TeamLogo_RAL,
  "growlers": TeamLogo_SD,
  "rush": TeamLogo_TOR,
  "cascades": TeamLogo_SEA,
  "shred": TeamLogo_SLC
};