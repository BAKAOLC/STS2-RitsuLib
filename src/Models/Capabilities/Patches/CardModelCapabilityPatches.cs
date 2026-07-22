#if !STS2_AT_LEAST_0_104_0
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers.Models;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Cards;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.HarmonyIl;

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

        /// <summary>
        ///     Applies capability title fragments after CardModel formats the base title.
        ///     在 CardModel 格式化基础标题后应用能力标题片段。
        /// </summary>
        internal sealed class TitlePatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_title";

            public static string Description => "Apply model-capability card title fragments";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), "Title", MethodType.Getter)];
            }

            public static void Postfix(CardModel __instance, ref string __result)
            {
                var baseTitleLocString = __instance.TitleLocString;
                var baseTitle = baseTitleLocString.GetFormattedText();
                var upgradeSuffix = GetUpgradeSuffix(__instance);
                var context = new CardTitleContext(
                    __instance,
                    baseTitleLocString,
                    baseTitle,
                    upgradeSuffix,
                    baseTitle + upgradeSuffix);
                CardModelCapabilityHost.ApplyTitleFragments(context, ref __result);
            }

            private static string GetUpgradeSuffix(CardModel card)
            {
                if (!card.IsUpgraded)
                    return "";

                return card.MaxUpgradeLevel > 1
                    ? $"+{card.CurrentUpgradeLevel}"
                    : "+";
            }
        }

        /// <summary>
        ///     Applies BaseLib-compatible type text modifiers before the plaque LocString is formatted.
        ///     在类型牌匾 LocString 格式化前应用与 BaseLib 兼容的类型文本修改器。
        /// </summary>
        internal sealed class TypeTextPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_type_text";

            public static string Description => "Apply BaseLib-compatible card type text modifiers";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(NCard), "UpdateTypePlaque")];
            }

            [HarmonyAfter(Const.BaseLibHarmonyId)]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var code = instructions.ToList();
                var applyMethod = AccessTools.Method(
                    typeof(CardTypeTextHook),
                    nameof(CardTypeTextHook.Apply));
                if (applyMethod == null || code.Any(instruction => instruction.Calls(applyMethod)))
                    return code;

                var toLocStringMethod = AccessTools.Method(
                    typeof(CardTypeExtensions),
                    nameof(CardTypeExtensions.ToLocString));
                var getFormattedTextMethod = AccessTools.Method(
                    typeof(LocString),
                    nameof(LocString.GetFormattedText));
                var modelGetter = AccessTools.PropertyGetter(typeof(NCard), nameof(NCard.Model));
                if (toLocStringMethod == null || getFormattedTextMethod == null || modelGetter == null)
                    return code;

                var toLocStringIndex = code.FindIndex(instruction => instruction.Calls(toLocStringMethod));
                var getFormattedTextIndex = toLocStringIndex < 0
                    ? -1
                    : code.FindIndex(
                        toLocStringIndex + 1,
                        instruction => instruction.Calls(getFormattedTextMethod));
                if (getFormattedTextIndex < 0)
                {
                    RitsuLibFramework.Logger.Warn(
                        "[ModelCapabilities] Card type text patch did not find the expected LocString formatting site.");
                    return code;
                }

                code.InsertRange(
                    getFormattedTextIndex,
                    [
                        CodeInstruction.LoadArgument(0),
                        new(OpCodes.Call, modelGetter),
                        new(OpCodes.Call, applyMethod),
                    ]);
                return code;
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

        internal sealed class EnergyCostPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_energy_cost";

            public static string Description => "Apply model-capability card energy cost modifiers";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardEnergyCost), nameof(CardEnergyCost.GetWithModifiers), [typeof(CostModifiers)])];
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                const string operation = "[ModelCapabilities] Card energy cost hook injection";
                var originalMethod = HarmonyIl.RequireMethod(
                    AccessTools.Method(
                        typeof(Hook),
                        nameof(Hook.ModifyEnergyCostInCombat),
                        [typeof(CombatStateCompat), typeof(CardModel), typeof(decimal)]),
                    operation);
                var replacementMethod = HarmonyIl.RequireMethod(
                    AccessTools.Method(
                        typeof(EnergyCostPatch),
                        nameof(ModifyEnergyCostInCombat)),
                    operation);

                var rewriter = HarmonyIlRewriter.From(instructions);
                var report = rewriter.ReplaceInstructions(
                    operation,
                    instruction => instruction.Calls(originalMethod),
                    [
                        CodeInstruction.LoadArgument(1),
                        HarmonyIl.Call(replacementMethod),
                    ],
                    code => code.Any(HarmonyIl.IsCall(replacementMethod)));
                return rewriter.InstructionsChecked(report);
            }

            public static void Postfix(CardEnergyCost __instance, CostModifiers modifiers, ref int __result)
            {
                if (modifiers.HasFlag(CostModifiers.Global) && __instance._card.CombatState != null)
                    return;

                __result = CardModelCapabilityHost.ApplyEnergyCost(__instance._card, modifiers, __result);
            }

            private static decimal ModifyEnergyCostInCombat(
                CombatStateCompat combatState,
                CardModel card,
                decimal originalCost,
                CostModifiers modifiers)
            {
                var localCost = modifiers.HasFlag(CostModifiers.Local)
                    ? CardModelCapabilityHost.ApplyEnergyCost(card, modifiers, (int)originalCost)
                    : (int)originalCost;
                return Hook.ModifyEnergyCostInCombat(combatState, card, localCost);
            }
        }

        internal sealed class HasLocalEnergyCostModifierPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_has_local_energy_cost_modifier";

            public static string Description => "Expose model-capability card energy cost modifiers as local modifiers";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardEnergyCost), "HasLocalModifiers", MethodType.Getter)];
            }

            public static void Postfix(CardEnergyCost __instance, ref bool __result)
            {
                if (!__result && CardModelCapabilityHost.HasEnergyCostContributors(__instance._card))
                    __result = true;
            }
        }

        internal sealed class StarCostPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_star_cost";

            public static string Description => "Apply model-capability card star cost modifiers";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), "CurrentStarCost", MethodType.Getter)];
            }

            public static void Postfix(CardModel __instance, ref int __result)
            {
                __result = CardModelCapabilityHost.ApplyStarCost(__instance, __result);
            }
        }

        internal sealed class StarCostColorPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_capability_star_cost_color";

            public static string Description => "Apply model-capability card star cost color";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(CardCostHelper), nameof(CardCostHelper.GetStarCostColor),
                        [typeof(CardModel), typeof(CombatStateCompat)]),
                ];
            }

            public static void Postfix(CardModel card, CombatStateCompat? state, ref CardCostColor __result)
            {
                if (state == null ||
                    __result == CardCostColor.InsufficientResources ||
                    card.HasStarCostX ||
                    !CardModelCapabilityHost.HasStarCostContributors(card))
                    return;

                var modifiedCost = card.GetStarCostWithModifiers();
                __result = modifiedCost.CompareTo(card.BaseStarCost) switch
                {
                    > 0 => CardCostColor.Increased,
                    < 0 => CardCostColor.Decreased,
                    _ => CardCostColor.Unmodified,
                };
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
#if STS2_AT_LEAST_0_109_0
                return [new(typeof(CardModel), "GetResultLocationForCardPlay", Type.EmptyTypes)];
#elif STS2_AT_LEAST_0_108_0
                return [new(typeof(CardModel), "GetResultPileTypeAndPositionForCardPlay", Type.EmptyTypes)];
#elif STS2_AT_LEAST_0_105_0
                return [new(typeof(CardModel), "GetResultPileTypeForCardPlay", Type.EmptyTypes)];
#else
                return [new(typeof(CardModel), "GetResultPileType", Type.EmptyTypes)];
#endif
            }

