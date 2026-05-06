using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     When BaseLib is loaded, registers <see cref="HealthBarVisualGraftRegistry.Aggregate" /> with BaseLib's
    ///     <c>HealthBarVisualGraftRegistry.RegisterForeign</c> so a single consumer can merge Ritsu graft metrics.
    /// </summary>
    internal static class BaseLibVisualGraftBridge
    {
        private const string SourceId = "ritsulib.visual_graft_registry";
        private static bool _registered;
        private static bool _interopOk;
        private static bool _loggedMissingRegistry;
        private static bool _loggedMissingRegisterForeign;
        private static Action<string, string, Func<Creature, object>>? _registerForeign;

        public static bool ShouldRitsuGraftStandDown()
        {
            return _registered && _interopOk;
        }

        public static void TryRegisterPrimary()
        {
            if (_registered)
                return;
            TryRegisterCore();
        }

        public static void TryRegisterSecondary()
        {
            if (_registered)
                return;
            TryRegisterCore();
        }

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
                    [typeof(string), typeof(string), typeof(Func<Creature, object>)],
                    null);

                if (registerForeign == null)
                {
                    _interopOk = false;
                    if (_loggedMissingRegisterForeign)
                        return;
                    _loggedMissingRegisterForeign = true;
                    RitsuLibFramework.Logger.Warn(
                        "[HealthBarGraft] BaseLib registry type does not expose " +
                        "RegisterForeign(string, string, Func<Creature, object>).");
                    return;
                }

                static object Handler(Creature c)
                {
                    return HealthBarVisualGraftRegistry.Aggregate(c);
                }

                _registerForeign ??= registerForeign.CreateDelegate<Action<string, string, Func<Creature, object>>>();
                _registerForeign(Const.ModId, SourceId, Handler);
                _registered = true;
                _interopOk = true;
                RitsuLibFramework.Logger.Info("[HealthBarGraft] Registered BaseLib visual graft bridge.");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[HealthBarGraft] Failed to register BaseLib bridge: {ex}");
            }
        }

        private static Type? ResolveBaseLibRegistryType()
        {
            var byQualifiedName = ExternalFrameworkRegistry.ResolveType("BaseLib.Hooks.HealthBarVisualGraftRegistry");
            if (byQualifiedName != null)
            {
                _interopOk = true;
                return byQualifiedName;
            }

            foreach (var mod in Sts2ModManagerCompat.EnumerateLoadedModsWithAssembly())
            {
                var assembly = mod.assembly;
                if (assembly == null)
                    continue;

                var type = assembly.GetType("BaseLib.Hooks.HealthBarVisualGraftRegistry");
                if (type == null) continue;
                _interopOk = true;
                return type;
            }

            if (!_loggedMissingRegistry)
            {
                _loggedMissingRegistry = true;
                RitsuLibFramework.Logger.Info("[HealthBarGraft] BaseLib graft registry type not found.");
            }

            var fallback = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("BaseLib.Hooks.HealthBarVisualGraftRegistry")).OfType<Type>()
                .FirstOrDefault();
            if (fallback != null)
                _interopOk = true;
            return fallback;
        }
    }
}
