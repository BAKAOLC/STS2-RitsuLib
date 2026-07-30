using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Godot;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Makes <see cref="CardCmd.AutoPlay" /> choose a random living player for
    ///         <see cref="TargetType.AnyPlayer" /> when no target is supplied, matching the vanilla fallback for
    ///         <see cref="TargetType.AnyEnemy" /> and <see cref="TargetType.AnyAlly" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         当未提供目标时，使 <see cref="CardCmd.AutoPlay" /> 为 <see cref="TargetType.AnyPlayer" />
    ///         随机选择一名存活玩家，与原版对 <see cref="TargetType.AnyEnemy" /> 和
    ///         <see cref="TargetType.AnyAlly" /> 的回退行为一致。
    ///     </para>
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
    ///     <para xml:lang="en">
    ///         Chooses a random valid target for a custom single-target card when <see cref="CardCmd.AutoPlay" /> is
    ///         called without an explicit target.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         当调用 <see cref="CardCmd.AutoPlay" /> 时未提供明确目标，为自定义单体目标卡牌随机选择一个有效目标。
    ///     </para>
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

            if (CombatManager.Instance.IsOverOrEnding
                || card.Owner.Creature.IsDead
                || card.Keywords.Contains(CardKeyword.Unplayable))
                return true;

            __result = HandleMissingTarget(choiceContext, card, combatState, type);
            return false;
        }

        private static async Task HandleMissingTarget(
            PlayerChoiceContext choiceContext,
            CardModel card,
            ICombatState combatState,
            AutoPlayType type)
        {
            if (!Hook.ShouldPlay(combatState, card, out var preventer, type))
            {
                await MoveToResultPileWithoutPlaying(choiceContext, card);
                var line = UnplayableReason.BlockedByHook.GetPlayerDialogueLine(preventer);
                if (line != null)
                {
                    var container = card.Owner.Creature.GetVfxContainer();
                    if (container != null)
                        RitsuGodotTreeCompat.AddChildSafely(
                            container,
                            NThoughtBubbleVfx.Create(line.GetFormattedText(), card.Owner.Creature, 1.0));
                }
                return;
            }

            await MoveToResultPileWithoutPlaying(choiceContext, card);
        }
    }
}
