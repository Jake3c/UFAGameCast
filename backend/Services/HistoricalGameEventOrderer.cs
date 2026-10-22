using UFAGameCast.Backend.Models;

namespace UFAGameCast.Backend.Services;

internal sealed record TimedGameEvent(
    GameEvent Event,
    string Source,
    int SourceIndex,
    bool WasPenaltyOpponent);

internal static class HistoricalGameEventOrderer
{
    public const int MaxTimestampSkewSeconds = 30;

    private static readonly HashSet<EventType> PullEvents =
    [
        EventType.PullInbounds,
        EventType.PullOutOfBounds
    ];

    public static GameEventOrderingPreviewResponse CreatePreview(
        string gameId,
        IReadOnlyList<TimedGameEvent> timestampOrderedEvents)
    {
        var events = timestampOrderedEvents.ToList();
        for (var index = 0; index < events.Count; index++)
        {
            events[index].Event.Id = index + 1;
        }

        var outgoingEdges = Enumerable.Range(0, events.Count)
            .Select(_ => new List<OrderingEdge>())
            .ToArray();
        var incomingEdgeCounts = new int[events.Count];
        var warnings = new List<GameEventOrderingWarning>();

        AddSourceOrderConstraints(events, outgoingEdges, incomingEdgeCounts);
        AddPointSequenceConstraints(events, outgoingEdges, incomingEdgeCounts, warnings);
        AddBlockSequenceConstraints(events, outgoingEdges, incomingEdgeCounts, warnings);

        var normalizedIndices = TopologicalSort(events, outgoingEdges, incomingEdgeCounts);
        var normalizedPositions = new int[events.Count];
        for (var position = 0; position < normalizedIndices.Count; position++)
        {
            normalizedPositions[normalizedIndices[position]] = position;
        }

        var reorders = FindReorders(events, outgoingEdges, normalizedPositions);
        var normalizedEvents = normalizedIndices.Select(index => events[index]).ToList();

        return new GameEventOrderingPreviewResponse(
            gameId,
            MaxTimestampSkewSeconds,
            CreateEntries(events),
            CreateEntries(normalizedEvents),
            reorders,
            warnings);
    }

    private static List<GameEventOrderingEntry> CreateEntries(
        IReadOnlyList<TimedGameEvent> events) =>
        events.Select((item, index) =>
            new GameEventOrderingEntry(index + 1, item.Source, item.SourceIndex, item.Event)).ToList();

    private static void AddSourceOrderConstraints(
        IReadOnlyList<TimedGameEvent> events,
        IReadOnlyList<List<OrderingEdge>> outgoingEdges,
        IList<int> incomingEdgeCounts)
    {
        foreach (var sourceEvents in Enumerable.Range(0, events.Count)
            .GroupBy(index => events[index].Source))
        {
            var orderedSourceEvents = sourceEvents
                .OrderBy(index => events[index].SourceIndex)
                .ToList();

            for (var index = 1; index < orderedSourceEvents.Count; index++)
            {
                AddEdge(
                    orderedSourceEvents[index - 1],
                    orderedSourceEvents[index],
                    "Preserve the statistician's source-feed order",
                    outgoingEdges,
                    incomingEdgeCounts);
            }
        }
    }

    private static void AddPointSequenceConstraints(
        IReadOnlyList<TimedGameEvent> events,
        IReadOnlyList<List<OrderingEdge>> outgoingEdges,
        IList<int> incomingEdgeCounts,
        ICollection<GameEventOrderingWarning> warnings)
    {
        var pointNumbers = AssignPointNumbers(events);

        for (var pullIndex = 0; pullIndex < events.Count; pullIndex++)
        {
            var pull = events[pullIndex];
            if (!PullEvents.Contains(pull.Event.Type) || pointNumbers[pullIndex] == 0)
            {
                continue;
            }

            for (var candidateIndex = 0; candidateIndex < events.Count; candidateIndex++)
            {
                if (candidateIndex == pullIndex ||
                    pointNumbers[candidateIndex] != pointNumbers[pullIndex])
                {
                    continue;
                }

                var candidate = events[candidateIndex];
                if (IsPointStart(candidate.Event.Type))
                {
                    TryApplyPrecedence(
                        candidateIndex,
                        pullIndex,
                        "Point starts precede the pull",
                        events,
                        outgoingEdges,
                        incomingEdgeCounts,
                        warnings);
                }
                else if (candidate.Source != pull.Source &&
                    !IsPointStructure(candidate.Event.Type))
                {
                    TryApplyPrecedence(
                        pullIndex,
                        candidateIndex,
                        "The pull precedes in-point actions",
                        events,
                        outgoingEdges,
                        incomingEdgeCounts,
                        warnings);
                }
            }
        }
    }

