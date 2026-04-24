using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     Invokes <c>SetHpBarContainerSizeWithOffsetsImmediately</c> on <see cref="NHealthBar" />.
    ///     Requires a publicized STS2 assembly.
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
