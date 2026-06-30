using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     Fixes <see cref="CardCmd.AutoPlay" /> for <see cref="TargetType.AnyPlayer" />.
    ///     Vanilla only resolves random targets for AnyEnemy and AnyAlly when target is null.
    ///     This patch adds the same RNG fallback for AnyPlayer (pick a random living player).
    ///     修复 <see cref="CardCmd.AutoPlay" /> 对 <see cref="TargetType.AnyPlayer" /> 的处理。
    ///     原版只会在目标为 null 时为 AnyEnemy 和 AnyAlly 解析随机目标。
    ///     此补丁为 AnyPlayer 添加相同的 RNG 后备逻辑（随机选择一名存活玩家）。
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

        public static void Prefix(CardModel card, ref Creature? target)
        {
            if (!AnyPlayerCardTargetingHelper.IsAnyPlayerMultiplayer(card) || target != null)
                return;

            var combatState = card.CombatState ?? card.Owner.Creature.CombatState;
            if (combatState == null)
                return;

            var candidates = combatState.PlayerCreatures
                .Where(c => c is { IsAlive: true, IsPlayer: true });
            target = card.Owner.RunState.Rng.CombatTargets.NextItem(candidates);
        }
    }

    /// <summary>
    ///     Resolves a random legal target for custom single-target cards when <see cref="CardCmd.AutoPlay" /> is called
    ///     without an explicit target.
    ///     当 <see cref="CardCmd.AutoPlay" /> 未传入明确目标时，为自定义单体目标卡牌随机解析一个合法目标。
    /// </summary>
    internal sealed class CardCmdAutoPlayCustomSingleTargetPatch : IPatchMethod
    {
        private static readonly Func<PlayerChoiceContext, CardModel, Task> MoveToResultPileWithoutPlaying =
            AccessTools.MethodDelegate<Func<PlayerChoiceContext, CardModel, Task>>(
                AccessTools.DeclaredMethod(typeof(CardCmd), "MoveToResultPileWithoutPlaying",
                    [typeof(PlayerChoiceContext), typeof(CardModel)]));

        public static string PatchId => "card_custom_single_target_auto_play";

        public static string Description =>
            "Resolve random custom single-target target in CardCmd.AutoPlay";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardCmd), nameof(CardCmd.AutoPlay))];
        }

        public static bool Prefix(PlayerChoiceContext choiceContext, CardModel card, ref Creature? target,
            AutoPlayType type,
            ref Task __result)
        {
            if (target != null || !CustomTargetTypeResolver.IsCustomSingleTargetType(card.TargetType))
                return true;

            var combatState = card.CombatState ?? card.Owner.Creature.CombatState;
            if (combatState == null)
                return true;

            var candidates = combatState.Creatures
                .Where(c =>
                    CustomTargetTypeResolver.TryIsAllowedSingleTarget(card.TargetType,
                        CustomTargetContext.ForCard(c, card),
                        out var allowed) &&
                    allowed);

            target = card.Owner.RunState.Rng.CombatTargets.NextItem(candidates);
            if (target != null)
                return true;

            if (ShouldLetVanillaHandleEarlyExit(card, combatState, type))
                return true;

            __result = MoveToResultPileWithoutPlaying(choiceContext, card);
            return false;
        }

        private static bool ShouldLetVanillaHandleEarlyExit(CardModel card, ICombatState combatState,
            AutoPlayType type)
        {
            return CombatManager.Instance.IsOverOrEnding
                   || card.Owner.Creature.IsDead
                   || card.Keywords.Contains(CardKeyword.Unplayable)
                   || !Hook.ShouldPlay(combatState, card, out _, type);
        }
    }
}
