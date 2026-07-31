using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Relics.Visibility
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Implement on a <see cref="RelicModel" /> to control whether RitsuLib creates ordinary relic UI for it.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <see cref="RelicModel" /> 上实现此接口，以控制 RitsuLib 是否为该遗物创建常规遗物界面。
    ///     </para>
    /// </summary>
    public interface IModRelicVisibility
    {
        /// <summary>
        ///     <para xml:lang="en">Gets whether the relic should appear in ordinary relic UI.</para>
        ///     <para xml:lang="zh-CN">获取该遗物是否应显示在常规遗物界面中。</para>
        /// </summary>
        bool IsRelicVisible { get; }
    }
}
