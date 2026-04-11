using System.Collections.Immutable;

namespace Adj.Manifest;

/// <summary>
/// In-memory Level 3 journal store. Implements the full query contract
/// and calibration scoring. Suitable for testing and reference use.
///
/// This is the composition seam: it implements IJournalStore (ADJ §7)
/// and can serve as ADP's CalibrationSource by calling GetCalibration().
/// </summary>
public sealed class InMemoryJournalStore : IJournalStore
{
    private readonly List<JournalEntry> _entries = [];
    private readonly object _lock = new();

    /// <summary>
    /// Appends an entry to the journal. Append-only — entries cannot be
    /// removed or modified after writing. Spec §8.1.
    /// </summary>
    public void Append(JournalEntry entry)
    {
        lock (_lock)
        {
            _entries.Add(entry);
        }
    }

    /// <summary>
    /// Appends multiple entries in order.
    /// </summary>
    public void AppendRange(IEnumerable<JournalEntry> entries)
    {
        lock (_lock)
        {
            _entries.AddRange(entries);
        }
    }

    /// <inheritdoc />
    public CalibrationScore GetCalibration(string agentId, string domain)
    {
        var pairs = GetScoringPairs(agentId, domain);
        if (pairs.Count == 0)
            return BrierScorer.GetDefault();

        return BrierScorer.Compute(pairs, DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public ImmutableList<JournalEntry> GetDeliberation(string deliberationId)
    {
        lock (_lock)
        {
            return _entries
                .Where(e => e.DeliberationId == deliberationId)
                .OrderBy(e => e.Timestamp)
                .ToImmutableList();
        }
    }

    /// <inheritdoc />
    public ConditionQualityMetrics GetConditionTrace(string agentId, TimeSpan window)
    {
        var cutoff = DateTimeOffset.UtcNow - window;

        lock (_lock)
        {
            var conditions = _entries
                .OfType<ProposalEmitted>()
                .Where(p => p.Proposal.AgentId == agentId && p.Timestamp >= cutoff)
                .SelectMany(p => p.Proposal.DissentConditions)
                .ToList();

            return ConditionQualityScorer.Compute(conditions);
        }
    }

    /// <inheritdoc />
    public OutcomeObserved? GetOutcome(string deliberationId)
    {
        lock (_lock)
        {
            // Most recent outcome (accounts for supersedes)
            return _entries
                .OfType<OutcomeObserved>()
                .Where(o => o.DeliberationId == deliberationId)
                .OrderByDescending(o => o.Timestamp)
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// Builds (confidence, outcome) pairs for Brier scoring.
    /// Joins proposals with their deliberation outcomes.
    /// </summary>
    private List<ScoringPair> GetScoringPairs(string agentId, string domain)
    {
        lock (_lock)
        {
            var proposals = _entries
                .OfType<ProposalEmitted>()
                .Where(p => p.Proposal.AgentId == agentId
                         && p.Proposal.Domain == domain
                         && p.Proposal.CalibrationAtStake)
                .ToList();

            var outcomes = _entries
                .OfType<OutcomeObserved>()
                .GroupBy(o => o.DeliberationId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(o => o.Timestamp).First());

            var pairs = new List<ScoringPair>();

            foreach (var proposal in proposals)
            {
                if (outcomes.TryGetValue(proposal.DeliberationId, out var outcome))
                {
                    pairs.Add(new ScoringPair(
                        Confidence: proposal.Proposal.Confidence,
                        Outcome: outcome.OutcomeValue,
                        Timestamp: outcome.ObservedAt
                    ));
                }
            }

            return pairs;
        }
    }

    /// <summary>
    /// Returns all entries in the store (for testing/audit).
    /// </summary>
    public ImmutableList<JournalEntry> GetAllEntries()
    {
        lock (_lock)
        {
            return _entries.ToImmutableList();
        }
    }
}
