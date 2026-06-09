using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Cards;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Models.Capabilities.Patches
{
    /// <summary>
    ///     Bridges model capabilities into card-facing behavior and display surfaces.
    ///     将模型能力桥接到卡牌侧行为与展示 surface。
    /// </summary>
    internal static class CardModelCapabilityPatches
    {
        private const string MissingLifecyclePatchWarning =
            "[ModelCapabilities] Card lifecycle patch did not find the expected IL call site.";

        private static void InsertDuplicatedReceiverNotification(
            List<CodeInstruction> code,
            int callIndex,
            MethodInfo notifyMethod)
        {
            var call = code[callIndex];
            var dup = new CodeInstruction(OpCodes.Dup);
            dup.labels.AddRange(call.labels);
            call.labels.Clear();

            code.Insert(callIndex, dup);
            code.Insert(callIndex + 2, new(OpCodes.Call, notifyMethod));
        }

        /// <summary>
        ///     Updates capability dynamic vars through the same card preview path as vanilla card dynamic vars.
        ///     通过与原版卡牌动态变量相同的卡牌预览路径更新能力动态变量。
        /// </summary>
        internal sealed class UpdateDynamicVarPreviewPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_dynamic_vars";
            public static string Description => "Update model-capability card dynamic vars through CardModel preview";
            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(CardModel), nameof(CardModel.UpdateDynamicVarPreview),
                        [typeof(CardPreviewMode), typeof(Creature), typeof(DynamicVarSet)]),
                ];
            }

            public static void Postfix(
                CardModel __instance,
                CardPreviewMode previewMode,
                Creature? target,
                DynamicVarSet dynamicVarSet)
            {
                if (ReferenceEquals(dynamicVarSet, __instance.DynamicVars))
                    CardModelCapabilityHost.UpdateDynamicVarPreviews(__instance, previewMode, target);
            }
        }

        internal sealed class CardTypePatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_type";

            public static string Description => "Apply model-capability card type overrides";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), "Type", MethodType.Getter)];
            }

            public static void Postfix(CardModel __instance, ref CardType __result)
            {
                __result = CardModelCapabilityHost.ApplyCardType(__instance, __result);
            }
        }

        internal sealed class CardRarityPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_rarity";

            public static string Description => "Apply model-capability card rarity overrides";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), "Rarity", MethodType.Getter)];
            }

            public static void Postfix(CardModel __instance, ref CardRarity __result)
            {
                __result = CardModelCapabilityHost.ApplyCardRarity(__instance, __result);
            }
        }

        internal sealed class TargetTypePatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_target_type";

            public static string Description => "Apply model-capability card target type overrides";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), "TargetType", MethodType.Getter)];
            }

            public static void Postfix(CardModel __instance, ref TargetType __result)
            {
                __result = CardModelCapabilityHost.ApplyTargetType(__instance, __result);
            }
        }

        internal sealed class TagsPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_tags";

            public static string Description => "Append model-capability card tags";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), "Tags", MethodType.Getter)];
            }

            public static void Postfix(CardModel __instance, ref IEnumerable<CardTag> __result)
            {
                __result = CardModelCapabilityHost.ApplyTags(__instance, __result);
            }
        }

        internal sealed class IsPlayablePatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_is_playable";

            public static string Description => "Apply model-capability card playability decisions";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), "IsPlayable", MethodType.Getter)];
            }

            public static void Postfix(CardModel __instance, ref bool __result)
            {
                __result = CardModelCapabilityHost.ApplyCanPlay(__instance, __result);
            }
        }

        internal sealed class HasTurnEndInHandEffectPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_turn_end_in_hand";

            public static string Description => "Apply model-capability turn-end-in-hand markers";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), "HasTurnEndInHandEffect", MethodType.Getter)];
            }

            public static void Postfix(CardModel __instance, ref bool __result)
            {
                if (!__result && CardModelCapabilityHost.HasTurnEndInHandEffect(__instance))
                    __result = true;
            }
        }

        internal sealed class ResultPileTypeForCardPlayPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_result_pile";

            public static string Description => "Apply model-capability card play result pile overrides";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
#if STS2_AT_LEAST_0_105_0
                return [new(typeof(CardModel), "GetResultPileTypeForCardPlay", Type.EmptyTypes)];
#else
                return [new(typeof(CardModel), "GetResultPileType", Type.EmptyTypes)];
