using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Audio.Debug;
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

        public static string Description => "为自定义群体目标显示筛选后的多目标指示";

        public static bool IsCritical => false;

        /// <summary>
        ///     Targets <c>NCardPlay.ShowMultiCreatureTargetingVisuals</c>.
        ///     目标方法为 <c>NCardPlay.ShowMultiCreatureTargetingVisuals</c>。
        /// </summary>
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardPlay), "ShowMultiCreatureTargetingVisuals")];
        }

        /// <summary>
        ///     Applies custom multi-target reticle filtering after vanilla visual processing.
        ///     在原版可视化处理后应用自定义群体目标的指示器筛选逻辑。
        /// </summary>
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

        public static string Description => "将自定义单体目标识别为单目标类型";

        public static bool IsCritical => false;

        /// <summary>
        ///     Targets <see cref="ActionTargetExtensions.IsSingleTarget" />.
        ///     目标方法为 <see cref="ActionTargetExtensions.IsSingleTarget" />。
        /// </summary>
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ActionTargetExtensions), nameof(ActionTargetExtensions.IsSingleTarget))];
        }

        /// <summary>
        ///     Marks custom single-target types as valid single-target types.
        ///     将自定义单体目标类型标记为有效的单体目标类型。
        /// </summary>
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

        public static string Description => "按自定义单体目标过滤候选生物";

        /// <summary>
        ///     Targets <see cref="NTargetManager.AllowedToTargetCreature" />.
        ///     目标方法为 <see cref="NTargetManager.AllowedToTargetCreature" />。
        /// </summary>
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NTargetManager), nameof(NTargetManager.AllowedToTargetCreature))];
        }

        /// <summary>
        ///     Replaces vanilla target validation when a custom single-target type is active.
        ///     当当前类型为自定义单体目标时，替换原版目标校验逻辑。
        /// </summary>
        public static bool Prefix(NTargetManager __instance, Creature creature, ref bool __result)
        {
            if (!CustomTargetTypeResolver.TryIsAllowedSingleTarget(__instance._validTargetsType, creature,
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

        public static string Description => "按自定义单体目标过滤 CanPlayTargeting";

        /// <summary>
        ///     Targets <see cref="CardModel.CanPlayTargeting" />.
        ///     目标方法为 <see cref="CardModel.CanPlayTargeting" />。
        /// </summary>
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), nameof(CardModel.CanPlayTargeting))];
        }

        /// <summary>
        ///     Applies custom predicate-based validation for single-target cards.
        ///     对单体目标卡牌应用基于自定义谓词的可打出校验。
        /// </summary>
        public static bool Prefix(CardModel __instance, Creature? target, ref bool __result)
        {
            if (target == null)
                return true;

            if (!CustomTargetTypeResolver.TryIsAllowedSingleTarget(__instance.TargetType, target, out var allowed))
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

        public static string Description => "按自定义单体目标过滤 IsValidTarget";

        /// <summary>
        ///     Targets <see cref="CardModel.IsValidTarget" />.
        ///     目标方法为 <see cref="CardModel.IsValidTarget" />。
        /// </summary>
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), nameof(CardModel.IsValidTarget), [typeof(Creature)])];
        }

        /// <summary>
        ///     Applies custom predicate-based target validity checks.
        ///     应用基于自定义谓词的目标有效性校验。
        /// </summary>
        public static bool Prefix(CardModel __instance, Creature? target, ref bool __result)
        {
            if (target == null)
                return true;

            if (!CustomTargetTypeResolver.TryIsAllowedSingleTarget(__instance.TargetType, target, out var allowed))
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

        public static string Description => "自定义单体目标使用鼠标单体选择流程";

        /// <summary>
        ///     Targets <c>NMouseCardPlay.TargetSelection</c>.
        ///     目标方法为 <c>NMouseCardPlay.TargetSelection</c>。
        /// </summary>
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMouseCardPlay), "TargetSelection", [typeof(TargetMode)])];
        }

        /// <summary>
        ///     Intercepts mouse targeting for custom single-target types.
        ///     拦截自定义单体目标类型的鼠标选目标流程。
        /// </summary>
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
            var cardNode = GetCardNode(instance);
            if (cardNode == null)
                return;

            TryShowEvokingOrbs(instance);
            cardNode.CardHighlight.AnimFlash();
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

        public static string Description => "自定义单体目标使用手柄单体选择流程";

        public static bool IsCritical => false;

        /// <summary>
        ///     Targets <see cref="NControllerCardPlay.Start" />.
        ///     目标方法为 <see cref="NControllerCardPlay.Start" />。
        /// </summary>
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NControllerCardPlay), nameof(NControllerCardPlay.Start), Type.EmptyTypes)];
        }

        /// <summary>
        ///     Replaces controller start logic when card uses a custom single-target type.
        ///     当卡牌使用自定义单体目标类型时，替换手柄起始逻辑。
        /// </summary>
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

        public static string Description => "为自定义单体目标提供手柄候选列表";

        public static bool IsCritical => false;

        /// <summary>
        ///     Targets the private single-target controller flow.
        ///     目标方法为私有的手柄单体选目标流程。
        /// </summary>
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NControllerCardPlay), "SingleCreatureTargeting", [typeof(TargetType)])];
        }

        /// <summary>
        ///     Intercepts controller single-targeting for custom single-target types.
        ///     拦截自定义单体目标类型的手柄单体选目标逻辑。
        /// </summary>
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
                    CustomTargetTypeResolver.TryIsAllowedSingleTarget(targetType, n.Entity, out var allowed) &&
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

        public static string Description => "修复自定义单体目标 TryPlayCard 丢失目标问题";

        /// <summary>
        ///     Targets <c>NCardPlay.TryPlayCard(Creature)</c>.
        ///     目标方法为 <c>NCardPlay.TryPlayCard(Creature)</c>。
        /// </summary>
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardPlay), "TryPlayCard", [typeof(Creature)])];
        }

        /// <summary>
        ///     Reimplements try-play path for custom single-target cards.
        ///     为自定义单体目标卡牌重实现 TryPlay 执行路径。
        /// </summary>
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
