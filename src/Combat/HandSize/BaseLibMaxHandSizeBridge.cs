using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.HandSize
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Bridges BaseLib's maximum-hand-size support by detecting its active patches, extending its calculator
    ///         with RitsuLib modifiers, and using its result as the base value when available.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         桥接 BaseLib 的手牌上限支持：检测其生效中的补丁，将 RitsuLib 修正接入其计算器，并在可用时以
    ///         BaseLib 的结果为基础值。
    ///     </para>
    /// </summary>
    internal static class BaseLibMaxHandSizeBridge
    {
        private static readonly Lock Gate = new();
        private static readonly Harmony Harmony = new($"{Const.ModId}.interop.max_hand_size");
        [ThreadStatic] private static int _suppressPostfixDepth;

        private static MethodInfo? _baseLibGetMaxHandSizeMethod;
        private static MethodInfo? _baseLibGetMaxHandSizeWithBaseMethod;
        private static MethodInfo? _baseLibGetMaxHandSizeFromCardMethod;
        private static Func<Player, int>? _baseLibGetMaxHandSize;
        private static int _failedMethodResolveVersion = -1;
        private static int _failedWithBaseResolveVersion = -1;
        private static int _failedFromCardResolveVersion = -1;
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
                   || IsSetupPlayerTurnPatchedByBaseLib()
                   || IsPatchedByBaseLib(new(typeof(CardConsoleCmd), nameof(CardConsoleCmd.Process),
                       [typeof(Player), typeof(string[])]));
        }

        private static bool IsSetupPlayerTurnPatchedByBaseLib()
        {
#if STS2_AT_LEAST_0_110_0
            return IsPatchedByBaseLib(new(typeof(CombatManager), nameof(CombatManager.SetupPlayerTurn),
                [typeof(CombatTurnState), typeof(Player), typeof(HookPlayerChoiceContext)], MethodType.Async));
#else
            return IsPatchedByBaseLib(new(typeof(CombatManager), nameof(CombatManager.SetupPlayerTurn),
                [typeof(Player), typeof(HookPlayerChoiceContext)]));
#endif
        }

        internal static bool IsBaseLibBaseAmountConsumer(CodeInstruction instruction)
        {
            return instruction.operand is MethodInfo method
                   && (IsSameMethod(method, TryResolveBaseLibGetMaxHandSizeWithBaseMethod())
                       || IsSameMethod(method, TryResolveBaseLibGetMaxHandSizeFromCardMethod()));
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
            if (Volatile.Read(ref _postfixPatched))
                return;

            lock (Gate)
            {
                if (Volatile.Read(ref _postfixPatched))
                    return;
                var getMaxMethod = TryResolveBaseLibGetMaxHandSizeWithBaseMethod();
                if (getMaxMethod == null)
                    return;

                var postfix = AccessTools.Method(typeof(BaseLibMaxHandSizeBridge),
                    nameof(BaseLibGetMaxHandSizePostfix));
                if (postfix == null)
                    throw new MissingMethodException(typeof(BaseLibMaxHandSizeBridge).FullName,
                        nameof(BaseLibGetMaxHandSizePostfix));

                Harmony.Patch(getMaxMethod, postfix: new(postfix));
                Volatile.Write(ref _postfixPatched, true);
                RitsuLibFramework.Logger.Info("[MaxHandSize] BaseLib hand-size bridge postfix installed.");
            }
        }

        private static bool TryGetBaseLibGetMaxHandSizeDelegate(out Func<Player, int> getter)
        {
            var cached = Volatile.Read(ref _baseLibGetMaxHandSize);
            if (cached != null)
            {
                getter = cached;
                return true;
            }

            if (Volatile.Read(ref _failedMethodResolveVersion) == ExternalFrameworkRegistry.AssemblyVersion)
            {
                getter = null!;
                return false;
            }

            lock (Gate)
            {
                cached = Volatile.Read(ref _baseLibGetMaxHandSize);
                if (cached != null)
                {
                    getter = cached;
                    return true;
                }

                if (!TryResolveBaseLibGetMaxHandSizeMethod(out var method))
                {
                    getter = null!;
                    return false;
                }

                _ = TryResolveBaseLibGetMaxHandSizeWithBaseMethod();
                var created = method.CreateDelegate<Func<Player, int>>();
                Volatile.Write(ref _baseLibGetMaxHandSize, created);
                getter = created;
                return true;
            }
        }

        private static bool TryResolveBaseLibGetMaxHandSizeMethod(out MethodInfo method)
        {
            var cached = Volatile.Read(ref _baseLibGetMaxHandSizeMethod);
            if (cached != null)
            {
                method = cached;
                return true;
            }

            var assemblyVersion = ExternalFrameworkRegistry.AssemblyVersion;
            if (Volatile.Read(ref _failedMethodResolveVersion) == assemblyVersion)
            {
                method = null!;
                return false;
            }

            lock (Gate)
            {
                cached = Volatile.Read(ref _baseLibGetMaxHandSizeMethod);
                if (cached != null)
                {
                    method = cached;
                    return true;
                }

                assemblyVersion = ExternalFrameworkRegistry.AssemblyVersion;
                if (Volatile.Read(ref _failedMethodResolveVersion) == assemblyVersion)
                {
                    method = null!;
                    return false;
                }

                var type = ResolveBaseLibMaxHandSizePatchType();
                if (type == null && !ExternalFrameworkRegistry.IsFrameworkPresent(ExternalFrameworkIds.BaseLib))
                {
                    Volatile.Write(ref _failedMethodResolveVersion, assemblyVersion);
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
                    Volatile.Write(ref _failedMethodResolveVersion, assemblyVersion);
                    if (!_loggedResolveFailure)
                    {
                        _loggedResolveFailure = true;
                        RitsuLibFramework.Logger.Info(
                            "[MaxHandSize] BaseLib hand-size calculator not found. Using RitsuLib-only path.");
                    }

                    method = null!;
                    return false;
                }

                Volatile.Write(ref _baseLibGetMaxHandSizeMethod, resolved);
                method = resolved;
                return true;
            }
        }

        private static MethodInfo? TryResolveBaseLibGetMaxHandSizeWithBaseMethod()
        {
            var cached = Volatile.Read(ref _baseLibGetMaxHandSizeWithBaseMethod);
            if (cached != null)
                return cached;

            var assemblyVersion = ExternalFrameworkRegistry.AssemblyVersion;
            if (Volatile.Read(ref _failedWithBaseResolveVersion) == assemblyVersion)
                return null;

            lock (Gate)
            {
                cached = Volatile.Read(ref _baseLibGetMaxHandSizeWithBaseMethod);
                if (cached != null)
                    return cached;

                assemblyVersion = ExternalFrameworkRegistry.AssemblyVersion;
                if (Volatile.Read(ref _failedWithBaseResolveVersion) == assemblyVersion)
                    return null;

                var type = ResolveBaseLibMaxHandSizePatchType();
                var resolved = type?.GetMethod(
                    "GetMaxHandSize",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(Player), typeof(int)],
                    null);

                if (resolved == null)
                {
                    Volatile.Write(ref _failedWithBaseResolveVersion, assemblyVersion);
                    return null;
                }

                PatchInliningGuard.Prepare(resolved);
                Volatile.Write(ref _baseLibGetMaxHandSizeWithBaseMethod, resolved);
                return resolved;
            }
        }

        private static MethodInfo? TryResolveBaseLibGetMaxHandSizeFromCardMethod()
        {
            var cached = Volatile.Read(ref _baseLibGetMaxHandSizeFromCardMethod);
            if (cached != null)
                return cached;

            var assemblyVersion = ExternalFrameworkRegistry.AssemblyVersion;
            if (Volatile.Read(ref _failedFromCardResolveVersion) == assemblyVersion)
                return null;

            lock (Gate)
            {
                cached = Volatile.Read(ref _baseLibGetMaxHandSizeFromCardMethod);
                if (cached != null)
                    return cached;

                assemblyVersion = ExternalFrameworkRegistry.AssemblyVersion;
                if (Volatile.Read(ref _failedFromCardResolveVersion) == assemblyVersion)
                    return null;

                var type = ResolveBaseLibMaxHandSizePatchType();
                var resolved = type?.GetMethod(
                    "GetMaxHandSizeFromCard",
                    BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    [typeof(CardModel), typeof(int)],
                    null);

                if (resolved == null)
                {
                    Volatile.Write(ref _failedFromCardResolveVersion, assemblyVersion);
                    return null;
                }

                Volatile.Write(ref _baseLibGetMaxHandSizeFromCardMethod, resolved);
                return resolved;
            }
        }

        private static bool IsSameMethod(MethodInfo method, MethodInfo? candidate)
        {
            return candidate != null
                   && method.Module == candidate.Module
                   && method.MetadataToken == candidate.MetadataToken;
        }

        private static Type? ResolveBaseLibMaxHandSizePatchType()
        {
            return ExternalFrameworkRegistry.ResolveType("BaseLib.Patches.Hooks.MaxHandSizePatch");
        }

        private static void BaseLibGetMaxHandSizePostfix(Player player, ref int __result)
        {
            if (_suppressPostfixDepth > 0)
                return;

            __result = MaxHandSizeCalculator.ApplyHookListenerModifiers(player, __result);
        }
    }
}
