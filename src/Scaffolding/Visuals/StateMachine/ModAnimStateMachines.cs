using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides convenience factories for the standard creature-animation state graph. They mirror baselib's
    ///         <c>CustomCharacterModel.SetupAnimationState</c> shape for Spine and for the non-Spine backends selected
    ///         from a visuals root.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为标准生物动画状态图提供便捷工厂。它们既适用于 Spine，也适用于从视觉根节点选择的非 Spine 后端，
    ///         状态图结构与 baselib 的 <c>CustomCharacterModel.SetupAnimationState</c> 对应。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         <see cref="Standard" /> produces a vanilla <see cref="CreatureAnimator" /> so callers can return it
    ///         directly from <c>CharacterModel.GenerateAnimator</c>; this is the closest drop-in replacement for the
    ///         baselib helper.
    ///     </para>
    ///     <para xml:lang="en">
    ///         <see cref="StandardCue" /> produces a backend-agnostic <see cref="ModAnimStateMachine" /> for
    ///         non-Spine visuals rooted at a <see cref="Node" /> (cue frame sequences, Godot animation player,
    ///         animated sprite).
    ///     </para>
    ///     <para xml:lang="en">
    ///         Terminal states (<c>Dead</c>) leave <see cref="ModAnimState.NextState" /> / <c>AnimState.NextState</c>
    ///         unset so completion does not auto-return to idle, matching the vanilla behaviour.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="Standard" /> 会生成原版 <see cref="CreatureAnimator" />，调用方可以直接从
    ///         <c>CharacterModel.GenerateAnimator</c> 返回它；这是最接近 baselib 辅助方法的直接替代方案。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="StandardCue" /> 会为以 <see cref="Node" /> 为根的非 Spine 视觉生成
    ///         与后端无关的 <see cref="ModAnimStateMachine" />（视觉提示帧序列、Godot 动画播放器、
    ///         动画精灵）。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         终止状态（<c>Dead</c>）会让 <see cref="ModAnimState.NextState" /> / <c>AnimState.NextState</c>
    ///         保持未设置，因此完成后不会自动回到 idle，与原版行为一致。
    ///     </para>
    /// </remarks>
    public static class ModAnimStateMachines
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a Spine <see cref="CreatureAnimator" /> with the standard idle, dead, hit, attack, cast, and
        ///         relaxed triggers. An optional <see langword="null" /> name reuses the idle state.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         构建带有待机、死亡、受击、攻击、施放和放松触发器的 Spine <see cref="CreatureAnimator" />。
        ///         可选动画名为 <see langword="null" /> 时复用待机状态。
        ///     </para>
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
            ArgumentException.ThrowIfNullOrWhiteSpace(idleName);
            ValidateOptionalAnimationName(deadName, nameof(deadName));
            ValidateOptionalAnimationName(hitName, nameof(hitName));
            ValidateOptionalAnimationName(attackName, nameof(attackName));
            ValidateOptionalAnimationName(castName, nameof(castName));
            ValidateOptionalAnimationName(relaxedName, nameof(relaxedName));

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
        ///     <para xml:lang="en">
        ///         Builds a non-Spine <see cref="ModAnimStateMachine" /> over <paramref name="visualsRoot" /> with the
        ///         standard idle, dead, hit, attack, cast, and relaxed triggers; optional <see langword="null" /> names
        ///         target the idle state.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <paramref name="visualsRoot" /> 上构建非 Spine <see cref="ModAnimStateMachine" />，含有标准的待机、
        ///         死亡、受击、攻击、施放和放松触发器；可选名称为 <see langword="null" /> 时会指向待机状态。
        ///     </para>
        /// </summary>
        /// <param name="visualsRoot">
        ///     <para xml:lang="en">Visual root node used by <see cref="CompositeBackendFactory" />.</para>
        ///     <para xml:lang="zh-CN"><see cref="CompositeBackendFactory" /> 使用的视觉根节点。</para>
        /// </param>
        /// <param name="character">
        ///     <para xml:lang="en">Optional character model used to discover cue sets.</para>
        ///     <para xml:lang="zh-CN">用于发现动画提示集合的可选角色模型。</para>
        /// </param>
        /// <param name="idleName">
        ///     <para xml:lang="en">Required looping idle animation ID.</para>
        ///     <para xml:lang="zh-CN">必填且循环播放的待机动画 ID。</para>
        /// </param>
        /// <param name="deadName">
        ///     <para xml:lang="en">Optional death animation ID; <see langword="null" /> targets idle.</para>
        ///     <para xml:lang="zh-CN">可选的死亡动画 ID；<see langword="null" /> 时指向待机动画。</para>
        /// </param>
        /// <param name="deadLoop">
        ///     <para xml:lang="en">Whether the death animation should loop.</para>
        ///     <para xml:lang="zh-CN">死亡动画是否循环。</para>
        /// </param>
        /// <param name="hitName">
        ///     <para xml:lang="en">Optional hit animation ID; <see langword="null" /> targets idle.</para>
        ///     <para xml:lang="zh-CN">可选的受击动画 ID；<see langword="null" /> 时指向待机动画。</para>
        /// </param>
        /// <param name="hitLoop">
        ///     <para xml:lang="en">Whether the hit animation should loop.</para>
        ///     <para xml:lang="zh-CN">受击动画是否循环。</para>
        /// </param>
        /// <param name="attackName">
        ///     <para xml:lang="en">Optional attack animation ID; <see langword="null" /> targets idle.</para>
        ///     <para xml:lang="zh-CN">可选的攻击动画 ID；<see langword="null" /> 时指向待机动画。</para>
        /// </param>
        /// <param name="attackLoop">
        ///     <para xml:lang="en">Whether the attack animation should loop.</para>
        ///     <para xml:lang="zh-CN">攻击动画是否循环。</para>
        /// </param>
        /// <param name="castName">
        ///     <para xml:lang="en">Optional cast animation ID; <see langword="null" /> targets idle.</para>
        ///     <para xml:lang="zh-CN">可选的施放动画 ID；<see langword="null" /> 时指向待机动画。</para>
        /// </param>
        /// <param name="castLoop">
        ///     <para xml:lang="en">Whether the cast animation should loop.</para>
        ///     <para xml:lang="zh-CN">施放动画是否循环。</para>
        /// </param>
        /// <param name="relaxedName">
        ///     <para xml:lang="en">Optional relaxed animation ID; <see langword="null" /> targets idle.</para>
        ///     <para xml:lang="zh-CN">可选的放松动画 ID；<see langword="null" /> 时指向待机动画。</para>
        /// </param>
        /// <param name="relaxedLoop">
        ///     <para xml:lang="en">Whether the relaxed animation should loop.</para>
        ///     <para xml:lang="zh-CN">放松动画是否循环。</para>
        /// </param>
        /// <param name="cueSet">
        ///     <para xml:lang="en">Optional explicit cue set, which takes precedence over character-derived cues.</para>
        ///     <para xml:lang="zh-CN">可选的显式视觉提示集，优先于从角色取得的视觉提示集。</para>
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
            ValidateOptionalAnimationName(deadName, nameof(deadName));
            ValidateOptionalAnimationName(hitName, nameof(hitName));
            ValidateOptionalAnimationName(attackName, nameof(attackName));
            ValidateOptionalAnimationName(castName, nameof(castName));
            ValidateOptionalAnimationName(relaxedName, nameof(relaxedName));

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
        ///     <para xml:lang="en">Builds the standard graph, preferring the character's merchant visual cue set.</para>
        ///     <para xml:lang="zh-CN">构建标准状态图，并优先使用角色的商店视觉提示集。</para>
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
        ///     <para xml:lang="en">Builds the standard graph, preferring the character's rest-site visual cue set.</para>
        ///     <para xml:lang="zh-CN">构建标准状态图，并优先使用角色的休息处视觉提示集。</para>
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

        private static void ValidateOptionalAnimationName(string? name, string paramName)
        {
            if (name != null && string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Optional animation names must not be empty or whitespace.", paramName);
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
