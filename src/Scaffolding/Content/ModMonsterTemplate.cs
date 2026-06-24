using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Content.Patches;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="MonsterModel" /> for mods: <see cref="IModMonsterAssetOverrides" /> supplies the visuals scene
    ///     path; override <see cref="TryCreateCreatureVisuals" /> to build <see cref="NCreatureVisuals" /> in code instead.
    ///     Use <see cref="RitsuGodotNodeFactories" /> for explicit <c>CreateFromResource</c> / <c>CreateFromScenePath</c>
    ///     construction. Register with <c>ModContentRegistry.RegisterMonster&lt;T&gt;()</c> or <c>Monster&lt;T&gt;()</c> on
    ///     the pack builder.
    ///     Mod 怪物的基础 <see cref="MonsterModel" />：<see cref="IModMonsterAssetOverrides" /> 提供视觉场景
    ///     路径；也可以重写 <see cref="TryCreateCreatureVisuals" /> 直接用代码构建 <see cref="NCreatureVisuals" />。
    ///     如需显式 <see cref="RitsuGodotNodeFactories" /> 构造 <c>CreateFromResource</c> / <c>CreateFromScenePath</c>，
    ///     请在包构建器上通过 <c>ModContentRegistry.RegisterMonster&lt;T&gt;()</c> 或 <c>Monster&lt;T&gt;()</c>
    ///     注册。
    /// </summary>
    /// <remarks>
    ///     When the monster's visuals should use a <see cref="ModAnimStateMachine" /> for combat triggers, override
    ///     <see cref="SetupCustomCombatAnimationStateMachine" /> to drive the creature with a
    ///     <see cref="ModAnimStateMachine" /> (the same state-machine pipeline used by
    ///     <see cref="STS2RitsuLib.Scaffolding.Characters.ModCharacterTemplate{TCardPool,TRelicPool,TPotionPool}" />).
    ///     <see cref="STS2RitsuLib.Scaffolding.Characters.ModCharacterTemplate{TCardPool,TRelicPool,TPotionPool}" />
    ///     当怪物视觉应使用 <see cref="ModAnimStateMachine" /> 处理战斗 trigger 时，请重写
    ///     <see cref="SetupCustomCombatAnimationStateMachine" />，用 <see cref="ModAnimStateMachine" /> 驱动该生物（与
    ///     使用的状态机管线相同）。
    /// </remarks>
#pragma warning disable CS0618
    // Template keeps the obsolete IModMonsterCreatureVisualsFactory wired so existing derived classes and external
    // consumers that type-check against the old interface name continue to work.
    public abstract class ModMonsterTemplate : MonsterModel, IModMonsterAssetOverrides,
        IModCreatureVisualsFactory, IModMonsterCreatureVisualsFactory, IModCreatureAnimatorFactory,
        IModCreatureCombatAnimationStateMachineFactory, IModNonSpineAnimationStateMachineFactory
