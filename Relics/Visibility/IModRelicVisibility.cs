using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Relics.Visibility
{
    /// <summary>
    ///     Implement on a <see cref="RelicModel" /> to control whether RitsuLib should create relic UI for it.
    ///     在 <see cref="RelicModel" /> 上实现此接口，以控制 RitsuLib 是否为该遗物创建 UI。
    /// </summary>
    public interface IModRelicVisibility
    {
        /// <summary>
        ///     True when the relic should be shown in the normal relic UI.
        ///     为 true 时，该遗物会显示在正常遗物 UI 中。
        /// </summary>
        bool IsRelicVisible { get; }
    }
}
