using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Relics;
using STS2RitsuLib.Relics.Visibility;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     <para xml:lang="en">Registers a relic-visibility rule. Returning <see langword="false" /> hides the relic from normal relic UI.</para>
        ///     <para xml:lang="zh-CN">注册遗物可见性规则。返回 <see langword="false" /> 会从正常遗物界面隐藏该遗物。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">Disposable registration handle.</para>
        ///     <para xml:lang="zh-CN">可释放的注册句柄。</para>
        /// </returns>
        public static IDisposable RegisterRelicVisibilityRule(string modId, Func<RelicModel, bool> isVisible)
        {
            return ModRelicVisibilityRegistry.Register(modId, isVisible);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns whether RitsuLib should show a relic in normal relic UI.</para>
        ///     <para xml:lang="zh-CN">返回 RitsuLib 是否应在正常遗物界面显示该遗物。</para>
        /// </summary>
        public static bool IsRelicVisible(RelicModel relic)
        {
            return ModRelicVisibilityRegistry.IsVisible(relic);
        }

        /// <summary>
        ///     <para xml:lang="en">Refreshes the active run's relic UI after relic-visibility state changes.</para>
        ///     <para xml:lang="zh-CN">在遗物可见性状态变更后刷新当前一局游戏的遗物界面。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">Whether an active relic inventory was found and changed.</para>
        ///     <para xml:lang="zh-CN">是否找到并变更了活动遗物栏。</para>
        /// </returns>
        public static bool RefreshRelicVisibility()
        {
            return ModRelicVisibilityUi.Refresh(NRun.Instance?.GlobalUi?.RelicInventory);
        }

        /// <summary>
        ///     <para xml:lang="en">Refreshes a specific relic inventory after relic-visibility state changes.</para>
        ///     <para xml:lang="zh-CN">在遗物可见性状态变更后刷新指定遗物栏。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">Whether the inventory contents changed.</para>
        ///     <para xml:lang="zh-CN">遗物栏内容是否已变更。</para>
        /// </returns>
        public static bool RefreshRelicVisibility(NRelicInventory inventory)
        {
            ArgumentNullException.ThrowIfNull(inventory);
            return ModRelicVisibilityUi.Refresh(inventory);
        }
    }
}
