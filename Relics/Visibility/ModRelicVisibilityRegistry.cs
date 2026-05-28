using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Relics.Visibility
{
    /// <summary>
    ///     Runtime registry for mod relic visibility rules.
    ///     Mod 遗物可见性规则的运行时注册表。
    /// </summary>
    public static class ModRelicVisibilityRegistry
    {
        private static readonly Lock SyncRoot = new();
        private static readonly List<VisibilityRuleRegistration> Rules = [];

        /// <summary>
        ///     Registers a visibility rule. Returning false hides the relic.
        ///     注册可见性规则。返回 false 会隐藏该遗物。
        /// </summary>
        /// <returns>
        ///     A disposable registration handle.
        ///     可释放的注册句柄。
        /// </returns>
        public static IDisposable Register(string modId, Func<RelicModel, bool> isVisible)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(isVisible);

            var registration = new VisibilityRuleRegistration(modId, isVisible);
            lock (SyncRoot)
            {
                Rules.Add(registration);
            }

            return registration;
        }

        /// <summary>
        ///     Returns whether a relic should be visible in normal relic UI.
        ///     返回遗物是否应在正常遗物 UI 中可见。
        /// </summary>
        public static bool IsVisible(RelicModel relic)
        {
            ArgumentNullException.ThrowIfNull(relic);

            if (relic is IModRelicVisibility { IsRelicVisible: false })
                return false;

            VisibilityRuleRegistration[] snapshot;
            lock (SyncRoot)
            {
                snapshot = [.. Rules];
            }

            foreach (var rule in snapshot)
                try
                {
                    if (!rule.IsVisible(relic))
                        return false;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RelicVisibility] Rule from '{rule.ModId}' failed for relic '{relic.Id}': {ex.Message}");
                }

            return true;
        }

        internal static int GetVisibleIndex(RelicModel relic, int vanillaIndex)
        {
            if (vanillaIndex <= 0)
                return vanillaIndex;

            try
            {
                var relics = relic.Owner.Relics;
                var limit = Math.Min(vanillaIndex, relics.Count);
                var visibleIndex = 0;
                for (var i = 0; i < limit; i++)
                    if (IsVisible(relics[i]))
                        visibleIndex++;

                return visibleIndex;
            }
            catch
            {
                return vanillaIndex;
            }
        }

        private sealed class VisibilityRuleRegistration(string modId, Func<RelicModel, bool> isVisible) : IDisposable
        {
            private bool _disposed;

            public string ModId { get; } = modId;
            public Func<RelicModel, bool> IsVisible { get; } = isVisible;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                lock (SyncRoot)
                {
                    Rules.Remove(this);
                }
            }
        }
    }
}
