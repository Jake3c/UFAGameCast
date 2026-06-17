import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { PlayEvent } from '../types/api';

interface PlayHistoryProps {
  plays: PlayEvent[];
  maxVisible?: number;
}

/**
 * Renders a scrollable list of recent plays with smooth animations
 */
export const PlayHistory: React.FC<PlayHistoryProps> = ({ plays, maxVisible = 10 }) => {
  const visiblePlays = plays.slice(0, maxVisible);

  return (
    <div className="">
      <div className="">
        <AnimatePresence mode="popLayout">
          {visiblePlays.map((play) => (
            <motion.div
              key={play.id}
              initial={{ opacity: 0, x: -20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: 20 }}
              transition={{ type: 'spring', stiffness: 300, damping: 30 }}
            >
              <PlayHistoryItem play={play} />
            </motion.div>
          ))}
        </AnimatePresence>

        {visiblePlays.length === 0 && (
          <div className="play-empty">
            <p>Waiting for plays...</p>
          </div>
        )}
      </div>
    </div>
  );
};

function PlayHistoryItem({ play }: { play: PlayEvent }) {
  console.log(play.eventType);
  const eventColors: Record<string, string> = {
    startOPoint: "bg-green-500",
    startDPoint: "bg-red-500"
  };

  const barColor =
    eventColors[play.eventType ?? "Other"] ?? "bg-gray-400";

  return (
    <div className="flex w-full items-center border-b border-gray-200 bg-white min-h-8">
      {/* Color bar */}
      <div className={`w-2 self-stretch ${barColor}`} />

      {/* Description */}
      <div className="flex-1 px-3 text-xs">
        {play.description}
      </div>

      {/* Time */}
      <div className="px-3 text-right text-sm text-gray-400 tabular-nums">
        {play.time}
      </div>
    </div>
  );
}