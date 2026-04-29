using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     When a card fly visual is spawned after the card has already been reparented, the node may no
    ///     longer carry enough "old pile" context to choose a correct start position. This patch consults
    ///     <see cref="ModCardPileFlightHistory" /> (fed by <see cref="CardPile.CardRemoved" />) to recover the
    ///     source pile and applies <see cref="ModCardPileSpec.FlightStartPositionResolver" /> when the source
    ///     pile is a mod pile.
    /// </summary>
    public sealed class ModCardPileCardFlyVfxStartPositionPatch : IPatchMethod
    {
        private static readonly FieldInfo? StartPositionField =
            AccessTools.Field(typeof(NCardFlyVfx), "_startPos");

        /// <inheritdoc />
        public static string PatchId => "ritsulib_mod_pile_card_fly_vfx_start_position";

        /// <inheritdoc />
        public static string Description => "Allow mod piles to customize NCardFlyVfx start positions";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardFlyVfx), nameof(NCardFlyVfx.Create))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Post-processes the created fly vfx and overwrites its start position when the recovered
        ///     source pile is a mod pile with a start-position resolver.
        /// </summary>
        public static void Postfix(NCard card, Vector2 end, bool isAddingToPile, string trailPath,
            ref NCardFlyVfx? __result)
        {
            if (__result == null || StartPositionField == null)
                return;

            var model = card.Model;
            if (model == null)
                return;

            var oldPile = ModCardPileFlightHistory.TryGetLastRemovedPile(model);
            if (oldPile == null)
                return;
            if (!ModCardPileRegistry.TryGetByPileType(oldPile.Type, out var definition))
                return;

            var resolver = definition.FlightStartPositionResolver;
            if (resolver == null)
                return;

            var targetPile = model.Pile ?? oldPile;
            var defaultStartPosition = ModCardPileLayout.GetTargetPosition(definition, card);
            var context =
                new ModCardPileFlightStartContext(definition, oldPile, targetPile, defaultStartPosition, card);
            var resolved = resolver(context) ?? defaultStartPosition;

            StartPositionField.SetValue(__result, resolved);
            card.GlobalPosition = resolved;
        }
        // ReSharper restore InconsistentNaming
    }
}