#endif
            }

            public static void Postfix(CardModel __instance, ref PileType __result)
            {
                __result = CardModelCapabilityHost.ApplyResultPileTypeForCardPlay(__instance, __result);
            }
        }

        internal sealed class TransformCarryOverPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_transform_carry_over";

            public static string Description => "Carry opted-in card capabilities to transform results";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardTransformation), nameof(CardTransformation.GetReplacement), [typeof(Rng)])];
            }

            public static void Postfix(CardTransformation __instance, CardModel? __result)
            {
                CardModelCapabilityHost.CarryOverTransformCapabilities(__instance.Original, __result);
            }
        }

        internal sealed class FromSerializableUpgradeReplayPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_from_serializable_upgrade_replay";

            public static string Description =>
                "Defer saved card capability imports until CardModel.FromSerializable upgrade replay completes";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), nameof(CardModel.FromSerializable), [typeof(SerializableCard)])];
            }

            public static void Prefix(out IDisposable __state)
            {
                __state = ModelCapabilityUpgradeReplayContext.BeginCardDeserializeReplay();
            }

            public static Exception? Finalizer(Exception? __exception, CardModel? __result, IDisposable? __state)
            {
                try
                {
                    if (__exception == null)
                        ModelCapabilityUpgradeReplayContext.FlushDeferredCardCapabilityImport(__result);
                }
                finally
                {
                    __state?.Dispose();
                }

                return __exception;
            }
        }

        internal sealed class UpgradeInternalPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_upgrade_lifecycle";

            public static string Description => "Notify card capabilities during CardModel upgrade lifecycle";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), nameof(CardModel.UpgradeInternal), Type.EmptyTypes)];
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var recalculateMethod = AccessTools.Method(
                    typeof(DynamicVarSet),
                    nameof(DynamicVarSet.RecalculateForUpgradeOrEnchant));
                var notifyMethod = AccessTools.Method(
                    typeof(CardModelCapabilityHost),
                    nameof(CardModelCapabilityHost.AfterOwnerCardUpgraded));

                var code = instructions.ToList();
                if (recalculateMethod == null || notifyMethod == null)
                    return code;

                var inserted = false;
                for (var i = 0; i < code.Count; i++)
                {
                    if (!code[i].Calls(recalculateMethod))
                        continue;

                    code.InsertRange(i + 1,
                    [
                        CodeInstruction.LoadArgument(0),
                        new(OpCodes.Call, notifyMethod),
                    ]);
                    inserted = true;
                    break;
                }

                if (!inserted)
                    RitsuLibFramework.Logger.Warn($"{MissingLifecyclePatchWarning} Patch={PatchId}");

                return code;
            }
        }

        internal sealed class FinalizeUpgradeInternalPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_finalize_upgrade_lifecycle";

            public static string Description =>
                "Finalize card capability dynamic vars with CardModel upgrade lifecycle";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), nameof(CardModel.FinalizeUpgradeInternal), Type.EmptyTypes)];
            }

            public static void Postfix(CardModel __instance)
            {
                CardModelCapabilityHost.AfterOwnerCardUpgradeFinalized(__instance);
            }
        }

        internal sealed class DowngradeInternalPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_downgrade_lifecycle";

            public static string Description => "Notify card capabilities during CardModel downgrade lifecycle";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), nameof(CardModel.DowngradeInternal), Type.EmptyTypes)];
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var afterDowngradedMethod = AccessTools.Method(typeof(CardModel), "AfterDowngraded");
                var notifyMethod = AccessTools.Method(
                    typeof(CardModelCapabilityHost),
                    nameof(CardModelCapabilityHost.AfterOwnerCardDowngraded));

                var code = instructions.ToList();
                if (afterDowngradedMethod == null || notifyMethod == null)
                    return code;

                var inserted = false;
                for (var i = 0; i < code.Count; i++)
                {
                    if (!code[i].Calls(afterDowngradedMethod))
                        continue;

                    code.InsertRange(i + 1,
                    [
                        CodeInstruction.LoadArgument(0),
                        new(OpCodes.Call, notifyMethod),
                    ]);
                    inserted = true;
                    break;
                }

                if (!inserted)
                    RitsuLibFramework.Logger.Warn($"{MissingLifecyclePatchWarning} Patch={PatchId}");

                return code;
            }
        }

        internal sealed class TransformPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_transform_lifecycle";

            public static string Description => "Notify card capabilities during CardCmd transform lifecycle";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(CardCmd), nameof(CardCmd.Transform),
                        [typeof(IEnumerable<CardTransformation>), typeof(Rng), typeof(CardPreviewStyle)],
                        MethodType.Async),
                ];
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var transformedFromMethod = AccessTools.Method(
                    typeof(CardModel),
                    nameof(CardModel.AfterTransformedFrom));
                var transformedToMethod = AccessTools.Method(
                    typeof(CardModel),
                    nameof(CardModel.AfterTransformedTo));
                var notifyFromMethod = AccessTools.Method(
                    typeof(CardModelCapabilityHost),
                    nameof(CardModelCapabilityHost.AfterOwnerCardTransformedFrom));
                var notifyToMethod = AccessTools.Method(
                    typeof(CardModelCapabilityHost),
                    nameof(CardModelCapabilityHost.AfterOwnerCardTransformedTo));

                var code = instructions.ToList();
                if (transformedFromMethod == null ||
                    transformedToMethod == null ||
                    notifyFromMethod == null ||
                    notifyToMethod == null)
                    return code;

                var inserted = 0;
                for (var i = 0; i < code.Count; i++)
                {
                    if (code[i].Calls(transformedFromMethod))
                    {
                        InsertDuplicatedReceiverNotification(code, i, notifyFromMethod);
                        i += 2;
                        inserted++;
                        continue;
                    }

                    if (!code[i].Calls(transformedToMethod))
                        continue;

                    InsertDuplicatedReceiverNotification(code, i, notifyToMethod);
                    i += 2;
                    inserted++;
                }

                if (inserted == 0)
                    RitsuLibFramework.Logger.Warn($"{MissingLifecyclePatchWarning} Patch={PatchId}");

                return code;
            }
        }

        /// <summary>
        ///     Applies capability description modifiers to normal card description rendering.
        ///     将能力描述修改器应用到常规卡牌描述渲染。
        /// </summary>
        internal sealed class DescriptionPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_description";
            public static string Description => "Apply model-capability card description modifiers";
            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [CardDescriptionPatchTarget.Create()];
            }

            public static void Postfix(
                CardModel __instance,
                object[] __args,
                ref string __result)
            {
                var pileType = __args is [{ } first, ..] ? (PileType)first : PileType.None;
                var previewType = __args is [_, var second, ..] ? second : null;
                var target = __args is [_, _, Creature creature, ..] ? creature : null;
                var isUpgradePreview = CardDescriptionPatchTarget.IsUpgradePreview(previewType);
                var context = new CardDescriptionContext(__instance, pileType, target, isUpgradePreview);
                CardModelCapabilityHost.ApplyDescriptionFragments(context, ref __result);
            }
        }

        /// <summary>
        ///     Appends capability hover tips to card hover tips.
        ///     将能力悬停提示追加到卡牌悬停提示。
        /// </summary>
        internal sealed class HoverTipsPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_hover_tips";
            public static string Description => "Append model-capability card hover tips";
            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), "HoverTips", MethodType.Getter)];
            }

            public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
            {
                var tips = CardModelCapabilityHost.GetHoverTips(__instance).ToArray();
                if (tips.Length == 0)
                    return;

                __result = __result.Concat(tips).Distinct().ToArray();
            }
        }

        /// <summary>
        ///     ORs capability glow predicates into gold hand glow.
        ///     将能力发光判定 OR 到金色手牌发光。
        /// </summary>
        internal sealed class ShouldGlowGoldPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_should_glow_gold";
            public static string Description => "Merge model-capability gold glow predicates";
            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), "ShouldGlowGold", MethodType.Getter)];
            }

            public static void Postfix(CardModel __instance, ref bool __result)
            {
                if (!__result && CardModelCapabilityHost.ShouldGlowGold(__instance))
                    __result = true;
            }
        }

        /// <summary>
        ///     ORs capability glow predicates into red hand glow.
        ///     将能力发光判定 OR 到红色手牌发光。
        /// </summary>
        internal sealed class ShouldGlowRedPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_should_glow_red";
            public static string Description => "Merge model-capability red glow predicates";
            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), "ShouldGlowRed", MethodType.Getter)];
            }

            public static void Postfix(CardModel __instance, ref bool __result)
            {
                if (!__result && CardModelCapabilityHost.ShouldGlowRed(__instance))
                    __result = true;
            }
        }
    }
}
