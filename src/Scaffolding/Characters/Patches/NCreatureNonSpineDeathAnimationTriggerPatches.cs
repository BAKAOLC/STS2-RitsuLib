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
    ///     <para xml:lang="en">
    ///         Dispatches the <c>Dead</c> animation trigger after <see cref="NCreature.StartDeathAnim" /> for
    ///         RitsuLib-managed creatures without a Spine animator. The base implementation only dispatches this
    ///         trigger through its Spine path, so non-Spine animation backends would otherwise receive no death
    ///         trigger.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         对由 RitsuLib 管理且没有 Spine 动画器的生物，在 <see cref="NCreature.StartDeathAnim" /> 后派发
    ///         <c>Dead</c> 动画触发器。游戏本体只会通过 Spine 路径派发该触发器，因此其他动画后端原本无法收到
    ///         死亡触发器。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         The patch applies only when the creature has no Spine animator and its character or monster model
    ///         provides a RitsuLib animation-state-machine factory, or its player character implements
    ///         <see cref="IModCharacterAssetOverrides" />. Other creatures are unaffected.
    ///     </para>
    ///     <para xml:lang="en">
    ///         When a finite animation duration is available, the returned duration is raised to that value, capped
    ///         at 30 seconds. Creatures scheduled for removal also receive a matching
    ///         <see cref="RitsuNonSpineDeathAnimationDelayer" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         此补丁仅适用于没有 Spine 动画器，且其角色或怪物模型提供 RitsuLib 动画状态机工厂的生物；玩家角色
    ///         实现 <see cref="IModCharacterAssetOverrides" /> 时也适用。其他生物不受影响。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         能够取得有效动画时长时，方法返回的时长会提高到该值，但最多为 30 秒。计划移除的生物还会获得时长
    ///         相同的 <see cref="RitsuNonSpineDeathAnimationDelayer" />。
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
    ///     <para xml:lang="en">
    ///         Dispatches the <c>Revive</c> animation trigger after <see cref="NCreature.StartReviveAnim" /> for
    ///         RitsuLib-managed creatures when the base implementation did not dispatch it.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         对由 RitsuLib 管理的生物，在游戏本体没有派发 <c>Revive</c> 动画触发器时，于
    ///         <see cref="NCreature.StartReviveAnim" /> 后补充派发该触发器。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         For non-Spine creatures, the scope matches
    ///         <see cref="NCreatureNonSpineDeathAnimationTriggerPatch" />. A Spine-backed creature is included only
    ///         when its mod combat state machine declares <c>Revive</c> and the base animator does not.
    ///     </para>
    ///     <para xml:lang="en">
    ///         If the base implementation also ran <c>AnimTempRevive</c>, its fade tween continues alongside the mod
    ///         animation.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         对非 Spine 生物，适用范围与 <see cref="NCreatureNonSpineDeathAnimationTriggerPatch" /> 相同。对于
    ///         基于 Spine 的生物，仅当模组战斗状态机声明了 <c>Revive</c> 而游戏本体动画器没有该触发器时才适用。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         如果游戏本体同时运行了 <c>AnimTempRevive</c>，其淡入淡出补间动画仍会与模组动画并行播放。
    ///     </para>
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
    ///     <para xml:lang="en">
    ///         Defines the shared eligibility checks used by the <see cref="NCreature.StartDeathAnim" /> and
    ///         <see cref="NCreature.StartReviveAnim" /> postfixes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义 <see cref="NCreature.StartDeathAnim" /> 与 <see cref="NCreature.StartReviveAnim" /> 后置补丁
    ///         共用的适用条件。
    ///     </para>
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