#pragma warning restore CS0618
    {
        CreatureAnimator? IModCreatureAnimatorFactory.TryCreateCreatureAnimator(MegaSprite controller)
        {
            return SetupCustomCreatureAnimator(controller);
        }

        ModAnimStateMachine? IModCreatureCombatAnimationStateMachineFactory.TryCreateCombatAnimationStateMachine(
            Node visualsRoot)
        {
            return ResolveCombatAnimationStateMachine(visualsRoot);
        }

        NCreatureVisuals? IModCreatureVisualsFactory.TryCreateCreatureVisuals()
        {
            return TryCreateCreatureVisuals();
        }

        /// <inheritdoc />
        public virtual MonsterAssetProfile AssetProfile => MonsterAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomVisualsPath => AssetProfile.VisualsScenePath;

#pragma warning disable CS0618
        NCreatureVisuals? IModMonsterCreatureVisualsFactory.TryCreateCreatureVisuals()
        {
            return TryCreateCreatureVisuals();
        }
#pragma warning restore CS0618

        ModAnimStateMachine? IModNonSpineAnimationStateMachineFactory.TryCreateNonSpineAnimationStateMachine(
            Node visualsRoot)
        {
            return ResolveCombatAnimationStateMachine(visualsRoot);
        }

        private ModAnimStateMachine? ResolveCombatAnimationStateMachine(Node visualsRoot)
        {
            var fromNew = SetupCustomCombatAnimationStateMachine(visualsRoot, this);
#pragma warning disable CS0618
            return fromNew ?? SetupCustomNonSpineAnimationStateMachine(visualsRoot, this);
#pragma warning restore CS0618
        }

        /// <summary>
        ///     Non-null value becomes combat visuals; otherwise paths (<see cref="CustomVisualsPath" /> / vanilla) apply.
        ///     返回非 null 值时作为战斗视觉；否则使用路径（<see cref="CustomVisualsPath" /> / 原版）。
        /// </summary>
        protected virtual NCreatureVisuals? TryCreateCreatureVisuals()
        {
            return null;
        }

        /// <summary>
        ///     Optional override producing a fully wired Spine <see cref="CreatureAnimator" /> (state graph for idle /
        ///     hit / attack / cast / die / relaxed). Return <see langword="null" /> to defer to vanilla
        ///     <see cref="MonsterModel.GenerateAnimator" />. Prefer <see cref="ModAnimStateMachines.Standard" /> to
        ///     match baselib semantics.
        ///     可选重写，用于生成已完整接线的 Spine <see cref="CreatureAnimator" />（idle / hit / attack / cast / die /
        ///     relaxed 状态图）。返回 <see langword="null" /> 则交给原版 <see cref="MonsterModel.GenerateAnimator" />。
        ///     优先使用 <see cref="ModAnimStateMachines.Standard" /> 以匹配 baselib 语义。
        /// </summary>
        /// <param name="controller">
        ///     Spine controller attached to the monster's combat visuals.
        ///     附加在怪物战斗视觉上的 Spine controller。
        /// </param>
        protected virtual CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller)
        {
            return null;
        }

        /// <summary>
        ///     Optional override producing a <see cref="ModAnimStateMachine" /> for the monster's combat visuals (any
        ///     animation backend, including Spine via <see cref="ModAnimStateMachineBuilder.BuildSpine" />). Return
        ///     <see langword="null" /> to defer to vanilla Spine <see cref="CreatureAnimator" /> triggers or the vanilla
        ///     single-shot playback path when there is no Spine animator.
        ///     可选重写，用于为怪物战斗视觉生成 <see cref="ModAnimStateMachine" />（可使用任意动画后端，包括通过
        ///     <see cref="ModAnimStateMachineBuilder.BuildSpine" /> 使用 Spine）。返回 <see langword="null" /> 时，
        ///     会回退到原版 Spine <see cref="CreatureAnimator" /> 触发，或在没有 Spine animator 时回退到原版单次播放路径。
        /// </summary>
        /// <param name="visualsRoot">
        ///     Combat visuals root node.
        ///     战斗视觉根节点。
        /// </param>
        /// <param name="monster">
        ///     Monster model (always <see langword="this" />, exposed for convenience).
        ///     怪物模型（始终为 <see langword="this" />，仅为方便而暴露）。
        /// </param>
        protected virtual ModAnimStateMachine? SetupCustomCombatAnimationStateMachine(Node visualsRoot,
            MonsterModel monster)
        {
            return null;
        }

        /// <inheritdoc cref="SetupCustomCombatAnimationStateMachine" />
        [Obsolete("Override SetupCustomCombatAnimationStateMachine instead.")]
        protected virtual ModAnimStateMachine? SetupCustomNonSpineAnimationStateMachine(Node visualsRoot,
            MonsterModel monster)
        {
            return SetupCustomCombatAnimationStateMachine(visualsRoot, monster);
        }
    }
}