#if STS2_AT_LEAST_0_109_0
            public static void Postfix(CardModel __instance, ref CardLocation __result)
            {
                __result.pileType = CardModelCapabilityHost.ApplyResultPileTypeForCardPlay(
                    __instance,
                    __result.pileType);
            }
#elif STS2_AT_LEAST_0_108_0
            public static void Postfix(CardModel __instance, ref (PileType, CardPilePosition) __result)
            {
                __result.Item1 = CardModelCapabilityHost.ApplyResultPileTypeForCardPlay(
                    __instance,
                    __result.Item1);
            }
#else
            public static void Postfix(CardModel __instance, ref PileType __result)
            {
                __result = CardModelCapabilityHost.ApplyResultPileTypeForCardPlay(__instance, __result);
            }
#endif
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
                    {
                        ModelCapabilityUpgradeReplayContext.FlushDeferredCardModelSavedDataImport(__result);
                        ModelCapabilityUpgradeReplayContext.FlushDeferredCardCapabilityImport(__result);
                    }
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

            public static void Prefix(CardModel __instance)
            {
                SecondaryResourceUpgradePreviewCosts.Capture(__instance);
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                const string operation = "[ModelCapabilities] Card upgrade lifecycle injection";
                var recalculateMethod = HarmonyIl.RequireMethod(
                    AccessTools.Method(
                        typeof(DynamicVarSet),
                        nameof(DynamicVarSet.RecalculateForUpgradeOrEnchant)),
                    operation);
                var notifyMethod = HarmonyIl.RequireMethod(
                    AccessTools.Method(
                        typeof(CardModelCapabilityHost),
                        nameof(CardModelCapabilityHost.AfterOwnerCardUpgraded)),
                    operation);

                var rewriter = HarmonyIlRewriter.From(instructions);
                var report = rewriter.InsertAfterCall(
                    operation,
                    recalculateMethod,
                    [
                        CodeInstruction.LoadArgument(0),
                        HarmonyIl.Call(notifyMethod),
                    ],
                    code => code.Any(HarmonyIl.IsCall(notifyMethod)));
                return rewriter.InstructionsChecked(report);
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
                SecondaryResourceUpgradePreviewCosts.Clear(__instance);
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
                const string operation = "[ModelCapabilities] Card downgrade lifecycle injection";
                var afterDowngradedMethod = HarmonyIl.RequireMethod(
                    AccessTools.Method(typeof(CardModel), "AfterDowngraded"),
                    operation);
                var resetSecondaryResourcesMethod = HarmonyIl.RequireMethod(
                    AccessTools.Method(
                        typeof(SecondaryResourceCardExtensions),
                        nameof(SecondaryResourceCardExtensions.ResetSecondaryResourcesForDowngrade)),
                    operation);
                var notifyMethod = HarmonyIl.RequireMethod(
                    AccessTools.Method(
                        typeof(CardModelCapabilityHost),
                        nameof(CardModelCapabilityHost.AfterOwnerCardDowngraded)),
                    operation);

                var rewriter = HarmonyIlRewriter.From(instructions);
                var report = rewriter.TryReplaceFirst(
                    operation,
                    HarmonyIlPattern.Sequence(HarmonyIl.IsCall(afterDowngradedMethod)),
                    [
                        CodeInstruction.LoadArgument(0),
                        HarmonyIl.Call(resetSecondaryResourcesMethod),
                        new(OpCodes.Callvirt, afterDowngradedMethod),
                        CodeInstruction.LoadArgument(0),
                        HarmonyIl.Call(notifyMethod),
                    ],
                    code => code.Any(HarmonyIl.IsCall(resetSecondaryResourcesMethod)) &&
                            code.Any(HarmonyIl.IsCall(notifyMethod)));
                return rewriter.InstructionsChecked(report);
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
                const string fromOperation = "[ModelCapabilities] Card transform-from lifecycle injection";
                const string toOperation = "[ModelCapabilities] Card transform-to lifecycle injection";
                var transformedFromMethod = HarmonyIl.RequireMethod(
                    AccessTools.Method(
                        typeof(CardModel),
                        nameof(CardModel.AfterTransformedFrom)),
                    fromOperation);
                var transformedToMethod = HarmonyIl.RequireMethod(
                    AccessTools.Method(
                        typeof(CardModel),
                        nameof(CardModel.AfterTransformedTo)),
                    toOperation);
                var notifyFromMethod = HarmonyIl.RequireMethod(
                    AccessTools.Method(
                        typeof(CardModelCapabilityHost),
                        nameof(CardModelCapabilityHost.AfterOwnerCardTransformedFrom)),
                    fromOperation);
                var notifyToMethod = HarmonyIl.RequireMethod(
                    AccessTools.Method(
                        typeof(CardModelCapabilityHost),
                        nameof(CardModelCapabilityHost.AfterOwnerCardTransformedTo)),
                    toOperation);

                var rewriter = HarmonyIlRewriter.From(instructions);
                var fromReport = rewriter.TryReplaceFirst(
                    fromOperation,
                    HarmonyIlPattern.Sequence(HarmonyIl.IsCall(transformedFromMethod)),
                    [
                        new(OpCodes.Dup),
                        new(OpCodes.Callvirt, transformedFromMethod),
                        HarmonyIl.Call(notifyFromMethod),
                    ],
                    code => code.Any(HarmonyIl.IsCall(notifyFromMethod)));
                var toReport = rewriter.TryReplaceFirst(
                    toOperation,
                    HarmonyIlPattern.Sequence(HarmonyIl.IsCall(transformedToMethod)),
                    [
                        new(OpCodes.Dup),
                        new(OpCodes.Callvirt, transformedToMethod),
                        HarmonyIl.Call(notifyToMethod),
                    ],
                    code => code.Any(HarmonyIl.IsCall(notifyToMethod)));
                return rewriter.InstructionsChecked([fromReport, toReport]);
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

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var enchantmentGetter = AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.Enchantment));
                var applyMethod = AccessTools.Method(
                    typeof(CardModelCapabilityHost),
                    nameof(CardModelCapabilityHost.ApplyDescriptionFragments),
                    [typeof(CardModel), typeof(PileType), typeof(object), typeof(Creature), typeof(List<string>)]);
                var previewType = CardDescriptionPatchTarget.GetDescriptionPreviewType();

                var code = instructions.ToList();
                if (enchantmentGetter == null || applyMethod == null)
                    return code;

                for (var i = 1; i < code.Count - 1; i++)
                {
                    if (!HarmonyIl.IsLdarg(0)(code[i]) || !code[i + 1].Calls(enchantmentGetter))
                        continue;

                    if (!HarmonyIl.TryGetLocalStore(code[i - 1], out var descriptionLinesLocal))
                        continue;

                    var injected = new List<CodeInstruction>
                    {
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.LoadArgument(1),
                        CodeInstruction.LoadArgument(2),
                        new(OpCodes.Box, previewType),
                        CodeInstruction.LoadArgument(3),
                        descriptionLinesLocal.Load(),
                        new(OpCodes.Call, applyMethod),
                    };

                    injected[0].labels.AddRange(code[i].labels);
                    code[i].labels.Clear();
                    code.InsertRange(i, injected);
                    return code;
                }

                RitsuLibFramework.Logger.Warn($"{MissingLifecyclePatchWarning} Patch={PatchId}");
                return code;
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
