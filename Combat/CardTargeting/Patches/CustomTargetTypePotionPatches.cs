using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     Shows "throw" for potions that use custom single-target types.
    ///     对使用自定义单体目标类型的药水显示“投掷”。
    /// </summary>
    internal sealed class NPotionPopupCustomSingleTargetLabelPatch : IPatchMethod
    {
        private static readonly FieldInfo HolderField = AccessTools.DeclaredField(typeof(NPotionPopup), "_holder");

        private static readonly FieldInfo
            UseButtonField = AccessTools.DeclaredField(typeof(NPotionPopup), "_useButton");

        public static string PatchId => "card_target_custom_potion_popup_label";

        public static string Description => "Show the throw button label for custom single-target potions";

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
            if (targetType.HasValue && CustomTargetTypeResolver.IsCustomSingleTargetType(targetType.Value))
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
}
