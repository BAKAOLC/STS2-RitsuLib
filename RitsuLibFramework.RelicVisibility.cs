using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Relics;
using STS2RitsuLib.Relics.Visibility;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     Registers a relic visibility rule. Returning false hides the relic from normal relic UI.
        ///     注册遗物可见性规则。返回 false 会将该遗物从正常遗物 UI 中隐藏。
        /// </summary>
        /// <returns>
        ///     A disposable registration handle.
        ///     可释放的注册句柄。
        /// </returns>
        public static IDisposable RegisterRelicVisibilityRule(string modId, Func<RelicModel, bool> isVisible)
        {
            return ModRelicVisibilityRegistry.Register(modId, isVisible);
        }

        /// <summary>
        ///     Returns whether RitsuLib should show a relic in normal relic UI.
        ///     返回 RitsuLib 是否应在正常遗物 UI 中显示该遗物。
        /// </summary>
        public static bool IsRelicVisible(RelicModel relic)
        {
            return ModRelicVisibilityRegistry.IsVisible(relic);
        }

        /// <summary>
        ///     Refreshes the active run relic UI after relic visibility state changes.
        ///     在遗物可见性状态变化后，刷新当前跑局的遗物 UI。
        /// </summary>
        /// <returns>
        ///     True if an active relic inventory was found and changed.
        ///     如果找到当前遗物栏且内容发生变化，则返回 true。
        /// </returns>
        public static bool RefreshRelicVisibility()
        {
            return ModRelicVisibilityUi.Refresh(NRun.Instance?.GlobalUi?.RelicInventory);
        }

        /// <summary>
        ///     Refreshes a specific relic inventory after relic visibility state changes.
        ///     在遗物可见性状态变化后，刷新指定遗物栏。
        /// </summary>
        /// <returns>
        ///     True if the inventory contents changed.
        ///     如果遗物栏内容发生变化，则返回 true。
        /// </returns>
        public static bool RefreshRelicVisibility(NRelicInventory inventory)
        {
            ArgumentNullException.ThrowIfNull(inventory);
            return ModRelicVisibilityUi.Refresh(inventory);
        }
    }
}
