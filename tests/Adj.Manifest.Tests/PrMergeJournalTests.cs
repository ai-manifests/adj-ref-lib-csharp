using System.Collections.Immutable;
using Adj.Manifest;

namespace Adj.Manifest.Tests;

/// <summary>
/// ADJ spec §9 worked example as an executable test.
/// Writes the full PR merge deliberation to a journal, records the outcome
/// three days later, then verifies calibration scoring matches §9.2.
/// </summary>
public class PrMergeJournalTests
{
    private const string DlbId = "dlb_01HMXJ3E9R";
    private const string TestRunner = "did:adp:test-runner-v2";
    private const string Scanner = "did:adp:security-scanner-v3";
    private const string Linter = "did:adp:style-linter-v1";

    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-04-11T14:32:00Z");
    private static readonly DateTimeOffset TOutcome = DateTimeOffset.Parse("2026-04-14T09:12:00Z");

    private static InMemoryJournalStore BuildJournalWithFullDeliberation()
    {
        var store = new InMemoryJournalStore();

        // Entry 1: deliberation_opened
        store.Append(new DeliberationOpened(
            EntryId: "adj_01",
            DeliberationId: DlbId,
            Timestamp: T0,
            PriorEntryHash: null,
            DecisionClass: "code.correctness",
            Action: new ActionDescriptor("merge_pull_request", "github.com/acme/api#4471",
                ImmutableDictionary<string, string>.Empty.Add("strategy", "squash")),
            Participants: ImmutableList.Create(TestRunner, Scanner, Linter),
            Config: new DeliberationConfig(MaxRounds: 3, ParticipationFloor: 0.50)
        ));

        // Entries 2-4: proposal_emitted
        store.Append(new ProposalEmitted(
            EntryId: "adj_02",
            DeliberationId: DlbId,
            Timestamp: T0.AddSeconds(9),
            PriorEntryHash: null,
            Proposal: new ProposalData(
                ProposalId: "prp_01HMXK4F7G",
                AgentId: TestRunner,
                Vote: "approve",
                Confidence: 0.86,
                Domain: "code.correctness",
                CalibrationAtStake: true,
                DissentConditions: ImmutableList.Create(
                    new ConditionRecord("dc_tr_01", "if any test marked critical regresses", "active", 0, null),
                    new ConditionRecord("dc_tr_02", "if coverage delta is negative", "active", 0, null)
                )
            )
        ));

        store.Append(new ProposalEmitted(
            EntryId: "adj_03",
            DeliberationId: DlbId,
            Timestamp: T0.AddSeconds(11),
            PriorEntryHash: null,
            Proposal: new ProposalData(
                ProposalId: "prp_01HMXK5A2B",
                AgentId: Scanner,
                Vote: "reject",
                Confidence: 0.79,
                Domain: "security.policy",
                CalibrationAtStake: true,
                DissentConditions: ImmutableList.Create(
                    new ConditionRecord("dc_ss_01", "if any code path in auth module remains untested", "active", 0, null),
                    new ConditionRecord("dc_ss_02", "if no security-focused test covers token validation", "active", 0, null)
                )
            )
        ));

        store.Append(new ProposalEmitted(
            EntryId: "adj_04",
            DeliberationId: DlbId,
            Timestamp: T0.AddSeconds(12),
            PriorEntryHash: null,
            Proposal: new ProposalData(
                ProposalId: "prp_01HMXK6C3D",
                AgentId: Linter,
                Vote: "approve",
                Confidence: 0.62,
                Domain: "code.style",
                CalibrationAtStake: true,
                DissentConditions: ImmutableList.Create(
                    new ConditionRecord("dc_sl_01", "if any public API name violates naming convention", "active", 0, null)
                )
            )
        ));

        // Entries 5-6: falsification evidence (test-runner → scanner)
        store.Append(new RoundEvent(
            EntryId: "adj_05",
            DeliberationId: DlbId,
            Timestamp: T0.AddSeconds(135),
            PriorEntryHash: null,
            Round: 1,
            EventKind: EventKind.FalsificationEvidence,
            AgentId: TestRunner,
            TargetAgentId: Scanner,
            TargetConditionId: "dc_ss_01",
            Payload: null
        ));

        store.Append(new RoundEvent(
            EntryId: "adj_06",
            DeliberationId: DlbId,
            Timestamp: T0.AddSeconds(136),
            PriorEntryHash: null,
            Round: 1,
            EventKind: EventKind.FalsificationEvidence,
            AgentId: TestRunner,
            TargetAgentId: Scanner,
            TargetConditionId: "dc_ss_02",
            Payload: null
        ));

        // Entries 7-8: acknowledge
        store.Append(new RoundEvent(
            EntryId: "adj_07",
            DeliberationId: DlbId,
            Timestamp: T0.AddSeconds(164),
            PriorEntryHash: null,
            Round: 1,
            EventKind: EventKind.Acknowledge,
            AgentId: Scanner,
            TargetAgentId: null,
            TargetConditionId: "dc_ss_01",
            Payload: null
        ));

        store.Append(new RoundEvent(
            EntryId: "adj_08",
            DeliberationId: DlbId,
            Timestamp: T0.AddSeconds(165),
            PriorEntryHash: null,
            Round: 1,
            EventKind: EventKind.Acknowledge,
            AgentId: Scanner,
            TargetAgentId: null,
            TargetConditionId: "dc_ss_02",
            Payload: null
        ));

        // Entry 9: revise (scanner reject → abstain)
        store.Append(new RoundEvent(
            EntryId: "adj_09",
            DeliberationId: DlbId,
            Timestamp: T0.AddSeconds(202),
            PriorEntryHash: null,
            Round: 1,
            EventKind: EventKind.Revise,
            AgentId: Scanner,
            TargetAgentId: null,
            TargetConditionId: null,
            Payload: null
        ));

        // Entry 10: deliberation_closed
        store.Append(new DeliberationClosed(
            EntryId: "adj_10",
            DeliberationId: DlbId,
            Timestamp: T0.AddSeconds(210),
            PriorEntryHash: null,
            Termination: TerminationState.Converged,
            RoundCount: 1,
            Tier: "partially_reversible",
            FinalTally: new TallyRecord(
                ApproveWeight: 0.89,
                RejectWeight: 0.00,
                AbstainWeight: 0.64,
                TotalWeight: 1.53,
                ApprovalFraction: 1.00,
                ParticipationFraction: 0.582,
                Threshold: 0.60
            ),
            Weights: ImmutableDictionary<string, double>.Empty
                .Add(TestRunner, 0.71)
                .Add(Scanner, 0.64)
                .Add(Linter, 0.18),
            CommittedAction: new ActionDescriptor("merge_pull_request", "github.com/acme/api#4471",
                ImmutableDictionary<string, string>.Empty.Add("strategy", "squash"))
        ));

        return store;
    }

