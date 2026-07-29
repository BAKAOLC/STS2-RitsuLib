using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Combat;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline.Patches
{
    internal static class ModCardHandOutlinePatchHelper
    {
        private static readonly FieldInfo? HandField = AccessTools.Field(typeof(NHandCardHolder), "_hand");

        internal static bool TryGetRule(
            NHandCardHolder? holder,
            out CardModel model,
            out ModCardHandOutlineEvaluation evaluation)
        {
            model = null!;
            evaluation = default;

            if (!TryGetCardModel(holder, out var m))
                return false;

            var evaluated = ModCardHandOutlineRegistry.EvaluateBest(m);
            if (evaluated is not { } e)
                return false;

            model = m;
            evaluation = e;
            return true;
        }

        internal static bool ApplyHighlight(
            NHandCardHolder? holder,
            CardModel model,
            ModCardHandOutlineEvaluation evaluation)
        {
            if (CombatManager.Instance is not { IsInProgress: true } ||
                !TryGetCardModel(holder, out var currentModel) ||
                !ReferenceEquals(currentModel, model))
                return false;

            try
            {
                var cardNode = holder!.CardNode;
                if (cardNode == null || !GodotObject.IsInstanceValid(cardNode) ||
                    !GodotObject.IsInstanceValid(cardNode.CardHighlight))
                    return false;

                var builtInShow = ShouldShowBuiltInHighlight(holder!, model);
                var force = evaluation.Rule.VisibleWhenUnplayable && !builtInShow;
                if (!builtInShow && !force)
                    return false;

                var highlight = cardNode.CardHighlight;
                if (force)
                    highlight.AnimShow();

                highlight.Modulate = evaluation.Color;
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        internal static void ApplyFlash(
            NHandCardHolder? holder,
            CardModel model,
            ModCardHandOutlineEvaluation evaluation)
        {
            if (!IsHolderUsable(holder))
                return;

            try
            {
                if (AccessTools.Field(typeof(NHandCardHolder), "_flash")?.GetValue(holder!) is not Control flash ||
                    !GodotObject.IsInstanceValid(flash))
                    return;

                var builtInShow = ShouldShowBuiltInHighlight(holder!, model);
                var force = evaluation.Rule.VisibleWhenUnplayable && !builtInShow;
                if (!builtInShow && !force)
                    return;

                flash.Modulate = evaluation.Color;
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private static bool TryGetCardModel(NHandCardHolder? holder, out CardModel model)
        {
            model = null!;

            if (!IsHolderUsable(holder))
                return false;

            try
            {
                if (holder!.CardNode is not { } cardNode ||
                    !GodotObject.IsInstanceValid(cardNode) ||
                    cardNode.Model is not { } m)
                    return false;

                model = m;
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        private static bool ShouldShowBuiltInHighlight(NHandCardHolder holder, CardModel model)
        {
            var canPlay = model.CanPlay();
            var inPlayPhase = model.IsOwnerPlayPhase();
            var shouldGlowRed = inPlayPhase && model.ShouldGlowRed;

            var selectModeOverride =
                (HandField?.GetValue(holder) as NPlayerHand)?.SelectModeGoldGlowOverride;
            var shouldGlowGold = selectModeOverride != null
                ? selectModeOverride(model)
                : inPlayPhase && canPlay && model.ShouldGlowGold;

            return canPlay || shouldGlowRed || shouldGlowGold;
        }

        private static bool IsHolderUsable(NHandCardHolder? holder)
        {
            if (holder == null || !GodotObject.IsInstanceValid(holder))
                return false;

            try
            {
                return holder.IsNodeReady() && holder.IsInsideTree();
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }
    }
}
