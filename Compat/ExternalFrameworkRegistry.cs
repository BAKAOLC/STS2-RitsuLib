namespace STS2RitsuLib.Compat
{
    /// <summary>
    ///     Stable framework ids used by runtime interop checks.
    ///     运行时互操作检查使用的稳定框架 id。
    /// </summary>
    internal static class ExternalFrameworkIds
    {
        public const string BaseLib = "baselib";
        public const string BaseLibToRitsuGenerated = "baselib-to-ritsu-generated";
        public const string ModConfig = "modconfig";
    }

    /// <summary>
    ///     Central registry for external framework presence checks.
    ///     Known frameworks are probe-based and can be extended with custom detectors.
    ///     外部框架存在性检查的中央注册表。
    ///     已知框架基于探测判断，并可通过自定义检测器扩展。
    /// </summary>
    internal static class ExternalFrameworkRegistry
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<string, Func<bool>> CustomDetectors = [];

        private static readonly Dictionary<string, ProbeSpec> BuiltInProbes = new(StringComparer.OrdinalIgnoreCase)
        {
            [ExternalFrameworkIds.BaseLib] = new(
                ExternalFrameworkIds.BaseLib,
                ["BaseLib.Patches.Hooks.MaxHandSizePatch", "BaseLib.Hooks.HealthBarForecastRegistry"]),
            [ExternalFrameworkIds.BaseLibToRitsuGenerated] = new(
                ExternalFrameworkIds.BaseLibToRitsuGenerated,
                ["BaseLibToRitsu.Generated.ModConfigRegistry"]),
            [ExternalFrameworkIds.ModConfig] = new(
                ExternalFrameworkIds.ModConfig,
                ["ModConfig.ModConfigApi"]),
        };

        private static readonly Dictionary<string, bool> PresenceCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Registers a custom framework detector. The latest detector with the same id wins.
        ///     注册自定义框架检测器。相同 id 的最新检测器生效。
        /// </summary>
        public static void RegisterFrameworkDetector(string frameworkId, Func<bool> detector)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(frameworkId);
            ArgumentNullException.ThrowIfNull(detector);

            lock (Gate)
            {
                CustomDetectors[frameworkId] = detector;
                PresenceCache.Remove(frameworkId);
            }
        }

        /// <summary>
        ///     Returns whether the specified framework appears to be present.
        ///     返回指定框架看起来是否存在。
        /// </summary>
        public static bool IsFrameworkPresent(string frameworkId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(frameworkId);

            lock (Gate)
            {
                if (PresenceCache.TryGetValue(frameworkId, out var cached))
                    return cached;

                var detected = DetectFrameworkCore(frameworkId);
                PresenceCache[frameworkId] = detected;
                return detected;
            }
        }

        /// <summary>
        ///     Refreshes all known framework presence states.
        ///     刷新所有已知框架的存在状态。
        /// </summary>
        public static void RefreshKnownFrameworkPresence(string reason)
        {
            lock (Gate)
            {
                PresenceCache.Clear();
                foreach (var frameworkId in BuiltInProbes.Keys)
                    PresenceCache[frameworkId] = DetectFrameworkCore(frameworkId);
                foreach (var frameworkId in CustomDetectors.Keys)
                    PresenceCache[frameworkId] = DetectFrameworkCore(frameworkId);
            }

            RitsuLibFramework.Logger.Info($"[Compat] External framework presence refreshed ({reason}).");
        }

        /// <summary>
        ///     Resolves <paramref name="fullTypeName" /> from loaded assemblies.
        ///     从已加载程序集中解析 <paramref name="fullTypeName" />。
        /// </summary>
        public static Type? ResolveType(string fullTypeName)
        {
            var byQualifiedName = Type.GetType(fullTypeName);
            if (byQualifiedName != null)
                return byQualifiedName;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? type = null;
                try
                {
                    type = assembly.GetType(fullTypeName, false);
                }
                catch
                {
                    // ignored
                }

                if (type != null)
                    return type;
            }

            return null;
        }

        private static bool DetectFrameworkCore(string frameworkId)
        {
            // ReSharper disable once InvertIf
            if (CustomDetectors.TryGetValue(frameworkId, out var customDetector))
                try
                {
                    return customDetector();
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Compat] Custom framework detector '{frameworkId}' failed: {ex.Message}");
                    return false;
                }

            return BuiltInProbes.TryGetValue(frameworkId, out var spec) &&
                   spec.TypeMarkers.Any(typeName => ResolveType(typeName) != null);
        }

        private readonly record struct ProbeSpec(
            string FrameworkId,
            IReadOnlyList<string> TypeMarkers);
    }
}