    [Fact]
    public void Full_deliberation_record_has_correct_entry_count()
    {
        var store = BuildJournalWithFullDeliberation();
        var entries = store.GetDeliberation(DlbId);

        // 1 opened + 3 proposals + 5 round events + 1 closed = 10
        Assert.Equal(10, entries.Count);
        Assert.IsType<DeliberationOpened>(entries[0]);
        Assert.IsType<DeliberationClosed>(entries[^1]);
    }

    [Fact]
    public void No_outcome_before_recording()
    {
        var store = BuildJournalWithFullDeliberation();
        Assert.Null(store.GetOutcome(DlbId));
    }

    [Fact]
    public void Calibration_returns_bootstrap_before_outcome()
    {
        var store = BuildJournalWithFullDeliberation();

        // No outcome yet → no scoring pairs → bootstrap default
        var cal = store.GetCalibration(TestRunner, "code.correctness");
        Assert.Equal(0.5, cal.Value);
        Assert.Equal(0, cal.SampleSize);
    }

    [Fact]
    public void Outcome_recorded_and_retrievable()
    {
        var store = BuildJournalWithFullDeliberation();

        store.Append(new OutcomeObserved(
            EntryId: "adj_11",
            DeliberationId: DlbId,
            Timestamp: TOutcome.AddMinutes(3),
            PriorEntryHash: null,
            ObservedAt: TOutcome,
            OutcomeClass: OutcomeClass.Binary,
            Success: 1.0,
            EvidenceRefs: ImmutableList.Create("ci:github-actions/run/8835001"),
            ReporterId: "did:adp:ci-monitor-v1",
            ReporterConfidence: 0.95,
            GroundTruth: true,
            Supersedes: null
        ));

        var outcome = store.GetOutcome(DlbId);
        Assert.NotNull(outcome);
        Assert.Equal(1.0, outcome.OutcomeValue);
        Assert.True(outcome.GroundTruth);
    }

