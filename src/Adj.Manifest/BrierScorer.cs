namespace Adj.Manifest;

/// <summary>
/// Calibration score triple — the contract ADP's CalibrationSource consumes.
/// Spec §5.2.
/// </summary>
public sealed record CalibrationScore(
    double Value,
    int SampleSize,
    TimeSpan Staleness
);

/// <summary>
/// A single (confidence, outcome) pair for scoring.
/// </summary>
public sealed record ScoringPair(
    double Confidence,
    double Outcome,
    DateTimeOffset Timestamp
);

/// <summary>
/// Brier score calibration scorer. Spec §5.1.
///
/// BS = (1/N) × Σ(cᵢ − oᵢ)²
/// calibration_value = 1 − BS
///
/// Proper scoring rule: maximized when stated confidence equals true probability.
/// </summary>
public static class BrierScorer
{
    /// <summary>
    /// Computes the calibration score from a set of (confidence, outcome) pairs.
    /// Returns the CalibrationScore triple for the query contract.
    /// </summary>
    public static CalibrationScore Compute(IReadOnlyList<ScoringPair> pairs, DateTimeOffset now)
    {
        if (pairs.Count == 0)
            return GetDefault();

        double brierSum = 0;
        DateTimeOffset mostRecent = DateTimeOffset.MinValue;

        foreach (var pair in pairs)
        {
            var diff = pair.Confidence - pair.Outcome;
            brierSum += diff * diff;

            if (pair.Timestamp > mostRecent)
                mostRecent = pair.Timestamp;
        }

        var brierScore = brierSum / pairs.Count;
        var calibrationValue = 1.0 - brierScore;
        var staleness = now - mostRecent;

        return new CalibrationScore(
            Value: Math.Clamp(calibrationValue, 0.0, 1.0),
            SampleSize: pairs.Count,
            Staleness: staleness > TimeSpan.Zero ? staleness : TimeSpan.Zero
        );
    }

    /// <summary>
    /// Incrementally updates a calibration score with a new pair.
    /// More efficient than recomputing from scratch when the full history
    /// is not available.
    /// </summary>
    public static CalibrationScore Update(
        CalibrationScore prior,
        ScoringPair newPair,
        DateTimeOffset now)
    {
        var priorBrier = 1.0 - prior.Value;
        var diff = newPair.Confidence - newPair.Outcome;
        var newBrierContribution = diff * diff;

        var newN = prior.SampleSize + 1;
        var newBrier = (prior.SampleSize * priorBrier + newBrierContribution) / newN;
        var staleness = now - newPair.Timestamp;

        return new CalibrationScore(
            Value: Math.Clamp(1.0 - newBrier, 0.0, 1.0),
            SampleSize: newN,
            Staleness: staleness > TimeSpan.Zero ? staleness : TimeSpan.Zero
        );
    }

    /// <summary>
    /// Bootstrap default. Spec §5.4.
    /// New agents enter with neutral value, zero samples, zero staleness.
    /// </summary>
    public static CalibrationScore GetDefault() =>
        new(Value: 0.5, SampleSize: 0, Staleness: TimeSpan.Zero);
}
