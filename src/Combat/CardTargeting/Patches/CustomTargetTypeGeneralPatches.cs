using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     Shows filtered multi-target visuals for registered custom multi-target types.
    ///     为已注册的自定义群体目标类型显示按规则筛选后的多目标可视化指示。
    /// </summary>
    internal sealed class NCardPlayShowMultiCreatureTargetingVisualsCustomTargetTypePatch : IPatchMethod
    {
        public static string PatchId => "card_target_custom_show_multi_target_visuals";

        public static string Description => "Show filtered multi-target visuals for custom target types";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardPlay), "ShowMultiCreatureTargetingVisuals")];
        }

        public static void Postfix(NCardPlay __instance)
        {
            if (__instance.Card is not { TargetType: var targetType })
                return;

            if (!CustomTargetTypeResolver.IsCustomMultiTargetType(targetType))
                return;

            __instance.CardNode?.UpdateVisuals(
                __instance.Card.Pile!.Type,
                CardPreviewMode.MultiCreatureTargeting);

            var room = NCombatRoom.Instance;
            if (room == null)
                return;

            foreach (var creatureNode in room.CreatureNodes)
                if (CustomTargetTypeResolver.TryShouldIncludeMultiTarget(targetType, creatureNode.Entity,
                        __instance.Card.Owner,
                        out var include) &&
                    include)
                    creatureNode.ShowMultiselectReticle();
        }
    }

    /// <summary>
    ///     Treats registered custom single-target types as single-target in target helpers.
    ///     将已注册的自定义单体目标类型识别为单体目标。
    /// </summary>
    internal sealed class ActionTargetExtensionsIsSingleTargetCustomTargetTypePatch : IPatchMethod
    {
        public static string PatchId => "card_target_custom_is_single_target";

        public static string Description => "Treat custom single-target types as single-target";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ActionTargetExtensions), nameof(ActionTargetExtensions.IsSingleTarget))];
        }

        public static void Postfix(TargetType targetType, ref bool __result)
        {
            if (__result)
                return;

            if (CustomTargetTypeResolver.IsCustomSingleTargetType(targetType))
                __result = true;
        }
    }

    /// <summary>
    ///     Delegates target-eligibility checks to custom single-target predicates.
    ///     将目标可选性判定委托给自定义单体目标谓词。
    /// </summary>
    internal sealed class NTargetManagerAllowedToTargetCreatureCustomTargetTypePatch : IPatchMethod
    {
        public static string PatchId => "card_target_custom_allowed_to_target_creature";

        public static string Description => "Filter candidate creatures with custom single-target predicates";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NTargetManager), nameof(NTargetManager.AllowedToTargetCreature))];
        }

        public static bool Prefix(NTargetManager __instance, Creature creature, ref bool __result)
        {
            var player = LocalContext.GetMe(creature.CombatState);
            if (player == null)
                return true;

            var context = CustomTargetTypeSelectionContext.CreateContext(__instance, creature, player);
            if (!CustomTargetTypeResolver.TryIsAllowedSingleTarget(__instance._validTargetsType, context,
                    out var allowed))
                return true;

            __result = allowed;
            return false;
        }
    }

    /// <summary>
    ///     Delegates <see cref="CardModel.CanPlayTargeting" /> to custom single-target predicates.
    ///     将 <see cref="CardModel.CanPlayTargeting" /> 的判定委托给自定义单体目标谓词。
    /// </summary>
    internal sealed class CardModelCanPlayTargetingCustomTargetTypePatch : IPatchMethod
    {
        public static string PatchId => "card_target_custom_can_play_targeting";

        public static string Description => "Filter CanPlayTargeting with custom single-target predicates";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), nameof(CardModel.CanPlayTargeting))];
        }

        public static bool Prefix(CardModel __instance, Creature? target, ref bool __result)
        {
            if (target == null)
                return true;

            if (!CustomTargetTypeResolver.TryIsAllowedSingleTarget(__instance.TargetType,
                    CustomTargetContext.ForCard(target, __instance),
                    out var allowed))
                return true;

            __result = allowed;
            return false;
        }
    }

    /// <summary>
    ///     Delegates <see cref="CardModel.IsValidTarget" /> to custom single-target predicates.
    ///     将 <see cref="CardModel.IsValidTarget" /> 的判定委托给自定义单体目标谓词。
    /// </summary>
    internal sealed class CardModelIsValidTargetCustomTargetTypePatch : IPatchMethod
    {
        public static string PatchId => "card_target_custom_is_valid_target";

        public static string Description => "Filter IsValidTarget with custom single-target predicates";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), nameof(CardModel.IsValidTarget), [typeof(Creature)])];
        }

        public static bool Prefix(CardModel __instance, Creature? target, ref bool __result)
        {
            if (target == null)
                return true;

            if (!CustomTargetTypeResolver.TryIsAllowedSingleTarget(__instance.TargetType,
                    CustomTargetContext.ForCard(target, __instance),
                    out var allowed))
                return true;

            __result = allowed;
            return false;
        }
    }

    /// <summary>
    ///     Routes mouse target selection to single-target flow for custom single-target types.
    ///     对自定义单体目标类型，将鼠标选目标流程路由到单体选目标分支。
    /// </summary>
    internal sealed class NMouseCardPlayTargetSelectionCustomTargetTypePatch : IPatchMethod
    {
        private static readonly Func<NCardPlay, CardModel?> GetCard =
            AccessTools.MethodDelegate<Func<NCardPlay, CardModel?>>(
                AccessTools.DeclaredPropertyGetter(typeof(NCardPlay), "Card"));

        private static readonly Func<NCardPlay, NCard?> GetCardNode =
            AccessTools.MethodDelegate<Func<NCardPlay, NCard?>>(
                AccessTools.DeclaredPropertyGetter(typeof(NCardPlay), "CardNode"));

        private static readonly Action<NCardPlay> TryShowEvokingOrbs =
            AccessTools.MethodDelegate<Action<NCardPlay>>(
                AccessTools.DeclaredMethod(typeof(NCardPlay), "TryShowEvokingOrbs"));

        private static readonly Func<NMouseCardPlay, TargetMode, TargetType, Task> SingleCreatureTargeting =
            AccessTools.MethodDelegate<Func<NMouseCardPlay, TargetMode, TargetType, Task>>(
                AccessTools.DeclaredMethod(typeof(NMouseCardPlay), "SingleCreatureTargeting",
                    [typeof(TargetMode), typeof(TargetType)]));

        public static string PatchId => "card_target_custom_mouse_target_selection";

        public static string Description => "Use mouse single-target flow for custom single-target types";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMouseCardPlay), "TargetSelection", [typeof(TargetMode)])];
        }

        public static bool Prefix(NMouseCardPlay __instance, TargetMode targetMode, ref Task __result)
        {
            var card = GetCard(__instance);
            if (card == null || !CustomTargetTypeResolver.IsCustomSingleTargetType(card.TargetType))
                return true;

            __result = RunTargeting(__instance, targetMode, card.TargetType);
            return false;
        }

        /// <summary>
        ///     Executes custom single-target mouse selection with card highlight feedback.
        ///     执行自定义单体目标的鼠标选择流程并保持卡牌高亮反馈。
        /// </summary>
        private static async Task RunTargeting(NMouseCardPlay instance, TargetMode targetMode, TargetType targetType)
        {
            var card = GetCard(instance);
            var cardNode = GetCardNode(instance);
            if (card == null || cardNode == null)
                return;

            TryShowEvokingOrbs(instance);
            cardNode.CardHighlight.AnimFlash();
            using var sourceContext = CustomTargetTypeSelectionContext.PushCard(NTargetManager.Instance, card);
            await SingleCreatureTargeting(instance, targetMode, targetType);
        }
    }

    /// <summary>
    ///     Routes controller start flow to single-target selection for custom single-target types.
    ///     对自定义单体目标类型，将手柄开始流程路由到单体选目标流程。
    /// </summary>
    internal sealed class NControllerCardPlayStartCustomTargetTypePatch : IPatchMethod
    {
        private static readonly Func<NCardPlay, CardModel?> GetCard =
            AccessTools.MethodDelegate<Func<NCardPlay, CardModel?>>(
                AccessTools.DeclaredPropertyGetter(typeof(NCardPlay), "Card"));

        private static readonly Func<NCardPlay, NCard?> GetCardNode =
            AccessTools.MethodDelegate<Func<NCardPlay, NCard?>>(
                AccessTools.DeclaredPropertyGetter(typeof(NCardPlay), "CardNode"));

        private static readonly Action<NCardPlay> TryShowEvokingOrbs =
            AccessTools.MethodDelegate<Action<NCardPlay>>(
                AccessTools.DeclaredMethod(typeof(NCardPlay), "TryShowEvokingOrbs"));

        private static readonly Action<NCardPlay> CenterCard =
            AccessTools.MethodDelegate<Action<NCardPlay>>(
                AccessTools.DeclaredMethod(typeof(NCardPlay), "CenterCard"));

        private static readonly Action<NCardPlay, CardModel> CannotPlayThisCardFtueCheck =
            AccessTools.MethodDelegate<Action<NCardPlay, CardModel>>(
                AccessTools.DeclaredMethod(typeof(NCardPlay), "CannotPlayThisCardFtueCheck", [typeof(CardModel)]));

        private static readonly Func<NControllerCardPlay, TargetType, Task> SingleCreatureTargeting =
            AccessTools.MethodDelegate<Func<NControllerCardPlay, TargetType, Task>>(
                AccessTools.DeclaredMethod(typeof(NControllerCardPlay), "SingleCreatureTargeting",
                    [typeof(TargetType)]));

        public static string PatchId => "card_target_custom_controller_start";

        public static string Description => "Use controller single-target flow for custom single-target types";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NControllerCardPlay), nameof(NControllerCardPlay.Start), Type.EmptyTypes)];
        }

        public static bool Prefix(NControllerCardPlay __instance)
        {
            var card = GetCard(__instance);
            if (card == null || !CustomTargetTypeResolver.IsCustomSingleTargetType(card.TargetType))
                return true;

            var cardNode = GetCardNode(__instance);
            if (cardNode == null)
                return false;

            NDebugAudioManager.Instance?.Play("card_select.mp3");
            NHoverTipSet.Remove(__instance.Holder);

            if (!card.CanPlay(out var reason, out var preventer))
            {
                CannotPlayThisCardFtueCheck(__instance, card);
                __instance.CancelPlayCard();
                var line = reason.GetPlayerDialogueLine(preventer);
                if (line != null)
                    NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(
                        NThoughtBubbleVfx.Create(line.GetFormattedText(), card.Owner.Creature, 1.0));
                return false;
            }

            TryShowEvokingOrbs(__instance);
            cardNode.CardHighlight.AnimFlash();
            CenterCard(__instance);
            TaskHelper.RunSafely(SingleCreatureTargeting(__instance, card.TargetType));
            return false;
        }
    }

    /// <summary>
    ///     Provides filtered controller candidate lists for custom single-target types.
    ///     为自定义单体目标类型提供按规则筛选的手柄候选目标列表。
    /// </summary>
    internal sealed class NControllerCardPlaySingleTargetingCustomTargetTypePatch : IPatchMethod
    {
        private static readonly Func<NCardPlay, CardModel?> GetCard =
            AccessTools.MethodDelegate<Func<NCardPlay, CardModel?>>(
                AccessTools.DeclaredPropertyGetter(typeof(NCardPlay), "Card"));

        private static readonly Func<NCardPlay, NCard?> GetCardNode =
            AccessTools.MethodDelegate<Func<NCardPlay, NCard?>>(
                AccessTools.DeclaredPropertyGetter(typeof(NCardPlay), "CardNode"));

        private static readonly Action<NCardPlay, NCreature> OnCreatureHover =
            AccessTools.MethodDelegate<Action<NCardPlay, NCreature>>(
                AccessTools.DeclaredMethod(typeof(NCardPlay), "OnCreatureHover", [typeof(NCreature)]));

        private static readonly Action<NCardPlay, NCreature> OnCreatureUnhover =
            AccessTools.MethodDelegate<Action<NCardPlay, NCreature>>(
                AccessTools.DeclaredMethod(typeof(NCardPlay), "OnCreatureUnhover", [typeof(NCreature)]));

        private static readonly Action<NCardPlay, Creature?> TryPlayCard =
            AccessTools.MethodDelegate<Action<NCardPlay, Creature?>>(
                AccessTools.DeclaredMethod(typeof(NCardPlay), "TryPlayCard", [typeof(Creature)]));

        public static string PatchId => "card_target_custom_controller_single_targeting";

        public static string Description => "Provide controller candidates for custom single-target types";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NControllerCardPlay), "SingleCreatureTargeting", [typeof(TargetType)])];
        }

        public static bool Prefix(NControllerCardPlay __instance, TargetType targetType, ref Task __result)
        {
            if (!CustomTargetTypeResolver.IsCustomSingleTargetType(targetType))
                return true;

            __result = RunTargeting(__instance, targetType);
            return false;
        }

        /// <summary>
        ///     Runs controller targeting with candidates filtered by custom predicate.
        ///     执行按自定义谓词筛选候选集的手柄选目标流程。
        /// </summary>
        private static async Task RunTargeting(NControllerCardPlay instance, TargetType targetType)
        {
            var card = GetCard(instance);
            var cardNode = GetCardNode(instance);
            if (card?.CombatState == null || cardNode == null)
            {
                instance.CancelPlayCard();
                return;
            }

            var room = NCombatRoom.Instance;
            if (room == null)
            {
                instance.CancelPlayCard();
                return;
            }

            var nodes = room.CreatureNodes
                .Where(n =>
                    CustomTargetTypeResolver.TryIsAllowedSingleTarget(targetType,
                        CustomTargetContext.ForCard(n.Entity, card),
                        out var allowed) &&
                    allowed)
                .ToList();

            if (nodes.Count == 0)
            {
                instance.CancelPlayCard();
                return;
            }

            var targetManager = NTargetManager.Instance;
            var hoverCallable = Callable.From((NCreature c) => OnCreatureHover(instance, c));
            var unhoverCallable = Callable.From((NCreature c) => OnCreatureUnhover(instance, c));

            try
            {
                using var sourceContext = CustomTargetTypeSelectionContext.PushCard(targetManager, card);
                targetManager.Connect(NTargetManager.SignalName.CreatureHovered, hoverCallable);
                targetManager.Connect(NTargetManager.SignalName.CreatureUnhovered, unhoverCallable);

                targetManager.StartTargeting(
                    targetType,
                    cardNode,
                    TargetMode.Controller,
                    () => !GodotObject.IsInstanceValid(instance)
                          || !NControllerManager.Instance!.IsUsingController,
                    null);

                room.RestrictControllerNavigation(nodes.Select(n => n.Hitbox));
                nodes.First().Hitbox.TryGrabFocus();

                var selected = (NCreature?)await targetManager.SelectionFinished();

                if (!GodotObject.IsInstanceValid(instance))
                    return;

                if (selected != null)
                    TryPlayCard(instance, selected.Entity);
                else
                    instance.CancelPlayCard();
            }
            finally
            {
                if (targetManager.IsConnected(NTargetManager.SignalName.CreatureHovered, hoverCallable))
                    targetManager.Disconnect(NTargetManager.SignalName.CreatureHovered, hoverCallable);

                if (targetManager.IsConnected(NTargetManager.SignalName.CreatureUnhovered, unhoverCallable))
                    targetManager.Disconnect(NTargetManager.SignalName.CreatureUnhovered, unhoverCallable);
            }
        }
    }

    /// <summary>
    ///     Preserves selected target when trying to play cards with custom single-target types.
    ///     在尝试打出自定义单体目标卡牌时保留并传递已选择目标。
    /// </summary>
    internal sealed class NCardPlayTryPlayCardCustomTargetTypePatch : IPatchMethod
    {
        private static readonly Action<NCardPlay, bool> InvokeCleanup =
            AccessTools.MethodDelegate<Action<NCardPlay, bool>>(
                AccessTools.DeclaredMethod(typeof(NCardPlay), "Cleanup", [typeof(bool)])!);

        public static string PatchId => "card_target_custom_try_play_card";

        public static string Description => "Preserve selected targets in TryPlayCard for custom single-target types";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardPlay), "TryPlayCard", [typeof(Creature)])];
        }

        public static bool Prefix(NCardPlay __instance, Creature? target)
        {
            var card = __instance.Card;
            if (card == null || !CustomTargetTypeResolver.IsCustomSingleTargetType(card.TargetType))
                return true;

            if (target == null || __instance.Holder.CardModel == null)
            {
                __instance.CancelPlayCard();
                return false;
            }

            if (!__instance.Holder.CardModel.CanPlayTargeting(target))
            {
                __instance.CannotPlayThisCardFtueCheck(__instance.Holder.CardModel);
                __instance.CancelPlayCard();
                return false;
            }

            __instance._isTryingToPlayCard = true;
            var played = card.TryManualPlay(target);
            __instance._isTryingToPlayCard = false;

            if (played)
            {
                __instance.AutoDisableCannotPlayCardFtueCheck();
                if (__instance.Holder.IsInsideTree())
                {
                    var size = __instance.GetViewport().GetVisibleRect().Size;
                    __instance.Holder.SetTargetPosition(new(size.X / 2f, size.Y - __instance.Holder.Size.Y));
                }

                InvokeCleanup(__instance, true);
                CardPlayUiFocus.AfterCardPlayFinished();
            }
            else
            {
                __instance.CancelPlayCard();
            }

            return false;
        }
    }
}