    private static void AddBlockSequenceConstraints(
        IReadOnlyList<TimedGameEvent> events,
        IReadOnlyList<List<OrderingEdge>> outgoingEdges,
        IList<int> incomingEdgeCounts,
        ICollection<GameEventOrderingWarning> warnings)
    {
        var blocks = Enumerable.Range(0, events.Count)
            .Where(index => events[index].Event.Type == EventType.Block)
            .ToList();
        var throwaways = Enumerable.Range(0, events.Count)
            .Where(index => events[index].Event.Type == EventType.ThrowawayByRecording)
            .ToList();
        var candidates = new List<BlockThrowawayMatch>();

        foreach (var blockIndex in blocks)
        {
            foreach (var throwawayIndex in throwaways)
            {
                var block = events[blockIndex];
                var throwaway = events[throwawayIndex];
                if (block.Event.IsHomeTeam == throwaway.Event.IsHomeTeam ||
                    GetTimestampSkewSeconds(block.Event.Timestamp, throwaway.Event.Timestamp) is not int skew ||
                    skew > MaxTimestampSkewSeconds)
                {
                    continue;
                }

                candidates.Add(new BlockThrowawayMatch(blockIndex, throwawayIndex, skew));
            }
        }

        var matchedBlocks = new HashSet<int>();
        var matchedThrowaways = new HashSet<int>();
        var matches = new Dictionary<int, BlockThrowawayMatch>();
        foreach (var candidate in candidates
            .OrderBy(candidate => candidate.TimestampSkewSeconds)
            .ThenBy(candidate => events[candidate.BlockIndex].Event.Timestamp)
            .ThenBy(candidate => events[candidate.BlockIndex].Source, StringComparer.Ordinal)
            .ThenBy(candidate => events[candidate.BlockIndex].SourceIndex)
            .ThenBy(candidate => events[candidate.ThrowawayIndex].Source, StringComparer.Ordinal)
            .ThenBy(candidate => events[candidate.ThrowawayIndex].SourceIndex))
        {
            if (matchedBlocks.Contains(candidate.BlockIndex) ||
                matchedThrowaways.Contains(candidate.ThrowawayIndex))
            {
                continue;
            }

            matchedBlocks.Add(candidate.BlockIndex);
            matchedThrowaways.Add(candidate.ThrowawayIndex);
            matches.Add(candidate.BlockIndex, candidate);
        }

        foreach (var blockIndex in blocks)
        {
            var block = events[blockIndex];
            if (matches.TryGetValue(blockIndex, out var match))
            {
                const string rule = "A block follows a throwaway by the opposing team";
                var throwawayIndex = match.ThrowawayIndex;
                var throwaway = events[throwawayIndex];
                if (HasPath(blockIndex, throwawayIndex, outgoingEdges))
                {
                    warnings.Add(CreateWarning(
                        throwaway,
                        block,
                        rule,
                        "Applying this rule would conflict with an existing ordering constraint.",
                        match.TimestampSkewSeconds));
                    continue;
                }

                AddEdge(throwawayIndex, blockIndex, rule, outgoingEdges, incomingEdgeCounts);
                continue;
            }

            var hasNearbyThrowaway = throwaways.Any(throwawayIndex =>
                GetTimestampSkewSeconds(
                    block.Event.Timestamp,
                    events[throwawayIndex].Event.Timestamp) is int skew &&
                skew <= MaxTimestampSkewSeconds);
            if (!hasNearbyThrowaway && IsNearPeriodEnd(block, events))
            {
                continue;
            }

            var reason = hasNearbyThrowaway
                ? "A nearby throwaway exists, but no available opposing-team throwaway could be paired with this block."
                : $"No opposing-team throwaway was found within {MaxTimestampSkewSeconds} seconds, and the block is not near a period end.";
            warnings.Add(CreateWarning(
                block,
                block,
                "A block follows a throwaway by the opposing team",
                reason,
                GetNearestThrowawaySkewSeconds(block, throwaways, events)));
        }
    }

