# Adj.Manifest

A .NET 8 reference implementation of the **Agent Deliberation Journal (ADJ)** specification — the append-only journal format that records every step of a multi-agent deliberation: when it opened, what proposals were emitted, what falsifications happened, when it closed, and what outcome was eventually observed.

This library is one of several reference implementations ([TypeScript](https://git.marketally.com/ai-manifests/adj-ref-lib-ts), [Python](https://git.marketally.com/ai-manifests/adj-ref-lib-py)) of the same spec. The spec itself is at [adp-manifest.dev](https://adp-manifest.dev) and is the source of truth; this library implements what the spec says.

## Install

Clone and build:

```bash
git clone https://git.marketally.com/ai-manifests/adj-ref-lib-csharp.git
cd adj-ref-lib-csharp
dotnet build
```

Reference the project from your own code:

```xml
<ItemGroup>
  <ProjectReference Include="..\path\to\Adj.Manifest\Adj.Manifest.csproj" />
</ItemGroup>
```

## Quick example

```csharp
using Adj.Manifest;

var store = new InMemoryJournalStore();

store.Append(new DeliberationOpened(
    EntryId: "adj_01HMX",
    DeliberationId: "dlb_42",
    Timestamp: DateTimeOffset.UtcNow,
    PriorEntryHash: null,
    Action: new ActionDescriptor("code.merge", "auto", BlastRadius.TeamScope),
    Config: new DeliberationConfig(MinAgents: 3, Timeout: TimeSpan.FromMinutes(5))
));

// ... append proposals, round events, deliberation close, outcome ...

var calibration = BrierScorer.ComputeCalibration(store.ScoringPairsFor("did:adp:my-agent-v1"));
// calibration.Value is the Brier-scored calibration score in [0, 1]
```

## API

All public types live in the `Adj.Manifest` namespace.

### Entry types (records)

- `JournalEntry` — abstract base carrying `EntryId`, `DeliberationId`, `Timestamp`, `PriorEntryHash`
- `DeliberationOpened` — §3.1, marks the start of a deliberation
- `ProposalEmitted` — §3.2, an agent's vote + confidence + justification
- `RoundEvent` — §3.3, per-round events (falsification, revision, tally)
- `DeliberationClosed` — §3.4, records termination state and final tally
- `OutcomeObserved` — §3.5, ground-truth outcome observed after the fact

### Value objects

`ActionDescriptor`, `TallyRecord`, `ProposalData`, `ConditionRecord`, `DeliberationConfig`, `CalibrationScore`, `ScoringPair`, `ConditionQualityMetrics`

### Enums

`EntryType`, `EventKind`, `TerminationState`, `OutcomeClass`

### Scorers

- `BrierScorer` — static class. `ComputeCalibration(pairs)` returns a calibration score. `Update(score, pair)` folds a new observation into an existing score.
- `ConditionQualityScorer` — static class. `Compute(entries)` returns per-condition quality metrics (how often the condition was falsified, how often it was load-bearing, etc.)

### Store

- `IJournalStore` — interface with `Append`, `GetDeliberation`, `GetOutcome`, `GetCalibration`, `ScoringPairsFor`
- `InMemoryJournalStore` — thread-safe in-memory implementation suitable for tests and prototypes

## Testing

```bash
dotnet test
```

4 test suites cover entry types, the Brier scorer, the in-memory store, and condition quality metrics.

## Spec

This library implements the Agent Deliberation Journal specification. Read the spec at [adp-manifest.dev](https://adp-manifest.dev). If the spec and this library disagree, the spec is correct and this is a bug.

## License

Apache-2.0 — see [`LICENSE`](LICENSE) for the full license text and [`NOTICE`](NOTICE) for attribution.
