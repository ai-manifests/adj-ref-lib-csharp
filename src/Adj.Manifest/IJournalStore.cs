using System.Collections.Immutable;

namespace Adj.Manifest;

/// <summary>
/// ADJ §7 query contract. The minimum query surface any conformant
/// journal must support at Level 2 compliance.
/// </summary>
public interface IJournalStore
{
    /// <summary>
    /// Returns the calibration score for an (agent, domain) pair.
    /// This is the query that implements ADP's CalibrationSource interface.
    /// Spec §7.1.
    /// </summary>
    CalibrationScore GetCalibration(string agentId, string domain);

    /// <summary>
    /// Returns all entries for a deliberation, ordered by timestamp.
    /// Spec §7.1.
    /// </summary>
    ImmutableList<JournalEntry> GetDeliberation(string deliberationId);

    /// <summary>
    /// Returns condition quality metrics for an agent over a time window.
    /// Spec §7.1.
    /// </summary>
    ConditionQualityMetrics GetConditionTrace(string agentId, TimeSpan window);

    /// <summary>
    /// Returns the outcome for a deliberation, or null if none recorded.
    /// Spec §7.1.
    /// </summary>
    OutcomeObserved? GetOutcome(string deliberationId);
}
