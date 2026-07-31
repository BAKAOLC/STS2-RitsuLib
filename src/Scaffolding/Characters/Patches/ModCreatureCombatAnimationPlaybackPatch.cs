using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Routes <see cref="NCreature.SetAnimationTrigger" /> to a <see cref="ModAnimStateMachine" /> when the
    ///         creature model provides one through <see cref="IModCreatureCombatAnimationStateMachineFactory" /> or
    ///         the legacy <see cref="IModNonSpineAnimationStateMachineFactory" />. This also supports Spine-backed
    ///         visuals when the factory returns a state machine, such as one created by
    ///         <see cref="ModAnimStateMachineBuilder.BuildSpine" />.
    ///     </para>
    ///     <para xml:lang="en">
    ///         When no state machine is available and the creature has no Spine animator, animation triggers fall
    ///         back to <see cref="ModCreatureVisualPlayback.TryPlayFromCreatureAnimatorTrigger" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         当生物模型通过 <see cref="IModCreatureCombatAnimationStateMachineFactory" /> 或旧版
    ///         <see cref="IModNonSpineAnimationStateMachineFactory" /> 提供 <see cref="ModAnimStateMachine" /> 时，
    ///         将 <see cref="NCreature.SetAnimationTrigger" /> 路由到该状态机。如果工厂返回了状态机，例如通过
    ///         <see cref="ModAnimStateMachineBuilder.BuildSpine" /> 创建的状态机，则同样支持基于 Spine 的形象。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         没有可用状态机且生物没有 Spine 动画器时，动画触发器会回退到
    ///         <see cref="ModCreatureVisualPlayback.TryPlayFromCreatureAnimatorTrigger" />。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         State machines are cached per visuals root via a
    ///         <see cref="ConditionalWeakTable{TKey,TValue}" /> so factories run at most once per combat lifetime.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         状态机通过 <see cref="ConditionalWeakTable{TKey,TValue}" /> 按形象根节点缓存，因此每场战斗中每个根节点
    ///         最多调用一次工厂。
    ///     </para>
    /// </remarks>
    [HarmonyBefore(Const.BaseLibHarmonyId)]
    internal class ModCreatureCombatAnimationPlaybackPatch : IPatchMethod
    {
        private static readonly ConditionalWeakTable<Node, StateMachineSlot> StateMachinesByVisuals = new();
        public static string PatchId => "mod_creature_combat_animation_playback";

        public static string Description =>
            "Route NCreature.SetAnimationTrigger through ModAnimStateMachine when opted in (Spine or non-Spine); "
            + "otherwise cue playback for non-Spine";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the cached combat <see cref="ModAnimStateMachine" /> for <paramref name="creature" /> when
        ///         its model's factory produced one; otherwise, returns <see langword="null" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当 <paramref name="creature" /> 所属模型的工厂生成了状态机时，返回缓存的战斗
        ///         <see cref="ModAnimStateMachine" />；否则返回 <see langword="null" />。
        ///     </para>
        /// </summary>
        internal static ModAnimStateMachine? TryGetCombatAnimationStateMachine(NCreature creature)
        {
            var visuals = creature.Visuals;
            if (visuals == null || !GodotObject.IsInstanceValid(visuals))
                return null;

            var entity = creature.Entity;
            if (entity == null)
                return null;

            var slot = StateMachinesByVisuals.GetValue(visuals, _ => new());
            slot.EnsureBuilt(entity.Player?.Character, entity.Monster, visuals);
            return slot.StateMachine;
        }

        internal static bool TryGetCurrentCombatAnimationDuration(NCreature creature, string trigger,
            out float seconds)
        {
            seconds = 0f;

            var stateMachine = TryGetCombatAnimationStateMachine(creature);
            if (stateMachine != null)
                return (stateMachine.TryGetCurrentAnimationRemaining(out seconds) ||
                        stateMachine.TryGetCurrentAnimationDuration(out seconds)) &&
                       seconds > 0f &&
                       float.IsFinite(seconds);

            return ModCreatureVisualPlayback.TryGetDurationFromCreatureAnimatorTrigger(creature, trigger,
                       out seconds) &&
                   seconds > 0f &&
                   float.IsFinite(seconds);
        }

        public static bool Prefix(NCreature __instance, string trigger)
        {
            if (TryRouteToStateMachine(__instance, trigger))
                return false;

            if (__instance.HasSpineAnimation)
                return true;

            return !ModCreatureVisualPlayback.TryPlayFromCreatureAnimatorTrigger(__instance, trigger);
        }

        private static bool TryRouteToStateMachine(NCreature creature, string trigger)
        {
            var visuals = creature.Visuals;
            if (visuals == null || !GodotObject.IsInstanceValid(visuals))
                return false;

            var entity = creature.Entity;
            if (entity == null)
                return false;

            var slot = StateMachinesByVisuals.GetValue(visuals, _ => new());
            slot.EnsureBuilt(entity.Player?.Character, entity.Monster, visuals);

            if (slot.StateMachine == null)
                return false;

            slot.StateMachine.SetTrigger(trigger);
            return true;
        }

        private sealed class StateMachineSlot
        {
            private bool _built;
            public ModAnimStateMachine? StateMachine { get; private set; }

            public void EnsureBuilt(CharacterModel? character, MonsterModel? monster, Node visuals)
            {
                if (_built)
                    return;

                StateMachine = BuildFrom(character, monster, visuals);
                _built = true;
            }

            private static ModAnimStateMachine? BuildFrom(CharacterModel? character, MonsterModel? monster,
                Node visuals)
            {
                if (character is IModCreatureCombatAnimationStateMachineFactory combatCharacter)
                {
                    var built = combatCharacter.TryCreateCombatAnimationStateMachine(visuals);
                    if (built != null)
                        return built;
                }

#pragma warning disable CS0618
                if (character is IModNonSpineAnimationStateMachineFactory legacyCharacter)
                {
                    var built = legacyCharacter.TryCreateNonSpineAnimationStateMachine(visuals);
                    if (built != null)
                        return built;
                }
#pragma warning restore CS0618

                if (monster is IModCreatureCombatAnimationStateMachineFactory combatMonster)
                {
                    var built = combatMonster.TryCreateCombatAnimationStateMachine(visuals);
                    if (built != null)
                        return built;
                }

#pragma warning disable CS0618
                if (monster is IModNonSpineAnimationStateMachineFactory legacyMonster)
                    return legacyMonster.TryCreateNonSpineAnimationStateMachine(visuals);
#pragma warning restore CS0618

                return null;
            }
        }
    }
}
