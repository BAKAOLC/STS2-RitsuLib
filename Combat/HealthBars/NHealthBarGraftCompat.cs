using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     Invokes <c>SetHpBarContainerSizeWithOffsetsImmediately</c> on <see cref="NHealthBar" />.
    ///     Invokes <c>SetHpBarContainerSizeWithOffsetImmediately</c> on <c>NHealthBar</c>.
    ///     Requires a publicized STS2 assembly.
    ///     在 <see cref="NHealthBar" /> 上调用 <c>SetHpBarContainerSizeWithOffsetsImmediately</c>。
    ///     在 <c>NHealthBar</c> 上调用 <c>SetHpBarContainerSizeWithOffsetImmediately</c>。
    ///     需要 publicized STS2 程序集。
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
