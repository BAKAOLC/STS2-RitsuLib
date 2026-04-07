using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline
{
    /// <summary>
    ///     When BaseLib is loaded, registers Ritsu's <see cref="ModCardHandOutlineRegistry.EvaluateBest" /> with BaseLib's
    ///     <c>ModCardHandOutlineRegistry.RegisterForeign</c> so one merged evaluator drives hand outlines.
    /// </summary>
    internal static class BaseLibModCardHandOutlineBridge
    {
        private const string SourceId = "ritsulib.registry";
        private static bool _registered;
        private static bool _baselibSupportsHandOutlineInterop;
        private static bool _loggedMissingInterop;
        private static bool _loggedMissingRegisterForeign;
        private static bool _loggedMissingRegisterForeignDynamic;
        private static bool _primaryAttemptIssued;
        private static bool _secondaryAttemptIssued;
        private static MethodInfo? _tryRefreshOutlineMethod;

        /// <summary>
        ///     When <see langword="true" />, Ritsu's <c>NHandCardHolder</c> outline postfixes should skip because BaseLib
        ///     already applies merged rules.
        /// </summary>
        public static bool ShouldRitsuHandOutlineStandDown()
        {
            return _registered && _baselibSupportsHandOutlineInterop;
        }

        /// <summary>
        ///     Attempts foreign registration during framework initialization (after BaseLib patches are active).
        /// </summary>
        public static void TryRegister()
        {
            TryRegisterPrimary();
        }

        /// <summary>
        ///     Attempts foreign registration from framework init (early load path).
        /// </summary>
        public static void TryRegisterPrimary()
        {
            if (_primaryAttemptIssued || _registered)
                return;
            _primaryAttemptIssued = true;
            TryRegisterCore();
        }

        /// <summary>
        ///     Attempts foreign registration from hand-outline postfix if <see cref="TryRegisterPrimary" /> ran before BaseLib
        ///     loaded.
        /// </summary>
        public static void TryRegisterSecondary()
        {
            if (_secondaryAttemptIssued || _registered)
                return;
            _secondaryAttemptIssued = true;
            TryRegisterCore();
        }

        /// <summary>
        ///     When bridged, forwards <see cref="ModCardHandOutlineRegistry.TryRefreshOutlineForHolder" /> to BaseLib.
        /// </summary>
        public static bool TryDelegatedRefresh(NHandCardHolder? holder)
        {
            if (!ShouldRitsuHandOutlineStandDown() || holder == null)
                return false;

            var mi = _tryRefreshOutlineMethod;
            if (mi == null)
                return false;

            try
            {
                return mi.Invoke(null, [holder]) is true;
            }
            catch
            {
                return false;
            }
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

                var tupleType = typeof(ValueTuple<Color, int, bool>);
                var nullableTupleType = typeof(Nullable<>).MakeGenericType(tupleType);
                var foreignFuncType = typeof(Func<,>).MakeGenericType(typeof(CardModel), nullableTupleType);
                var dynamicTupleType = typeof(ValueTuple<Func<Color>, int, bool>);
                var dynamicNullableTupleType = typeof(Nullable<>).MakeGenericType(dynamicTupleType);
                var foreignDynamicFuncType =
                    typeof(Func<,>).MakeGenericType(typeof(CardModel), dynamicNullableTupleType);

                var registerForeign = registryType.GetMethod(
                    "RegisterForeign",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(string), typeof(string), foreignFuncType],
                    null);
                var registerForeignDynamic = registryType.GetMethod(
                    "RegisterForeignDynamic",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(string), typeof(string), foreignDynamicFuncType],
                    null);

                if (registerForeign == null && registerForeignDynamic == null)
                {
                    _baselibSupportsHandOutlineInterop = false;
                    if (_loggedMissingRegisterForeign)
                        return;
                    _loggedMissingRegisterForeign = true;
                    RitsuLibFramework.Logger.Warn(
                        "[ModCardHandOutline] BaseLib registry type '" + registryType.FullName +
                        "' does not expose RegisterForeign(string, string, Func<CardModel, (Color,int,bool)?>); interop unavailable.");

                    return;
                }

                _tryRefreshOutlineMethod = registryType.GetMethod(
                    "TryRefreshOutlineForHolder",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(NHandCardHolder)],
                    null);

                var bridgeMethod = typeof(BaseLibModCardHandOutlineBridge).GetMethod(
                    nameof(EvaluateForeignTuple),
                    BindingFlags.NonPublic | BindingFlags.Static);
                var dynamicBridgeMethod = typeof(BaseLibModCardHandOutlineBridge).GetMethod(
                    nameof(EvaluateForeignDynamicTuple),
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (registerForeignDynamic != null && dynamicBridgeMethod != null)
                {
                    var dynamicDel = Delegate.CreateDelegate(foreignDynamicFuncType, dynamicBridgeMethod);
                    registerForeignDynamic.Invoke(null, [Const.ModId, SourceId, dynamicDel]);
                }
                else
                {
                    if (registerForeign == null || bridgeMethod == null)
                        return;
                    var del = Delegate.CreateDelegate(foreignFuncType, bridgeMethod);
                    registerForeign.Invoke(null, [Const.ModId, SourceId, del]);
                    if (!_loggedMissingRegisterForeignDynamic)
                    {
                        _loggedMissingRegisterForeignDynamic = true;
                        RitsuLibFramework.Logger.Info(
                            "[ModCardHandOutline] BaseLib does not expose RegisterForeignDynamic; using static foreign bridge.");
                    }
                }

                _registered = true;
                _baselibSupportsHandOutlineInterop = true;
                RitsuLibFramework.Logger.Info("[ModCardHandOutline] Registered BaseLib bridge provider.");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModCardHandOutline] Failed to register BaseLib bridge provider: {ex}");
            }
        }

        private static (Color Color, int Priority, bool VisibleWhenUnplayable)? EvaluateForeignTuple(CardModel model)
        {
            var e = ModCardHandOutlineRegistry.EvaluateBest(model);
            if (e == null)
                return null;

            return (e.Value.ResolveColor(model), e.Value.Priority, e.Value.VisibleWhenUnplayable);
        }

        private static (Func<Color> ResolveColor, int Priority, bool VisibleWhenUnplayable)?
            EvaluateForeignDynamicTuple(
                CardModel model)
        {
            var e = ModCardHandOutlineRegistry.EvaluateBest(model);
            if (e == null)
                return null;

            return (() => e.Value.ResolveColor(model), e.Value.Priority, e.Value.VisibleWhenUnplayable);
        }

        private static Type? ResolveBaseLibRegistryType()
        {
            var registryType = ResolveRegistryTypeFromLoadedAssemblies();
            _baselibSupportsHandOutlineInterop = registryType != null;

            if (!_baselibSupportsHandOutlineInterop)
            {
                if (_loggedMissingInterop)
                    return null;
                _loggedMissingInterop = true;
                RitsuLibFramework.Logger.Info(
                    "[ModCardHandOutline] BaseLib detected but hand-outline interop API is unavailable.");
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
                if (assembly.GetType("BaseLib.Hooks.ModCardHandOutlineRegistry") != null)
                    return true;
            }

            return false;
        }

        private static Type? ResolveRegistryTypeFromLoadedAssemblies()
        {
            var byQualifiedName = Type.GetType("BaseLib.Hooks.ModCardHandOutlineRegistry, BaseLib");
            if (byQualifiedName != null)
                return byQualifiedName;

            foreach (var mod in Sts2ModManagerCompat.EnumerateLoadedModsWithAssembly())
            {
                var assembly = mod.assembly;
                if (assembly == null)
                    continue;

                var type = assembly.GetType("BaseLib.Hooks.ModCardHandOutlineRegistry");
                if (type != null)
                    return type;
            }

            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("BaseLib.Hooks.ModCardHandOutlineRegistry")).OfType<Type>()
                .FirstOrDefault();
        }
    }
}
