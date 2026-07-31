using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Builds animation backends from nodes beneath a visuals root, in priority order: cue frames or static textures,
    ///         Spine, a Godot animation-tree state machine, a Godot animation player, and a Godot animated sprite.
    ///         Multiple discovered backends are combined into <see cref="CompositeAnimationBackend" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         按优先级从视觉效果根节点下构建动画后端：视觉提示帧或静态纹理、Spine、Godot 动画树状态机、
    ///         Godot 动画播放器和 Godot 动画精灵。发现多个后端时，会将其组合为
    ///         <see cref="CompositeAnimationBackend" />。
    ///     </para>
    /// </summary>
    public static class CompositeBackendFactory
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds the available backends, returning the sole backend directly or a composite when multiple backends
        ///         are found. Throws when no supported backend is available.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         构建可用的动画后端；仅发现一个时直接返回该后端，发现多个时返回组合后端。
        ///         没有任何受支持的后端时抛出异常。
        ///     </para>
        /// </summary>
        /// <param name="visualsRoot">
        ///     <para xml:lang="en">The root beneath which backends are discovered.</para>
        ///     <para xml:lang="zh-CN">用于发现动画后端的根节点。</para>
        /// </param>
        /// <param name="character">
        ///     <para xml:lang="en">
        ///         An optional character model from which to obtain a <see cref="VisualCueSet" /> when
        ///         <paramref name="cueSet" /> is <see langword="null" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的角色模型；当 <paramref name="cueSet" /> 为 <see langword="null" /> 时从中获取
        ///         <see cref="VisualCueSet" />。
        ///     </para>
        /// </param>
        /// <param name="cueSet">
        ///     <para xml:lang="en">An optional explicit cue set, which takes precedence over character-derived cues.</para>
        ///     <para xml:lang="zh-CN">可选的显式视觉提示集，优先于从角色模型取得的视觉提示。</para>
        /// </param>
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

            var animationTree =
                FindNode<AnimationTree>(visualsRoot) ?? SearchRecursive<AnimationTree>(visualsRoot);
            if (animationTree is { TreeRoot: AnimationNodeStateMachine })
                backends.Add(new AnimationTreeStateMachineBackend(animationTree));

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
                    $"No animation backend could be built for '{visualsRoot.Name}' (no cues, Spine, AnimationTree, AnimationPlayer or AnimatedSprite2D).");

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
