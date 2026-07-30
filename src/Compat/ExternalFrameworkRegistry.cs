using System.Collections.Concurrent;

namespace STS2RitsuLib.Compat
{
    /// <summary>
    ///     <para xml:lang="en">Defines stable framework IDs used by runtime interoperability checks.</para>
    ///     <para xml:lang="zh-CN">定义运行时互操作检查使用的稳定框架 ID。</para>
    /// </summary>
    internal static class ExternalFrameworkIds
    {
        public const string BaseLib = "baselib";
        public const string BaseLibToRitsuGenerated = "baselib-to-ritsu-generated";
        public const string JmcModLib = "jmcmodlib";
        public const string ModConfig = "modconfig";
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Detects external frameworks through known type markers or registered custom detectors.
    ///     </para>
    ///     <para xml:lang="zh-CN">通过已知类型标记或已注册的自定义检测器判断外部框架是否存在。</para>
    /// </summary>
    internal static class ExternalFrameworkRegistry
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<string, Func<bool>> CustomDetectors =
            new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, TypeResolution> TypeCache = new(StringComparer.Ordinal);

        private static readonly Dictionary<string, ProbeSpec> BuiltInProbes = new(StringComparer.OrdinalIgnoreCase)
        {
            [ExternalFrameworkIds.BaseLib] = new(
                ExternalFrameworkIds.BaseLib,
                ["BaseLib.Patches.Hooks.MaxHandSizePatch", "BaseLib.Hooks.HealthBarForecastRegistry"]),
            [ExternalFrameworkIds.BaseLibToRitsuGenerated] = new(
                ExternalFrameworkIds.BaseLibToRitsuGenerated,
                ["BaseLibToRitsu.Generated.ModConfigRegistry"]),
            [ExternalFrameworkIds.JmcModLib] = new(
                ExternalFrameworkIds.JmcModLib,
                ["JmcModLib.Config.ConfigManager", "JmcModLib.Core.ModRegistry"]),
            [ExternalFrameworkIds.ModConfig] = new(
                ExternalFrameworkIds.ModConfig,
                ["ModConfig.ModConfigApi"]),
        };

        private static readonly Dictionary<string, bool> PresenceCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     <para xml:lang="en">Registers a custom detector, replacing any detector with the same framework ID.</para>
        ///     <para xml:lang="zh-CN">注册自定义检测器，并替换框架 ID 相同的现有检测器。</para>
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
        ///     <para xml:lang="en">Returns whether the specified framework is detected.</para>
        ///     <para xml:lang="zh-CN">返回是否检测到指定框架。</para>
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
        ///     <para xml:lang="en">Clears cached results and detects every known framework again.</para>
        ///     <para xml:lang="zh-CN">清除缓存结果，并重新检测所有已知框架。</para>
        /// </summary>
        public static void RefreshKnownFrameworkPresence(string reason)
        {
            TypeCache.Clear();
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
        ///     <para xml:lang="en">Resolves <paramref name="fullTypeName" /> from loaded assemblies.</para>
        ///     <para xml:lang="zh-CN">从已加载的程序集中解析 <paramref name="fullTypeName" />。</para>
        /// </summary>
        public static Type? ResolveType(string fullTypeName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullTypeName);

            if (TypeCache.TryGetValue(fullTypeName, out var cached))
                return cached.Type;

            var resolved = ResolveTypeCore(fullTypeName);
            return TypeCache.GetOrAdd(fullTypeName, new TypeResolution(resolved)).Type;
        }

        private static Type? ResolveTypeCore(string fullTypeName)
        {
            var byQualifiedName = Type.GetType(fullTypeName);
            if (byQualifiedName != null)
                return byQualifiedName;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                try
                {
                    var type = assembly.GetType(fullTypeName, false);
                    if (type != null)
                        return type;
                }
                catch
                {
                    // ignored
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

        private readonly record struct TypeResolution(Type? Type);
    }
}