    [Fact]
    public void Calibration_updates_after_outcome_matches_spec_section_9_2()
    {
        var store = BuildJournalWithFullDeliberation();

        // Record outcome: binary success
        store.Append(new OutcomeObserved(
            EntryId: "adj_11",
            DeliberationId: DlbId,
            Timestamp: TOutcome.AddMinutes(3),
            PriorEntryHash: null,
            ObservedAt: TOutcome,
            OutcomeClass: OutcomeClass.Binary,
            Success: 1.0,
            EvidenceRefs: ImmutableList.Create("ci:github-actions/run/8835001"),
            ReporterId: "did:adp:ci-monitor-v1",
            ReporterConfidence: 0.95,
            GroundTruth: true,
            Supersedes: null
        ));

        // Test-runner: confidence 0.86, outcome 1.0
        // Brier contribution: (0.86 - 1.0)² = 0.0196
        // With only 1 sample: cal = 1 - 0.0196 = 0.9804
        var trCal = store.GetCalibration(TestRunner, "code.correctness");
        Assert.Equal(1, trCal.SampleSize);
        Assert.InRange(trCal.Value, 0.97, 0.99); // 1 - 0.0196 ≈ 0.98

        // Scanner: confidence 0.79, outcome 1.0
        // Brier contribution: (0.79 - 1.0)² = 0.0441
        var scCal = store.GetCalibration(Scanner, "security.policy");
        Assert.Equal(1, scCal.SampleSize);
        Assert.InRange(scCal.Value, 0.95, 0.97); // 1 - 0.0441 ≈ 0.956

        // Linter: confidence 0.62, outcome 1.0
        // Brier contribution: (0.62 - 1.0)² = 0.1444
        var ltCal = store.GetCalibration(Linter, "code.style");
        Assert.Equal(1, ltCal.SampleSize);
        Assert.InRange(ltCal.Value, 0.84, 0.87); // 1 - 0.1444 ≈ 0.856
    }

    [Fact]
    public void Incremental_brier_update_matches_full_recompute()
    {
        // Verify incremental update produces the same result as full compute.
        // Spec §5.5: prior cal = 0.85, N = 312, new pair (0.86, 1.0)
        var prior = new CalibrationScore(Value: 0.85, SampleSize: 312, Staleness: TimeSpan.FromDays(18));
        var newPair = new ScoringPair(Confidence: 0.86, Outcome: 1.0, Timestamp: TOutcome);

        var updated = BrierScorer.Update(prior, newPair, TOutcome);

        // Expected: new BS = (312 * 0.15 + 0.0196) / 313 = 0.1496
        // New cal = 1 - 0.1496 = 0.8504
        Assert.Equal(313, updated.SampleSize);
        Assert.InRange(updated.Value, 0.849, 0.852);
    }

