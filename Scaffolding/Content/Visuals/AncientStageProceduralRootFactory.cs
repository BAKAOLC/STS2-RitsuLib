using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Content.Visuals
{
    /// <summary>
    ///     Builds the runtime <see cref="Control" /> tree for an <see cref="AncientEventStageProceduralVisualSet" />:
    ///     an optional looping <see cref="VideoStreamPlayer" /> background and cue-driven <see cref="Sprite2D" /> layers.
    ///     为 <see cref="AncientEventStageProceduralVisualSet" /> 构建运行时 <see cref="Control" /> 树：
    ///     可选的循环 <see cref="VideoStreamPlayer" /> 背景，以及由 cue 驱动的 <see cref="Sprite2D" /> 图层。
    /// </summary>
    public static class AncientStageProceduralRootFactory
    {
        private static PackedScene? _placeholderBackgroundPackedScene;

        /// <summary>
        ///     Empty scene used as a placeholder so <c>EventModel.CreateBackgroundScene</c> can complete before the
        ///     layout patch mounts the procedural layers.
        ///     空场景占位符，使 <c>EventModel.CreateBackgroundScene</c> 可以在
        ///     布局补丁挂载程序化图层之前完成。
        /// </summary>
        public static PackedScene PlaceholderBackgroundPackedScene
        {
            get
            {
                if (_placeholderBackgroundPackedScene != null)
                    return _placeholderBackgroundPackedScene;

                var placeholder = new Control { Name = "RitsuAncientStagePlaceholder" };
                _placeholderBackgroundPackedScene = new();
                _placeholderBackgroundPackedScene.Pack(placeholder);
                return _placeholderBackgroundPackedScene;
            }
        }

        /// <summary>
        ///     Creates the procedural layer root, attaches it to <paramref name="host" />, and starts configured playback.
        ///     创建程序化图层根节点，将其附加到 <paramref name="host" />，并启动已配置的播放。
        /// </summary>
        public static Control BuildAndMount(NAncientBgContainer host, AncientEventStageProceduralVisualSet stage)
        {
            ArgumentNullException.ThrowIfNull(host);
            ArgumentNullException.ThrowIfNull(stage);

            var outer = new Control { Name = "RitsuAncientStageProcedural" };
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

            var fgCue = string.IsNullOrWhiteSpace(stage.ForegroundLoopCueName) ? "loop" : stage.ForegroundLoopCueName!;
            ModCreatureVisualPlayback.TryPlayOnVisualRoot(fgLayer, null, fgCue, true, stage.ForegroundCueSet);

            return outer;
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
            if (stream == null)
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
