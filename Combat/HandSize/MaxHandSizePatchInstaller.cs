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

namespace STS2RitsuLib.Combat.HandSize
{
    /// <summary>
    ///     Installs RitsuLib max-hand-size patches.
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

#if !STS2_V_0_103_2
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
                    DynamicPatchBuilder.FromMethod(typeof(MaxHandSizePatchInstaller), nameof(PlayerArg0Transpiler));
                var transpilerPlayerArg1 =
                    DynamicPatchBuilder.FromMethod(typeof(MaxHandSizePatchInstaller), nameof(PlayerArg1Transpiler));
                var transpilerStateMachine =
                    DynamicPatchBuilder.FromMethod(typeof(MaxHandSizePatchInstaller), nameof(StateMachineTranspiler));
                var cardOnPlayTranspiler =
                    DynamicPatchBuilder.FromMethod(typeof(MaxHandSizePatchInstaller), nameof(CardOnPlayTranspiler));

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
#if STS2_V_0_103_2
                RitsuLibFramework.Logger.Info("[MaxHandSize] RitsuLib hand-size patch set installed (compat 0.103.2 profile).");
#else
                RitsuLibFramework.Logger.Info(
                    "[MaxHandSize] RitsuLib hand-size patch set installed (0.104.0 profile).");
#endif
            }
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
#if STS2_V_0_103_2
            // 0.103.2 compatibility signatures keep most hand-limit checks as hard-coded constants.
            // Match literal 10 only.
            return (ins.opcode == OpCodes.Ldc_I4_S && ins.operand is sbyte and DefaultMaxHandSize)
                   || (ins.opcode == OpCodes.Ldc_I4 && ins.operand is DefaultMaxHandSize);
#else
            // 0.104.0 switched many call sites to CardPile.MaxCardsInHand.
            // Match both legacy literal 10 and property getter invocations.
            var isDefaultConst =
                (ins.opcode == OpCodes.Ldc_I4_S && ins.operand is sbyte and DefaultMaxHandSize)
                || (ins.opcode == OpCodes.Ldc_I4 && ins.operand is DefaultMaxHandSize);

            var isMaxCardsGetter =
                (ins.opcode == OpCodes.Call || ins.opcode == OpCodes.Callvirt)
                && ins.operand is MethodInfo method
                && method == MaxCardsInHandGetter;

            return isDefaultConst || isMaxCardsGetter;
#endif
        }

        private static IEnumerable<CodeInstruction> PlayerArg0Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var ins in instructions)
            {
                if (IsMaxHandSizeToken(ins))
                {
                    yield return new(OpCodes.Ldarg_0);
                    yield return new(OpCodes.Call, GetMaxHandSizeMethod);
                    continue;
                }

                yield return ins;
            }
        }

        private static IEnumerable<CodeInstruction> PlayerArg1Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var ins in instructions)
            {
                if (IsMaxHandSizeToken(ins))
                {
                    yield return new(OpCodes.Ldarg_1);
                    yield return new(OpCodes.Call, GetMaxHandSizeMethod);
                    continue;
                }

                yield return ins;
            }
        }

        private static IEnumerable<CodeInstruction> StateMachineTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var loadPlayer = FindStateMachinePlayerLoad(code);
            if (loadPlayer == null)
            {
                RitsuLibFramework.Logger.Warn(
                    "[MaxHandSize] State-machine transpiler could not resolve Player load pattern; skipped replacements.");
                return code;
            }

            for (var i = 0; i < code.Count; i++)
            {
                if (!IsMaxHandSizeToken(code[i]))
                    continue;

                code[i] = new(OpCodes.Call, GetMaxHandSizeMethod);
                code.InsertRange(i, loadPlayer.Select(ci => ci.Clone()));
                i += loadPlayer.Count;
            }

            return code;
        }

        private static IEnumerable<CodeInstruction> CardOnPlayTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var loadCard = FindStateMachineCardLoad(code);
            if (loadCard == null)
            {
                RitsuLibFramework.Logger.Warn(
                    "[MaxHandSize] Card OnPlay transpiler could not resolve Card load pattern; skipped replacements.");
                return code;
            }

            for (var i = 0; i < code.Count; i++)
            {
                if (!IsMaxHandSizeToken(code[i]))
                    continue;

                code[i] = new(OpCodes.Call, GetMaxHandSizeFromCardMethod);
                code.InsertRange(i, loadCard.Select(ci => ci.Clone()));
                i += loadCard.Count;
            }

            return code;
        }

        private static IReadOnlyList<CodeInstruction>? FindStateMachinePlayerLoad(IReadOnlyList<CodeInstruction> code)
        {
            for (var i = 0; i < code.Count - 1; i++)
            {
                if (code[i].opcode != OpCodes.Ldarg_0)
                    continue;
                if (code[i + 1].opcode != OpCodes.Ldfld)
                    continue;
                if (code[i + 1].operand is not FieldInfo field || field.FieldType != typeof(Player))
                    continue;

                return [code[i].Clone(), code[i + 1].Clone()];
            }

            return null;
        }

        private static IReadOnlyList<CodeInstruction>? FindStateMachineCardLoad(IReadOnlyList<CodeInstruction> code)
        {
            for (var i = 0; i < code.Count - 1; i++)
            {
                if (code[i].opcode != OpCodes.Ldarg_0)
                    continue;
                if (code[i + 1].opcode != OpCodes.Ldfld)
                    continue;
                if (code[i + 1].operand is not FieldInfo field || !typeof(CardModel).IsAssignableFrom(field.FieldType))
                    continue;

                return [code[i].Clone(), code[i + 1].Clone()];
            }

            return null;
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
            if (selectCardShortcutsField == null || draggedHolderIndexField == null || getShortcutMethod == null)
                return instructions;

            var list = instructions.ToList();
            for (var i = 0; i < list.Count - 4; i++)
            {
                if (list[i].opcode != OpCodes.Ldarg_0)
                    continue;
                if (list[i + 1].opcode != OpCodes.Ldfld || !Equals(list[i + 1].operand, selectCardShortcutsField))
                    continue;
                if (list[i + 2].opcode != OpCodes.Ldarg_0)
                    continue;
                if (list[i + 3].opcode != OpCodes.Ldfld || !Equals(list[i + 3].operand, draggedHolderIndexField))
                    continue;
                if (list[i + 4].opcode != OpCodes.Ldelem_Ref)
                    continue;

                list.RemoveRange(i, 5);
                list.InsertRange(i,
                [
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, draggedHolderIndexField),
                    new(OpCodes.Call, getShortcutMethod),
                ]);
                break;
            }

            return list;
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
            var u = handSize <= 1 ? 0f : 2f * cardIndex / (handSize - 1f) - 1f;
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
            var u = handSize <= 1 ? 0f : 2f * cardIndex / (handSize - 1f) - 1f;
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
