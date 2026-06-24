using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Fires the <c>Dead</c> animation trigger for RitsuLib-managed creatures without a Spine animator after
    ///     <see cref="NCreature.StartDeathAnim" /> runs. Vanilla gates the entire trigger dispatch (including death
    ///     SFX) behind <c>_spineAnimator != null</c>, so mod creatures using <c>AnimatedSprite2D</c>, Godot
    ///     <c>AnimationPlayer</c>, or cue-frame-sequence backends never receive the trigger — the most visible symptom
    ///     for players is that the death animation does not play when the run is abandoned or the player dies in
    ///     combat.
    ///     在 <see cref="NCreature.StartDeathAnim" /> 运行后，为没有 Spine animator 的 RitsuLib 管理生物触发
    ///     <c>Dead</c> 动画触发器。原版将整个触发器派发（包括死亡
    ///     SFX）门控在 <c>_spineAnimator != null</c> 之后，因此使用 <c>AnimatedSprite2D</c>、Godot
    ///     <c>AnimationPlayer</c> 或 cue-frame-sequence 后端的 mod 生物永远收不到该触发器，最明显的玩家症状
    ///     是跑局被放弃或玩家在战斗中死亡时，死亡动画不会播放。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Scope:</b> the postfix only fires when all of the following hold, so foreign creatures that do not
    ///         opt into the RitsuLib visuals pipeline are untouched:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>the creature has no Spine animator;</description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     the creature's model (either <c>Entity.Player?.Character</c> or <c>Entity.Monster</c>)
    ///                     opts into RitsuLib visuals by implementing
    ///                     <see cref="IModCreatureCombatAnimationStateMachineFactory" /> (or the legacy
    ///                     <see cref="IModNonSpineAnimationStateMachineFactory" />), or — for players only —
    ///                     <see cref="IModCharacterAssetOverrides" /> (which pulls the cue-playback path).
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         When all guards pass, the patch calls <see cref="NCreature.SetAnimationTrigger" />, which
    ///         <see cref="ModCreatureCombatAnimationPlaybackPatch" /> routes through the model's
    ///         <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.ModAnimStateMachine" /> (when registered)
    ///         or the legacy cue playback
    ///         (<see cref="STS2RitsuLib.Scaffolding.Characters.Visuals.ModCreatureVisualPlayback" />).
    ///     </para>
    ///     <para>
    ///         This patch does not attempt to backfill the death-animation length returned from
    ///         <see cref="NCreature.StartDeathAnim" /> — vanilla already returns <c>0f</c> for non-Spine creatures
    ///         unless a monster sets <see cref="MonsterModel.DeathAnimLengthOverride" />.
    ///     </para>
    ///     <para>
    ///         <b>Scope:</b> postfix 仅在以下所有条件成立时触发，因此未选择加入 RitsuLib 视觉管线的外部生物不受影响：
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>该生物没有 Spine animator；</description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     该生物的模型（<c>Entity.Player?.Character</c> 或 <c>Entity.Monster</c>）
    ///                     通过实现 <see cref="IModCreatureCombatAnimationStateMachineFactory" />（或旧版
    ///                     <see cref="IModNonSpineAnimationStateMachineFactory" />），或者仅对玩家通过
    ///                     <see cref="IModCharacterAssetOverrides" />（它会拉取 cue 播放路径），选择加入 RitsuLib 视觉。
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         当所有 guard 通过时，patch 调用 <see cref="NCreature.SetAnimationTrigger" />；
    ///         <see cref="ModCreatureCombatAnimationPlaybackPatch" /> 会将其通过模型的
    ///         <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.ModAnimStateMachine" />（已注册时）
    ///         或旧版 cue 播放
    ///     </para>
    ///     <para>
    ///         此 patch 不尝试回填从
    ///         <see cref="NCreature.StartDeathAnim" /> 返回的死亡动画长度；原版已对非 Spine 生物返回 <c>0f</c>，
    ///         除非某个怪物设置了 <see cref="MonsterModel.DeathAnimLengthOverride" />。
    ///     </para>
    /// </remarks>
    internal class NCreatureNonSpineDeathAnimationTriggerPatch : IPatchMethod
    {
        public static string PatchId => "ncreature_non_spine_death_animation_trigger";

        public static string Description =>
            "Dispatch the Dead animation trigger for RitsuLib-managed non-Spine creatures so StartDeathAnim animates correctly";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCreature), nameof(NCreature.StartDeathAnim))];
        }

        public static void Postfix(NCreature __instance, bool shouldRemove, ref float __result)
        {
            if (!CombatAnimationStateMachineTriggerScope.AppliesToDeathPostfix(__instance))
                return;

            __instance.SetAnimationTrigger("Dead");

            if (!ModCreatureCombatAnimationPlaybackPatch.TryGetCurrentCombatAnimationDuration(
                    __instance,
                    "Dead",
                    out var seconds))
                return;

            seconds = Math.Min(seconds, 30f);
            if (seconds > __result)
                __result = seconds;

            if (shouldRemove)
                RitsuNonSpineDeathAnimationDelayer.Install(__instance, seconds);
        }
    }

    /// <summary>
    ///     Fires the <c>Revive</c> animation trigger for RitsuLib-managed creatures after
    ///     <see cref="NCreature.StartReviveAnim" /> when vanilla would not dispatch it. Vanilla only dispatches the
    ///     trigger when a Spine animator exists and <see cref="CreatureAnimator.HasTrigger" /> reports
    ///     <c>Revive</c>; otherwise it falls back to <c>AnimTempRevive</c> (a fade-out / fade-in tween on the visuals
    ///     root), which silently swallows any <c>Revive</c> state the mod creature registered on a
    ///     <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.ModAnimStateMachine" /> without a matching
    ///     <c>Revive</c> branch on the vanilla <see cref="CreatureAnimator" />.
    ///     在 <see cref="NCreature.StartReviveAnim" /> 后、原版不会派发时，为 RitsuLib 管理的生物触发
    ///     <c>Revive</c> 动画触发器。原版只会在存在 Spine animator 且 <see cref="CreatureAnimator.HasTrigger" /> 报告
    ///     <c>Revive</c> 时派发该触发器；否则会 fallback 到 <c>AnimTempRevive</c>（视觉
    ///     根节点上的淡出/淡入 tween），这会静默吞掉 mod 生物在
    ///     <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.ModAnimStateMachine" /> 上注册但在原版
    ///     <see cref="CreatureAnimator" /> 上没有匹配
    ///     <c>Revive</c> 分支的任何 <c>Revive</c> 状态。
    /// </summary>
    /// <remarks>
    ///     Scope mirrors <see cref="NCreatureNonSpineDeathAnimationTriggerPatch" /> for non-Spine creatures. For
    ///     Spine-backed creatures with a combat state machine that declares <c>Revive</c>, the postfix may dispatch
    ///     <c>Revive</c> when the vanilla animator does not expose that trigger (see interface remarks on keeping both
    ///     in sync). The vanilla fade tween still runs alongside the triggered animation when <c>AnimTempRevive</c>
    ///     also ran; mods that want a clean revive animation should treat the brief fade as expected behaviour.
    ///     作用域与非 Spine 生物的 <see cref="NCreatureNonSpineDeathAnimationTriggerPatch" /> 保持一致。对于
    ///     带有声明 <c>Revive</c> 的战斗状态机的 Spine 支持生物，当原版 animator 未暴露该触发器时，postfix 可能派发
    ///     <c>Revive</c>（关于保持两者同步，见接口备注）。当 <c>AnimTempRevive</c>
    ///     也运行时，原版淡化 tween 仍会与触发的动画并行运行；想要干净复活动画的 mod 应将这段短暂淡化视为预期行为。
    /// </remarks>
    internal class NCreatureNonSpineReviveAnimationTriggerPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NCreature, CreatureAnimator?> SpineAnimatorRef =
            AccessTools.FieldRefAccess<NCreature, CreatureAnimator?>("_spineAnimator");

        public static string PatchId => "ncreature_non_spine_revive_animation_trigger";

        public static string Description =>
            "Dispatch the Revive animation trigger for RitsuLib-managed creatures so StartReviveAnim animates correctly";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCreature), nameof(NCreature.StartReviveAnim))];
        }

        public static void Postfix(NCreature __instance)
        {
            if (!CombatAnimationStateMachineTriggerScope.AppliesToRevivePostfix(__instance))
                return;

            if (__instance.HasSpineAnimation)
            {
                var animator = SpineAnimatorRef(__instance);
                if (animator != null && animator.HasTrigger("Revive"))
                    return;
            }

            __instance.SetAnimationTrigger("Revive");
        }
    }

    /// <summary>
    ///     Shared gate used by combat animation lifecycle postfixes so scope stays consistent across
    ///     <see cref="NCreature.StartDeathAnim" /> / <see cref="NCreature.StartReviveAnim" />.
    ///     战斗动画生命周期 postfix 使用的共享 gate，使作用域在
    ///     <see cref="NCreature.StartDeathAnim" /> / <see cref="NCreature.StartReviveAnim" /> 之间保持一致。
    /// </summary>
    internal static class CombatAnimationStateMachineTriggerScope
    {
        public static bool AppliesToDeathPostfix(NCreature creature)
        {
            return !creature.HasSpineAnimation && AppliesToRitsuLibVisuals(creature);
        }

        public static bool AppliesToRevivePostfix(NCreature creature)
        {
            if (!creature.HasSpineAnimation)
                return AppliesToRitsuLibVisuals(creature);

            var sm = ModCreatureCombatAnimationPlaybackPatch.TryGetCombatAnimationStateMachine(creature);
            return sm != null && sm.HasTrigger("Revive");
        }

        private static bool AppliesToRitsuLibVisuals(NCreature creature)
        {
            var entity = creature.Entity;
            if (entity == null)
                return false;

            var character = entity.Player?.Character;
            var monster = entity.Monster;

            switch (character)
            {
                case IModCreatureCombatAnimationStateMachineFactory:
#pragma warning disable CS0618
                case IModNonSpineAnimationStateMachineFactory:
#pragma warning restore CS0618
                    return true;
            }

            switch (monster)
            {
                case IModCreatureCombatAnimationStateMachineFactory:
#pragma warning disable CS0618
                case IModNonSpineAnimationStateMachineFactory:
#pragma warning restore CS0618
                    return true;
            }

            return character is IModCharacterAssetOverrides;
        }
    }
}
