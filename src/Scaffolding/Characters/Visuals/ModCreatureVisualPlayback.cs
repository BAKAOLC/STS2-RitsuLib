using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Visuals;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Characters.Visuals
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides shared animation playback for mod creature visuals. It first checks textures and data-driven
    ///         frame sequences in <see cref="IModCharacterAssetOverrides.VisualCues" />, then tries Spine tracks or
    ///         Godot <see cref="AnimationPlayer" />, <see cref="AnimationTree" />, and
    ///         <see cref="AnimatedSprite2D" /> nodes as appropriate.
    ///     </para>
    ///     <para xml:lang="en">
    ///         Procedural roots created through <see cref="RitsuGodotNodeFactories" /> use the same cue aliases and
    ///         trigger mapping as the patched <see cref="NCreature.SetAnimationTrigger" /> path.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为模组生物形象提供统一的动画播放入口。它会先检查
    ///         <see cref="IModCharacterAssetOverrides.VisualCues" /> 中的纹理和数据驱动帧序列，再根据情况尝试
    ///         Spine 轨道或 Godot 的 <see cref="AnimationPlayer" />、<see cref="AnimationTree" /> 及
    ///         <see cref="AnimatedSprite2D" /> 节点。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过 <see cref="RitsuGodotNodeFactories" /> 创建的程序化根节点，与已补丁处理的
    ///         <see cref="NCreature.SetAnimationTrigger" /> 路径使用相同的提示别名和触发器映射。
    ///     </para>
    /// </summary>
    public static class ModCreatureVisualPlayback
    {
        private static readonly ConditionalWeakTable<Node, Func<string[], bool>> GodotAnimHandlers = [];

        private static readonly ConditionalWeakTable<Node, RitsuCreatureVisualRegistration>
            RitsuCreatureVisuals = [];

        private static readonly AccessTools.FieldRef<NMerchantRoom, List<Player>> MerchantRoomPlayersRef =
            AccessTools.FieldRefAccess<NMerchantRoom, List<Player>>("_players");

        private static readonly ConditionalWeakTable<NFakeMerchant, FakeMerchantPlayerVisualSlot>
            FakeMerchantVisualSlots =
                [];

        private static readonly ConditionalWeakTable<NMerchantCharacter, CharacterModel> FakeMerchantBoothCharacter =
            [];

        private static readonly string[] DieCueNames = ["die", "death", "dead", "Dead"];

        private static readonly string[] IdleCueNames = ["idle", "Idle", "relaxed_loop"];

        private static readonly string[] RelaxedCueNames = ["relaxed", "Relaxed", "relaxed_loop", "idle", "Idle"];

        private static readonly string[] RestSiteLoopCueNames =
            ["relaxed", "Relaxed", "relaxed_loop", "idle", "Idle"];

        private static readonly string[] HurtCueNames = ["hurt", "hit", "Hit"];

        private static readonly string[] AttackCueNames = ["attack", "Attack"];

        private static readonly string[] CastCueNames = ["cast", "Cast"];

        private static readonly string[] ReviveCueNames = ["revive", "Revive"];

        private static readonly string[] StunCueNames = ["stun", "Stun"];

        internal static void RegisterRitsuCreatureVisual(Node visual)
        {
            RitsuCreatureVisuals.Remove(visual);
            RitsuCreatureVisuals.Add(visual, new());
        }

        private static bool ShouldOwnCreatureVisualPlayback(NCreature creature)
        {
            var visuals = creature.Visuals;
            if (visuals != null && RitsuCreatureVisuals.TryGetValue(visuals, out _))
                return true;

            return creature.Entity?.Player?.Character is IModCharacterAssetOverrides { VisualCues: not null };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to play a logical animation cue on combat <see cref="NCreatureVisuals" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试在战斗 <see cref="NCreatureVisuals" /> 上播放逻辑动画提示。
        ///     </para>
        /// </summary>
        /// <param name="visuals">
        ///     <para xml:lang="en">The creature-visuals root.</para>
        ///     <para xml:lang="zh-CN">生物形象根节点。</para>
        /// </param>
        /// <param name="character">
        ///     <para xml:lang="en">The owning character model used to resolve visual cues.</para>
        ///     <para xml:lang="zh-CN">用于解析形象提示的所属角色模型。</para>
        /// </param>
        /// <param name="primaryCue">
        ///     <para xml:lang="en">The primary cue name, such as <c>die</c>.</para>
        ///     <para xml:lang="zh-CN">主要提示名称，例如 <c>die</c>。</para>
        /// </param>
        /// <param name="alternateCueNames">
        ///     <para xml:lang="en">Optional cue aliases to try instead of the default aliases.</para>
        ///     <para xml:lang="zh-CN">用于替代默认别名进行尝试的可选提示别名。</para>
        /// </param>
        /// <param name="loop">
        ///     <para xml:lang="en">Whether Spine playback should loop.</para>
        ///     <para xml:lang="zh-CN">Spine 动画是否循环播放。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if any supported playback path applied the cue; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         任一受支持的播放路径成功应用提示时返回 <see langword="true" />；否则返回
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryPlayCue(NCreatureVisuals visuals, CharacterModel? character, string primaryCue,
            ReadOnlySpan<string> alternateCueNames = default, bool loop = false)
        {
            if (!GodotObject.IsInstanceValid(visuals) || string.IsNullOrWhiteSpace(primaryCue))
                return false;

            var names = alternateCueNames.Length > 0
                ? alternateCueNames
                : BuildDefaultAlternateNames(primaryCue);

            CueFrameSequencePlayer.StopUnder(visuals);

            if (TryApplyVisualCues(visuals, character, names, null))
                return true;

            return TryPlaySpine(visuals, names, loop) || TryPlayGodotAnimations(visuals, names);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to play a world-scene animation or static cue on an arbitrary visual root, such as an
        ///         <see cref="NMerchantCharacter" /> or rest-site character.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试在任意世界场景形象根节点上播放动画或静态提示，例如 <see cref="NMerchantCharacter" /> 或
        ///         休息处角色。
        ///     </para>
        /// </summary>
        /// <param name="root">
        ///     <para xml:lang="en">The visual root.</para>
        ///     <para xml:lang="zh-CN">形象根节点。</para>
        /// </param>
        /// <param name="character">
        ///     <para xml:lang="en">The character model used to resolve visual cues.</para>
        ///     <para xml:lang="zh-CN">用于解析形象提示的角色模型。</para>
        /// </param>
        /// <param name="animName">
        ///     <para xml:lang="en">The logical animation or cue name.</para>
        ///     <para xml:lang="zh-CN">逻辑动画或提示名称。</para>
        /// </param>
        /// <param name="loop">
        ///     <para xml:lang="en">Whether Spine playback should loop.</para>
        ///     <para xml:lang="zh-CN">Spine 动画是否循环播放。</para>
        /// </param>
        /// <param name="cueSetOverride">
        ///     <para xml:lang="en">
        ///         An optional cue set to use instead of <see cref="IModCharacterAssetOverrides.VisualCues" />, such
        ///         as a merchant or rest-site cue set from
        ///         <see cref="IModCharacterAssetOverrides.WorldProceduralVisuals" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         用于替代 <see cref="IModCharacterAssetOverrides.VisualCues" /> 的可选提示集合，例如
        ///         <see cref="IModCharacterAssetOverrides.WorldProceduralVisuals" /> 中的商人或休息处提示集合。
        ///     </para>
        /// </param>
        public static bool TryPlayOnVisualRoot(Node root, CharacterModel? character, string animName, bool loop = false,
            VisualCueSet? cueSetOverride = null)
        {
            if (!GodotObject.IsInstanceValid(root) || string.IsNullOrWhiteSpace(animName))
                return false;

            var names = BuildDefaultAlternateNames(animName);

            CueFrameSequencePlayer.StopUnder(root);

            return TryApplyVisualCues(root, character, names, cueSetOverride)
                   || TryPlayGodotAnimations(root, names);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         For a RitsuLib-managed creature without a Spine animator, maps an animator trigger to a cue and
        ///         attempts to play it on <see cref="NCreature.Visuals" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         对由 RitsuLib 管理且没有 Spine 动画器的生物，将动画器触发器映射为提示，并尝试在
        ///         <see cref="NCreature.Visuals" /> 上播放。
        ///     </para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if the mapped cue was played; otherwise, <see langword="false" />. A
        ///         Spine-backed or unmanaged creature always returns <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         成功播放映射后的提示时返回 <see langword="true" />；否则返回 <see langword="false" />。基于 Spine
        ///         或不由 RitsuLib 管理的生物始终返回 <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryPlayFromCreatureAnimatorTrigger(NCreature creature, string trigger)
        {
            if (creature.HasSpineAnimation || !ShouldOwnCreatureVisualPlayback(creature))
                return false;

            var primary = MapAnimatorTriggerToCue(trigger);
            var character = creature.Entity?.Player?.Character;
            return TryPlayCue(creature.Visuals, character, primary);
        }

        internal static bool TryGetDurationFromCreatureAnimatorTrigger(NCreature creature, string trigger,
            out float seconds)
        {
            seconds = 0f;
            if (creature.HasSpineAnimation ||
                !GodotObject.IsInstanceValid(creature.Visuals) ||
                !ShouldOwnCreatureVisualPlayback(creature))
                return false;

            var primary = MapAnimatorTriggerToCue(trigger);
            var names = BuildDefaultAlternateNames(primary);
            var character = creature.Entity?.Player?.Character;
            var cueMatch = TryGetVisualCueDuration(character, names, out seconds, out var visualCueMatched);
            return visualCueMatched ? cueMatch : TryGetGodotAnimationDuration(creature.Visuals, names, out seconds);
        }

        internal static bool TryResolveMerchantCharacterModel(NMerchantRoom? room, NMerchantCharacter visual,
            out CharacterModel? character)
        {
            character = null;
            if (room == null)
                return false;

            var visuals = room.PlayerVisuals;
            for (var i = 0; i < visuals.Count; i++)
            {
                if (!ReferenceEquals(visuals[i], visual))
                    continue;

                var players = MerchantRoomPlayersRef(room);
                if (players == null || i >= players.Count)
                    return false;

                character = players[i].Character;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves the owning <see cref="CharacterModel" /> for a merchant-booth
        ///         <see cref="NMerchantCharacter" /> in either <see cref="NMerchantRoom" /> or the Fake Merchant event.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析 <see cref="NMerchantRoom" /> 或“伪装商人”事件中摊位
        ///         <see cref="NMerchantCharacter" /> 所属的 <see cref="CharacterModel" />。
        ///     </para>
        /// </summary>
        internal static bool TryResolveMerchantCharacterModel(NMerchantCharacter visual,
            out CharacterModel? character)
        {
            if (TryResolveMerchantCharacterModel(NMerchantRoom.Instance, visual, out character))
                return true;

            if (!FakeMerchantBoothCharacter.TryGetValue(visual, out var fromFakeBooth))
            {
                character = null;
                return false;
            }

            character = fromFakeBooth;
            return true;
        }

        internal static void RegisterFakeMerchantPlayerVisuals(NFakeMerchant screen, List<NMerchantCharacter> ordered,
            IReadOnlyList<Player> players)
        {
            var slot = FakeMerchantVisualSlots.GetValue(screen, _ => new());
            slot.Visuals.Clear();
            slot.Visuals.AddRange(ordered);

            var n = Math.Min(ordered.Count, players.Count);
            for (var i = 0; i < n; i++)
            {
                FakeMerchantBoothCharacter.Remove(ordered[i]);
                FakeMerchantBoothCharacter.Add(ordered[i], players[i].Character);
            }
        }

        internal static IReadOnlyList<NMerchantCharacter>? TryGetFakeMerchantPlayerVisuals(NFakeMerchant screen)
        {
            return FakeMerchantVisualSlots.TryGetValue(screen, out var slot) ? slot.Visuals : null;
        }

        internal static string MapWorldAnimationToStateMachineTrigger(string animName)
        {
            if (string.IsNullOrWhiteSpace(animName))
                return animName;

            return animName.ToLowerInvariant() switch
            {
                "idle" => CreatureAnimator.idleTrigger,
                "relaxed" or "relaxed_loop" or "overgrowth_loop" or "hive_loop" or "glory_loop" => "Relaxed",
                "die" or "death" or "dead" => CreatureAnimator.deathTrigger,
                "hurt" or "hit" => CreatureAnimator.hitTrigger,
                "attack" => CreatureAnimator.attackTrigger,
                "cast" => CreatureAnimator.castTrigger,
                "revive" => CreatureAnimator.reviveTrigger,
                "stun" => "Stun",
                _ => animName,
            };
        }

        private static string MapAnimatorTriggerToCue(string trigger)
        {
            return trigger switch
            {
                CreatureAnimator.idleTrigger => "idle",
                CreatureAnimator.attackTrigger => "attack",
                CreatureAnimator.castTrigger => "cast",
                CreatureAnimator.hitTrigger => "hurt",
                CreatureAnimator.deathTrigger => "die",
                CreatureAnimator.reviveTrigger => "revive",
                "Stun" or "stun" => "stun",
                _ => trigger.ToLowerInvariant(),
            };
        }

        private static ReadOnlySpan<string> BuildDefaultAlternateNames(string primaryCue)
        {
            return primaryCue.ToLowerInvariant() switch
            {
                "die" => DieCueNames,
                "idle" => IdleCueNames,
                "relaxed" or "relaxed_loop" => RelaxedCueNames,
                "overgrowth_loop" or "hive_loop" or "glory_loop" => RestSiteLoopFallback(primaryCue),
                "hurt" => HurtCueNames,
                "attack" => AttackCueNames,
                "cast" => CastCueNames,
                "revive" => ReviveCueNames,
                "stun" => StunCueNames,
                _ => TwoNameFallback(primaryCue),
            };
        }

        private static string[] TwoNameFallback(string primaryCue)
        {
            var lower = primaryCue.ToLowerInvariant();
            return string.Equals(primaryCue, lower, StringComparison.Ordinal)
                ? [primaryCue]
                : [primaryCue, lower];
        }

        private static string[] RestSiteLoopFallback(string primaryCue)
        {
            return [primaryCue, .. RestSiteLoopCueNames];
        }

        private static bool TryApplyVisualCues(Node visualsRoot, CharacterModel? character,
            ReadOnlySpan<string> names, VisualCueSet? cueOverride)
        {
            var cues = cueOverride;
            if (cues == null && character is IModCharacterAssetOverrides { VisualCues: { } cc })
                cues = cc;

            if (cues == null)
                return false;

            return TryApplyFrameSequences(visualsRoot, cues, names)
                   || TryApplySingleTextureCues(visualsRoot, cues, names);
        }

        private static bool TryApplyFrameSequences(Node visualsRoot, VisualCueSet cues,
            ReadOnlySpan<string> names)
        {
            var map = cues.FrameSequenceByCue;
            if (map == null || map.Count == 0)
                return false;

            var sprite = FindPrimarySprite2D(visualsRoot);
            if (sprite == null)
                return false;

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (!TryGetFrameSequence(map, name, out var sequence) || sequence == null)
                    continue;

                if (sequence.Frames.Count == 0)
                    continue;

                var player = CueFrameSequencePlayer.EnsureUnder(visualsRoot);
                return player.TryStart(sprite, sequence);
            }

            return false;
        }

        private static bool TryGetVisualCueDuration(CharacterModel? character, ReadOnlySpan<string> names,
            out float seconds, out bool matched)
        {
            seconds = 0f;
            matched = false;
            if (character is not IModCharacterAssetOverrides { VisualCues: { } cues })
                return false;

            if (cues.FrameSequenceByCue is { Count: > 0 } sequences)
                foreach (var name in names)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    if (!TryGetFrameSequence(sequences, name, out var sequence) || sequence == null)
                        continue;

                    seconds = GetSequenceDuration(sequence);
                    if (seconds <= 0f)
                        continue;

                    matched = true;
                    return true;
                }

            if (cues.TexturePathByCue is not { Count: > 0 } textures)
                return false;

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (!TryGetCuePath(textures, name, out var path) || string.IsNullOrWhiteSpace(path))
                    continue;

                matched = true;
                return false;
            }

            return false;
        }

        private static bool TryApplySingleTextureCues(Node visualsRoot,
            VisualCueSet cues, ReadOnlySpan<string> names)
        {
            var map = cues.TexturePathByCue;
            if (map == null || map.Count == 0)
                return false;

            var sprite = FindPrimarySprite2D(visualsRoot);
            if (sprite == null)
                return false;

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (!TryGetCuePath(map, name, out var path) || string.IsNullOrWhiteSpace(path))
                    continue;

                var tex = ResourceLoader.Load<Texture2D>(path);
                if (tex == null)
                    continue;

                sprite.Texture = tex;
                if (cues.TextureStyleByCue is { Count: > 0 } styles &&
                    TryGetCueStyle(styles, name, out var style))
                    style.ApplyTo(sprite);

                return true;
            }

            return false;
        }

        private static bool TryGetFrameSequence(IReadOnlyDictionary<string, VisualFrameSequence> map,
            string key, out VisualFrameSequence? sequence)
        {
            if (map.TryGetValue(key, out var found))
            {
                sequence = found;
                return true;
            }

            foreach (var kv in map)
            {
                if (!string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    continue;

                sequence = kv.Value;
                return true;
            }

            sequence = null;
            return false;
        }

        private static bool TryGetCuePath(IReadOnlyDictionary<string, string> map, string key, out string? path)
        {
            if (map.TryGetValue(key, out path) && !string.IsNullOrWhiteSpace(path))
                return true;

            foreach (var kv in map)
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    path = kv.Value;
                    return !string.IsNullOrWhiteSpace(path);
                }

            path = null;
            return false;
        }

        private static bool TryGetCueStyle(IReadOnlyDictionary<string, VisualNodeStyle> map, string key,
            out VisualNodeStyle? style)
        {
            if (map.TryGetValue(key, out var found))
            {
                style = found;
                return true;
            }

            foreach (var kv in map)
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    style = kv.Value;
                    return true;
                }

            style = null;
            return false;
        }

        private static Sprite2D? FindPrimarySprite2D(Node root)
        {
            var direct = root.GetNodeOrNull("%Visuals") ?? root.GetNodeOrNull("Visuals");
            if (direct is Sprite2D s)
                return s;

            if (root is Sprite2D rootSprite)
                return rootSprite;

            return SearchRecursive<Sprite2D>(root);
        }

        private static bool TryPlaySpine(NCreatureVisuals visuals, ReadOnlySpan<string> names, bool loop)
        {
            if (!visuals.HasSpineAnimation)
                return false;

            var spine = visuals.SpineBody;
            if (spine == null)
                return false;

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                try
                {
                    spine.GetAnimationState().SetAnimation(name, loop);
                    return true;
                }
                catch
                {
                    // Invalid track name or skeleton state; try next alias.
                }
            }

            return false;
        }

        private static bool TryPlayGodotAnimations(Node root, ReadOnlySpan<string> names)
        {
            if (GodotAnimHandlers.TryGetValue(root, out var handler)) return handler(NamesToArray(names));
            handler = BuildGodotAnimHandler(root);
            if (handler == null)
                return false;

            GodotAnimHandlers.Add(root, handler);

            return handler(NamesToArray(names));
        }

        private static bool TryGetGodotAnimationDuration(Node root, ReadOnlySpan<string> names, out float seconds)
        {
            seconds = 0f;
            var animationPlayer = FindNode<AnimationPlayer>(root) ?? SearchRecursive<AnimationPlayer>(root);
            if (animationPlayer != null && TryGetAnimationPlayerDuration(animationPlayer, names, out seconds))
                return true;

            var animatedSprite = FindNode<AnimatedSprite2D>(root) ?? SearchRecursive<AnimatedSprite2D>(root);
            return animatedSprite != null && TryGetAnimatedSpriteDuration(animatedSprite, names, out seconds);
        }

        private static string[] NamesToArray(ReadOnlySpan<string> names)
        {
            var arr = new string[names.Length];
            names.CopyTo(arr);
            return arr;
        }

        private static Func<string[], bool>? BuildGodotAnimHandler(Node root)
        {
            return FindNode<AnimationTree>(root)?.UseAnimationTree()
                   ?? SearchRecursive<AnimationTree>(root)?.UseAnimationTree()
                   ?? FindNode<AnimationPlayer>(root)?.UseAnimationPlayer()
                   ?? FindNode<AnimatedSprite2D>(root)?.UseAnimatedSprite2D()
                   ?? SearchRecursive<AnimationPlayer>(root)?.UseAnimationPlayer()
                   ?? SearchRecursive<AnimatedSprite2D>(root)?.UseAnimatedSprite2D();
        }

        private static Func<string[], bool>? UseAnimationTree(this AnimationTree animationTree)
        {
            if (animationTree.TreeRoot is not AnimationNodeStateMachine treeRoot)
                return null;

            var playback = animationTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
            if (playback == null)
                return null;

            return animNames =>
            {
                foreach (var name in animNames)
                {
                    if (string.IsNullOrWhiteSpace(name) || !treeRoot.HasNode(name))
                        continue;

                    animationTree.Active = true;
                    playback.Travel(name);
                    return true;
                }

                return false;
            };
        }

        private static Func<string[], bool> UseAnimatedSprite2D(this AnimatedSprite2D animSprite)
        {
            return animNames =>
            {
                foreach (var name in animNames)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    if (animSprite.SpriteFrames == null || !animSprite.SpriteFrames.HasAnimation(name)) continue;
                    animSprite.Play(name);
                    return true;
                }

                return false;
            };
        }

        private static Func<string[], bool> UseAnimationPlayer(this AnimationPlayer animPlayer)
        {
            return animNames =>
            {
                foreach (var name in animNames)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    if (!animPlayer.HasAnimation(name)) continue;
                    if (animPlayer.CurrentAnimation == name)
                        animPlayer.Stop();

                    animPlayer.Play(name);
                    return true;
                }

                return false;
            };
        }

        private static bool TryGetAnimationPlayerDuration(AnimationPlayer player, ReadOnlySpan<string> names,
            out float seconds)
        {
            seconds = 0f;
            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name) || !player.HasAnimation(name))
                    continue;

                var animation = player.GetAnimation(name);
                if (animation == null)
                    continue;

                seconds = ScaleDuration(animation.Length, player.SpeedScale);
                return seconds > 0f;
            }

            return false;
        }

        private static bool TryGetAnimatedSpriteDuration(AnimatedSprite2D sprite, ReadOnlySpan<string> names,
            out float seconds)
        {
            seconds = 0f;
            var frames = sprite.SpriteFrames;
            if (frames == null)
                return false;

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name) || !frames.HasAnimation(name))
                    continue;

                seconds = GetAnimatedSpriteDuration(sprite, frames, name);
                return seconds > 0f;
            }

            return false;
        }

        private static float GetSequenceDuration(VisualFrameSequence sequence)
        {
            return sequence.Frames.Select(frame => frame.DurationSeconds)
                .Select(seconds => !float.IsFinite(seconds) || seconds <= 0f ? 1f / 60f : seconds).Sum();
        }

        private static float GetAnimatedSpriteDuration(AnimatedSprite2D sprite, SpriteFrames frames, string name)
        {
            var speed = (float)Math.Abs(frames.GetAnimationSpeed(name) * sprite.SpeedScale);
            if (speed <= 0f)
                return 0f;

            var total = 0f;
            var frameCount = frames.GetFrameCount(name);
            for (var i = 0; i < frameCount; i++)
                total += frames.GetFrameDuration(name, i);

            return total / speed;
        }

        private static float ScaleDuration(float seconds, float speedScale)
        {
            if (!float.IsFinite(seconds) || seconds <= 0f)
                return 0f;

            var speed = Math.Abs(speedScale);
            return speed <= 0f ? seconds : seconds / speed;
        }

        private static T? FindNode<T>(Node root) where T : class
        {
            var typeName = typeof(T).Name;
            var n = root.GetNodeOrNull(typeName)
                    ?? root.GetNodeOrNull("Visuals/" + typeName)
                    ?? root.GetNodeOrNull("Body/" + typeName);
            return n as T;
        }

        private static T? SearchRecursive<T>(Node parent) where T : class
        {
            foreach (var child in parent.GetChildren())
            {
                if (child is T match)
                    return match;

                var found = SearchRecursive<T>(child);
                if (found != null)
                    return found;
            }

            return null;
        }

        private sealed class RitsuCreatureVisualRegistration;

        private sealed class FakeMerchantPlayerVisualSlot
        {
            internal readonly List<NMerchantCharacter> Visuals = [];
        }
    }
}
