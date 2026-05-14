using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Runtime <see cref="NCreatureVisuals" /> factory for any combat creature model (player characters and
    ///     monsters). Implement on the model type — typically by subclassing
    ///     <see cref="STS2RitsuLib.Scaffolding.Characters.ModCharacterTemplate{TCardPool,TRelicPool,TPotionPool}" />
    ///     or <see cref="ModMonsterTemplate" />, though the templates are convenience and not required. Non-null
    ///     <see cref="TryCreateCreatureVisuals" /> replaces the path-based <c>CreateVisuals</c> on both
    ///     <see cref="CharacterModel" /> and <see cref="MonsterModel" />.
    ///     任意战斗生物模型（玩家角色和怪物）的运行时 <see cref="NCreatureVisuals" /> 工厂。请在模型类型上实现 - 通常通过继承
    ///     <see cref="STS2RitsuLib.Scaffolding.Characters.ModCharacterTemplate{TCardPool,TRelicPool,TPotionPool}" /> 或
    ///     <see cref="ModMonsterTemplate" /> 完成，不过这些模板只是便利封装，并非必需。非 null 的 <see cref="TryCreateCreatureVisuals" /> 会替换
    ///     <see cref="CharacterModel" /> 和 <see cref="MonsterModel" /> 上基于路径的 <c>CreateVisuals</c>。
    /// </summary>
    public interface IModCreatureVisualsFactory
    {
        /// <summary>
        ///     Combat visuals from code, or <c>null</c> to fall through to asset paths.
        ///     由代码创建的战斗视觉；返回 <c>null</c> 则继续使用ResourcePath。
        /// </summary>
        NCreatureVisuals? TryCreateCreatureVisuals();
    }

    /// <summary>
    ///     Obsolete monster-specific alias of <see cref="IModCreatureVisualsFactory" /> kept for backward
    ///     compatibility with existing mods. New code should implement
    ///     <see cref="IModCreatureVisualsFactory" /> — which works for both monsters and player characters —
    ///     instead. The routing patch still honours this interface when present on a <see cref="MonsterModel" />.
    ///     已过时的怪物专用 <see cref="IModCreatureVisualsFactory" /> 别名，为兼容现有 mod 保留。新代码应改为实现
    ///     <see cref="IModCreatureVisualsFactory" />，它同时适用于怪物和玩家角色。当 <see cref="MonsterModel" />
    ///     上存在此接口时，路由补丁仍会识别它。
    /// </summary>
    [Obsolete(
        "Implement IModCreatureVisualsFactory instead; the replacement applies to both monsters and player characters.",
        false)]
    public interface IModMonsterCreatureVisualsFactory
    {
        /// <summary>
        ///     Combat visuals from code, or <c>null</c> to use asset paths.
        ///     由代码创建的战斗视觉；返回 <c>null</c> 则使用ResourcePath。
        /// </summary>
        NCreatureVisuals? TryCreateCreatureVisuals();
    }

    /// <summary>
    ///     Obsolete character-specific alias of <see cref="IModCreatureVisualsFactory" /> kept for backward
    ///     compatibility with existing mods. New code should implement
    ///     <see cref="IModCreatureVisualsFactory" /> — which works for both monsters and player characters —
    ///     instead. The routing patch still honours this interface when present on a <see cref="CharacterModel" />.
    ///     已过时的角色专用 <see cref="IModCreatureVisualsFactory" /> 别名，为兼容现有 mod 保留。新代码应改为实现
    ///     <see cref="IModCreatureVisualsFactory" />，它同时适用于怪物和玩家角色。当 <see cref="CharacterModel" />
    ///     上存在此接口时，路由补丁仍会识别它。
    /// </summary>
    [Obsolete(
        "Implement IModCreatureVisualsFactory instead; the replacement applies to both monsters and player characters.",
        false)]
    public interface IModCharacterCreatureVisualsFactory
    {
        /// <summary>
        ///     Combat visuals from code, or <c>null</c> to use asset paths.
        ///     由代码创建的战斗视觉；返回 <c>null</c> 则使用ResourcePath。
        /// </summary>
        NCreatureVisuals? TryCreateCreatureVisuals();
    }

    /// <summary>
    ///     Runtime encounter combat root <see cref="Control" />. Set <see cref="SuppliesEncounterCombatSceneFromFactory" />
    ///     when using a factory without <c>CustomEncounterScenePath</c> so <c>HasScene</c> stays correct.
    ///     运行时遭遇战斗根 <see cref="Control" />。当使用工厂且没有 <see cref="SuppliesEncounterCombatSceneFromFactory" /> 时，请设置
    ///     <c>CustomEncounterScenePath</c>，以保持 <c>HasScene</c> 正确。
    /// </summary>
    public interface IModEncounterCombatSceneFactory
    {
        /// <summary>
        ///     <c>true</c> when this encounter provides a scene only via the factory (no path).
        ///     当此遭遇只通过工厂提供场景（没有路径）时为 <c>true</c>。
        /// </summary>
        bool SuppliesEncounterCombatSceneFromFactory { get; }

        /// <summary>
        ///     Combat UI root from code, or <c>null</c> to load the default encounter scene.
        ///     由代码创建的战斗 UI 根节点；返回 <c>null</c> 则加载默认遭遇场景。
        /// </summary>
        Control? TryCreateEncounterCombatScene();
    }

    /// <summary>
    ///     Runtime layout <see cref="PackedScene" /> for <see cref="MegaCrit.Sts2.Core.Models.EventModel.CreateScene" />.
    ///     供 <see cref="PackedScene" /> 使用的运行时布局 <see cref="MegaCrit.Sts2.Core.Models.EventModel.CreateScene" />。
    /// </summary>
    public interface IModEventLayoutPackedSceneFactory
    {
        /// <summary>
        ///     Layout scene from code, or <c>null</c> to resolve <c>LayoutScenePath</c>.
        ///     由代码创建的布局场景；返回 <c>null</c> 则解析 <c>LayoutScenePath</c>。
        /// </summary>
        PackedScene? TryCreateLayoutPackedScene();
    }

    /// <summary>
    ///     Runtime background <see cref="PackedScene" /> for
    ///     <see cref="MegaCrit.Sts2.Core.Models.EventModel.CreateBackgroundScene" />.
    ///     <see cref="PackedScene" />。
    ///     供 <see cref="MegaCrit.Sts2.Core.Models.EventModel.CreateBackgroundScene" /> 使用的运行时背景 <see cref="PackedScene" />。
    ///     <see cref="PackedScene" />。
    /// </summary>
    public interface IModEventBackgroundPackedSceneFactory
    {
        /// <summary>
        ///     Background scene from code, or <c>null</c> to use path resolution.
        ///     由代码创建的背景场景；返回 <c>null</c> 则使用路径解析。
        /// </summary>
        PackedScene? TryCreateBackgroundPackedScene();
    }

    /// <summary>
    ///     Runtime event VFX <see cref="Node2D" />. Use <see cref="SuppliesCustomEventVfx" /> when VFX is code-built and
    ///     there is no VFX scene file on disk.
    ///     <see cref="SuppliesCustomEventVfx" />。
    ///     运行时事件 VFX <see cref="Node2D" />。当 VFX 由代码构建且磁盘上没有 VFX 场景文件时，请使用 <see cref="SuppliesCustomEventVfx" />。
    ///     <see cref="SuppliesCustomEventVfx" />。
    /// </summary>
    public interface IModEventVfxFactory
    {
        /// <summary>
        ///     <c>true</c> when <see cref="TryCreateEventVfx" /> should run instead of loading the default VFX path.
        ///     当应运行 <see cref="TryCreateEventVfx" /> 而不是加载默认 VFX 路径时为 <c>true</c>。
        /// </summary>
        bool SuppliesCustomEventVfx { get; }

        /// <summary>
        ///     VFX root from code, or <c>null</c> to fall through to path-based loading.
        ///     由代码创建的 VFX 根节点；返回 <c>null</c> 则继续基于路径加载。
        /// </summary>
        Node2D? TryCreateEventVfx();
    }

    /// <summary>
    ///     Runtime orb presentation <see cref="Node2D" /> for <c>OrbModel.CreateSprite</c>. Match the node shape and animation
    ///     setup that vanilla expects (e.g. Spine idle) if other systems assume it.
    ///     供 <see cref="Node2D" /> 使用的运行时充能球表现 <c>OrbModel.CreateSprite</c>。如果其它系统依赖原版形态，
    ///     请匹配原版期望的节点结构和动画设置（例如 Spine idle）。
    /// </summary>
    public interface IModOrbSpriteFactory
    {
        /// <summary>
        ///     Orb sprite node from code, or <c>null</c> to instantiate from the visuals scene path.
        ///     由代码创建的充能球 sprite 节点；返回 <c>null</c> 则从视觉场景路径实例化。
        /// </summary>
        Node2D? TryCreateOrbSprite();
    }

    /// <summary>
    ///     Runtime Spine <see cref="CreatureAnimator" /> factory for any combat creature model (player characters
    ///     and monsters). Overrides the default vanilla <c>GenerateAnimator</c> so mods can wire custom
    ///     <see cref="AnimState" /> graphs (idle / hit / attack / cast / die / relaxed) without subclassing
    ///     <c>NCreature</c>. Prefer
    ///     <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.ModAnimStateMachines.Standard" /> for the
    ///     standard shape; return <see langword="null" /> to fall through to vanilla behaviour.
    ///     <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.ModAnimStateMachines.Standard" />；
    ///     任意战斗生物模型（玩家角色和怪物）的运行时 Spine <see cref="CreatureAnimator" /> 工厂。它会覆盖默认原版 <c>GenerateAnimator</c>，让 mod 可以接入自定义
    ///     <see cref="AnimState" /> 图（idle / hit / attack / cast / die / relaxed），而无需继承 <c>NCreature</c>。标准结构优先使用
    ///     <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.ModAnimStateMachines.Standard" />；返回
    ///     <see langword="null" /> 则回退到原版行为。
    ///     <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.ModAnimStateMachines.Standard" />。
    /// </summary>
    public interface IModCreatureAnimatorFactory
    {
        /// <summary>
        ///     Returns a fully wired <see cref="CreatureAnimator" />, or <see langword="null" /> to defer to vanilla.
        ///     返回已完整接线的 <see cref="CreatureAnimator" />；返回 <see langword="null" /> 则交给原版。
        /// </summary>
        /// <param name="controller">
        ///     Spine controller attached to the creature's combat visuals.
        ///     附加在生物战斗视觉上的 Spine controller。
        /// </param>
        CreatureAnimator? TryCreateCreatureAnimator(MegaSprite controller);
    }

    /// <summary>
    ///     Obsolete character-specific alias of <see cref="IModCreatureAnimatorFactory" /> kept for backward
    ///     compatibility with existing mods. New code should implement
    ///     <see cref="IModCreatureAnimatorFactory" /> — which works for both monsters and player characters —
    ///     instead. The routing patch still honours this interface when present on a <see cref="CharacterModel" />.
    ///     已过时的角色专用 <see cref="IModCreatureAnimatorFactory" /> 别名，为兼容现有 mod 保留。新代码应改为实现
    ///     <see cref="IModCreatureAnimatorFactory" />，它同时适用于怪物和玩家角色。当 <see cref="CharacterModel" />
    ///     上存在此接口时，路由补丁仍会识别它。
    /// </summary>
    [Obsolete(
        "Implement IModCreatureAnimatorFactory instead; the replacement applies to both monsters and player characters.",
        false)]
    public interface IModCharacterCreatureAnimatorFactory
    {
        /// <summary>
        ///     Returns a fully wired <see cref="CreatureAnimator" />, or <see langword="null" /> to defer to vanilla.
        ///     返回已完整接线的 <see cref="CreatureAnimator" />；返回 <see langword="null" /> 则交给原版。
        /// </summary>
        /// <param name="controller">
        ///     Spine controller attached to the character's combat visuals.
        ///     附加在角色战斗视觉上的 Spine controller。
        /// </param>
        CreatureAnimator? TryCreateCreatureAnimator(MegaSprite controller);
    }

    /// <summary>
    ///     Runtime combat <see cref="ModAnimStateMachine" /> factory for any creature model (player characters,
    ///     monsters, or other <see cref="AbstractModel" />) whose <see cref="NCreature.SetAnimationTrigger" /> flow
    ///     should be routed through <see cref="ModAnimStateMachine.SetTrigger" /> instead of (or in addition to) the
    ///     vanilla <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator" /> path. Works for non-Spine backends
    ///     (cue frames, Godot <see cref="AnimationPlayer" />, <see cref="AnimatedSprite2D" />, composite) and for
    ///     Spine when the machine is built over <see cref="MegaCrit.Sts2.Core.Bindings.MegaSpine.MegaSprite" /> (for
    ///     example <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.ModAnimStateMachineBuilder.BuildSpine" />).
    ///     <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.ModAnimStateMachineBuilder.BuildSpine" />）。
    ///     任意生物模型（玩家角色、怪物或其它 <see cref="AbstractModel" />）的运行时战斗 <see cref="ModAnimStateMachine" /> 工厂；当其
    ///     <see cref="NCreature.SetAnimationTrigger" /> 流程应路由到 <see cref="ModAnimStateMachine.SetTrigger" />，而不是（或同时）走原版
    ///     <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator" /> 路径时使用。适用于非 Spine 后端（cue 帧、Godot
    ///     <see cref="AnimationPlayer" />、<see cref="AnimatedSprite2D" />、组合），也适用于构建在
    ///     <see cref="MegaCrit.Sts2.Core.Bindings.MegaSpine.MegaSprite" /> 之上的 Spine 状态机（例如
    ///     <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.ModAnimStateMachineBuilder.BuildSpine" />。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Typical implementers subclass
    ///         <see cref="STS2RitsuLib.Scaffolding.Characters.ModCharacterTemplate{TCardPool,TRelicPool,TPotionPool}" />
    ///         or <see cref="ModMonsterTemplate" />, but the templates are convenience. The contract is opt-in via the
    ///         interface itself: any model type implementing this interface is routed through
    ///         <see cref="STS2RitsuLib.Scaffolding.Characters.Patches.ModCreatureCombatAnimationPlaybackPatch" /> —
    ///         template subclassing is <b>not</b> required.
    ///     </para>
    ///     <para>
    ///         <see cref="ModAnimStateMachine.SetTrigger" /> receives the same trigger names that vanilla would
    ///         dispatch to a Spine animator (<c>Idle</c>, <c>Attack</c>, <c>Cast</c>, <c>Hit</c>, <c>Dead</c>,
    ///         <c>Revive</c>, …).
    ///         （<c>Idle</c>、<c>Attack</c>、<c>Cast</c>、<c>Hit</c>、<c>Dead</c>、<c>Revive</c>、…）。
    ///     </para>
    ///     <para>
    ///         When this factory returns non-null for a Spine-backed creature, the routing patch consumes
    ///         <see cref="NCreature.SetAnimationTrigger" /> before the vanilla <c>_spineAnimator</c> path runs; keep
    ///         <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator.HasTrigger" /> in sync for <c>Revive</c> if you
    ///         rely on vanilla <see cref="NCreature.StartReviveAnim" /> gating, or rely on the RitsuLib revive postfix
    ///         when the animator does not declare <c>Revive</c>.
    ///     </para>
    ///     <para>
    ///         常见实现会继承
    ///         <see cref="STS2RitsuLib.Scaffolding.Characters.ModCharacterTemplate{TCardPool,TRelicPool,TPotionPool}" /> 或
    ///         <see cref="ModMonsterTemplate" />，但模板只是便利封装。契约本身通过接口 opt-in：任何实现此接口的模型类型都会通过
    ///         <see cref="STS2RitsuLib.Scaffolding.Characters.Patches.ModCreatureCombatAnimationPlaybackPatch" /> 路由 -
    ///         <b>不</b>要求继承模板。
    ///     </para>
    ///     <para>
    ///         <see cref="ModAnimStateMachine.SetTrigger" /> 接收与原版会分派给 Spine animator 的相同 trigger 名称（<c>Idle</c>、<c>Attack</c>
    ///         、<c>Cast</c>、<c>Hit</c>、<c>Dead</c>、<c>Revive</c>、…）。
    ///     </para>
    ///     <para>
    ///         当此工厂为 Spine 支持的生物返回非 null 时，路由补丁会在原版 <c>_spineAnimator</c> 路径运行前消耗 <see cref="NCreature.SetAnimationTrigger" />
    ///         ；如果你依赖原版 <see cref="NCreature.StartReviveAnim" /> gating，请保持
    ///         <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator.HasTrigger" /> 中的 <c>Revive</c> 同步，或者在 animator 未声明
    ///         <c>Revive</c> 时依赖 RitsuLib revive postfix。
    ///     </para>
    /// </remarks>
    public interface IModCreatureCombatAnimationStateMachineFactory
    {
        /// <summary>
        ///     Builds a state machine bound to <paramref name="visualsRoot" />, or <see langword="null" /> to defer
        ///     to vanilla Spine <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator" /> triggers or, when there is
        ///     no Spine animator, to the single-shot cue playback path. Called at most once per combat visuals lifetime
        ///     (cached by the routing patch via a <see cref="ConditionalWeakTable{TKey,TValue}" /> keyed on
        ///     <paramref name="visualsRoot" />).
        ///     构建绑定到 <paramref name="visualsRoot" /> 的状态机；返回 <see langword="null" /> 则回退到原版 Spine
        ///     <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator" /> trigger，若没有 Spine animator，则回退到
        ///     单次 cue 播放路径。每个战斗视觉生命周期最多调用一次（路由补丁会通过以
        ///     <see cref="ConditionalWeakTable{TKey,TValue}" /> 为键的 <paramref name="visualsRoot" /> 缓存）。
        /// </summary>
        /// <param name="visualsRoot">
        ///     Combat visuals root (typically an <see cref="NCreatureVisuals" />).
        ///     战斗视觉根节点（通常是 <see cref="NCreatureVisuals" />）。
        /// </param>
        ModAnimStateMachine? TryCreateCombatAnimationStateMachine(Node visualsRoot);
    }

    /// <inheritdoc cref="IModCreatureCombatAnimationStateMachineFactory" />
    [Obsolete("Use IModCreatureCombatAnimationStateMachineFactory and TryCreateCombatAnimationStateMachine.")]
    public interface IModNonSpineAnimationStateMachineFactory
    {
        /// <inheritdoc cref="IModCreatureCombatAnimationStateMachineFactory.TryCreateCombatAnimationStateMachine" />
        ModAnimStateMachine? TryCreateNonSpineAnimationStateMachine(Node visualsRoot);
    }

    /// <summary>
    ///     Runtime <see cref="ModAnimStateMachine" /> factory for mod characters in merchant / rest-site contexts.
    ///     Implement when the merchant visuals need state transitions rather than single-shot playback.
    ///     Mod 角色在商人 / 休息点上下文中的运行时 <see cref="ModAnimStateMachine" /> 工厂。当商人视觉需要状态迁移而不是
    ///     单次播放时实现此接口。
    /// </summary>
    public interface IModCharacterMerchantAnimationStateMachineFactory
    {
        /// <summary>
        ///     Builds a merchant-context state machine, or <see langword="null" /> to defer to the single-shot path.
        ///     构建商人上下文状态机；返回 <see langword="null" /> 则交给单次播放路径。
        /// </summary>
        /// <param name="merchantRoot">
        ///     Merchant character root.
        ///     商人角色根节点。
        /// </param>
        /// <param name="character">
        ///     Owning character model for cue lookup.
        ///     用于 cue 查找的所属角色模型。
        /// </param>
        ModAnimStateMachine? TryCreateMerchantAnimationStateMachine(Node merchantRoot, CharacterModel character);
    }
}
