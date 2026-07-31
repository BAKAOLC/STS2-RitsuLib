using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a runtime <see cref="NCreatureVisuals" /> factory for any combat creature model, including
    ///         player characters and monsters. Implement this interface on the model type. The provided character and
    ///         monster templates are convenient base classes, but are not required.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为任意战斗生物模型（包括玩家角色和怪物）定义运行时 <see cref="NCreatureVisuals" /> 工厂。
    ///         请在模型类型上实现此接口。RitsuLib 提供的角色和怪物模板可作为便利的基类，但并非必需。
    ///     </para>
    /// </summary>
    public interface IModCreatureVisualsFactory
    {
        /// <summary>
        ///     <para xml:lang="en">Creates the combat visuals, or returns <see langword="null" /> to use asset paths.</para>
        ///     <para xml:lang="zh-CN">创建战斗视觉；返回 <see langword="null" /> 时改用资源路径。</para>
        /// </summary>
        NCreatureVisuals? TryCreateCreatureVisuals();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the obsolete monster-specific equivalent of <see cref="IModCreatureVisualsFactory" />.
    ///         It remains supported for compatibility with existing mods.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供已过时的怪物专用 <see cref="IModCreatureVisualsFactory" /> 等效接口。
    ///         为兼容现有模组，RitsuLib 仍支持此接口。
    ///     </para>
    /// </summary>
    [Obsolete(
        "Implement IModCreatureVisualsFactory instead; the replacement applies to both monsters and player characters.",
        false)]
    public interface IModMonsterCreatureVisualsFactory
    {
        /// <summary>
        ///     <para xml:lang="en">Creates the combat visuals, or returns <see langword="null" /> to use asset paths.</para>
        ///     <para xml:lang="zh-CN">创建战斗视觉；返回 <see langword="null" /> 时改用资源路径。</para>
        /// </summary>
        NCreatureVisuals? TryCreateCreatureVisuals();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the obsolete character-specific equivalent of <see cref="IModCreatureVisualsFactory" />.
    ///         It remains supported for compatibility with existing mods.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供已过时的角色专用 <see cref="IModCreatureVisualsFactory" /> 等效接口。
    ///         为兼容现有模组，RitsuLib 仍支持此接口。
    ///     </para>
    /// </summary>
    [Obsolete(
        "Implement IModCreatureVisualsFactory instead; the replacement applies to both monsters and player characters.",
        false)]
    public interface IModCharacterCreatureVisualsFactory
    {
        /// <summary>
        ///     <para xml:lang="en">Creates the combat visuals, or returns <see langword="null" /> to use asset paths.</para>
        ///     <para xml:lang="zh-CN">创建战斗视觉；返回 <see langword="null" /> 时改用资源路径。</para>
        /// </summary>
        NCreatureVisuals? TryCreateCreatureVisuals();
    }

    /// <summary>
    ///     <para xml:lang="en">Defines a factory that creates an encounter's combat scene root at runtime.</para>
    ///     <para xml:lang="zh-CN">定义在运行时创建遭遇战斗场景根节点的工厂。</para>
    /// </summary>
    public interface IModEncounterCombatSceneFactory
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the encounter provides its combat scene exclusively through this factory.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取该遭遇是否仅通过此工厂提供战斗场景。</para>
        /// </summary>
        bool SuppliesEncounterCombatSceneFromFactory { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates the combat UI root, or returns <see langword="null" /> to load the default encounter scene.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建战斗界面根节点；返回 <see langword="null" /> 时加载默认遭遇场景。
        ///     </para>
        /// </summary>
        Control? TryCreateEncounterCombatScene();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a runtime layout scene factory for <see cref="MegaCrit.Sts2.Core.Models.EventModel.CreateScene" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="MegaCrit.Sts2.Core.Models.EventModel.CreateScene" /> 定义运行时布局场景工厂。
    ///     </para>
    /// </summary>
    public interface IModEventLayoutPackedSceneFactory
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates the layout scene, or returns <see langword="null" /> to resolve <c>LayoutScenePath</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建布局场景；返回 <see langword="null" /> 时解析 <c>LayoutScenePath</c>。
        ///     </para>
        /// </summary>
        PackedScene? TryCreateLayoutPackedScene();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a runtime background scene factory for
    ///         <see cref="MegaCrit.Sts2.Core.Models.EventModel.CreateBackgroundScene" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="MegaCrit.Sts2.Core.Models.EventModel.CreateBackgroundScene" /> 定义运行时背景场景工厂。
    ///     </para>
    /// </summary>
    public interface IModEventBackgroundPackedSceneFactory
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates the background scene, or returns <see langword="null" /> to resolve an asset path.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建背景场景；返回 <see langword="null" /> 时解析资源路径。
        ///     </para>
        /// </summary>
        PackedScene? TryCreateBackgroundPackedScene();
    }

    /// <summary>
    ///     <para xml:lang="en">Defines a factory that creates an event's VFX root at runtime.</para>
    ///     <para xml:lang="zh-CN">定义在运行时创建事件 VFX 根节点的工厂。</para>
    /// </summary>
    public interface IModEventVfxFactory
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether <see cref="TryCreateEventVfx" /> should run instead of loading the default VFX path.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取是否应调用 <see cref="TryCreateEventVfx" />，而非加载默认 VFX 路径。
        ///     </para>
        /// </summary>
        bool SuppliesCustomEventVfx { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates the VFX root, or returns <see langword="null" /> to load it from an asset path.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建 VFX 根节点；返回 <see langword="null" /> 时从资源路径加载。</para>
        /// </summary>
        Node2D? TryCreateEventVfx();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a runtime orb presentation factory for <c>OrbModel.CreateSprite</c>.
    ///     </para>
    ///     <para xml:lang="zh-CN">为 <c>OrbModel.CreateSprite</c> 定义运行时充能球表现工厂。</para>
    /// </summary>
    public interface IModOrbSpriteFactory
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates the orb sprite node, or returns <see langword="null" /> to instantiate its visuals scene.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建充能球精灵节点；返回 <see langword="null" /> 时实例化其视觉场景。
        ///     </para>
        /// </summary>
        Node2D? TryCreateOrbSprite();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a runtime Spine <see cref="CreatureAnimator" /> factory for any combat creature model,
    ///         including player characters and monsters. It can replace the animator generated by the base game
    ///         without requiring a custom <see cref="NCreature" /> subclass.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为任意战斗生物模型（包括玩家角色和怪物）定义运行时 Spine <see cref="CreatureAnimator" /> 工厂。
    ///         无需自定义 <see cref="NCreature" /> 子类，即可替换原版游戏生成的动画器。
    ///     </para>
    /// </summary>
    public interface IModCreatureAnimatorFactory
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a configured <see cref="CreatureAnimator" />, or returns <see langword="null" /> to use
        ///         the base-game animator.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建配置完毕的 <see cref="CreatureAnimator" />；返回 <see langword="null" /> 时使用原版动画器。
        ///     </para>
        /// </summary>
        /// <param name="controller">
        ///     <para xml:lang="en">The Spine controller attached to the creature's combat visuals.</para>
        ///     <para xml:lang="zh-CN">附加到生物战斗视觉的 Spine 控制器。</para>
        /// </param>
        CreatureAnimator? TryCreateCreatureAnimator(MegaSprite controller);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the obsolete character-specific equivalent of <see cref="IModCreatureAnimatorFactory" />.
    ///         It remains supported for compatibility with existing mods.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供已过时的角色专用 <see cref="IModCreatureAnimatorFactory" /> 等效接口。
    ///         为兼容现有模组，RitsuLib 仍支持此接口。
    ///     </para>
    /// </summary>
    [Obsolete(
        "Implement IModCreatureAnimatorFactory instead; the replacement applies to both monsters and player characters.",
        false)]
    public interface IModCharacterCreatureAnimatorFactory
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a configured <see cref="CreatureAnimator" />, or returns <see langword="null" /> to use
        ///         the base-game animator.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建配置完毕的 <see cref="CreatureAnimator" />；返回 <see langword="null" /> 时使用原版动画器。
        ///     </para>
        /// </summary>
        /// <param name="controller">
        ///     <para xml:lang="en">The Spine controller attached to the character's combat visuals.</para>
        ///     <para xml:lang="zh-CN">附加到角色战斗视觉的 Spine 控制器。</para>
        /// </param>
        CreatureAnimator? TryCreateCreatureAnimator(MegaSprite controller);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a runtime combat <see cref="ModAnimStateMachine" /> factory for creature models whose
    ///         <see cref="NCreature.SetAnimationTrigger" /> calls should be handled by
    ///         <see cref="ModAnimStateMachine.SetTrigger" />. It supports Spine and non-Spine animation backends.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为生物模型定义运行时战斗 <see cref="ModAnimStateMachine" /> 工厂，使其
    ///         <see cref="NCreature.SetAnimationTrigger" /> 调用可由 <see cref="ModAnimStateMachine.SetTrigger" />
    ///         处理。该工厂支持 Spine 和非 Spine 动画后端。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Typical implementers subclass
    ///         <see cref="STS2RitsuLib.Scaffolding.Characters.ModCharacterTemplate{TCardPool,TRelicPool,TPotionPool}" />
    ///         or <see cref="ModMonsterTemplate" />, but the templates are convenience. The contract is opt-in via the
    ///         interface itself: any model type implementing this interface is routed through
    ///         <see cref="STS2RitsuLib.Scaffolding.Characters.Patches.ModCreatureCombatAnimationPlaybackPatch" /> —
    ///         template subclassing is <b>not</b> required.
    ///     </para>
    ///     <para xml:lang="en">
    ///         <see cref="ModAnimStateMachine.SetTrigger" /> receives the same trigger names that vanilla would
    ///         dispatch to a Spine animator (<c>Idle</c>, <c>Attack</c>, <c>Cast</c>, <c>Hit</c>, <c>Dead</c>,
    ///         <c>Revive</c>, and others).
    ///     </para>
    ///     <para xml:lang="en">
    ///         When this factory returns non-null for a Spine-backed creature, the routing patch consumes
    ///         <see cref="NCreature.SetAnimationTrigger" /> before the vanilla <c>_spineAnimator</c> path runs; keep
    ///         <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator.HasTrigger" /> in sync for <c>Revive</c> if you
    ///         rely on vanilla <see cref="NCreature.StartReviveAnim" /> gating, or rely on the RitsuLib revive postfix
    ///         when the animator does not declare <c>Revive</c>.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         常见实现会继承
    ///         <see cref="STS2RitsuLib.Scaffolding.Characters.ModCharacterTemplate{TCardPool,TRelicPool,TPotionPool}" /> 或
    ///         <see cref="ModMonsterTemplate" />，但模板只是便利封装。该接口采用显式选择加入的方式：
    ///         任何实现此接口的模型类型都会由
    ///         <see cref="STS2RitsuLib.Scaffolding.Characters.Patches.ModCreatureCombatAnimationPlaybackPatch" />
    ///         路由，<b>无需</b>继承模板。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="ModAnimStateMachine.SetTrigger" /> 接收与原版分派给 Spine 动画器相同的触发器名称，
    ///         包括 <c>Idle</c>、<c>Attack</c>、<c>Cast</c>、<c>Hit</c>、<c>Dead</c> 和 <c>Revive</c> 等。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         当此工厂为使用 Spine 后端的生物返回非空状态机时，路由补丁会先处理
    ///         <see cref="NCreature.SetAnimationTrigger" />，不再进入原版 <c>_spineAnimator</c> 路径。
    ///         若依赖原版 <see cref="NCreature.StartReviveAnim" /> 的条件判断，请让
    ///         <see cref="MegaCrit.Sts2.Core.Animation.CreatureAnimator.HasTrigger" /> 正确报告 <c>Revive</c>；
    ///         动画器未声明 <c>Revive</c> 时，也可依赖 RitsuLib 的复活后置补丁。
    ///     </para>
    /// </remarks>
    public interface IModCreatureCombatAnimationStateMachineFactory
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a state machine bound to <paramref name="visualsRoot" />, or returns
        ///         <see langword="null" /> to use the Spine animator or single-shot cue path. The routing patch calls
        ///         this method at most once during each combat visuals lifetime.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建绑定到 <paramref name="visualsRoot" /> 的状态机；返回 <see langword="null" /> 时改用 Spine
        ///         动画器或单次视觉提示播放路径。每个战斗视觉生命周期内，路由补丁至多调用此方法一次。
        ///     </para>
        /// </summary>
        /// <param name="visualsRoot">
        ///     <para xml:lang="en">The combat visuals root, typically an <see cref="NCreatureVisuals" />.</para>
        ///     <para xml:lang="zh-CN">战斗视觉根节点，通常为 <see cref="NCreatureVisuals" />。</para>
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
    ///     <para xml:lang="en">
    ///         Defines a runtime <see cref="ModAnimStateMachine" /> factory for mod characters shown in merchant
    ///         contexts.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为商人场景中显示的模组角色定义运行时 <see cref="ModAnimStateMachine" /> 工厂。
    ///     </para>
    /// </summary>
    public interface IModCharacterMerchantAnimationStateMachineFactory
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a merchant-context state machine, or returns <see langword="null" /> to use single-shot
        ///         cue playback.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建商人场景状态机；返回 <see langword="null" /> 时使用单次视觉提示播放。
        ///     </para>
        /// </summary>
        /// <param name="merchantRoot">
        ///     <para xml:lang="en">The merchant character root.</para>
        ///     <para xml:lang="zh-CN">商人角色根节点。</para>
        /// </param>
        /// <param name="character">
        ///     <para xml:lang="en">The owning character model used to look up visual cues.</para>
        ///     <para xml:lang="zh-CN">用于查找视觉提示的所属角色模型。</para>
        /// </param>
        ModAnimStateMachine? TryCreateMerchantAnimationStateMachine(Node merchantRoot, CharacterModel character);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a runtime <see cref="ModAnimStateMachine" /> factory for mod characters shown at rest sites.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为休息处显示的模组角色定义运行时 <see cref="ModAnimStateMachine" /> 工厂。
    ///     </para>
    /// </summary>
    public interface IModCharacterRestSiteAnimationStateMachineFactory
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a rest-site state machine, or returns <see langword="null" /> to use single-shot cue
        ///         playback.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建休息处状态机；返回 <see langword="null" /> 时使用单次视觉提示播放。
        ///     </para>
        /// </summary>
        /// <param name="restSiteRoot">
        ///     <para xml:lang="en">The rest-site character root.</para>
        ///     <para xml:lang="zh-CN">休息处角色根节点。</para>
        /// </param>
        /// <param name="character">
        ///     <para xml:lang="en">The owning character model used to look up visual cues.</para>
        ///     <para xml:lang="zh-CN">用于查找视觉提示的所属角色模型。</para>
        /// </param>
        ModAnimStateMachine? TryCreateRestSiteAnimationStateMachine(Node restSiteRoot, CharacterModel character);
    }
}
