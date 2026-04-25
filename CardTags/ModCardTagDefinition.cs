using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.CardTags
{
    /// <summary>
    ///     Immutable row for a mod-registered <see cref="CardTag" /> minted from a qualified string id.
    /// </summary>
    public sealed record ModCardTagDefinition(string ModId, string Id, CardTag CardTagValue);
}
