import React from 'react';

interface TeamTileProps {
  name: string;
  teamColor: string;
  teamLogo: string;
  onClick: () => void;
}

export const TeamTile: React.FC<TeamTileProps> = ({ name, teamColor, teamLogo, onClick }) => (
  <button
    type="button"
    className="team-tile"
    style={{ '--team-color': teamColor } as React.CSSProperties}
    onClick={onClick}
  >
    <img src={teamLogo} alt="" className="team-tile-logo" />
    <span className="team-tile-name">{name}</span>
  </button>
);
