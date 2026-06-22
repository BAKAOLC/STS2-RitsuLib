using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     Shows "throw" for potions that use custom creature-target types.
    ///     对使用自定义生物目标类型的药水显示“投掷”。
    /// </summary>
    internal sealed class NPotionPopupCustomTargetLabelPatch : IPatchMethod
    {
        private static readonly FieldInfo HolderField = AccessTools.DeclaredField(typeof(NPotionPopup), "_holder");

        private static readonly FieldInfo
            UseButtonField = AccessTools.DeclaredField(typeof(NPotionPopup), "_useButton");

        public static string PatchId => "card_target_custom_potion_popup_label";

        public static string Description => "Show the throw button label for custom creature-target potions";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NPotionPopup), nameof(NPotionPopup._Ready))];
        }

        public static void Postfix(NPotionPopup __instance)
        {
            if (HolderField.GetValue(__instance) is not NPotionHolder holder ||
                UseButtonField.GetValue(__instance) is not NPotionPopupButton useButton)
                return;

            var targetType = holder.Potion?.Model.TargetType;
            if (targetType.HasValue &&
                (CustomTargetTypeResolver.IsCustomSingleTargetType(targetType.Value) ||
                 CustomTargetTypeResolver.IsCustomMultiTargetType(targetType.Value)))
                useButton.SetLocKey("POTION_POPUP.throw");
        }
    }

    /// <summary>
    ///     Routes custom single-target potions into creature targeting instead of self-use.
    ///     将自定义单体目标药水路由到生物选目标流程，而不是自用。
    /// </summary>
    internal sealed class NPotionHolderUsePotionCustomSingleTargetPatch : IPatchMethod
    {
        private static readonly Func<NPotionHolder, bool> ShouldCancelTargeting =
            AccessTools.MethodDelegate<Func<NPotionHolder, bool>>(
                AccessTools.DeclaredMethod(typeof(NPotionHolder), "ShouldCancelTargeting"));

        public static string PatchId => "card_target_custom_potion_use";

        public static string Description => "Route custom single-target potions through targeting";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NPotionHolder), nameof(NPotionHolder.UsePotion))];
        }

        public static bool Prefix(NPotionHolder __instance, ref Task __result)
        {
            var potion = __instance.Potion?.Model;
            if (potion == null || !CustomTargetTypeResolver.IsCustomSingleTargetType(potion.TargetType))
                return true;

            if (!CombatManager.Instance.IsInProgress || NCombatRoom.Instance == null)
                return true;

            __result = UseCustomTargetPotion(__instance, potion);
            return false;
        }

        private static async Task UseCustomTargetPotion(NPotionHolder holder, PotionModel potion)
        {
            RunManager.Instance.HoveredModelTracker.OnLocalPotionSelected(potion);
            try
            {
                await TargetCreature(holder, potion);
            }
            finally
            {
                RunManager.Instance.HoveredModelTracker.OnLocalPotionDeselected();
            }
        }

        private static async Task TargetCreature(NPotionHolder holder, PotionModel potion)
        {
            var room = NCombatRoom.Instance;
            var targetManager = NTargetManager.Instance;
            var targetType = potion.TargetType;
            var isUsingController = NControllerManager.Instance?.IsUsingController == true;

            if (room == null)
            {
                holder.TryGrabFocus();
                return;
            }

            var controllerTargets = isUsingController
                ? room.CreatureNodes
                    .Where(n =>
                        CustomTargetTypeResolver.TryIsAllowedSingleTarget(targetType,
                            CustomTargetContext.ForPotion(n.Entity, potion),
                            out var allowed) &&
                        allowed)
                    .ToList()
                : [];

            if (isUsingController && controllerTargets.Count == 0)
            {
                holder.TryGrabFocus();
                return;
            }

            var startPosition = holder.GlobalPosition
                                + Vector2.Right * holder.Size.X * 0.5f
                                + Vector2.Down * 50f;

            using var sourceContext = CustomTargetTypeSelectionContext.PushPotion(targetManager, potion);
            targetManager.StartTargeting(
                targetType,
                startPosition,
                isUsingController ? TargetMode.Controller : TargetMode.ClickMouseToTarget,
                () => ShouldCancelTargeting(holder),
                node => node is NCreature);

            if (isUsingController)
            {
                room.RestrictControllerNavigation(controllerTargets.Select(n => n.Hitbox));
                controllerTargets.First().Hitbox.TryGrabFocus();
            }

            try
            {
                var selected = await targetManager.SelectionFinished();
                if (selected is NCreature creature)
                    potion.EnqueueManualUse(creature.Entity);
            }
            finally
            {
                room.EnableControllerNavigation();
                holder.TryGrabFocus();
            }
        }
    }

    /// <summary>
    ///     Validates custom single-target potion targets through their registered predicate.
    ///     通过注册谓词校验自定义单体目标药水的目标。
    /// </summary>
    internal sealed class PotionModelIsValidTargetCustomTargetTypePatch : IPatchMethod
    {
        public static string PatchId => "card_target_custom_potion_is_valid_target";

        public static string Description => "Filter potion IsValidTarget with custom single-target predicates";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PotionModel), nameof(PotionModel.IsValidTarget), [typeof(Creature)])];
        }

        public static bool Prefix(PotionModel __instance, Creature? target, ref bool __result)
        {
            if (!CustomTargetTypeResolver.IsCustomSingleTargetType(__instance.TargetType))
                return true;

            if (target == null)
            {
                __result = false;
                return false;
            }

            if (!CustomTargetTypeResolver.TryIsAllowedSingleTarget(__instance.TargetType,
                    CustomTargetContext.ForPotion(target, __instance),
                    out var allowed))
                return true;

            __result = allowed;
            return false;
        }
    }

    /// <summary>
    ///     Uses custom multi-target predicates to place potion throw VFX over the affected creature group.
    ///     使用自定义群体目标谓词，将药水投掷特效定位到实际受影响的生物集合中心。
    /// </summary>
    internal sealed class PotionModelOnUseWrapperCustomMultiTargetVfxPatch : IPatchMethod
    {
        private static readonly MethodInfo? GetCreaturesOnSideMethod =
            AccessTools.DeclaredMethod(typeof(ICombatState), nameof(ICombatState.GetCreaturesOnSide),
                [typeof(CombatSide)]);

        private static readonly MethodInfo? GetPotionVfxTargetsMethod =
            AccessTools.DeclaredMethod(typeof(PotionModelOnUseWrapperCustomMultiTargetVfxPatch),
                nameof(GetPotionVfxTargets));

        public static string PatchId => "card_target_custom_potion_multi_target_vfx";

        public static string Description => "Use custom multi-target predicates for potion throw VFX";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                PatchTarget.AsyncMethod<PotionModel>(
                    nameof(PotionModel.OnUseWrapper),
                    typeof(PlayerChoiceContext),
                    typeof(Creature)),
            ];
        }

        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            const string operation = "[CustomTargetType] Redirect potion multi-target VFX target list";
            var rewriter = HarmonyIlRewriter.From(instructions);

            if (GetCreaturesOnSideMethod == null || GetPotionVfxTargetsMethod == null)
            {
                RitsuLibFramework.Logger.Warn($"{operation}: Could not resolve target methods.");
                return rewriter.Instructions();
            }

            var stateMachineType = __originalMethod.DeclaringType;
            if (stateMachineType == null || !TryResolvePotionField(stateMachineType, out var potionField))
            {
                RitsuLibFramework.Logger.Warn(
                    $"{operation}: Could not resolve PotionModel field on {stateMachineType?.FullName ?? "<null>"}.");
                return rewriter.Instructions();
            }

            var report = rewriter.ReplaceEach(
                operation,
                static (code, index) => HarmonyIl.IsCallTo(code[index], GetCreaturesOnSideMethod),
                (_, _) =>
                [
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, potionField),
                    new(OpCodes.Call, GetPotionVfxTargetsMethod),
                ],
                static code => code.Any(instruction => HarmonyIl.IsCallTo(instruction, GetPotionVfxTargetsMethod)));
            if (!report.Succeeded || report.Applied != 2)
                RitsuLibFramework.Logger.Warn(report.Describe());

            return rewriter.InstructionsChecked(operation);
        }

        private static IReadOnlyList<Creature> GetPotionVfxTargets(
            ICombatState combatState,
            CombatSide side,
            PotionModel potion)
        {
            if (CustomTargetTypeResolver.IsCustomMultiTargetType(potion.TargetType))
                return potion.GetTargets();

            return combatState.GetCreaturesOnSide(side);
        }

        private static bool TryResolvePotionField(Type stateMachineType, out FieldInfo potionField)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            potionField = stateMachineType.GetFields(flags)
                .FirstOrDefault(static field => field.FieldType == typeof(PotionModel))!;
            return potionField != null;
        }
    }
}
