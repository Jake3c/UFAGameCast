import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { PlayEvent } from '../types/api';

interface PlayHistoryProps {
  plays: PlayEvent[];
  maxVisible?: number;
}

/**
 * Renders a scrollable list of recent plays with smooth animations
 * Newest plays appear at the top
 */
export const PlayHistory: React.FC<PlayHistoryProps> = ({
  plays,
  maxVisible = 10,
}) => {
  // Ensure newest-first ordering
  const visiblePlays = [...plays]
    .slice(-maxVisible)
    .reverse();

  return (
    <div className="w-full h-full overflow-hidden">
      <div className="w-full flex flex-col">
        <AnimatePresence initial={false} mode="popLayout">
          {visiblePlays.map((play) => (
            <motion.div
              key={play.id}
              layout
              initial={{ opacity: 0, x: -10 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: 10 }}
              transition={{ type: 'spring', stiffness: 300, damping: 30 }}
              className="w-full"
            >
              <PlayHistoryItem play={play} />
            </motion.div>
          ))}
        </AnimatePresence>

        {visiblePlays.length === 0 && (
          <div className="p-4 text-sm text-gray-400 text-center">
            Waiting for plays...
          </div>
        )}
      </div>
    </div>
  );
};

// =========================================================
// ITEM
// =========================================================

function PlayHistoryItem({ play }: { play: PlayEvent }) {

  const barColor = getEventColor(play.eventType);

  if(isEndOfPeriodEvent(play.eventType)) {
    return (
      <div className="flex w-full items-stretch pl-4 txt-lg pb-1 font-semibold bg-slate-800 text-white my-2">
        {play.description}
      </div>
    );
  }

  return (
    <div className={`flex items-stretch min-h-[32px] rounded-md py-0.5 bg-gray-200 mx-1 my-0.5 border-l-4 ${barColor}`}>


      {/* Description */}
      <div className="flex-1 px-3 py-2 text-xs min-w-52">
        {play.description}
      </div>
      
      {/* Code */}
      <div className="flex-1 px-3 py-2 text-xs">
        {play.eventType}
      </div>

      {/* Code */}
      <div className="flex-1 px-3 py-2 text-xs">
        {play.initiatorName}
      </div>

      {/* Time */}
      <div className="px-3 py-2 text-xs text-right text-gray-400 tabular-nums whitespace-nowrap">
        {play.time}
      </div>
    </div>
  );
}

function isEndOfPeriodEvent(eventType: string){
    return eventType === "endQ1" || eventType === "endQ3" || eventType === "endRegulation" || eventType === "endOT1" || eventType === "endOT2" || eventType === "halftime";
}

function getEventColor(eventType: string){

  // The commented out rows represent the full list of possible eventTypes, but we should not be receiving any eventTypes
  // that would result in "black"
  const eventColors: Record<string, string> = {
    //startDPoint : 'bg-black',
    //startOPoint : 'bg-black',
    midpointTimeoutRecording : 'border-l-yellow-500',
    betweenPointTimeoutRecording : 'border-l-yellow-500',
    midpointTimeoutOpponent : 'border-l-yellow-500',
    //betweenPointTimeoutOpponent : 'bg-black',
    pullInbounds : 'border-l-orange-500',
    pullOutOfBounds : 'border-l-orange-300',
    offsidesRecording : 'border-l-pink-500',
    offsidesOpponent : 'border-l-pink-500',
    block : 'border-l-blue-500',
    callahanThrownByOpponent : 'border-l-gold',
    //throwawayByOpponent : 'bg-black',
    //stallAgainstOpponent : 'bg-black',
    //scoreByOpponent : 'bg-black',
    penaltyRecording : 'border-l-pink-500',
    //penaltyOpponent : 'bg-black',
    pass : 'border-l-gray-500',
    goal : 'border-l-green-500',
    drop : 'border-l-red-500',
    droppedPull : 'border-l-red-500',
    throwawayByRecording : 'border-l-red-500',
    //callahanThrownByRecording : 'bg-black',
    stallAgainstRecording : 'border-l-pink-500',
    injury : 'border-l-yellow-500',
    playerMisconductFoul : 'border-l-red-500',
    playerEjected : 'border-l-red-800',
    endQ1 : 'border-l-teal-500',
    halftime : 'border-l-teal-500',
    endQ3 : 'border-l-teal-500',
    endRegulation : 'border-l-teal-500',
    endOT1 : 'border-l-teal-500',
    endOT2 : 'border-l-teal-500',
    delayed : 'border-l-red-500'
  };

  return eventColors[eventType] ?? 'bg-black';
}