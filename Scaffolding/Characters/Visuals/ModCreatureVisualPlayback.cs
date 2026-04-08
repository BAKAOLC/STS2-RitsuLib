using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
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
    /// </summary>
    public static class ModCreatureVisualPlayback
    {
        private static readonly ConditionalWeakTable<Node, Func<string[], bool>> GodotAnimHandlers = new();

        private static readonly AccessTools.FieldRef<NMerchantRoom, List<Player>> MerchantRoomPlayersRef =
            AccessTools.FieldRefAccess<NMerchantRoom, List<Player>>("_players");

        private static readonly string[] DieCueNames = ["die", "death", "dead", "Dead"];

        private static readonly string[] IdleCueNames = ["idle", "Idle", "relaxed_loop"];

        private static readonly string[] HurtCueNames = ["hurt", "hit", "Hit"];

        private static readonly string[] AttackCueNames = ["attack", "Attack"];

        private static readonly string[] CastCueNames = ["cast", "Cast"];

        private static readonly string[] ReviveCueNames = ["revive", "Revive"];

        /// <summary>
        ///     Attempts to play a logical cue (idle, die, hurt, …) on combat-style <see cref="NCreatureVisuals" />.
        /// </summary>
        /// <param name="visuals">Creature visuals root.</param>
        /// <param name="character">Owning character model, for cue texture lookup.</param>
        /// <param name="primaryCue">Primary name (e.g. <c>die</c>).</param>
        /// <param name="alternateCueNames">Extra names to try for textures / Spine / Godot nodes.</param>
        /// <param name="loop">Spine loop flag when a Spine body is used.</param>
        /// <returns><see langword="true" /> when some layer applied the cue.</returns>
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
        /// </summary>
        /// <param name="root">Visual root (merchant, rest-site character, …).</param>
        /// <param name="character">Owner for cue lookup.</param>
        /// <param name="animName">Logical animation / cue name.</param>
        /// <param name="loop">Loop hint for Spine (non-Spine paths ignore where not applicable).</param>
        /// <param name="cueSetOverride">
        ///     When set (e.g. <see cref="IModCharacterAssetOverrides.WorldProceduralVisuals" /> merchant / rest cues),
        ///     used instead of <see cref="IModCharacterAssetOverrides.VisualCues" /> for texture / frame lookup.
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
        /// </summary>
        /// <returns><see langword="false" /> when Spine is active (caller should run vanilla).</returns>
        public static bool TryPlayFromCreatureAnimatorTrigger(NCreature creature, string trigger)
        {
            if (creature.HasSpineAnimation)
                return false;

            var primary = MapAnimatorTriggerToCue(trigger);
            var character = creature.Entity?.Player?.Character;
            return TryPlayCue(creature.Visuals, character, primary);
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
                _ => trigger.ToLowerInvariant(),
            };
        }

        private static ReadOnlySpan<string> BuildDefaultAlternateNames(string primaryCue)
        {
            return primaryCue.ToLowerInvariant() switch
            {
                "die" => DieCueNames,
                "idle" => IdleCueNames,
                "hurt" => HurtCueNames,
                "attack" => AttackCueNames,
                "cast" => CastCueNames,
                "revive" => ReviveCueNames,
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

        private static bool TryApplyVisualCues(Node visualsRoot, CharacterModel? character,
            ReadOnlySpan<string> names, VisualCueSet? cueOverride)
        {
            var cues = cueOverride;
            if (cues == null && character is IModCharacterAssetOverrides { VisualCues: { } cc })
                cues = cc;

            if (cues == null)
                return false;

            return TryApplyFrameSequences(visualsRoot, cues, names)
                   || TryApplySingleTextureCues(visualsRoot, cues.TexturePathByCue, names);
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

        private static bool TryApplySingleTextureCues(Node visualsRoot,
            IReadOnlyDictionary<string, string>? map, ReadOnlySpan<string> names)
        {
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
    }
}
