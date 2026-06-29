import React from 'react';
import { motion } from 'framer-motion';
import { getTeamLogo } from '../utils/teamLogos';

interface FieldVisualizationProps {
  homeTeam: string;
  discPosition?: { x: number; y: number };
  width?: number;
  height?: number;
}

/**
 * Renders a bird's-eye view of the field with animated player positions and disc
 */
export const FieldVisualization: React.FC<FieldVisualizationProps> = ({
  discPosition,
  homeTeam,
}) => {
  // SVG coordinates: 120 units wide x 53 units tall with 20-unit endzones
  const fieldWidth = 120;
  const fieldHeight = 53;
  const endzoneDepth = 20;

  var homeTeamLogo = getTeamLogo(homeTeam);

  return (
    <div className="field-visualization-container">
      <svg
        viewBox={`0 0 ${fieldWidth} ${fieldHeight}`}
        className="field-svg"
        style={{ width: `100%`, height: `100%` }}
      >
        {/* Field background */}
        <rect width={fieldWidth} height={fieldHeight} fill="#2d5016" />

        {/* Sidelines */}
        <line x1="0" y1="0" x2={fieldWidth} y2="0" stroke="white" strokeWidth="0.5" />
        <line x1="0" y1={fieldHeight} x2={fieldWidth} y2={fieldHeight} stroke="white" strokeWidth="0.5" />

        {/* Endlines */}
        <line x1="0" y1="0" x2="0" y2={fieldHeight} stroke="white" strokeWidth="0.5" />
        <line x1={fieldWidth} y1="0" x2={fieldWidth} y2={fieldHeight} stroke="white" strokeWidth="0.5" />

        {/* Midfield line */}
        <line x1={fieldWidth / 2} y1="0" x2={fieldWidth / 2} y2={fieldHeight} stroke="white" strokeWidth="0.5" strokeDasharray="2" />

        {/* Goal areas (end zones) */}
        <rect x="0" y="0" width={endzoneDepth} height={fieldHeight} fill="rgba(255, 255, 255, 0.05)" />
        <rect x={fieldWidth - endzoneDepth} y="0" width={endzoneDepth} height={fieldHeight} fill="rgba(255, 255, 255, 0.05)" />

        {/* Home field logo */}
        <image
            href={homeTeamLogo}
            x={fieldWidth / 2 - 20}
            y={fieldHeight / 2 - 20}
            width="40"
            height="40"
            opacity="0.40"
            preserveAspectRatio="xMidYMid meet"
        />

        {/* Disc */}
        {discPosition && <DiscMarker position={discPosition} />}
      </svg>
    </div>
  );
};

interface DiscMarkerProps {
  position: { x: number; y: number };
}

/**
 * Single disc marker with animation
 */
const DiscMarker: React.FC<DiscMarkerProps> = ({ position }) => {
  const discRadius = 1.6;

  return (
    <motion.g
      initial={{ x: 0, y: 0 }}
      animate={{ x: position.x, y: position.y }}
      transition={{ type: 'spring', damping: 20, stiffness: 100 }}
    >
      {/* Disc circle */}
      <circle
        cx={0}
        cy={0}
        r={discRadius}
        fill="#ffffff"
        opacity="0.95"
        
        filter="drop-shadow(0 0 0.5px rgba(0, 0, 0, 0.3))"
      />

      {/* Disc border */}
      <circle
        cx={0}
        cy={0}
        r={discRadius}
        fill="none"
        stroke="#c7c6bf"
        strokeWidth="0.5"
      />

      <title>Disc</title>
    </motion.g>
  );
};
