using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.HandSize
{
    /// <summary>
    ///     Bridge for BaseLib max-hand-size capability:
    ///     1) detect whether BaseLib hand-size patches are active;
    ///     2) patch BaseLib calculator postfix so both libraries share modifier data;
    ///     3) resolve BaseLib value as authoritative source when available.
    /// </summary>
    internal static class BaseLibMaxHandSizeBridge
    {
        private static readonly Lock Gate = new();
        private static readonly Harmony Harmony = new($"{Const.ModId}.interop.max_hand_size");
        [ThreadStatic] private static int _suppressPostfixDepth;

        private static MethodInfo? _baseLibGetMaxHandSizeMethod;
        private static Func<Player, int>? _baseLibGetMaxHandSize;
        private static bool _postfixPatched;
        private static bool _loggedResolveFailure;

        internal static void TryInitialize()
        {
            EnsureBaseLibPostfixPatched();
        }

        internal static bool IsBaseLibHandSizePatchActive()
        {
            if (!TryResolveBaseLibGetMaxHandSizeMethod(out _))
                return false;

            return IsPatchedByBaseLib(new(typeof(CardPileCmd),
                       nameof(CardPileCmd.CheckIfDrawIsPossibleAndShowThoughtBubbleIfNot), [typeof(Player)]))
                   || IsPatchedByBaseLib(new(typeof(CombatManager), nameof(CombatManager.SetupPlayerTurn),
                       [typeof(Player), typeof(HookPlayerChoiceContext)]))
                   || IsPatchedByBaseLib(new(typeof(CardConsoleCmd), nameof(CardConsoleCmd.Process),
                       [typeof(Player), typeof(string[])]));
        }

        internal static bool TryGetMaxHandSizeFromBaseLib(Player player, out int amount)
        {
            amount = 0;
            if (!TryGetBaseLibGetMaxHandSizeDelegate(out var getter))
                return false;

            EnsureBaseLibPostfixPatched();

            try
            {
                _suppressPostfixDepth++;
                amount = getter(player);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[MaxHandSize] BaseLib bridge invocation failed: {ex.Message}");
            }
            finally
            {
                _suppressPostfixDepth = Math.Max(0, _suppressPostfixDepth - 1);
            }

            return false;
        }

        private static bool IsPatchedByBaseLib(ModPatchTarget target)
        {
            var method = PatchTargetMethodResolver.Resolve(target);
            if (method == null)
                return false;

            var patchInfo = Harmony.GetPatchInfo(method);
            if (patchInfo == null)
                return false;

            return patchInfo.Prefixes.Any(p => p.owner == Const.BaseLibHarmonyId)
                   || patchInfo.Postfixes.Any(p => p.owner == Const.BaseLibHarmonyId)
                   || patchInfo.Transpilers.Any(p => p.owner == Const.BaseLibHarmonyId)
                   || patchInfo.Finalizers.Any(p => p.owner == Const.BaseLibHarmonyId);
        }

        private static void EnsureBaseLibPostfixPatched()
        {
            lock (Gate)
            {
                if (_postfixPatched)
                    return;
                if (!TryResolveBaseLibGetMaxHandSizeMethod(out var getMaxMethod))
                    return;

                var postfix = AccessTools.Method(typeof(BaseLibMaxHandSizeBridge),
                    nameof(BaseLibGetMaxHandSizePostfix));
                if (postfix == null)
                    throw new MissingMethodException(typeof(BaseLibMaxHandSizeBridge).FullName,
                        nameof(BaseLibGetMaxHandSizePostfix));

                Harmony.Patch(getMaxMethod, postfix: new(postfix));
                _postfixPatched = true;
                RitsuLibFramework.Logger.Info("[MaxHandSize] BaseLib hand-size bridge postfix installed.");
            }
        }

        private static bool TryGetBaseLibGetMaxHandSizeDelegate(out Func<Player, int> getter)
        {
            lock (Gate)
            {
                if (_baseLibGetMaxHandSize != null)
                {
                    getter = _baseLibGetMaxHandSize;
                    return true;
                }

                if (!TryResolveBaseLibGetMaxHandSizeMethod(out var method))
                {
                    getter = null!;
                    return false;
                }

                _baseLibGetMaxHandSize = method.CreateDelegate<Func<Player, int>>();
                getter = _baseLibGetMaxHandSize;
                return true;
            }
        }

        private static bool TryResolveBaseLibGetMaxHandSizeMethod(out MethodInfo method)
        {
            lock (Gate)
            {
                if (_baseLibGetMaxHandSizeMethod != null)
                {
                    method = _baseLibGetMaxHandSizeMethod;
                    return true;
                }

                var type = ResolveBaseLibMaxHandSizePatchType();
                if (type == null && !ExternalFrameworkRegistry.IsFrameworkPresent(ExternalFrameworkIds.BaseLib))
                {
                    method = null!;
                    return false;
                }

                var resolved = type?.GetMethod(
                    "GetMaxHandSize",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(Player)],
                    null);

                if (resolved == null)
                {
                    if (!_loggedResolveFailure)
                    {
                        _loggedResolveFailure = true;
                        RitsuLibFramework.Logger.Info(
                            "[MaxHandSize] BaseLib hand-size calculator not found. Using RitsuLib-only path.");
                    }

                    method = null!;
                    return false;
                }

                _baseLibGetMaxHandSizeMethod = resolved;
                method = resolved;
                return true;
            }
        }

        private static Type? ResolveBaseLibMaxHandSizePatchType()
        {
            var byQualifiedName = ExternalFrameworkRegistry.ResolveType("BaseLib.Patches.Hooks.MaxHandSizePatch");
            if (byQualifiedName != null)
                return byQualifiedName;

            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("BaseLib.Patches.Hooks.MaxHandSizePatch"))
                .OfType<Type>()
                .FirstOrDefault();
        }

        // ReSharper disable InconsistentNaming
        private static void BaseLibGetMaxHandSizePostfix(Player player, ref int __result)
            // ReSharper restore InconsistentNaming
        {
            if (_suppressPostfixDepth > 0)
                return;

            __result = MaxHandSizeCalculator.ApplyHookListenerModifiers(player, __result);
        }
    }
}
