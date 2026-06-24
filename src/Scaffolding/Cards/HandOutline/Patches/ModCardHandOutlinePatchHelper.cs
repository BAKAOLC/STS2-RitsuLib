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
            NHandCardHolder holder,
            out CardModel model,
            out ModCardHandOutlineEvaluation evaluation)
        {
            model = null!;
            evaluation = default;

            if (!holder.IsNodeReady() || holder.CardNode?.Model is not { } m)
                return false;

            var evaluated = ModCardHandOutlineRegistry.EvaluateBest(m);
            if (evaluated is not { } e)
                return false;

            model = m;
            evaluation = e;
            return true;
        }

        internal static void ApplyHighlight(
            NHandCardHolder holder,
            CardModel model,
            ModCardHandOutlineEvaluation evaluation)
        {
            if (CombatManager.Instance is not { IsInProgress: true })
                return;

            var inPlayPhase = model.IsOwnerPlayPhase();
            var shouldGlowRed = inPlayPhase && model.ShouldGlowRed;
            var shouldGlowGold = inPlayPhase && model.CanPlay() && model.ShouldGlowGold;
            var vanillaShow = model.CanPlay() || shouldGlowRed || shouldGlowGold;
            var force = evaluation.Rule.VisibleWhenUnplayable && !vanillaShow;
            if (!vanillaShow && !force)
                return;

            var highlight = holder.CardNode!.CardHighlight;
            if (force)
                highlight.AnimShow();

            var c = evaluation.Color;
            highlight.Modulate = new(c.R, c.G, c.B, highlight.Modulate.A);
        }

        internal static void ApplyFlash(
            NHandCardHolder holder,
            CardModel model,
            ModCardHandOutlineEvaluation evaluation)
        {
            if (AccessTools.Field(typeof(NHandCardHolder), "_flash")?.GetValue(holder) is not Control flash ||
                !GodotObject.IsInstanceValid(flash))
                return;

            var inPlayPhase = model.IsOwnerPlayPhase();
            var shouldGlowRed = inPlayPhase && model.ShouldGlowRed;
            var shouldGlowGold = inPlayPhase && model.CanPlay() && model.ShouldGlowGold;
            var vanillaShow = model.CanPlay() || shouldGlowRed || shouldGlowGold;
            var force = evaluation.Rule.VisibleWhenUnplayable && !vanillaShow;
            if (!vanillaShow && !force)
                return;

            var c = evaluation.Color;
            flash.Modulate = new(c.R, c.G, c.B, flash.Modulate.A);
        }
    }
}
