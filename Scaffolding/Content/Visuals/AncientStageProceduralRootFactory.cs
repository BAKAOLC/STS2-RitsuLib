using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens;
using STS2RitsuLib.Scaffolding.Characters.Visuals;

namespace STS2RitsuLib.Scaffolding.Content.Visuals
{
    /// <summary>
    ///     Builds the runtime <see cref="Control" /> tree for an <see cref="AncientEventStageProceduralVisualSet" />:
    ///     an optional looping <see cref="VideoStreamPlayer" /> background and cue-driven <see cref="Sprite2D" /> layers.
    ///     为 <c>AncientEventStageProceduralVisualSet</c> 构建运行时 <c>Control</c> 节点树：
    ///     可包含循环播放的视频背景，以及由 cue 驱动的 <c>Sprite2D</c> 图层。
    /// </summary>
    public static class AncientStageProceduralRootFactory
    {
        private static PackedScene? _placeholderBackgroundPackedScene;

        /// <summary>
        ///     Empty scene used as a placeholder so <c>EventModel.CreateBackgroundScene</c> can complete before the
        ///     layout patch mounts the procedural layers.
        ///     空场景占位符；让 <c>EventModel.CreateBackgroundScene</c> 先正常返回，随后由布局补丁挂载程序化图层。
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
        ///     创建程序化图层根节点，将其挂到 <c>host</c> 下，并启动已配置的背景 / 前景播放。
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
                RitsuLibFramework.Logger.Error(
                    "[AncientStage] StageProcedural has neither BackgroundVideoPath nor BackgroundCueSet.");

            Control? fgLayer = null;
            if (stage.ForegroundCueSet != null)
            {
                fgLayer = CreateSpriteLayer("RitsuAncientStageFg");
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
                RitsuLibFramework.Logger.Error($"[AncientStage] Background video not found: '{path}'");
                outer.AddChild(video);
                return;
            }

            var stream = ResourceLoader.Load<VideoStream>(path);
            if (stream == null)
            {
                RitsuLibFramework.Logger.Error($"[AncientStage] Could not load VideoStream: '{path}'");
                outer.AddChild(video);
                return;
            }

            video.Stream = stream;
            video.Autoplay = true;
            outer.AddChild(video);
        }

        private static void MountBackgroundCues(Control outer, AncientEventStageProceduralVisualSet stage)
        {
            var bgLayer = CreateSpriteLayer("RitsuAncientStageBg");
            outer.AddChild(bgLayer);

            var bgCue = string.IsNullOrWhiteSpace(stage.BackgroundLoopCueName) ? "loop" : stage.BackgroundLoopCueName!;
            ModCreatureVisualPlayback.TryPlayOnVisualRoot(bgLayer, null, bgCue, true, stage.BackgroundCueSet);
        }

        private static Control CreateSpriteLayer(string layerName)
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

            layer.Resized += () => CenterSprite(layer, sprite);
            Callable.From(() => CenterSprite(layer, sprite)).CallDeferred();

            return layer;
        }

        private static void CenterSprite(Control layer, Sprite2D sprite)
        {
            if (!GodotObject.IsInstanceValid(layer) || !GodotObject.IsInstanceValid(sprite))
                return;

            sprite.Position = layer.Size * 0.5f;
        }
    }
}
