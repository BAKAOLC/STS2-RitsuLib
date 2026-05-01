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
    ///     Fixes <see cref="NControllerCardPlay.Start" /> for <see cref="TargetType.AnyPlayer" /> in multiplayer.
    ///     Vanilla routes AnyPlayer to <c>MultiCreatureTargeting</c> (confirm-to-play, no arrow).
    ///     This patch routes it to a custom single-creature targeting flow with the correct
    ///     candidate list (all living players including self).
    /// </summary>
    internal sealed class NControllerCardPlayStartAnyPlayerPatch : IPatchMethod
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

        public static string PatchId => "card_any_player_controller_start";

        public static string Description =>
            "Route AnyPlayer cards to SingleCreatureTargeting in NControllerCardPlay.Start";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NControllerCardPlay), nameof(NControllerCardPlay.Start), Type.EmptyTypes)];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(NControllerCardPlay __instance)
            // ReSharper restore InconsistentNaming
        {
            var card = GetCard(__instance);
            if (!AnyPlayerCardTargetingHelper.IsAnyPlayerMultiplayer(card))
                return true;

            var cardNode = GetCardNode(__instance);
            if (card == null || cardNode == null)
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
            TaskHelper.RunSafely(
                SingleCreatureTargeting(__instance, TargetType.AnyPlayer));

            return false;
        }
    }

    /// <summary>
    ///     Fixes <see cref="NControllerCardPlay" />'s private <c>SingleCreatureTargeting</c> for
    ///     <see cref="TargetType.AnyPlayer" />. Vanilla's switch only handles AnyEnemy and AnyAlly,
    ///     leaving the candidate list empty for AnyPlayer (immediately cancels).
    ///     This patch provides the correct list: all living player creatures.
    /// </summary>
    internal sealed class NControllerCardPlaySingleTargetingAnyPlayerPatch : IPatchMethod
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

        public static string PatchId => "card_any_player_controller_single_targeting";

        public static string Description =>
            "Provide AnyPlayer candidate list in NControllerCardPlay.SingleCreatureTargeting";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NControllerCardPlay), "SingleCreatureTargeting", [typeof(TargetType)])];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(NControllerCardPlay __instance, TargetType targetType, ref Task __result)
            // ReSharper restore InconsistentNaming
        {
            if (targetType != TargetType.AnyPlayer)
                return true;

            __result = AnyPlayerControllerTargeting(__instance);
            return false;
        }

        private static async Task AnyPlayerControllerTargeting(NControllerCardPlay instance)
        {
            var card = GetCard(instance);
            var cardNode = GetCardNode(instance);
            if (card?.CombatState == null)
            {
                instance.CancelPlayCard();
                return;
            }

            if (cardNode == null)
            {
                instance.CancelPlayCard();
                return;
            }

            var targetManager = NTargetManager.Instance;

            var list = card.CombatState!.PlayerCreatures
                .Where(c => c is { IsAlive: true, IsPlayer: true })
                .ToList();

            if (list.Count == 0)
            {
                instance.CancelPlayCard();
                return;
            }

            var nodes = list
                .Select(c => NCombatRoom.Instance!.GetCreatureNode(c))
                .OfType<NCreature>()
                .ToList();

            if (nodes.Count == 0)
            {
                instance.CancelPlayCard();
                return;
            }

            var hoverCallable = Callable.From((NCreature c) => OnCreatureHover(instance, c));
            var unhoverCallable = Callable.From((NCreature c) => OnCreatureUnhover(instance, c));

            try
            {
                targetManager.Connect(NTargetManager.SignalName.CreatureHovered, hoverCallable);
                targetManager.Connect(NTargetManager.SignalName.CreatureUnhovered, unhoverCallable);
                targetManager.StartTargeting(
                    TargetType.AnyPlayer, cardNode, TargetMode.Controller,
                    () => !GodotObject.IsInstanceValid(instance)
                          || !NControllerManager.Instance!.IsUsingController,
                    null);

                NCombatRoom.Instance!.RestrictControllerNavigation(nodes.Select(n => n.Hitbox));
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
}
