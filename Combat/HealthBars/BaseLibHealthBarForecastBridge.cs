using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     When BaseLib is loaded, registers <see cref="HealthBarForecastRegistry.GetSegments" /> with BaseLib's
    ///     <c>HealthBarForecastRegistry.RegisterForeign</c> so a single renderer can consume Ritsu-typed segments.
    /// </summary>
    /// <remarks>
    ///     <see cref="ShouldRitsuRendererStandDown" /> becomes true after a successful bridge so duplicate overlays are
    ///     not drawn.
    /// </remarks>
    internal static class BaseLibHealthBarForecastBridge
    {
        private const string SourceId = "ritsulib.registry";
        private static bool _registered;
        private static bool _baselibSupportsForecastInterop;
        private static bool _loggedMissingInterop;
        private static bool _loggedMissingRegisterForeign;
        private static bool _primaryAttemptIssued;
        private static bool _secondaryAttemptIssued;

        /// <summary>
        ///     When <see langword="true" />, Ritsu's <c>NHealthBar</c> forecast postfixes should skip drawing because BaseLib
        ///     already merged this mod's segments.
        /// </summary>
        public static bool ShouldRitsuRendererStandDown()
        {
            return _registered && _baselibSupportsForecastInterop;
        }

        /// <summary>
        ///     Attempts foreign registration from <c>NHealthBar._Ready</c> (early load path).
        /// </summary>
        public static void TryRegisterPrimary()
        {
            if (_primaryAttemptIssued || _registered)
                return;
            _primaryAttemptIssued = true;
            TryRegisterCore();
        }

        /// <summary>
        ///     Attempts foreign registration from forecast render path if <see cref="TryRegisterPrimary" /> did not run yet.
        /// </summary>
        public static void TryRegisterSecondary()
        {
            if (_secondaryAttemptIssued || _registered)
                return;
            _secondaryAttemptIssued = true;
            TryRegisterCore();
        }

        /// <summary>
        ///     Alias for <see cref="TryRegisterPrimary" />.
        /// </summary>
        public static void TryRegister()
        {
            TryRegisterPrimary();
        }

        private static void TryRegisterCore()
        {
            if (_registered)
                return;
            if (!IsBaseLibLoaded())
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
                    if (!_loggedMissingRegisterForeign)
                    {
                        _loggedMissingRegisterForeign = true;
                        RitsuLibFramework.Logger.Warn(
                            $"[HealthBarForecast] BaseLib registry type '{registryType.FullName}' does not expose " +
                            "RegisterForeign(string, string, Func<Creature, IEnumerable<object>>); forecast interop unavailable.");
                    }

                    return;
                }

                var provider = GetSegmentsForCreature;
                registerForeign.Invoke(null, [Const.ModId, SourceId, provider]);
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

        private static bool IsBaseLibLoaded()
        {
            foreach (var mod in Sts2ModManagerCompat.EnumerateLoadedModsWithAssembly())
            {
                var assembly = mod.assembly;
                if (assembly == null)
                    continue;
                if (assembly.GetType("BaseLib.Hooks.HealthBarForecastRegistry") != null)
                    return true;
            }

            return false;
        }

        private static Type? ResolveRegistryTypeFromLoadedAssemblies()
        {
            var byQualifiedName = Type.GetType("BaseLib.Hooks.HealthBarForecastRegistry, BaseLib");
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

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("BaseLib.Hooks.HealthBarForecastRegistry");
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}
