using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.CardTags
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes a registered mod card tag and its dynamic <see cref="CardTag" /> value.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述已注册的模组卡牌标签及其动态 <see cref="CardTag" /> 值。
    ///     </para>
    /// </summary>
    public sealed record ModCardTagDefinition(string ModId, string Id, CardTag CardTagValue);
}
