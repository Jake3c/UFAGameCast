import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { GameEvent, PlayerNames } from '../types/api';
import { getTeamLogo } from '../utils/teamLogos';
import UfaLogo from '../resources/UFAIcon.png';

const eventDisplayNames: Record<string, string> = {
  startDPoint: 'Start D Point',
  startOPoint: 'Start O Point',
  midpointTimeoutRecording: 'Timeout',
  betweenPointTimeoutRecording: 'Timeout',
  midpointTimeoutOpponent: 'Timeout',
  betweenPointTimeoutOpponent: 'Timeout',
  pullInbounds: 'Pull',
  pullOutOfBounds: 'Pull (OB)',
  offsidesRecording: 'Offsides',
  offsidesOpponent: 'Offsides',
  block: 'Block',
  callahanThrownByOpponent: 'CALLAHAN',
  throwawayByOpponent: 'Throwaway',
  stallAgainstOpponent: 'Stall',
  scoreByOpponent: 'Score',
  penaltyRecording: 'Penalty',
  penaltyOpponent: 'Penalty',
  pass: 'Pass',
  goal: 'GOAL',
  drop: 'Drop',
  droppedPull: 'Dropped Pull',
  throwawayByRecording: 'Throwaway',
  callahanThrownByRecording: 'Callahan',
  stallAgainstRecording: 'Stall',
  injury: 'Injury',
  playerMisconductFoul: 'Misconduct Foul',
  playerEjected: 'Player Ejected',
  endQ1: 'End of First Quarter',
  halftime: 'Halftime',
  endQ3: 'End of Third Quarter',
  endRegulation: 'End of Regulation',
  endOT1: 'End of First Overtime',
  endOT2: 'End of Second Overtime',
  delayed: 'Delayed',
  postponed: 'Postponed',
};

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
  const displayPlays = combineStartingPointEvents(plays);

  return (
    <div className="w-full h-full overflow-hidden">
      <div className="w-full flex flex-col">
        <AnimatePresence initial={false} mode="popLayout">
          {displayPlays.map(({ play, pairedStart, pairedCallahan }) => (
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
                pairedStart={pairedStart}
                pairedCallahan={pairedCallahan}
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

function combineStartingPointEvents(plays: GameEvent[]) {
  const displayPlays: { play: GameEvent; pairedStart?: GameEvent; pairedCallahan?: GameEvent }[] = [];

  for (let index = 0; index < plays.length; index++) {
    const play = plays[index];
    const nextPlay = plays[index + 1];

    if (
      nextPlay !== undefined &&
      ((play.type === 'callahanThrownByOpponent' && nextPlay.type === 'callahanThrownByRecording') ||
        (play.type === 'callahanThrownByRecording' && nextPlay.type === 'callahanThrownByOpponent'))
    ) {
      const callahanEvent = play.type === 'callahanThrownByOpponent' ? play : nextPlay;
      const throwEvent = play.type === 'callahanThrownByRecording' ? play : nextPlay;
      displayPlays.push({ play: callahanEvent, pairedCallahan: throwEvent });
      index++;
      continue;
    }

    const isMatchingStartPair =
      nextPlay !== undefined &&
      ((play.type === 'startDPoint' && nextPlay.type === 'startOPoint') ||
        (play.type === 'startOPoint' && nextPlay.type === 'startDPoint'));

    displayPlays.push({
      play,
      pairedStart: isMatchingStartPair ? nextPlay : undefined,
    });

    if (isMatchingStartPair) {
      index++;
    }
  }

  return displayPlays;
}

// =========================================================
// ITEM
// =========================================================

function PlayHistoryItem({
  play,
  pairedStart,
  pairedCallahan,
  homeTeam,
  awayTeam,
  playerNames,
}: {
  play: GameEvent;
  pairedStart?: GameEvent;
  pairedCallahan?: GameEvent;
  homeTeam: string;
  awayTeam: string;
  playerNames: PlayerNames;
}) {
  const [isStartingPointExpanded, setIsStartingPointExpanded] = useState(false);
  const startDPoint = play.type === 'startDPoint' ? play : pairedStart?.type === 'startDPoint' ? pairedStart : undefined;
  const startOPoint = play.type === 'startOPoint' ? play : pairedStart?.type === 'startOPoint' ? pairedStart : undefined;
  const isStartingPointPair = startDPoint !== undefined && startOPoint !== undefined;
  const isGoal = play.type === 'goal';
  const isStartingLine = play.type === 'startDPoint' || play.type === 'startOPoint';
  const isEndOfPeriod = isEndOfPeriodEvent(play.type);
  const isGameStatus = isGameStatusEvent(play.type);
  const teamName = play.isHomeTeam ? homeTeam : awayTeam;
  const teamLogo = isEndOfPeriod ? UfaLogo : getTeamLogo(teamName);
  const teamLogoAlt = isEndOfPeriod ? 'UFA logo' : `${teamName} logo`;
  const startDTeamName = startDPoint?.isHomeTeam ? homeTeam : awayTeam;
  const startOTeamName = startOPoint?.isHomeTeam ? homeTeam : awayTeam;

  return (
    <article className={`play-event-row play-event-row--${getEventTone(play.type)}${isGoal ? ' play-event-row--goal' : ''}${isGameStatus ? ' play-event-row--status' : ''}`}>
      {isStartingPointPair ? (
        <>
          <button
            type="button"
            className="play-event-starting-point-toggle"
            aria-expanded={isStartingPointExpanded}
            aria-controls={`starting-lineups-${play.id}`}
            onClick={() => setIsStartingPointExpanded((expanded) => !expanded)}
          >
            <span className="play-event-type-group">
              <img className="play-event-team-logo" src={getTeamLogo(startDTeamName)} alt="" />
              <span className="play-event-type">{`${toTitleCase(startDTeamName)} pulling to ${toTitleCase(startOTeamName)}`}</span>
            </span>
            <span className="play-event-starting-point-action">
              {isStartingPointExpanded ? 'Hide players' : 'Show players'}
              <span aria-hidden="true">{isStartingPointExpanded ? '−' : '+'}</span>
            </span>
          </button>
          <AnimatePresence initial={false}>
            {isStartingPointExpanded && (
              <motion.div
                id={`starting-lineups-${play.id}`}
                className="play-event-starting-point-lineups"
                initial={{ height: 0, opacity: 0 }}
                animate={{ height: 'auto', opacity: 1 }}
                exit={{ height: 0, opacity: 0 }}
                transition={{ duration: 0.2 }}
              >
                <span>
                  <strong>Defense:</strong>{' '}
                  {(startDPoint.line ?? []).map((playerId) => formatPlayerName(playerId, playerNames)).join(', ')}
                </span>
                <span>
                  <strong>Offense:</strong>{' '}
                  {(startOPoint.line ?? []).map((playerId) => formatPlayerName(playerId, playerNames)).join(', ')}
                </span>
              </motion.div>
            )}
          </AnimatePresence>
        </>
      ) : (
        <>
          {isGameStatus ? (
            <>
              <span className="play-event-status-rule" aria-hidden="true" />
              <span className="play-event-status-label">{eventDisplayNames[play.type] ?? play.type}</span>
              <span className="play-event-status-rule" aria-hidden="true" />
            </>
          ) : (
            <>
              <span className="play-event-type-group">
                {teamLogo && <img className="play-event-team-logo" src={teamLogo} alt={teamLogoAlt} />}
                <span className={`play-event-type${play.type === 'callahanThrownByOpponent' ? ' play-event-type--callahan' : ''}`}>{eventDisplayNames[play.type] ?? play.type}</span>
              </span>
              <span className="play-event-details">
                {pairedCallahan !== undefined ? (
                  <>
                    <strong>{formatPlayerName(play.defender, playerNames)}</strong>
                    {' intercepts '}
                    <strong>{formatPlayerName(pairedCallahan.thrower, playerNames)}</strong>
                  </>
                ) : isGoal ? (
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
                  <strong>{formatPlayerName(play.defender, playerNames)}</strong>
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
            </>
          )}
        </>
      )}
    </article>
  );
}

function formatPlayerName(playerId: string | null, playerNames: PlayerNames) {
  if (!playerId) return '';
  return playerNames[playerId.toLowerCase()] ?? playerId;
}

function toTitleCase(value: string) {
  return value.replace(/\b\w/g, (character) => character.toUpperCase());
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

function isGameStatusEvent(eventType: string) {
  return isEndOfPeriodEvent(eventType) || eventType === 'delayed' || eventType === 'postponed';
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