    [Fact]
    public void Condition_quality_tracks_falsification_ratio()
    {
        var store = BuildJournalWithFullDeliberation();

        // Scanner published 2 conditions, both were tested (entries 5-8)
        // But condition quality is computed from the proposal data, not round events.
        // The proposal has conditions with TestedInRound = null (initial state).
        // In a real system, the journal would update condition records when
        // round events are processed. For this test, build conditions with
        // tested state directly.
        var testedConditions = new List<ConditionRecord>
        {
            new("dc_ss_01", "if any code path in auth module remains untested", "falsified", 0, TestedInRound: 1),
            new("dc_ss_02", "if no security-focused test covers token validation", "falsified", 0, TestedInRound: 1),
        };

        var metrics = ConditionQualityScorer.Compute(testedConditions);

        Assert.Equal(1.0, metrics.FalsificationRatio); // 2/2
        Assert.Equal(2, metrics.ConditionsTested);
        Assert.Equal(2, metrics.ConditionsPublished);
        Assert.Equal(0.0, metrics.AmendmentFrequency);
    }

    [Fact]
    public void Condition_quality_detects_untestable_conditions()
    {
        // Agent publishes 10 conditions, only 2 ever get tested
        var conditions = Enumerable.Range(0, 10)
            .Select(i => new ConditionRecord(
                $"dc_{i:D2}",
                $"condition {i}",
                i < 2 ? "falsified" : "active",
                0,
                i < 2 ? 1 : null))
            .ToList();

        var metrics = ConditionQualityScorer.Compute(conditions);

        Assert.Equal(0.2, metrics.FalsificationRatio); // 2/10 — below 0.3 threshold
        Assert.Equal(10, metrics.ConditionsPublished);
        Assert.Equal(2, metrics.ConditionsTested);
    }

    [Fact]
    public void Bootstrap_agent_gets_default_calibration()
    {
        var store = new InMemoryJournalStore();

        var cal = store.GetCalibration("did:adp:new-agent", "code.correctness");

        Assert.Equal(0.5, cal.Value);
        Assert.Equal(0, cal.SampleSize);
        Assert.Equal(TimeSpan.Zero, cal.Staleness);
    }

    [Fact]
    public void Outcome_supersedes_replaces_prior()
    {
        var store = BuildJournalWithFullDeliberation();

        // First outcome: success
        store.Append(new OutcomeObserved(
            EntryId: "adj_11",
            DeliberationId: DlbId,
            Timestamp: TOutcome.AddMinutes(3),
            PriorEntryHash: null,
            ObservedAt: TOutcome,
            OutcomeClass: OutcomeClass.Binary,
            Success: 1.0,
            EvidenceRefs: ImmutableList.Create("ci:github-actions/run/8835001"),
            ReporterId: "did:adp:ci-monitor-v1",
            ReporterConfidence: 0.95,
            GroundTruth: true,
            Supersedes: null
        ));

        // Correction: actually failed (false positive CI)
        store.Append(new OutcomeObserved(
            EntryId: "adj_12",
            DeliberationId: DlbId,
            Timestamp: TOutcome.AddDays(1),
            PriorEntryHash: null,
            ObservedAt: TOutcome.AddHours(6),
            OutcomeClass: OutcomeClass.Binary,
            Success: 0.0,
            EvidenceRefs: ImmutableList.Create("incident:pagerduty/INC-8823"),
            ReporterId: "did:adp:incident-monitor-v1",
            ReporterConfidence: 0.99,
            GroundTruth: true,
            Supersedes: "adj_11"
        ));

        // Most recent outcome wins
        var outcome = store.GetOutcome(DlbId);
        Assert.NotNull(outcome);
        Assert.Equal(0.0, outcome.OutcomeValue);
        Assert.Equal("adj_11", outcome.Supersedes);

        // Calibration now scored against failure
        var trCal = store.GetCalibration(TestRunner, "code.correctness");
        // confidence 0.86, outcome 0.0 → Brier = (0.86 - 0)² = 0.7396
        // cal = 1 - 0.7396 = 0.2604
        Assert.InRange(trCal.Value, 0.25, 0.27);
    }
}
