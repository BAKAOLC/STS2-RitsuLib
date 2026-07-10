using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Scaffolding.Combat;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline.Patches
{
    internal static class ModCardHandOutlinePatchHelper
    {
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

        internal static void ApplyHighlight(
            NHandCardHolder? holder,
            CardModel model,
            ModCardHandOutlineEvaluation evaluation)
        {
            if (CombatManager.Instance is not { IsInProgress: true } ||
                !TryGetCardModel(holder, out var currentModel) ||
                !ReferenceEquals(currentModel, model))
                return;

            try
            {
                var cardNode = holder!.CardNode;
                if (cardNode == null || !GodotObject.IsInstanceValid(cardNode) ||
                    !GodotObject.IsInstanceValid(cardNode.CardHighlight))
                    return;

                var inPlayPhase = model.IsOwnerPlayPhase();
                var canPlay = model.CanPlay();
                var shouldGlowRed = inPlayPhase && model.ShouldGlowRed;
                var shouldGlowGold = inPlayPhase && canPlay && model.ShouldGlowGold;
                var vanillaShow = canPlay || shouldGlowRed || shouldGlowGold;
                var force = evaluation.Rule.VisibleWhenUnplayable && !vanillaShow;
                if (!vanillaShow && !force)
                    return;

                var highlight = cardNode.CardHighlight;
                if (force)
                    highlight.AnimShow();

                var c = evaluation.Color;
                highlight.Modulate = new(c.R, c.G, c.B, highlight.Modulate.A);
            }
            catch (ObjectDisposedException)
            {
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

                var inPlayPhase = model.IsOwnerPlayPhase();
                var canPlay = model.CanPlay();
                var shouldGlowRed = inPlayPhase && model.ShouldGlowRed;
                var shouldGlowGold = inPlayPhase && canPlay && model.ShouldGlowGold;
                var vanillaShow = canPlay || shouldGlowRed || shouldGlowGold;
                var force = evaluation.Rule.VisibleWhenUnplayable && !vanillaShow;
                if (!vanillaShow && !force)
                    return;

                var c = evaluation.Color;
                flash.Modulate = new(c.R, c.G, c.B, flash.Modulate.A);
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
