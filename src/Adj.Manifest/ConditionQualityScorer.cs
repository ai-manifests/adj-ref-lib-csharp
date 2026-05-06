namespace Adj.Manifest;

/// <summary>
/// Condition quality metrics for an agent. Spec §6.
/// </summary>
public sealed record ConditionQualityMetrics(
    double FalsificationRatio,
    double AmendmentFrequency,
    int ConditionsPublished,
    int ConditionsTested,
    int TotalAmendments
);

/// <summary>
/// Computes condition quality metrics from an agent's dissent condition history.
/// Spec §6.
/// </summary>
public static class ConditionQualityScorer
{
    /// <summary>
    /// Computes condition quality metrics from a set of condition records
    /// across deliberations.
    /// </summary>
    public static ConditionQualityMetrics Compute(IReadOnlyList<ConditionRecord> conditions)
    {
        if (conditions.Count == 0)
        {
            return new ConditionQualityMetrics(
                FalsificationRatio: 0,
                AmendmentFrequency: 0,
                ConditionsPublished: 0,
                ConditionsTested: 0,
                TotalAmendments: 0
            );
        }

        int tested = 0;
        int totalAmendments = 0;

        foreach (var c in conditions)
        {
            if (c.TestedInRound.HasValue)
                tested++;
            totalAmendments += c.AmendmentCount;
        }

        return new ConditionQualityMetrics(
            FalsificationRatio: (double)tested / conditions.Count,
            AmendmentFrequency: (double)totalAmendments / conditions.Count,
            ConditionsPublished: conditions.Count,
            ConditionsTested: tested,
            TotalAmendments: totalAmendments
        );
    }
}
