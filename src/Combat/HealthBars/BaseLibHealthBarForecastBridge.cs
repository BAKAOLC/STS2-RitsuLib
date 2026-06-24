using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     When BaseLib is loaded, registers <see cref="HealthBarForecastRegistry.GetSegments" /> with BaseLib's
    ///     <c>HealthBarForecastRegistry.RegisterForeign</c> so a single renderer can consume Ritsu-typed segments.
    ///     加载 BaseLib 时，将 <see cref="HealthBarForecastRegistry.GetSegments" /> 注册到 BaseLib 的
    ///     <c>HealthBarForecastRegistry.RegisterForeign</c>，使单个渲染器可以消费 Ritsu 类型的片段。
    /// </summary>
    /// <remarks>
    ///     <see cref="ShouldRitsuRendererStandDown" /> becomes true after a successful bridge so duplicate overlays are
    ///     not drawn.
    ///     成功桥接后，<see cref="ShouldRitsuRendererStandDown" /> 变为 true，从而不绘制重复覆盖层。
    /// </remarks>
    internal static class BaseLibHealthBarForecastBridge
    {
        private const string SourceId = "ritsulib.registry";
        private static bool _registered;
        private static bool _baselibSupportsForecastInterop;
        private static bool _loggedMissingInterop;
        private static bool _loggedMissingRegisterForeign;
        private static Action<string, string, Func<Creature, IEnumerable<object>>>? _registerForeign;

        /// <summary>
        ///     When <see langword="true" />, Ritsu's <c>NHealthBar</c> forecast postfixes should skip drawing because BaseLib
        ///     already merged this mod's segments.
        ///     为 <see langword="true" /> 时，Ritsu 的 <c>NHealthBar</c> forecast postfix 应跳过绘制，因为 BaseLib
        ///     已经合并了此 mod 的片段。
        /// </summary>
        public static bool ShouldRitsuRendererStandDown()
        {
            return _registered && _baselibSupportsForecastInterop;
        }

        /// <summary>
        ///     Attempts foreign registration from <c>NHealthBar._Ready</c> (early load path).
        ///     从 <c>NHealthBar._Ready</c> 尝试 foreign 注册（早期加载路径）。
        /// </summary>
        public static void TryRegisterPrimary()
        {
            if (_registered)
                return;
            TryRegisterCore();
        }

        /// <summary>
        ///     Attempts foreign registration from forecast render path if <see cref="TryRegisterPrimary" /> did not run yet.
        ///     如果 <see cref="TryRegisterPrimary" /> 尚未运行，则从 forecast 渲染路径尝试 foreign 注册。
        /// </summary>
        public static void TryRegisterSecondary()
        {
            if (_registered)
                return;
            TryRegisterCore();
        }

        /// <summary>
        ///     Alias for <see cref="TryRegisterPrimary" />.
        ///     <see cref="TryRegisterPrimary" /> 的别名。
        /// </summary>
        public static void TryRegister()
        {
            TryRegisterPrimary();
        }

        private static void TryRegisterCore()
        {
            if (_registered)
                return;
            if (!ExternalFrameworkRegistry.IsFrameworkPresent(ExternalFrameworkIds.BaseLib))
                return;

            try
            {
                var registryType = ResolveBaseLibRegistryType();
                if (registryType == null)
                    return;

                var registerForeign = registryType.GetMethod(
                    "RegisterForeign",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(string), typeof(string), typeof(Func<Creature, IEnumerable<object>>)],
                    null);

                if (registerForeign == null)
                {
                    _baselibSupportsForecastInterop = false;
                    if (_loggedMissingRegisterForeign) return;
                    _loggedMissingRegisterForeign = true;
                    RitsuLibFramework.Logger.Warn(
                        $"[HealthBarForecast] BaseLib registry type '{registryType.FullName}' does not expose " +
                        "RegisterForeign(string, string, Func<Creature, IEnumerable<object>>); forecast interop unavailable.");

                    return;
                }

                var provider = GetSegmentsForCreature;
                _registerForeign ??=
                    registerForeign.CreateDelegate<Action<string, string, Func<Creature, IEnumerable<object>>>>();
                _registerForeign(Const.ModId, SourceId, provider);
                _registered = true;
                _baselibSupportsForecastInterop = true;
                RitsuLibFramework.Logger.Info("[HealthBarForecast] Registered BaseLib bridge provider.");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[HealthBarForecast] Failed to register BaseLib bridge provider: {ex}");
            }
        }

        private static IEnumerable<object> GetSegmentsForCreature(Creature creature)
        {
            return HealthBarForecastRegistry.GetSegments(creature)
                .Select(registered => (object)registered.Segment)
                .ToArray();
        }

        private static Type? ResolveBaseLibRegistryType()
        {
            var registryType = ResolveRegistryTypeFromLoadedAssemblies();
            _baselibSupportsForecastInterop = registryType != null;

            if (!_baselibSupportsForecastInterop)
            {
                if (_loggedMissingInterop)
                    return null;
                _loggedMissingInterop = true;
                RitsuLibFramework.Logger.Info(
                    "[HealthBarForecast] BaseLib detected but forecast interop API is unavailable.");
                return null;
            }

            _loggedMissingInterop = false;

            return registryType;
        }

        private static Type? ResolveRegistryTypeFromLoadedAssemblies()
        {
            var byQualifiedName = ExternalFrameworkRegistry.ResolveType("BaseLib.Hooks.HealthBarForecastRegistry");
            if (byQualifiedName != null)
                return byQualifiedName;

            var loadedWithAssembly = Sts2ModManagerCompat.EnumerateLoadedModsWithAssembly();
            foreach (var mod in loadedWithAssembly)
            {
                var assembly = mod.assembly;
                if (assembly == null)
                    continue;

                var type = assembly.GetType("BaseLib.Hooks.HealthBarForecastRegistry");
                if (type != null)
                    return type;
            }

            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("BaseLib.Hooks.HealthBarForecastRegistry")).OfType<Type>()
                .FirstOrDefault();
        }
    }
}
