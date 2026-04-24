using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     Invokes private <c>SetHpBarContainerSizeWithOffsetsImmediately</c> across game versions (0.99.1+).
    /// </summary>
    internal static class NHealthBarGraftCompat
    {
        private static readonly Lazy<MethodInfo?> SetHpBarContainerSizeWithOffsetsImmediately = new(ResolveResizeMethod);

        private static MethodInfo? ResolveResizeMethod()
        {
            return typeof(NHealthBar).GetMethod(
                "SetHpBarContainerSizeWithOffsetsImmediately",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                [typeof(Vector2)],
                null);
        }

        internal static void TryResizeHpBarContainer(NHealthBar healthBar, Vector2 size)
        {
            var method = SetHpBarContainerSizeWithOffsetsImmediately.Value;
            if (method == null)
                return;

            try
            {
                method.Invoke(healthBar, [size]);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[HealthBarGraft] Failed to resize HP bar container: {ex}");
            }
        }
    }
}
