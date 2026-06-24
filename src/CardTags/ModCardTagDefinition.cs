using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.CardTags
{
    /// <summary>
    ///     Immutable row for a mod-registered <see cref="CardTag" /> minted from a qualified string id.
    ///     表示从限定字符串 ID 生成的 mod 注册 <see cref="CardTag" /> 的不可变记录。
    /// </summary>
    public sealed record ModCardTagDefinition(string ModId, string Id, CardTag CardTagValue);
}
