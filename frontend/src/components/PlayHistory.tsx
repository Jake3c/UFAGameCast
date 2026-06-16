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
    <div className="play-history-container">
      <h2 className="play-history-title">Recent Plays</h2>
      <div className="play-list">
        <AnimatePresence mode="popLayout">
          {visiblePlays.map((play) => (
            <motion.div
              key={play.id}
              className="play-item"
              initial={{ opacity: 0, x: -20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: 20 }}
              transition={{ type: 'spring', stiffness: 300, damping: 30 }}
            >
              <div className="play-time">
                {new Date(play.timestamp).toLocaleTimeString([], {
                  hour: '2-digit',
                  minute: '2-digit',
                  second: '2-digit',
                })}
              </div>
              <div className="play-description">{play.description}</div>
              <div className={`play-event-type ${play.eventType?.toLowerCase() ?? 'other'}`}>
                {play.eventType || 'Other'}
              </div>
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
