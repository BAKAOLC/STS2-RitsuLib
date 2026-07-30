using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Content.Visuals
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Builds the runtime <see cref="Control" /> tree for an <see cref="AncientEventStageProceduralVisualSet" />,
    ///         including an optional looping <see cref="VideoStreamPlayer" /> background and cue-driven
    ///         <see cref="Sprite2D" /> layers.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="AncientEventStageProceduralVisualSet" /> 构建运行时 <see cref="Control" /> 节点树，
    ///         其中可包含循环播放的 <see cref="VideoStreamPlayer" /> 背景和由视觉提示驱动的 <see cref="Sprite2D" /> 图层。
    ///     </para>
    /// </summary>
    public static class AncientStageProceduralRootFactory
    {
        private static PackedScene? _placeholderBackgroundPackedScene;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the empty placeholder scene that allows <c>EventModel.CreateBackgroundScene</c> to complete before
        ///         the layout patch mounts the procedural layers.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取空白占位场景，使 <c>EventModel.CreateBackgroundScene</c> 能在布局补丁挂载程序化图层之前完成。
        ///     </para>
        /// </summary>
        public static PackedScene PlaceholderBackgroundPackedScene
        {
            get
            {
                if (_placeholderBackgroundPackedScene != null &&
                    GodotObject.IsInstanceValid(_placeholderBackgroundPackedScene))
                    return _placeholderBackgroundPackedScene;

                var placeholder = new Control { Name = "RitsuAncientStagePlaceholder" };
                var packedScene = new PackedScene();
                var error = packedScene.Pack(placeholder);
                placeholder.Free();
                if (error != Error.Ok)
                {
                    packedScene.Dispose();
                    throw new InvalidOperationException(
                        $"Could not pack the Ancient-stage placeholder scene: {error}.");
                }

                _placeholderBackgroundPackedScene = packedScene;
                return packedScene;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates the procedural layer root, attaches it to <paramref name="host" />, and starts configured playback.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建程序化图层的根节点，将其附加到 <paramref name="host" />，并按配置开始播放。
        ///     </para>
        /// </summary>
        public static Control BuildAndMount(NAncientBgContainer host, AncientEventStageProceduralVisualSet stage)
        {
            ArgumentNullException.ThrowIfNull(host);
            ArgumentNullException.ThrowIfNull(stage);
            // Preserve the public-facing object name in the exception.
#pragma warning disable CA1513
            if (!GodotObject.IsInstanceValid(host))
                throw new ObjectDisposedException(nameof(host));
#pragma warning restore CA1513

            var outer = new Control { Name = "RitsuAncientStageProcedural" };
            try
            {
                outer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                outer.OffsetLeft = 0;
                outer.OffsetTop = 0;
                outer.OffsetRight = 0;
                outer.OffsetBottom = 0;
                outer.MouseFilter = Control.MouseFilterEnum.Ignore;

                if (!string.IsNullOrWhiteSpace(stage.BackgroundVideoPath))
                    MountBackgroundVideo(outer, stage.BackgroundVideoPath.Trim());
                else if (stage.BackgroundCueSet != null)
                    MountBackgroundCues(outer, stage);
                else
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        "[AncientStage] StageProcedural has neither BackgroundVideoPath nor BackgroundCueSet.");

                Control? fgLayer = null;
                if (stage.ForegroundCueSet != null)
                {
                    fgLayer = CreateSpriteLayer("RitsuAncientStageFg", stage.ForegroundLayerStyle);
                    outer.AddChild(fgLayer);
                }

                host.AddChildSafely(outer);

                if (stage.ForegroundCueSet == null || fgLayer == null)
                    return outer;

                var fgCue = string.IsNullOrWhiteSpace(stage.ForegroundLoopCueName)
                    ? "loop"
                    : stage.ForegroundLoopCueName!;
                ModCreatureVisualPlayback.TryPlayOnVisualRoot(fgLayer, null, fgCue, true, stage.ForegroundCueSet);

                return outer;
            }
            catch
            {
                // Keep cleanup conditional while preserving a single rethrow point.
                // ReSharper disable once InvertIf
                if (GodotObject.IsInstanceValid(outer))
                {
                    if (outer.GetParent() is { } parent && GodotObject.IsInstanceValid(parent))
                        parent.RemoveChildSafely(outer);
                    outer.QueueFreeSafely();
                }

                throw;
            }
        }

        private static void MountBackgroundVideo(Control outer, string path)
        {
            var video = new VideoStreamPlayer { Name = "RitsuAncientStageBgVideo" };
            video.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            video.OffsetLeft = 0;
            video.OffsetTop = 0;
            video.OffsetRight = 0;
            video.OffsetBottom = 0;
            video.MouseFilter = Control.MouseFilterEnum.Ignore;
            video.Expand = true;
            video.Loop = true;

            if (!ResourceLoader.Exists(path))
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[AncientStage] Background video not found: '{path}'");
                outer.AddChild(video);
                return;
            }

            var stream = ResourceLoader.Load<VideoStream>(path);
            if (stream == null || !GodotObject.IsInstanceValid(stream))
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[AncientStage] Could not load VideoStream: '{path}'");
                outer.AddChild(video);
                return;
            }

            video.Stream = stream;
            video.Autoplay = true;
            outer.AddChild(video);
        }

        private static void MountBackgroundCues(Control outer, AncientEventStageProceduralVisualSet stage)
        {
            var bgLayer = CreateSpriteLayer("RitsuAncientStageBg", stage.BackgroundLayerStyle);
            outer.AddChild(bgLayer);

            var bgCue = string.IsNullOrWhiteSpace(stage.BackgroundLoopCueName) ? "loop" : stage.BackgroundLoopCueName!;
            ModCreatureVisualPlayback.TryPlayOnVisualRoot(bgLayer, null, bgCue, true, stage.BackgroundCueSet);
        }

        private static Control CreateSpriteLayer(string layerName, VisualNodeStyle? style = null)
        {
            var layer = new Control { Name = layerName };
            layer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            layer.OffsetLeft = 0;
            layer.OffsetTop = 0;
            layer.OffsetRight = 0;
            layer.OffsetBottom = 0;
            layer.MouseFilter = Control.MouseFilterEnum.Ignore;

            var sprite = new Sprite2D { Name = "Visuals", Centered = true };
            layer.AddChild(sprite);

            layer.Resized += () => CenterSprite(layer, sprite, style);
            Callable.From(() => CenterSprite(layer, sprite, style)).CallDeferred();

            return layer;
        }

        private static void CenterSprite(Control layer, Sprite2D sprite, VisualNodeStyle? style)
        {
            if (!GodotObject.IsInstanceValid(layer) || !GodotObject.IsInstanceValid(sprite))
                return;

            var center = layer.Size * 0.5f;
            if (style == null)
                sprite.Position = center;
            else
                style.ApplyTo(sprite, center);
        }
    }
}
