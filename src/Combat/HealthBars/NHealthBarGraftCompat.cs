using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Invokes the publicized <c>NHealthBar.SetHpBarContainerSizeWithOffsetsImmediately</c> method.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         调用经公开化处理的 <c>NHealthBar.SetHpBarContainerSizeWithOffsetsImmediately</c> 方法。
    ///     </para>
    /// </summary>
    internal static class NHealthBarGraftCompat
    {
        internal static void TryResizeHpBarContainer(NHealthBar healthBar, Vector2 size)
        {
            try
            {
                healthBar.SetHpBarContainerSizeWithOffsetsImmediately(size);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[HealthBarGraft] Failed to resize HP bar container: {ex}");
            }
        }
    }
}
