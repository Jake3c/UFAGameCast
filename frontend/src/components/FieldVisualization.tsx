import React from 'react';
import { motion } from 'framer-motion';
import { getTeamLogo } from '../utils/teamLogos';

export const FIELD_WIDTH = 120;
export const FIELD_HEIGHT = 53;

export function mapGamePositionToField(position: { x: number; y: number }) {
  return {
    x: Math.max(0, Math.min(FIELD_WIDTH, position.y)),
    y: Math.max(0, Math.min(FIELD_HEIGHT, position.x + FIELD_HEIGHT / 2)),
  };
}

export function mapGamePositionToFieldForTeam(
  position: { x: number; y: number },
  isHomeTeam: boolean,
) {
  const fieldPosition = mapGamePositionToField(position);
  return {
    ...fieldPosition,
    x: isHomeTeam ? FIELD_WIDTH - fieldPosition.x : fieldPosition.x,
  };
}

interface FieldVisualizationProps {
  homeTeam: string;
  discPosition?: { x: number; y: number };
  passAnimation?: PassAnimation;
  onPassAnimationComplete?: (animationId: number) => void;
  width?: number;
  height?: number;
}

export interface PassAnimation {
  id: number;
  from: { x: number; y: number };
  to: { x: number; y: number };
  isAnimating: boolean;
}

/**
 * Renders a bird's-eye view of the field with animated player positions and disc
 */
export const FieldVisualization: React.FC<FieldVisualizationProps> = ({
  discPosition,
  passAnimation,
  onPassAnimationComplete,
  homeTeam,
}) => {
  const endzoneDepth = 20;

  var homeTeamLogo = getTeamLogo(homeTeam);

  return (
    <div className="field-visualization-container">
      <svg
        viewBox={`0 0 ${FIELD_WIDTH} ${FIELD_HEIGHT}`}
        className="field-svg"
        style={{ width: `100%`, height: `100%` }}
      >
        {/* Field background */}
        <rect width={FIELD_WIDTH} height={FIELD_HEIGHT} fill="#2d5016" />

        {/* Sidelines */}
        <line x1="0" y1="0" x2={FIELD_WIDTH} y2="0" stroke="white" strokeWidth="0.5" />
        <line x1="0" y1={FIELD_HEIGHT} x2={FIELD_WIDTH} y2={FIELD_HEIGHT} stroke="white" strokeWidth="0.5" />

        {/* Endlines */}
        <line x1="0" y1="0" x2="0" y2={FIELD_HEIGHT} stroke="white" strokeWidth="0.5" />
        <line x1={FIELD_WIDTH} y1="0" x2={FIELD_WIDTH} y2={FIELD_HEIGHT} stroke="white" strokeWidth="0.5" />

        {/* Midfield line */}
        <line x1={FIELD_WIDTH / 2} y1="0" x2={FIELD_WIDTH / 2} y2={FIELD_HEIGHT} stroke="white" strokeWidth="0.5" strokeDasharray="2" />

        {/* Goal areas (end zones) */}
        <rect x="0" y="0" width={endzoneDepth} height={FIELD_HEIGHT} fill="rgba(255, 255, 255, 0.05)" />
        <rect x={FIELD_WIDTH - endzoneDepth} y="0" width={endzoneDepth} height={FIELD_HEIGHT} fill="rgba(255, 255, 255, 0.05)" />

        {/* Home field logo */}
        <image
            href={homeTeamLogo}
            x={FIELD_WIDTH / 2 - 20}
            y={FIELD_HEIGHT / 2 - 20}
            width="40"
            height="40"
            opacity="0.40"
            preserveAspectRatio="xMidYMid meet"
        />

        {/* Disc */}
        {passAnimation?.isAnimating ? (
          <PassDiscAnimation
            key={passAnimation.id}
            animation={passAnimation}
            onComplete={onPassAnimationComplete}
          />
        ) : passAnimation ? (
          <DiscMarker position={passAnimation.to} animateOnMount={false} />
        ) : (
          discPosition && <DiscMarker position={discPosition} />
        )}
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
const DiscMarker: React.FC<DiscMarkerProps & { animateOnMount?: boolean }> = ({
  position,
  animateOnMount = true,
}) => {
  if (!animateOnMount) {
    return (
      <g transform={`translate(${position.x} ${position.y})`}>
        <DiscShape />
      </g>
    );
  }

  return (
    <motion.g
      initial={{ x: 0, y: 0 }}
      animate={{ x: position.x, y: position.y }}
      transition={{ type: 'spring', damping: 20, stiffness: 100 }}
    >
      <DiscShape />
    </motion.g>
  );
};

function PassDiscAnimation({
  animation,
  onComplete,
}: {
  animation: PassAnimation;
  onComplete?: (animationId: number) => void;
}) {
  return (
    <motion.g
      initial={{ x: animation.from.x, y: animation.from.y }}
      animate={{ x: animation.to.x, y: animation.to.y }}
      transition={{ duration: 1, ease: 'linear' }}
      onAnimationComplete={() => onComplete?.(animation.id)}
    >
      <DiscShape />
    </motion.g>
  );
}

function DiscShape() {
  const discRadius = 1.6;

  return (
    <>
      <circle
        cx={0}
        cy={0}
        r={discRadius}
        fill="#ffffff"
        opacity="0.95"
        filter="drop-shadow(0 0 0.5px rgba(0, 0, 0, 0.3))"
      />
      <circle
        cx={0}
        cy={0}
        r={discRadius}
        fill="none"
        stroke="#c7c6bf"
        strokeWidth="0.5"
      />
      <title>Disc</title>
    </>
  );
}
