using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.Costs
{
    /// <summary>
    ///     One persisted custom cost spend for the current combat (see <see cref="CustomCardPlayCostLedger" />).
    /// </summary>
    public readonly record struct CustomCardPlayCostLedgerEntry(
        DateTimeOffset OccurredAtUtc,
        int CombatRound,
        ModelId CardId,
        string KindId,
        decimal Amount,
        string? DisplayHint,
        ulong PlayerNetId);
}
