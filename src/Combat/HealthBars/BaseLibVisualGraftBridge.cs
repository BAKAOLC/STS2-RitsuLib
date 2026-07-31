using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Bridges <see cref="HealthBarVisualGraftRegistry.Aggregate" /> to BaseLib's
    ///         <c>HealthBarVisualGraftRegistry.RegisterForeign</c> API so a single renderer can consume both libraries'
    ///         visual-extension metrics.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将 <see cref="HealthBarVisualGraftRegistry.Aggregate" /> 桥接到 BaseLib 的
    ///         <c>HealthBarVisualGraftRegistry.RegisterForeign</c> API，使同一个渲染器能够处理两个库的
    ///         生命条视觉扩展参数。
    ///     </para>
    /// </summary>
    internal static class BaseLibVisualGraftBridge
    {
        private const string SourceId = "ritsulib.visual_graft_registry";
        private static readonly Lock Gate = new();
        private static bool _registered;
        private static bool _interopOk;
        private static bool _loggedMissingRegistry;
        private static bool _loggedMissingRegisterForeign;
        private static Action<string, string, Func<Creature, object>>? _registerForeign;

        public static bool ShouldRitsuGraftStandDown()
        {
            lock (Gate)
            {
                return _registered && _interopOk;
            }
        }

        public static void TryRegisterPrimary()
        {
            TryRegisterCore();
        }

        public static void TryRegisterSecondary()
        {
            TryRegisterCore();
        }

        public static void TryRegister()
        {
            TryRegisterPrimary();
        }

        private static void TryRegisterCore()
        {
            lock (Gate)
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

                    _registerForeign ??=
                        registerForeign.CreateDelegate<Action<string, string, Func<Creature, object>>>();
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
        }

        private static Type? ResolveBaseLibRegistryType()
        {
            var registryType = ExternalFrameworkRegistry.ResolveType("BaseLib.Hooks.HealthBarVisualGraftRegistry");
            if (registryType != null)
                return registryType;

            if (!_loggedMissingRegistry)
            {
                _loggedMissingRegistry = true;
                RitsuLibFramework.Logger.Info("[HealthBarGraft] BaseLib graft registry type not found.");
            }

            _interopOk = false;
            return null;
        }
    }
}
