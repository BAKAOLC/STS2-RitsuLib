using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Builders;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.Combat.HandSize
{
    /// <summary>
    ///     Installs RitsuLib max-hand-size patches.
    ///     安装 RitsuLib 最大手牌数补丁。
    /// </summary>
    internal static class MaxHandSizePatchInstaller
    {
        private const int DefaultMaxHandSize = 10;
        private static readonly Lock Gate = new();
        private static bool _patched;

        private static readonly MethodInfo GetMaxHandSizeMethod =
            AccessTools.Method(typeof(MaxHandSizeCalculator), nameof(MaxHandSizeCalculator.Calculate))
            ?? throw new MissingMethodException(typeof(MaxHandSizeCalculator).FullName,
                nameof(MaxHandSizeCalculator.Calculate));

        private static readonly MethodInfo GetMaxHandSizeFromCardMethod =
            AccessTools.Method(typeof(MaxHandSizeCalculator), nameof(MaxHandSizeCalculator.CalculateFromCardOwner))
            ?? throw new MissingMethodException(typeof(MaxHandSizeCalculator).FullName,
                nameof(MaxHandSizeCalculator.CalculateFromCardOwner));

#if STS2_AT_LEAST_0_104_0
        private static readonly MethodInfo MaxCardsInHandGetter =
            AccessTools.PropertyGetter(typeof(CardPile), nameof(CardPile.MaxCardsInHand))
            ?? throw new MissingMethodException(typeof(CardPile).FullName, nameof(CardPile.MaxCardsInHand));
#endif

        internal static void EnsurePatched()
        {
            lock (Gate)
            {
                if (_patched)
                    return;

                var builder = new DynamicPatchBuilder("max_hand_size");
                var transpilerPlayerArg0 =
                    FromMethodAfterBaseLib(nameof(PlayerArg0Transpiler));
                var transpilerPlayerArg1 =
                    FromMethodAfterBaseLib(nameof(PlayerArg1Transpiler));
                var transpilerStateMachine =
                    FromMethodAfterBaseLib(nameof(StateMachineTranspiler));
                var cardOnPlayTranspiler =
                    FromMethodAfterBaseLib(nameof(CardOnPlayTranspiler));

                TryAddMethodPatch(builder, typeof(CardPileCmd),
                    nameof(CardPileCmd.CheckIfDrawIsPossibleAndShowThoughtBubbleIfNot),
                    [typeof(Player)], transpilerPlayerArg0);
                TryAddMethodPatch(builder, typeof(CombatManager), nameof(CombatManager.SetupPlayerTurn),
                    [typeof(Player), typeof(HookPlayerChoiceContext)], transpilerPlayerArg1);
                TryAddMethodPatch(builder, typeof(CardConsoleCmd), nameof(CardConsoleCmd.Process),
                    [typeof(Player), typeof(string[])], transpilerPlayerArg1);

                TryAddAsyncMoveNextPatch(builder, AccessTools.Method(typeof(CardPileCmd), nameof(CardPileCmd.Draw),
                        [typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool)]),
                    transpilerStateMachine, "Patch CardPileCmd.Draw state machine max-hand-size constants");
                TryAddAsyncMoveNextPatch(builder, AccessTools.Method(typeof(CardPileCmd), nameof(CardPileCmd.Add),
                    [
                        typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition),
                        typeof(AbstractModel), typeof(bool),
                    ]),
                    transpilerStateMachine, "Patch CardPileCmd.Add state machine max-hand-size constants");

                TryAddAsyncMoveNextPatch(builder, AccessTools.Method(typeof(Scrawl), "OnPlay",
                        [typeof(PlayerChoiceContext), typeof(CardPlay)]),
                    cardOnPlayTranspiler, "Patch Scrawl.OnPlay hand-size constant");
                TryAddAsyncMoveNextPatch(builder, AccessTools.Method(typeof(Dredge), "OnPlay",
                        [typeof(PlayerChoiceContext), typeof(CardPlay)]),
                    cardOnPlayTranspiler, "Patch Dredge.OnPlay hand-size constant");
                TryAddAsyncMoveNextPatch(builder, AccessTools.Method(typeof(CrashLanding), "OnPlay",
                        [typeof(PlayerChoiceContext), typeof(CardPlay)]),
                    cardOnPlayTranspiler, "Patch CrashLanding.OnPlay hand-size constant");
                TryAddAsyncMoveNextPatch(builder, AccessTools.Method(typeof(Pillage), "OnPlay",
                        [typeof(PlayerChoiceContext), typeof(CardPlay)]),
                    cardOnPlayTranspiler, "Patch Pillage.OnPlay hand-size constant");

                TryAddMethodPatch(builder, typeof(NPlayerHand), nameof(NPlayerHand.StartCardPlay),
                    [typeof(NHandCardHolder), typeof(bool)],
                    DynamicPatchBuilder.FromMethod(typeof(MaxHandSizePatchInstaller),
                        nameof(StartCardPlayTranspiler)));

                TryAddMethodPatch(builder, typeof(HandPosHelper), nameof(HandPosHelper.GetPosition),
                    [typeof(int), typeof(int)], prefix: DynamicPatchBuilder.FromMethod(
                        typeof(MaxHandSizePatchInstaller),
                        nameof(GetPositionPrefix)));
                TryAddMethodPatch(builder, typeof(HandPosHelper), nameof(HandPosHelper.GetAngle),
                    [typeof(int), typeof(int)], prefix: DynamicPatchBuilder.FromMethod(
                        typeof(MaxHandSizePatchInstaller),
                        nameof(GetAnglePrefix)));
                TryAddMethodPatch(builder, typeof(HandPosHelper), nameof(HandPosHelper.GetScale),
                    [typeof(int)], prefix: DynamicPatchBuilder.FromMethod(typeof(MaxHandSizePatchInstaller),
                        nameof(GetScalePrefix)));

                if (!RitsuLibFramework.GetFrameworkPatcher(RitsuLibFramework.FrameworkPatcherArea.Core).ApplyDynamic(
                        builder))
                {
                    RitsuLibFramework.Logger.Warn("[MaxHandSize] Dynamic patch apply reported a critical failure.");
                    return;
                }

                _patched = true;
#if !STS2_AT_LEAST_0_104_0
                RitsuLibFramework.Logger.Info("[MaxHandSize] RitsuLib hand-size patch set installed (compat 0.103.2 profile).");
#elif !STS2_AT_LEAST_0_105_0
                RitsuLibFramework.Logger.Info(
                    "[MaxHandSize] RitsuLib hand-size patch set installed (compat 0.104.0 profile).");
#else
                RitsuLibFramework.Logger.Info(
                    "[MaxHandSize] RitsuLib hand-size patch set installed (modern profile).");
#endif
            }
        }

        private static HarmonyMethod FromMethodAfterBaseLib(string methodName)
        {
            var method = DynamicPatchBuilder.FromMethod(typeof(MaxHandSizePatchInstaller), methodName);
            method.after = [Const.BaseLibHarmonyId];
            return method;
        }

        private static void TryAddMethodPatch(
            DynamicPatchBuilder builder,
            Type targetType,
            string methodName,
            Type[] parameterTypes,
            HarmonyMethod? transpiler = null,
            HarmonyMethod? prefix = null)
        {
            try
            {
                builder.AddMethod(
                    targetType,
                    methodName,
                    parameterTypes,
                    prefix,
                    transpiler: transpiler,
                    isCritical: false,
                    description: $"Patch {targetType.Name}.{methodName} max-hand-size constants");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[MaxHandSize] Skipped patch '{targetType.Name}.{methodName}': {ex.Message}");
            }
        }

        private static void TryAddAsyncMoveNextPatch(
            DynamicPatchBuilder builder,
            MethodInfo? asyncMethod,
            HarmonyMethod transpiler,
            string description)
        {
            if (asyncMethod == null)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[MaxHandSize] Skip dynamic patch: missing async method for '{description}'.");
                return;
            }

            MethodBase? moveNext;
            try
            {
                moveNext = AccessTools.AsyncMoveNext(asyncMethod);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[MaxHandSize] Skip dynamic patch '{description}': resolve MoveNext failed: {ex.Message}");
                return;
            }

            if (moveNext == null)
            {
                RitsuLibFramework.Logger.Warn($"[MaxHandSize] Skip dynamic patch '{description}': MoveNext not found.");
                return;
            }

            builder.Add(
                moveNext,
                transpiler: transpiler,
                isCritical: false,
                description: description);
        }

        private static bool IsMaxHandSizeToken(CodeInstruction ins)
        {
#if !STS2_AT_LEAST_0_104_0
            return HarmonyIl.LoadsInt32(ins, DefaultMaxHandSize);
#else
            return HarmonyIl.LoadsInt32(ins, DefaultMaxHandSize)
                   || HarmonyIl.IsCall(MaxCardsInHandGetter)(ins);
#endif
        }

        private static bool IsBaseLibBaseAmountToken(IReadOnlyList<CodeInstruction> code, int index)
        {
            return index + 1 < code.Count
                   && IsMaxHandSizeToken(code[index])
                   && BaseLibMaxHandSizeBridge.IsBaseLibBaseAmountConsumer(code[index + 1]);
        }

        private static bool IsRitsuMaxHandSizeReplacementSite(IReadOnlyList<CodeInstruction> code, int index)
        {
            return IsMaxHandSizeToken(code[index]) && !IsBaseLibBaseAmountToken(code, index);
        }

        private static List<CodeInstruction> RewriteMaxHandSizeLoads(
            IEnumerable<CodeInstruction> instructions,
            Func<IReadOnlyList<CodeInstruction>, int, IReadOnlyList<CodeInstruction>> buildReplacement,
            string operation,
            MethodInfo alreadyInstalledCall)
        {
            var rewriter = HarmonyIlRewriter.From(instructions);
            var report = rewriter.ReplaceEach(
                operation,
                IsRitsuMaxHandSizeReplacementSite,
                buildReplacement,
                code => ContainsCall(code, alreadyInstalledCall));
            WarnIfRewriteUnsatisfied(report);
            return rewriter.InstructionsChecked(operation);
        }

        private static IEnumerable<CodeInstruction> PlayerArg0Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return RewriteMaxHandSizeLoads(instructions,
                static (_, _) => [HarmonyIl.Ldarg(0), HarmonyIl.Call(GetMaxHandSizeMethod)],
                "[MaxHandSize] PlayerArg0 max-hand-size replacement",
                GetMaxHandSizeMethod);
        }

        private static IEnumerable<CodeInstruction> PlayerArg1Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return RewriteMaxHandSizeLoads(instructions,
                static (_, _) => [HarmonyIl.Ldarg(1), HarmonyIl.Call(GetMaxHandSizeMethod)],
                "[MaxHandSize] PlayerArg1 max-hand-size replacement",
                GetMaxHandSizeMethod);
        }

        private static IEnumerable<CodeInstruction> StateMachineTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var rewriter = HarmonyIlRewriter.From(instructions);
            var loadPlayer = FindStateMachinePlayerLoad(rewriter);
            if (loadPlayer == null)
            {
                RitsuLibFramework.Logger.Warn(
                    "[MaxHandSize] State-machine transpiler could not resolve Player load pattern; skipped replacements.");
                return rewriter.Instructions();
            }

            var report = rewriter.ReplaceEach(
                "[MaxHandSize] State-machine max-hand-size replacement",
                IsRitsuMaxHandSizeReplacementSite,
                (_, _) => [.. HarmonyIl.CloneAll(loadPlayer), HarmonyIl.Call(GetMaxHandSizeMethod)],
                code => ContainsCall(code, GetMaxHandSizeMethod));
            WarnIfRewriteUnsatisfied(report);
            return rewriter.InstructionsChecked("[MaxHandSize] State-machine max-hand-size replacement");
        }

        private static IEnumerable<CodeInstruction> CardOnPlayTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var rewriter = HarmonyIlRewriter.From(instructions);
            var loadCard = FindStateMachineCardLoad(rewriter);
            if (loadCard == null)
            {
                RitsuLibFramework.Logger.Warn(
                    "[MaxHandSize] Card OnPlay transpiler could not resolve Card load pattern; skipped replacements.");
                return rewriter.Instructions();
            }

            var report = rewriter.ReplaceEach(
                "[MaxHandSize] Card OnPlay max-hand-size replacement",
                IsRitsuMaxHandSizeReplacementSite,
                (_, _) => [.. HarmonyIl.CloneAll(loadCard), HarmonyIl.Call(GetMaxHandSizeFromCardMethod)],
                code => ContainsCall(code, GetMaxHandSizeFromCardMethod));
            WarnIfRewriteUnsatisfied(report);
            return rewriter.InstructionsChecked("[MaxHandSize] Card OnPlay max-hand-size replacement");
        }

        private static IReadOnlyList<CodeInstruction>? FindStateMachinePlayerLoad(HarmonyIlRewriter rewriter)
        {
            return FindStateMachineFieldLoad(rewriter, field => field.FieldType == typeof(Player),
                "[MaxHandSize] state-machine Player field load");
        }

        private static IReadOnlyList<CodeInstruction>? FindStateMachineCardLoad(HarmonyIlRewriter rewriter)
        {
            return FindStateMachineFieldLoad(rewriter, field => typeof(CardModel).IsAssignableFrom(field.FieldType),
                "[MaxHandSize] state-machine CardModel field load");
        }

        private static IReadOnlyList<CodeInstruction>? FindStateMachineFieldLoad(
            HarmonyIlRewriter rewriter,
            Func<FieldInfo, bool> fieldPredicate,
            string description)
        {
            var pattern = HarmonyIlPattern.Sequence(HarmonyIl.IsLdarg(0), HarmonyIl.IsLdfld());
            return (from match in rewriter.FindMatches(pattern, description).Items
                    where fieldPredicate(match.GetFieldOperand(rewriter.Code, 1))
                    select (IReadOnlyList<CodeInstruction>?)
                        [match.InstructionAt(rewriter.Code, 0).Clone(), match.InstructionAt(rewriter.Code, 1).Clone()])
                .FirstOrDefault();
        }

        // ReSharper disable InconsistentNaming
        private static StringName GetShortcutOrDefault(NPlayerHand hand, int idx)
            // ReSharper restore InconsistentNaming
        {
            var arr = hand._selectCardShortcuts;
            return idx >= 0 && idx < arr.Length ? arr[idx] : MegaInput.releaseCard;
        }

        // ReSharper disable InconsistentNaming
        private static IEnumerable<CodeInstruction> StartCardPlayTranspiler(IEnumerable<CodeInstruction> instructions)
            // ReSharper restore InconsistentNaming
        {
            var selectCardShortcutsField = AccessTools.Field(typeof(NPlayerHand), "_selectCardShortcuts");
            var draggedHolderIndexField = AccessTools.Field(typeof(NPlayerHand), "_draggedHolderIndex");
            var getShortcutMethod = AccessTools.Method(typeof(MaxHandSizePatchInstaller), nameof(GetShortcutOrDefault));
            var rewriter = HarmonyIlRewriter.From(instructions);
            if (selectCardShortcutsField == null || draggedHolderIndexField == null || getShortcutMethod == null)
                return rewriter.Instructions();

            var pattern = HarmonyIlPattern.Sequence(
                HarmonyIl.IsLdarg(0),
                HarmonyIl.IsLdfld(selectCardShortcutsField),
                HarmonyIl.IsLdarg(0),
                HarmonyIl.IsLdfld(draggedHolderIndexField),
                HarmonyIl.Is(OpCodes.Ldelem_Ref));

            var report = rewriter.TryReplaceFirst(
                "[MaxHandSize] StartCardPlay shortcut bounds replacement",
                pattern,
                [
                    HarmonyIl.Ldarg(0),
                    HarmonyIl.Ldarg(0),
                    HarmonyIl.Ldfld(draggedHolderIndexField),
                    HarmonyIl.Call(getShortcutMethod),
                ],
                code => ContainsCall(code, getShortcutMethod));
            WarnIfRewriteUnsatisfied(report);
            WarnIfFirstMatchWasAmbiguous(report);

            return rewriter.InstructionsChecked("[MaxHandSize] StartCardPlay shortcut bounds replacement");
        }

        private static bool ContainsCall(IReadOnlyList<CodeInstruction> code, MethodInfo method)
        {
            return code.Any(HarmonyIl.IsCall(method));
        }

        private static void WarnIfRewriteUnsatisfied(HarmonyIlRewriteReport report)
        {
            if (report.Succeeded || report.Matches == 0)
                return;

            RitsuLibFramework.Logger.Warn(report.Describe());
        }

        private static void WarnIfFirstMatchWasAmbiguous(HarmonyIlRewriteReport report)
        {
            if (report.Matches <= 1 || report.Applied != 1)
                return;

            RitsuLibFramework.Logger.Warn(
                $"{report.Operation} found {report.Matches} matches; only the first was rewritten.");
        }

        private static float GetInferredHalfSpread(int handSize)
        {
            var capped = Math.Min(handSize, 14);
            var t = (capped - 10) / 4f;
            t = Mathf.Clamp(t, 0f, 1f);
            return Mathf.Lerp(610f, 690f, t);
        }

        // ReSharper disable InconsistentNaming
        private static bool GetPositionPrefix(int handSize, int cardIndex, ref Vector2 __result)
            // ReSharper restore InconsistentNaming
        {
            if (handSize <= 10)
                return true;

            if (cardIndex < 0 || cardIndex >= handSize)
                throw new ArgumentOutOfRangeException(nameof(cardIndex),
                    $"Card index {cardIndex} is outside hand size {handSize}.");

            var halfSpread = GetInferredHalfSpread(handSize);
            var edgeLift = Math.Max(72f, 88f - (handSize - 10) * 1.5f);
            var u = 2f * cardIndex / (handSize - 1f) - 1f;
            var x = halfSpread * u;
            var y = Math.Min(18f, -64f + edgeLift * u * u);
            __result = new(x, y);

            return false;
        }

        // ReSharper disable InconsistentNaming
        private static bool GetAnglePrefix(int handSize, int cardIndex, ref float __result)
            // ReSharper restore InconsistentNaming
        {
            if (handSize <= 10)
                return true;

            if (cardIndex < 0 || cardIndex >= handSize)
                throw new ArgumentOutOfRangeException(nameof(cardIndex),
                    $"Card index {cardIndex} is outside hand size {handSize}.");

            var halfSpread = GetInferredHalfSpread(handSize);
            var edgeLift = Math.Max(72f, 88f - (handSize - 10) * 1.5f);
            var u = 2f * cardIndex / (handSize - 1f) - 1f;
            var dyDu = 2f * edgeLift * u;
            var dxDu = Math.Max(1f, halfSpread);
            var angle = Mathf.RadToDeg(Mathf.Atan2(dyDu, dxDu));
            __result = Mathf.Clamp(angle, -18f, 18f);

            return false;
        }

        // ReSharper disable InconsistentNaming
        private static bool GetScalePrefix(int handSize, ref Vector2 __result)
            // ReSharper restore InconsistentNaming
        {
            if (handSize <= 10)
                return true;

            var scalar = 0.64f * MathF.Pow(0.95f, handSize - 11);
            __result = Vector2.One * Math.Max(0.48f, scalar);

            return false;
        }
    }
}
