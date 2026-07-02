using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine
{
    /// <summary>
    ///     Top-level convenience factories for animation state machines. Mirrors the semantics of
    ///     baselib's <c>CustomCharacterModel.SetupAnimationState</c> but usable against any
    ///     <see cref="IAnimationBackend" /> (Spine, Godot animation player / animated sprite, cue frame sequences).
    ///     动画状态机的顶层便捷工厂。语义对应 baselib 的
    ///     <c>CustomCharacterModel.SetupAnimationState</c>，但可用于任何
    ///     <see cref="IAnimationBackend" />（Spine、Godot animation player / animated sprite、cue 帧序列）。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="Standard" /> produces a vanilla <see cref="CreatureAnimator" /> so callers can return it
    ///         directly from <c>CharacterModel.GenerateAnimator</c>; this is the closest drop-in replacement for the
    ///         baselib helper.
    ///     </para>
    ///     <para>
    ///         <see cref="StandardCue" /> produces a backend-agnostic <see cref="ModAnimStateMachine" /> for
    ///         non-Spine visuals rooted at a <see cref="Node" /> (cue frame sequences, Godot animation player,
    ///         animated sprite).
    ///     </para>
    ///     <para>
    ///         Terminal states (<c>Dead</c>) leave <see cref="ModAnimState.NextState" /> / <c>AnimState.NextState</c>
    ///         unset so completion does not auto-return to idle, matching the vanilla behaviour.
    ///     </para>
    ///     <para>
    ///         <see cref="Standard" /> 会生成原版 <see cref="CreatureAnimator" />，调用方可以直接从
    ///         <c>CharacterModel.GenerateAnimator</c> 返回它；这是最接近 baselib helper 的
    ///         直接替代方案。
    ///     </para>
    ///     <para>
    ///         <see cref="StandardCue" /> 会为以 <see cref="Node" /> 为根的非 Spine 视觉生成
    ///         与后端无关的 <see cref="ModAnimStateMachine" />（cue 帧序列、Godot animation player、
    ///         animated sprite）。
    ///     </para>
    ///     <para>
    ///         终止状态（<c>Dead</c>）会让 <see cref="ModAnimState.NextState" /> / <c>AnimState.NextState</c>
    ///         保持未设置，因此完成后不会自动回到 idle，与原版行为一致。
    ///     </para>
    /// </remarks>
    public static class ModAnimStateMachines
    {
        /// <summary>
        ///     Builds a vanilla Spine <see cref="CreatureAnimator" /> matching the standard idle / dead / hit /
        ///     attack / cast / relaxed shape. Null animation names collapse to the idle state (vanilla behaviour).
        ///     构建一个原版 Spine <see cref="CreatureAnimator" />，匹配标准 idle / dead / hit /
        ///     attack / cast / relaxed 结构。null 动画名会折叠到 idle 状态（原版行为）。
        /// </summary>
        public static CreatureAnimator Standard(MegaSprite controller,
            string idleName,
            string? deadName = null, bool deadLoop = false,
            string? hitName = null, bool hitLoop = false,
            string? attackName = null, bool attackLoop = false,
            string? castName = null, bool castLoop = false,
            string? relaxedName = null, bool relaxedLoop = true)
        {
            ArgumentNullException.ThrowIfNull(controller);

            var idle = new AnimState(idleName, true);
            var dead = deadName == null ? idle : new(deadName, deadLoop);
            var hit = hitName == null
                ? idle
                : new(hitName, hitLoop) { NextState = idle };
            var attack = attackName == null
                ? idle
                : new(attackName, attackLoop) { NextState = idle };
            var cast = castName == null
                ? idle
                : new(castName, castLoop) { NextState = idle };

            AnimState relaxed;
            if (relaxedName == null)
            {
                relaxed = idle;
            }
            else
            {
                relaxed = new(relaxedName, relaxedLoop);
                relaxed.AddBranch("Idle", idle);
            }

            var animator = new CreatureAnimator(idle, controller);
            animator.AddAnyState("Idle", idle);
            animator.AddAnyState("Dead", dead);
            animator.AddAnyState("Hit", hit);
            animator.AddAnyState("Attack", attack);
            animator.AddAnyState("Cast", cast);
            animator.AddAnyState("Relaxed", relaxed);
            return animator;
        }

        /// <summary>
        ///     Builds a non-Spine <see cref="ModAnimStateMachine" /> over <paramref name="visualsRoot" /> matching
        ///     the standard idle / dead / hit / attack / cast / relaxed shape; null names fall back to idle.
        ///     在 <paramref name="visualsRoot" /> 上构建非 Spine <see cref="ModAnimStateMachine" />，匹配
        ///     标准 idle / dead / hit / attack / cast / relaxed 结构；null 名称会回退到 idle。
        /// </summary>
        /// <param name="visualsRoot">
        ///     Visuals root used by <see cref="CompositeBackendFactory" />.
        ///     供 <see cref="CompositeBackendFactory" /> 使用的视觉根节点。
        /// </param>
        /// <param name="character">
        ///     Optional character model used to discover cue sets.
        ///     用于发现 cue set 的可选角色模型。
        /// </param>
        /// <param name="idleName">
        ///     Idle animation id (always required; looping).
        ///     Idle 动画 id（始终必需；循环）。
        /// </param>
        /// <param name="deadName">
        ///     Optional die animation id; <see langword="null" /> falls back to idle.
        ///     可选死亡动画 id；<see langword="null" /> 会回退到 idle。
        /// </param>
        /// <param name="deadLoop">
        ///     Loop flag for the die animation.
        ///     死亡动画的循环标记。
        /// </param>
        /// <param name="hitName">
        ///     Optional hit animation id; <see langword="null" /> falls back to idle.
        ///     可选受击动画 id；<see langword="null" /> 会回退到 idle。
        /// </param>
        /// <param name="hitLoop">
        ///     Loop flag for the hit animation.
        ///     受击动画的循环标记。
        /// </param>
        /// <param name="attackName">
        ///     Optional attack animation id; <see langword="null" /> falls back to idle.
        ///     可选攻击动画 id；<see langword="null" /> 会回退到 idle。
        /// </param>
        /// <param name="attackLoop">
        ///     Loop flag for the attack animation.
        ///     攻击动画的循环标记。
        /// </param>
        /// <param name="castName">
        ///     Optional cast animation id; <see langword="null" /> falls back to idle.
        ///     可选施放动画 id；<see langword="null" /> 会回退到 idle。
        /// </param>
        /// <param name="castLoop">
        ///     Loop flag for the cast animation.
        ///     施放动画的循环标记。
        /// </param>
        /// <param name="relaxedName">
        ///     Optional relaxed animation id; <see langword="null" /> falls back to idle.
        ///     可选 relaxed 动画 id；<see langword="null" /> 会回退到 idle。
        /// </param>
        /// <param name="relaxedLoop">
        ///     Loop flag for the relaxed animation.
        ///     relaxed 动画的循环标记。
        /// </param>
        /// <param name="cueSet">
        ///     Optional explicit cue set, overriding the character-derived one.
        ///     可选显式 cue set，会覆盖从角色派生的 cue set。
        /// </param>
        public static ModAnimStateMachine StandardCue(Node visualsRoot, CharacterModel? character,
            string idleName,
            string? deadName = null, bool deadLoop = false,
            string? hitName = null, bool hitLoop = false,
            string? attackName = null, bool attackLoop = false,
            string? castName = null, bool castLoop = false,
            string? relaxedName = null, bool relaxedLoop = true,
            VisualCueSet? cueSet = null)
        {
            ArgumentNullException.ThrowIfNull(visualsRoot);
            ArgumentException.ThrowIfNullOrWhiteSpace(idleName);

            var builder = ModAnimStateMachineBuilder.Create()
                .AddState(idleName, true).AsInitial().Done();

            AddOptional(builder, deadName, deadLoop, idleName, false);
            AddOptional(builder, hitName, hitLoop, idleName, true);
            AddOptional(builder, attackName, attackLoop, idleName, true);
            AddOptional(builder, castName, castLoop, idleName, true);

            var relaxedTarget = idleName;
            if (relaxedName != null && !string.Equals(relaxedName, idleName, StringComparison.Ordinal))
            {
                builder.AddState(relaxedName, relaxedLoop).Done();
                builder.AddBranch(relaxedName, "Idle", idleName);
                relaxedTarget = relaxedName;
            }

            builder.AddAnyState("Idle", idleName);
            builder.AddAnyState("Dead", deadName ?? idleName);
            builder.AddAnyState("Hit", hitName ?? idleName);
            builder.AddAnyState("Attack", attackName ?? idleName);
            builder.AddAnyState("Cast", castName ?? idleName);
            builder.AddAnyState("Relaxed", relaxedTarget);

            return builder.BuildForVisualsRoot(visualsRoot, character, cueSet);
        }

        /// <summary>
        ///     Builds a standard merchant-context state machine using merchant world cues when available.
        ///     使用可用的商店 world cue 构建标准商店上下文状态机。
        /// </summary>
        public static ModAnimStateMachine StandardMerchantCue(Node visualsRoot, CharacterModel? character,
            string idleName,
            string? deadName = null, bool deadLoop = false,
            string? hitName = null, bool hitLoop = false,
            string? attackName = null, bool attackLoop = false,
            string? castName = null, bool castLoop = false,
            string? relaxedName = null, bool relaxedLoop = true,
            VisualCueSet? cueSet = null)
        {
            return StandardCue(
                visualsRoot,
                character,
                idleName,
                deadName,
                deadLoop,
                hitName,
                hitLoop,
                attackName,
                attackLoop,
                castName,
                castLoop,
                relaxedName,
                relaxedLoop,
                cueSet ?? TryGetMerchantCueSet(character));
        }

        /// <summary>
        ///     Builds a standard rest-site-context state machine using rest-site world cues when available.
        ///     使用可用的休息点 world cue 构建标准休息点上下文状态机。
        /// </summary>
        public static ModAnimStateMachine StandardRestSiteCue(Node visualsRoot, CharacterModel? character,
            string idleName,
            string? deadName = null, bool deadLoop = false,
            string? hitName = null, bool hitLoop = false,
            string? attackName = null, bool attackLoop = false,
            string? castName = null, bool castLoop = false,
            string? relaxedName = null, bool relaxedLoop = true,
            VisualCueSet? cueSet = null)
        {
            return StandardCue(
                visualsRoot,
                character,
                idleName,
                deadName,
                deadLoop,
                hitName,
                hitLoop,
                attackName,
                attackLoop,
                castName,
                castLoop,
                relaxedName,
                relaxedLoop,
                cueSet ?? TryGetRestSiteCueSet(character));
        }

        private static void AddOptional(ModAnimStateMachineBuilder builder, string? name, bool loop, string idleName,
            bool hasNext)
        {
            if (name == null || string.Equals(name, idleName, StringComparison.Ordinal))
                return;

            var scope = builder.AddState(name, loop);
            if (hasNext)
                scope.WithNext(idleName);
        }

        private static VisualCueSet? TryGetMerchantCueSet(CharacterModel? character)
        {
            return character is not IModCharacterAssetOverrides overrides
                ? null
                : overrides.WorldProceduralVisuals?.Merchant?.CueSet ?? overrides.VisualCues;
        }

        private static VisualCueSet? TryGetRestSiteCueSet(CharacterModel? character)
        {
            return character is not IModCharacterAssetOverrides overrides
                ? null
                : overrides.WorldProceduralVisuals?.RestSite?.CueSet ?? overrides.VisualCues;
        }
    }
}