    private static bool IsNearPeriodEnd(
        TimedGameEvent block,
        IReadOnlyList<TimedGameEvent> events)
    {
        foreach (var item in events)
        {
            var skew = GetTimestampSkewSeconds(block.Event.Timestamp, item.Event.Timestamp);
            if (IsEndOfPeriodEvent(item.Event.Type) &&
                skew.HasValue &&
                skew.Value <= MaxTimestampSkewSeconds)
            {
                return true;
            }
        }

        return false;
    }

    private static int? GetNearestThrowawaySkewSeconds(
        TimedGameEvent block,
        IReadOnlyList<int> throwawayIndices,
        IReadOnlyList<TimedGameEvent> events)
    {
        var skews = throwawayIndices
            .Select(index => GetTimestampSkewSeconds(
                block.Event.Timestamp,
                events[index].Event.Timestamp))
            .Where(skew => skew.HasValue)
            .Select(skew => skew!.Value)
            .ToArray();

        return skews.Length == 0 ? null : skews.Min();
    }

    private static int[] AssignPointNumbers(IReadOnlyList<TimedGameEvent> events)
    {
        var pointNumbers = new int[events.Count];
        foreach (var sourceEvents in Enumerable.Range(0, events.Count)
            .GroupBy(index => events[index].Source))
        {
            var pointNumber = 0;
            var previousWasStart = false;
            foreach (var index in sourceEvents.OrderBy(index => events[index].SourceIndex))
            {
                var isStart = IsPointStart(events[index].Event.Type);
                if (isStart && !previousWasStart)
                {
                    pointNumber++;
                }

                pointNumbers[index] = pointNumber;
                previousWasStart = isStart;
            }
        }

        return pointNumbers;
    }

    private static void TryApplyPrecedence(
        int beforeIndex,
        int afterIndex,
        string rule,
        IReadOnlyList<TimedGameEvent> events,
        IReadOnlyList<List<OrderingEdge>> outgoingEdges,
        IList<int> incomingEdgeCounts,
        ICollection<GameEventOrderingWarning> warnings)
    {
        var before = events[beforeIndex];
        var after = events[afterIndex];

        var skewSeconds = GetTimestampSkewSeconds(
            before.Event.Timestamp,
            after.Event.Timestamp);
        if (!skewSeconds.HasValue)
        {
            return;
        }

        if (skewSeconds.Value > MaxTimestampSkewSeconds)
        {
            if (beforeIndex > afterIndex)
            {
                warnings.Add(CreateWarning(
                    before,
                    after,
                    rule,
                    $"The timestamp difference exceeds the {MaxTimestampSkewSeconds}-second reorder limit.",
                    skewSeconds));
            }

            return;
        }

        if (beforeIndex < afterIndex)
        {
            return;
        }

        if (HasPath(afterIndex, beforeIndex, outgoingEdges))
        {
            warnings.Add(CreateWarning(
                before,
                after,
                rule,
                "Applying this rule would conflict with an existing ordering constraint.",
                skewSeconds));
            return;
        }

        AddEdge(beforeIndex, afterIndex, rule, outgoingEdges, incomingEdgeCounts);
    }

    private static GameEventOrderingWarning CreateWarning(
        TimedGameEvent before,
        TimedGameEvent after,
        string rule,
        string reason,
        int? skewSeconds) =>
        new(
            before.Event.Id,
            before.Event.Type,
            before.Source,
            after.Event.Id,
            after.Event.Type,
            after.Source,
            rule,
            reason,
            skewSeconds);

    private static void AddEdge(
        int from,
        int to,
        string rule,
        IReadOnlyList<List<OrderingEdge>> outgoingEdges,
        IList<int> incomingEdgeCounts)
    {
        if (outgoingEdges[from].Any(edge => edge.To == to))
        {
            return;
        }

        outgoingEdges[from].Add(new OrderingEdge(to, rule));
        incomingEdgeCounts[to]++;
    }

