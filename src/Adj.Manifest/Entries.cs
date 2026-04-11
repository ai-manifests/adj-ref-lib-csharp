using System.Collections.Immutable;

namespace Adj.Manifest;

/// <summary>
/// Common envelope shared by all journal entries. Spec §3.0.
/// </summary>
public abstract record JournalEntry(
    string EntryId,
    EntryType EntryType,
    string DeliberationId,
    DateTimeOffset Timestamp,
    string? PriorEntryHash
);

/// <summary>
/// Spec §3.1 — written when a deliberation begins.
/// </summary>
public sealed record DeliberationOpened(
    string EntryId,
    string DeliberationId,
    DateTimeOffset Timestamp,
    string? PriorEntryHash,
    string DecisionClass,
    ActionDescriptor Action,
    ImmutableList<string> Participants,
    DeliberationConfig? Config
) : JournalEntry(EntryId, EntryType.DeliberationOpened, DeliberationId, Timestamp, PriorEntryHash);

/// <summary>
/// Spec §3.2 — written when an agent submits a proposal.
/// Stores the full ADP proposal as an opaque object.
/// </summary>
public sealed record ProposalEmitted(
    string EntryId,
    string DeliberationId,
    DateTimeOffset Timestamp,
    string? PriorEntryHash,
    ProposalData Proposal
) : JournalEntry(EntryId, EntryType.ProposalEmitted, DeliberationId, Timestamp, PriorEntryHash);

/// <summary>
/// Spec §3.3 — one per state-machine transition during belief-update.
/// </summary>
public sealed record RoundEvent(
    string EntryId,
    string DeliberationId,
    DateTimeOffset Timestamp,
    string? PriorEntryHash,
    int Round,
    EventKind EventKind,
    string AgentId,
    string? TargetAgentId,
    string? TargetConditionId,
    ImmutableDictionary<string, object>? Payload
) : JournalEntry(EntryId, EntryType.RoundEvent, DeliberationId, Timestamp, PriorEntryHash);

/// <summary>
/// Spec §3.4 — written when a deliberation reaches a terminal state.
/// </summary>
public sealed record DeliberationClosed(
    string EntryId,
    string DeliberationId,
    DateTimeOffset Timestamp,
    string? PriorEntryHash,
    TerminationState Termination,
    int RoundCount,
    string Tier,
    TallyRecord FinalTally,
    ImmutableDictionary<string, double> Weights,
    ActionDescriptor? CommittedAction
) : JournalEntry(EntryId, EntryType.DeliberationClosed, DeliberationId, Timestamp, PriorEntryHash);

/// <summary>
/// Spec §3.5 — written when a measurable result is observed.
/// </summary>
public sealed record OutcomeObserved(
    string EntryId,
    string DeliberationId,
    DateTimeOffset Timestamp,
    string? PriorEntryHash,
    DateTimeOffset ObservedAt,
    OutcomeClass OutcomeClass,
    double Success,
    ImmutableList<string> EvidenceRefs,
    string ReporterId,
    double ReporterConfidence,
    bool GroundTruth,
    string? Supersedes
) : JournalEntry(EntryId, EntryType.OutcomeObserved, DeliberationId, Timestamp, PriorEntryHash)
{
    /// <summary>
    /// Returns the outcome as a value in [0, 1] for calibration scoring.
    /// Binary true = 1.0, false = 0.0. Graded uses Success directly.
    /// </summary>
    public double OutcomeValue => Success;
}

/// <summary>
/// Describes the action being deliberated.
/// </summary>
public sealed record ActionDescriptor(
    string Kind,
    string Target,
    ImmutableDictionary<string, string>? Parameters
);

/// <summary>
/// Deliberation configuration.
/// </summary>
public sealed record DeliberationConfig(
    int MaxRounds = 3,
    double ParticipationFloor = 0.50
);

/// <summary>
/// Snapshot of tally results stored in deliberation_closed.
/// </summary>
public sealed record TallyRecord(
    double ApproveWeight,
    double RejectWeight,
    double AbstainWeight,
    double TotalWeight,
    double ApprovalFraction,
    double ParticipationFraction,
    double Threshold
);

/// <summary>
/// Minimal proposal data needed for calibration scoring.
/// The journal stores the full ADP proposal; this extracts the fields ADJ needs.
/// </summary>
public sealed record ProposalData(
    string ProposalId,
    string AgentId,
    string Vote,
    double Confidence,
    string Domain,
    bool CalibrationAtStake,
    ImmutableList<ConditionRecord> DissentConditions
);

/// <summary>
/// Minimal dissent condition record for condition quality scoring.
/// </summary>
public sealed record ConditionRecord(
    string Id,
    string Condition,
    string Status,
    int AmendmentCount,
    int? TestedInRound
);
