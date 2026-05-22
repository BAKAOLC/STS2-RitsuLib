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
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Models.Capabilities.Patches
{
    /// <summary>
    ///     Bridges model components into card-facing display surfaces.
    ///     将模型组件桥接到卡牌侧展示 surface。
    /// </summary>
    internal static class CardModelComponentCapabilityPatches
    {
        private const string MissingLifecyclePatchWarning =
            "[ModelComponents] Card lifecycle patch did not find the expected IL call site.";

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
        ///     Updates component dynamic vars through the same card preview path as vanilla card dynamic vars.
        ///     通过与原版卡牌动态变量相同的卡牌预览路径更新组件动态变量。
        /// </summary>
        internal sealed class UpdateDynamicVarPreviewPatch : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "ritsulib_card_component_dynamic_vars";

            /// <inheritdoc />
            public static string Description => "Update model-component card dynamic vars through CardModel preview";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(CardModel), nameof(CardModel.UpdateDynamicVarPreview),
                        [typeof(CardPreviewMode), typeof(Creature), typeof(DynamicVarSet)]),
                ];
            }

            // ReSharper disable InconsistentNaming
            public static void Postfix(
                    CardModel __instance,
                    CardPreviewMode previewMode,
                    Creature? target,
                    DynamicVarSet dynamicVarSet)
                // ReSharper restore InconsistentNaming
            {
                if (ReferenceEquals(dynamicVarSet, __instance.DynamicVars))
                    CardModelComponentCapabilityHost.UpdateDynamicVarPreviews(__instance, previewMode, target);
            }
        }

        internal sealed class FromSerializableUpgradeReplayPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_component_from_serializable_upgrade_replay";

            public static string Description =>
                "Defer saved card component imports until CardModel.FromSerializable upgrade replay completes";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), nameof(CardModel.FromSerializable), [typeof(SerializableCard)])];
            }

            // ReSharper disable once InconsistentNaming
            public static void Prefix(out IDisposable __state)
            {
                __state = ModelComponentUpgradeReplayContext.BeginCardDeserializeReplay();
            }

            // ReSharper disable InconsistentNaming
            public static Exception? Finalizer(Exception? __exception, CardModel? __result, IDisposable? __state)
                // ReSharper restore InconsistentNaming
            {
                try
                {
                    if (__exception == null)
                        ModelComponentUpgradeReplayContext.FlushDeferredCardComponentImport(__result);
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
            public static string PatchId => "ritsulib_card_component_upgrade_lifecycle";

            public static string Description => "Notify card components during CardModel upgrade lifecycle";

            public static bool IsCritical => false;

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
                    typeof(CardModelComponentCapabilityHost),
                    nameof(CardModelComponentCapabilityHost.AfterOwnerCardUpgraded));

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
            public static string PatchId => "ritsulib_card_component_finalize_upgrade_lifecycle";

            public static string Description => "Finalize card component dynamic vars with CardModel upgrade lifecycle";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), nameof(CardModel.FinalizeUpgradeInternal), Type.EmptyTypes)];
            }

            // ReSharper disable InconsistentNaming
            public static void Postfix(CardModel __instance)
                // ReSharper restore InconsistentNaming
            {
                CardModelComponentCapabilityHost.AfterOwnerCardUpgradeFinalized(__instance);
            }
        }

        internal sealed class DowngradeInternalPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_card_component_downgrade_lifecycle";

            public static string Description => "Notify card components during CardModel downgrade lifecycle";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), nameof(CardModel.DowngradeInternal), Type.EmptyTypes)];
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var afterDowngradedMethod = AccessTools.Method(typeof(CardModel), "AfterDowngraded");
                var notifyMethod = AccessTools.Method(
                    typeof(CardModelComponentCapabilityHost),
                    nameof(CardModelComponentCapabilityHost.AfterOwnerCardDowngraded));

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
            public static string PatchId => "ritsulib_card_component_transform_lifecycle";

            public static string Description => "Notify card components during CardCmd transform lifecycle";

            public static bool IsCritical => false;

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
                    typeof(CardModelComponentCapabilityHost),
                    nameof(CardModelComponentCapabilityHost.AfterOwnerCardTransformedFrom));
                var notifyToMethod = AccessTools.Method(
                    typeof(CardModelComponentCapabilityHost),
                    nameof(CardModelComponentCapabilityHost.AfterOwnerCardTransformedTo));

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
        ///     Applies component description modifiers to normal card description rendering.
        ///     将组件描述修改器应用到常规卡牌描述渲染。
        /// </summary>
        internal sealed class DescriptionForPilePatch : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "ritsulib_card_component_description_for_pile";

            /// <inheritdoc />
            public static string Description => "Apply model-component card description modifiers";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(CardModel), nameof(CardModel.GetDescriptionForPile),
                        [typeof(PileType), typeof(Creature)]),
                ];
            }

            // ReSharper disable InconsistentNaming
            public static void Postfix(
                    CardModel __instance,
                    PileType pileType,
                    Creature? target,
                    ref string __result)
                // ReSharper restore InconsistentNaming
            {
                var context = new CardDescriptionComponentContext(__instance, pileType, target, false);
                CardModelComponentCapabilityHost.ApplyDescriptionFragments(context, ref __result);
            }
        }

        /// <summary>
        ///     Applies component description modifiers to upgrade-preview text.
        ///     将组件描述修改器应用到升级预览文本。
        /// </summary>
        internal sealed class DescriptionForUpgradePreviewPatch : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "ritsulib_card_component_description_for_upgrade_preview";

            /// <inheritdoc />
            public static string Description => "Apply model-component card description modifiers to upgrade previews";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), nameof(CardModel.GetDescriptionForUpgradePreview), Type.EmptyTypes)];
            }

            // ReSharper disable InconsistentNaming
            public static void Postfix(CardModel __instance, ref string __result)
                // ReSharper restore InconsistentNaming
            {
                var context = new CardDescriptionComponentContext(__instance, PileType.None, null, true);
                CardModelComponentCapabilityHost.ApplyDescriptionFragments(context, ref __result);
            }
        }

        /// <summary>
        ///     Appends component hover tips to card hover tips.
        ///     将组件悬停提示追加到卡牌悬停提示。
        /// </summary>
        internal sealed class HoverTipsPatch : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "ritsulib_card_component_hover_tips";

            /// <inheritdoc />
            public static string Description => "Append model-component card hover tips";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), "HoverTips", MethodType.Getter)];
            }

            // ReSharper disable InconsistentNaming
            public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
                // ReSharper restore InconsistentNaming
            {
                var tips = CardModelComponentCapabilityHost.GetHoverTips(__instance).ToArray();
                if (tips.Length == 0)
                    return;

                __result = __result.Concat(tips).Distinct().ToArray();
            }
        }

        /// <summary>
        ///     ORs component glow predicates into gold hand glow.
        ///     将组件发光判定 OR 到金色手牌发光。
        /// </summary>
        internal sealed class ShouldGlowGoldPatch : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "ritsulib_card_component_should_glow_gold";

            /// <inheritdoc />
            public static string Description => "Merge model-component gold glow predicates";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), "ShouldGlowGold", MethodType.Getter)];
            }

            // ReSharper disable InconsistentNaming
            public static void Postfix(CardModel __instance, ref bool __result)
                // ReSharper restore InconsistentNaming
            {
                if (!__result && CardModelComponentCapabilityHost.ShouldGlowGold(__instance))
                    __result = true;
            }
        }

        /// <summary>
        ///     ORs component glow predicates into red hand glow.
        ///     将组件发光判定 OR 到红色手牌发光。
        /// </summary>
        internal sealed class ShouldGlowRedPatch : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "ritsulib_card_component_should_glow_red";

            /// <inheritdoc />
            public static string Description => "Merge model-component red glow predicates";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), "ShouldGlowRed", MethodType.Getter)];
            }

            // ReSharper disable InconsistentNaming
            public static void Postfix(CardModel __instance, ref bool __result)
                // ReSharper restore InconsistentNaming
            {
                if (!__result && CardModelComponentCapabilityHost.ShouldGlowRed(__instance))
                    __result = true;
            }
        }
    }
}
