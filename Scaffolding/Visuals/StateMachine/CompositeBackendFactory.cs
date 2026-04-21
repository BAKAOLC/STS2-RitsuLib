using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine
{
    /// <summary>
    ///     Helper that composes a <see cref="CompositeAnimationBackend" /> from the nodes found under a visuals
    ///     root, in priority order: cue frame sequences / static textures, Spine, Godot animation player,
    ///     Godot animated sprite.
    /// </summary>
    public static class CompositeBackendFactory
    {
        /// <summary>
        ///     Builds the composite backend. Returns the cue-only backend when no Godot / Spine nodes are found,
        ///     or a truly-empty (single backend) pass-through when cues are unavailable.
        /// </summary>
        /// <param name="visualsRoot">Root node under which backends are discovered.</param>
        /// <param name="character">
        ///     Optional character model used to pull <see cref="VisualCueSet" /> when
        ///     <paramref name="cueSet" /> is <see langword="null" />.
        /// </param>
        /// <param name="cueSet">Optional explicit cue set; takes priority over the character-derived one.</param>
        public static IAnimationBackend Build(Node visualsRoot, CharacterModel? character = null,
            VisualCueSet? cueSet = null)
        {
            ArgumentNullException.ThrowIfNull(visualsRoot);

            var resolvedCues = cueSet ?? TryGetCharacterCueSet(character);
            var sprite = FindPrimarySprite2D(visualsRoot);

            List<IAnimationBackend> backends = [];
            if (resolvedCues != null && sprite != null)
                backends.Add(new CueAnimationBackend(visualsRoot, sprite, resolvedCues));

            if (visualsRoot is NCreatureVisuals { HasSpineAnimation: true, SpineBody: { } spine })
                backends.Add(new SpineAnimationBackend(spine));

            var animationPlayer =
                FindNode<AnimationPlayer>(visualsRoot) ?? SearchRecursive<AnimationPlayer>(visualsRoot);
            if (animationPlayer != null)
                backends.Add(new GodotAnimationPlayerBackend(animationPlayer));

            var animatedSprite = FindNode<AnimatedSprite2D>(visualsRoot) ??
                                 SearchRecursive<AnimatedSprite2D>(visualsRoot);
            if (animatedSprite != null)
                backends.Add(new AnimatedSprite2DBackend(animatedSprite));

            if (backends.Count == 0)
                throw new InvalidOperationException(
                    $"No animation backend could be built for '{visualsRoot.Name}' (no cues, Spine, AnimationPlayer or AnimatedSprite2D).");

            return backends.Count == 1 ? backends[0] : new CompositeAnimationBackend(backends, visualsRoot);
        }

        private static VisualCueSet? TryGetCharacterCueSet(CharacterModel? character)
        {
            return character is not IModCharacterAssetOverrides overrides
                ? null
                : overrides.VisualCues ?? overrides.WorldProceduralVisuals?.Merchant?.CueSet;
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
