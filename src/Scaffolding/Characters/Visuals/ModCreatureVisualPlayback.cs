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
    ///     Central playback for mod creature visuals: optional per-cue textures and data-only frame sequences from
    ///     <see cref="IModCharacterAssetOverrides.VisualCues" />, Spine tracks when present, otherwise Godot
    ///     <see cref="AnimationPlayer" /> / <see cref="AnimatedSprite2D" /> under the visuals subtree. Procedural roots from
    ///     <see cref="RitsuGodotNodeFactories" /> (explicit <c>CreateFromResource</c> / <c>CreateFromScenePath</c>, e.g.
    ///     <c>Texture2D</c> → <see cref="NCreatureVisuals" />) use the same cue names and trigger mapping as patched
    ///     <see cref="NCreature.SetAnimationTrigger" />.
    ///     Mod 生物视觉的集中播放入口：优先使用 <see cref="IModCharacterAssetOverrides.VisualCues" /> 中可选的逐 cue 贴图和
    ///     纯数据帧序列；存在 Spine 时使用 Spine track；否则使用视觉子树下的 Godot <see cref="AnimationPlayer" />
    ///     <see cref="AnimatedSprite2D" />。来自 <see cref="RitsuGodotNodeFactories" /> 的程序化根节点（显式
    ///     <c>CreateFromResource</c> / <c>CreateFromScenePath</c>，例如 <c>Texture2D</c> →
    ///     <see cref="NCreatureVisuals" />）使用与已 patch 的 <see cref="NCreature.SetAnimationTrigger" /> 相同的 cue
    ///     名称和 trigger 映射。
    /// </summary>
    public static class ModCreatureVisualPlayback
    {
        private static readonly ConditionalWeakTable<Node, Func<string[], bool>> GodotAnimHandlers = new();

        private static readonly AccessTools.FieldRef<NMerchantRoom, List<Player>> MerchantRoomPlayersRef =
            AccessTools.FieldRefAccess<NMerchantRoom, List<Player>>("_players");

        private static readonly ConditionalWeakTable<NFakeMerchant, FakeMerchantPlayerVisualSlot>
            FakeMerchantVisualSlots =
                new();

        private static readonly ConditionalWeakTable<NMerchantCharacter, CharacterModel> FakeMerchantBoothCharacter =
            new();

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

        /// <summary>
        ///     Attempts to play a logical cue (idle, die, hurt, …) on combat-style <see cref="NCreatureVisuals" />.
        ///     尝试在战斗风格 <see cref="NCreatureVisuals" /> 上播放逻辑 cue（idle、die、hurt、…）。
        /// </summary>
        /// <param name="visuals">
        ///     Creature visuals root.
        ///     生物视觉根节点。
        /// </param>
        /// <param name="character">
        ///     Owning character model, for cue texture lookup.
        ///     拥有者角色模型，用于 cue 贴图查找。
        /// </param>
        /// <param name="primaryCue">
        ///     Primary name (e.g. <c>die</c>).
        ///     主名称（例如 <c>die</c>）。
        /// </param>
        /// <param name="alternateCueNames">
        ///     Extra names to try for textures / Spine / Godot nodes.
        ///     为贴图 / Spine / Godot 节点额外尝试的名称。
        /// </param>
        /// <param name="loop">
        ///     Spine loop flag when a Spine body is used.
        ///     使用 Spine body 时的 Spine 循环标记。
        /// </param>
        /// <returns>
        ///     <see langword="true" /> when some layer applied the cue.
        ///     当某一层成功应用 cue 时返回 <see langword="true" />。
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
        ///     Plays a merchant-room style animation or static cue on an arbitrary root (typically
        ///     <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantCharacter" />).
        ///     在任意根节点上播放商人房间风格动画或静态 cue（通常是
        /// </summary>
        /// <param name="root">
        ///     Visual root (merchant, rest-site character, …).
        ///     视觉根节点（商人、休息点角色、…）。
        /// </param>
        /// <param name="character">
        ///     Owner for cue lookup.
        ///     用于 cue 查找的拥有者。
        /// </param>
        /// <param name="animName">
        ///     Logical animation / cue name.
        ///     逻辑动画 / cue 名称。
        /// </param>
        /// <param name="loop">
        ///     Loop hint for Spine (non-Spine paths ignore where not applicable).
        ///     Spine 循环提示（非 Spine 路径在不适用时忽略）。
        /// </param>
        /// <param name="cueSetOverride">
        ///     When set (e.g. <see cref="IModCharacterAssetOverrides.WorldProceduralVisuals" /> merchant / rest cues),
        ///     used instead of <see cref="IModCharacterAssetOverrides.VisualCues" /> for texture / frame lookup.
        ///     设置时（例如 <see cref="IModCharacterAssetOverrides.WorldProceduralVisuals" /> 的商人 / 休息点 cue），
        ///     用它替代 <see cref="IModCharacterAssetOverrides.VisualCues" /> 进行贴图 / 帧查找。
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
        ///     When the creature has no Spine animator, plays the mapped cue on <see cref="NCreature.Visuals" />.
        ///     当生物没有 Spine animator 时，在 <see cref="NCreature.Visuals" /> 上播放映射后的 cue。
        /// </summary>
        /// <returns>
        ///     <see langword="false" /> when Spine is active (caller should run vanilla).
        ///     Spine 激活时返回 <see langword="false" />（调用方应继续运行原版逻辑）。
        /// </returns>
        public static bool TryPlayFromCreatureAnimatorTrigger(NCreature creature, string trigger)
        {
            if (creature.HasSpineAnimation)
                return false;

            var primary = MapAnimatorTriggerToCue(trigger);
            var character = creature.Entity?.Player?.Character;
            return TryPlayCue(creature.Visuals, character, primary);
        }

        internal static bool TryGetDurationFromCreatureAnimatorTrigger(NCreature creature, string trigger,
            out float seconds)
        {
            seconds = 0f;
            if (creature.HasSpineAnimation || !GodotObject.IsInstanceValid(creature.Visuals))
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
        ///     Resolves the owning <see cref="CharacterModel" /> for a merchant-booth <see cref="NMerchantCharacter" />
        ///     in either <see cref="NMerchantRoom" /> or the fake-merchant event screen.
        ///     为 <see cref="CharacterModel" /> 或 fake-merchant 事件界面中的商人摊位 <see cref="NMerchantCharacter" />
        ///     解析所属 <see cref="NMerchantRoom" />。
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
                FakeMerchantBoothCharacter.Add(ordered[i], players[i].Character);
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
            return [primaryCue, ..RestSiteLoopCueNames];
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

                    matched = true;
                    seconds = GetSequenceDuration(sequence);
                    return seconds > 0f;
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
            return FindNode<AnimationPlayer>(root)?.UseAnimationPlayer()
                   ?? FindNode<AnimatedSprite2D>(root)?.UseAnimatedSprite2D()
                   ?? SearchRecursive<AnimationPlayer>(root)?.UseAnimationPlayer()
                   ?? SearchRecursive<AnimatedSprite2D>(root)?.UseAnimatedSprite2D();
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

        private sealed class FakeMerchantPlayerVisualSlot
        {
            internal readonly List<NMerchantCharacter> Visuals = [];
        }
    }
}
