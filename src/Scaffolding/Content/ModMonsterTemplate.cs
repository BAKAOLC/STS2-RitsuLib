using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Content.Patches;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a base <see cref="MonsterModel" /> for mods with a creature-visuals scene replacement,
    ///         optional runtime visual creation, and optional custom animation systems.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为模组提供基础 <see cref="MonsterModel" />，支持替换生物形象场景、在运行时创建形象，以及提供
    ///         可选的自定义动画系统。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Override <see cref="SetupCustomCombatAnimationStateMachine" /> when combat triggers should be handled
    ///         by a <see cref="ModAnimStateMachine" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         如需由 <see cref="ModAnimStateMachine" /> 处理战斗触发器，请重写
    ///         <see cref="SetupCustomCombatAnimationStateMachine" />。
    ///     </para>
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
        ///     <para xml:lang="en">
        ///         Returns runtime-created combat visuals, or <see langword="null" /> to use custom or base-game paths.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回运行时创建的战斗形象；返回 <see langword="null" /> 时使用自定义或游戏本体路径。
        ///     </para>
        /// </summary>
        protected virtual NCreatureVisuals? TryCreateCreatureVisuals()
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a custom Spine <see cref="CreatureAnimator" />, or <see langword="null" /> to defer to
        ///         <see cref="MonsterModel.GenerateAnimator" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回自定义 Spine <see cref="CreatureAnimator" />；返回 <see langword="null" /> 时交由
        ///         <see cref="MonsterModel.GenerateAnimator" /> 处理。
        ///     </para>
        /// </summary>
        /// <param name="controller">
        ///     <para xml:lang="en">The Spine controller attached to the monster's combat visuals.</para>
        ///     <para xml:lang="zh-CN">附加到怪物战斗形象的 Spine 控制器。</para>
        /// </param>
        protected virtual CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller)
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a <see cref="ModAnimStateMachine" /> for the monster's combat visuals, or
        ///         <see langword="null" /> to use the base animation path. Any supported backend may be used,
        ///         including Spine through <see cref="ModAnimStateMachineBuilder.BuildSpine" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回用于怪物战斗形象的 <see cref="ModAnimStateMachine" />；返回 <see langword="null" /> 时使用
        ///         游戏本体的动画路径。可以使用任意受支持的后端，包括通过
        ///         <see cref="ModAnimStateMachineBuilder.BuildSpine" /> 使用 Spine。
        ///     </para>
        /// </summary>
        /// <param name="visualsRoot">
        ///     <para xml:lang="en">The combat-visuals root node.</para>
        ///     <para xml:lang="zh-CN">战斗形象根节点。</para>
        /// </param>
        /// <param name="monster">
        ///     <para xml:lang="en">The monster model, always <see langword="this" />.</para>
        ///     <para xml:lang="zh-CN">怪物模型，始终为 <see langword="this" />。</para>
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
            return null;
        }
    }
}