    private static List<int> TopologicalSort(
        IReadOnlyList<TimedGameEvent> events,
        IReadOnlyList<List<OrderingEdge>> outgoingEdges,
        IList<int> incomingEdgeCounts)
    {
        var available = new SortedSet<int>(
            Enumerable.Range(0, events.Count).Where(index => incomingEdgeCounts[index] == 0));
        var result = new List<int>(events.Count);
        var remainingIncomingEdgeCounts = incomingEdgeCounts.ToArray();

        while (available.Count > 0)
        {
            var next = available.Min;
            available.Remove(next);
            result.Add(next);

            foreach (var edge in outgoingEdges[next])
            {
                remainingIncomingEdgeCounts[edge.To]--;
                if (remainingIncomingEdgeCounts[edge.To] == 0)
                {
                    available.Add(edge.To);
                }
            }
        }

        if (result.Count != events.Count)
        {
            throw new InvalidOperationException("The historical event ordering constraints contain a cycle.");
        }

        return result;
    }

    private static IReadOnlyList<GameEventOrderingChange> FindReorders(
        IReadOnlyList<TimedGameEvent> events,
        IReadOnlyList<List<OrderingEdge>> outgoingEdges,
        IReadOnlyList<int> normalizedPositions)
    {
        var reorders = new List<GameEventOrderingChange>();
        for (var originalEarlier = 0; originalEarlier < events.Count; originalEarlier++)
        {
            for (var originalLater = originalEarlier + 1; originalLater < events.Count; originalLater++)
            {
                if (normalizedPositions[originalEarlier] < normalizedPositions[originalLater])
                {
                    continue;
                }

                var moved = events[originalLater];
                var overtaken = events[originalEarlier];
                reorders.Add(new GameEventOrderingChange(
                    moved.Event.Id,
                    moved.Event.Type,
                    moved.Source,
                    originalLater + 1,
                    normalizedPositions[originalLater] + 1,
                    overtaken.Event.Id,
                    overtaken.Event.Type,
                    overtaken.Source,
                    GetTimestampSkewSeconds(moved.Event.Timestamp, overtaken.Event.Timestamp),
                    FindPathRule(originalLater, originalEarlier, outgoingEdges)));
            }
        }

        return reorders;
    }

    private static int? GetTimestampSkewSeconds(long? firstTimestamp, long? secondTimestamp)
    {
        if (firstTimestamp is not long first || secondTimestamp is not long second)
        {
            return null;
        }

        var difference = Math.Abs((decimal)first - second);
        return difference > int.MaxValue ? int.MaxValue : (int)difference;
    }

    private static string FindPathRule(
        int from,
        int to,
        IReadOnlyList<List<OrderingEdge>> outgoingEdges)
    {
        var visited = new bool[outgoingEdges.Count];
        var queue = new Queue<(int Node, string FirstRule)>();
        queue.Enqueue((from, string.Empty));
        visited[from] = true;

        while (queue.Count > 0)
        {
            var (node, firstRule) = queue.Dequeue();
            foreach (var edge in outgoingEdges[node])
            {
                if (visited[edge.To])
                {
                    continue;
                }

                var pathRule = firstRule.Length == 0 ? edge.Rule : firstRule;
                if (edge.To == to)
                {
                    return pathRule;
                }

                visited[edge.To] = true;
                queue.Enqueue((edge.To, pathRule));
            }
        }

        return "Ordering constraint";
    }

    private static bool HasPath(
        int from,
        int to,
        IReadOnlyList<List<OrderingEdge>> outgoingEdges)
    {
        var visited = new bool[outgoingEdges.Count];
        var pending = new Stack<int>();
        pending.Push(from);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (current == to)
            {
                return true;
            }

            if (visited[current])
            {
                continue;
            }

            visited[current] = true;
            foreach (var edge in outgoingEdges[current])
            {
                pending.Push(edge.To);
            }
        }

        return false;
    }

    private static bool IsPointStart(EventType eventType) =>
        eventType is EventType.StartOPoint or EventType.StartDPoint;

    private static bool IsEndOfPeriodEvent(EventType eventType) =>
        eventType is EventType.EndQ1 or
            EventType.Halftime or
            EventType.EndQ3 or
            EventType.EndRegulation or
            EventType.EndOT1 or
            EventType.EndOT2;

    private static bool IsPointStructure(EventType eventType) =>
        IsPointStart(eventType) ||
        PullEvents.Contains(eventType) ||
        eventType is EventType.EndQ1 or
            EventType.Halftime or
            EventType.EndQ3 or
            EventType.EndRegulation or
            EventType.EndOT1 or
            EventType.EndOT2;

    private sealed record OrderingEdge(int To, string Rule);

    private sealed record BlockThrowawayMatch(
        int BlockIndex,
        int ThrowawayIndex,
        int TimestampSkewSeconds);
}
