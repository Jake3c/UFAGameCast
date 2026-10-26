import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { GameEvent, PlayerNames } from '../types/api';
import { getTeamLogo } from '../utils/teamLogos';
import UfaLogo from '../resources/UFAIcon.png';

interface PlayHistoryProps {
  plays: GameEvent[];
  homeTeam: string;
  awayTeam: string;
  playerNames: PlayerNames;
  emptyMessage?: string;
}

/**
 * Renders the supplied play history in chronological order.
 */
export const PlayHistory: React.FC<PlayHistoryProps> = ({
  plays,
  homeTeam,
  awayTeam,
  playerNames,
  emptyMessage = 'Waiting for plays...',
}) => {
  return (
    <div className="w-full h-full overflow-hidden">
      <div className="w-full flex flex-col">
        <AnimatePresence initial={false} mode="popLayout">
          {plays.map((play) => (
            <motion.div
              key={play.id}
              layout
              initial={{ opacity: 0, x: -10 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: 10 }}
              transition={{ type: 'spring', stiffness: 300, damping: 30 }}
              className="w-full"
            >
              <PlayHistoryItem
                play={play}
                homeTeam={homeTeam}
                awayTeam={awayTeam}
                playerNames={playerNames}
              />
            </motion.div>
          ))}
        </AnimatePresence>

        {plays.length === 0 && (
          <div className="p-4 text-sm text-gray-400 text-center">
            {emptyMessage}
          </div>
        )}
      </div>
    </div>
  );
};

// =========================================================
// ITEM
// =========================================================

function PlayHistoryItem({
  play,
  homeTeam,
  awayTeam,
  playerNames,
}: {
  play: GameEvent;
  homeTeam: string;
  awayTeam: string;
  playerNames: PlayerNames;
}) {
  const isGoal = play.type === 'goal';
  const isStartingLine = play.type === 'startDPoint' || play.type === 'startOPoint';
  const isEndOfPeriod = isEndOfPeriodEvent(play.type);
  const teamName = play.isHomeTeam ? homeTeam : awayTeam;
  const teamLogo = isEndOfPeriod ? UfaLogo : getTeamLogo(teamName);
  const teamLogoAlt = isEndOfPeriod ? 'UFA logo' : `${teamName} logo`;

  return (
    <article className={`play-event-row play-event-row--${getEventTone(play.type)}${isGoal ? ' play-event-row--goal' : ''}`}>
      <span className="play-event-type-group">
        {teamLogo && <img className="play-event-team-logo" src={teamLogo} alt={teamLogoAlt} />}
        <span className="play-event-type">{isGoal ? 'GOAL' : play.type}</span>
      </span>
      <span className="play-event-details">
        {isGoal ? (
          <>
            <strong>{formatPlayerName(play.thrower, playerNames)}</strong>
            {' to '}
            <strong>{formatPlayerName(play.receiver, playerNames)}</strong>
          </>
        ) : play.type === 'pass' ? (
          `${formatPlayerName(play.thrower, playerNames)} to ${formatPlayerName(play.receiver, playerNames)}`
        ) : play.type === 'throwawayByRecording' ? (
          formatPlayerName(play.thrower, playerNames)
        ) : play.type === 'callahanThrownByOpponent' ? (
          formatPlayerName(play.defender, playerNames)
        ) : play.type === 'callahanThrownByRecording' ? (
          formatPlayerName(play.thrower, playerNames)
        ) : play.type === 'block' ? (
          formatPlayerName(play.defender, playerNames)
        ) : play.type === 'drop' ? (
          formatPlayerName(play.receiver, playerNames)
        ) : play.type === 'pullInbounds' ? (
          formatPlayerName(play.puller, playerNames)
        ) : isStartingLine ? (
          (play.line ?? []).map((playerId) => formatPlayerName(playerId, playerNames)).join(', ')
        ) : null}
      </span>
    </article>
  );
}

function formatPlayerName(playerId: string | null, playerNames: PlayerNames) {
  if (!playerId) return '';
  return playerNames[playerId.toLowerCase()] ?? playerId;
}

function isEndOfPeriodEvent(eventType: string) {
  return [
    'endQ1',
    'halftime',
    'endQ3',
    'endRegulation',
    'endOT1',
    'endOT2',
  ].includes(eventType);
}

function getEventTone(eventType: string) {
  if (eventType === 'callahanThrownByRecording') return 'callahan-recording';
  if (eventType === 'callahanThrownByOpponent') return 'callahan-opponent';
  if (eventType === 'goal') return 'score';
  if (eventType.toLowerCase().includes('penalty')) return 'penalty';
  if (eventType.toLowerCase().includes('throwaway')) return 'throwaway';
  if (eventType === 'block') return 'block';
  return 'default';
}