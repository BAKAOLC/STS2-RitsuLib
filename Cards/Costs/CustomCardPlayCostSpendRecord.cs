namespace STS2RitsuLib.Cards.Costs
{
    /// <summary>
    ///     Describes a custom play cost line for UI, ledgers, and telemetry (stable <see cref="KindId" /> for aggregation).
    /// </summary>
    public readonly record struct CustomCardPlayCostSpendRecord(
        string KindId,
        decimal Amount,
        string? DisplayHint = null);
}
