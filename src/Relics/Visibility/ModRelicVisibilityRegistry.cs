using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Relics.Visibility
{
    /// <summary>
    ///     <para xml:lang="en">Provides a runtime registry of mod-defined relic visibility rules.</para>
    ///     <para xml:lang="zh-CN">提供模组自定义遗物可见性规则的运行时注册表。</para>
    /// </summary>
    public static class ModRelicVisibilityRegistry
    {
        private static readonly Lock SyncRoot = new();
        private static readonly List<VisibilityRuleRegistration> Rules = [];

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a visibility rule. Returning <see langword="false" /> from the rule hides the relic.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册可见性规则。规则返回 <see langword="false" /> 时会隐藏相应遗物。
        ///     </para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">A handle whose disposal unregisters the rule.</para>
        ///     <para xml:lang="zh-CN">释放后会注销该规则的句柄。</para>
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
        ///     <para xml:lang="en">Returns whether a relic should appear in ordinary relic UI.</para>
        ///     <para xml:lang="zh-CN">返回遗物是否应显示在常规遗物界面中。</para>
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

            // ReSharper disable once MemberHidesStaticFromOuterClass
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
