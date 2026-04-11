namespace Adj.Manifest;

public enum EntryType
{
    DeliberationOpened,
    ProposalEmitted,
    RoundEvent,
    DeliberationClosed,
    OutcomeObserved
}

public enum EventKind
{
    FalsificationEvidence,
    Acknowledge,
    Reject,
    Amend,
    Revise,
    ChallengeTier,
    TierResponse,
    Timeout
}

public enum TerminationState
{
    Converged,
    PartialCommit,
    Deadlocked
}

public enum OutcomeClass
{
    Binary,
    Graded
}
