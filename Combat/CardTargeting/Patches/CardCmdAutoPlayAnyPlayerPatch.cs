using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     Fixes <see cref="CardCmd.AutoPlay" /> for <see cref="TargetType.AnyPlayer" />.
    ///     Vanilla only resolves random targets for AnyEnemy and AnyAlly when target is null.
    ///     This patch adds the same RNG fallback for AnyPlayer (pick a random living player).
    /// </summary>
    internal sealed class CardCmdAutoPlayAnyPlayerPatch : IPatchMethod
    {
        public static string PatchId => "card_any_player_auto_play";

        public static string Description =>
            "Resolve random AnyPlayer target in CardCmd.AutoPlay";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardCmd), nameof(CardCmd.AutoPlay))];
        }

        // ReSharper disable InconsistentNaming
        public static void Prefix(CardModel card, ref Creature? target)
            // ReSharper restore InconsistentNaming
        {
            if (card.TargetType != TargetType.AnyPlayer || target != null)
                return;

            var combatState = card.CombatState ?? card.Owner.Creature.CombatState;
            if (combatState == null)
                return;

            var candidates = combatState.PlayerCreatures
                .Where(c => c is { IsAlive: true, IsPlayer: true });
            target = card.Owner.RunState.Rng.CombatTargets.NextItem(candidates);
        }
    }
}
