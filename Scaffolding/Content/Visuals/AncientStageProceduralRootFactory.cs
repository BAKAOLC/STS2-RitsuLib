using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens;
using STS2RitsuLib.Scaffolding.Characters.Visuals;

namespace STS2RitsuLib.Scaffolding.Content.Visuals
{
    /// <summary>
    ///     Builds a minimal Control tree for <see cref="AncientEventStageProceduralVisualSet" />: optional
    ///     <see cref="VideoStreamPlayer" /> and/or cue-driven <see cref="Sprite2D" /> layers.
    /// </summary>
    public static class AncientStageProceduralRootFactory
    {
        private static PackedScene? _placeholderBackgroundPackedScene;

        /// <summary>
        ///     Empty control packed once so <c>EventModel.CreateBackgroundScene</c> can succeed when only procedural layers
        ///     are used (replaced in layout postfix).
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
        ///     Creates the layered root, parents it under <paramref name="host" />, and starts background / foreground
        ///     playback.
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
                fgLayer = CreateSpriteLayer("RitsuAncientStageFg", outer);
                outer.AddChild(fgLayer);
                fgLayer.Owner = outer;
            }

            host.AddChildSafely(outer);
            outer.Owner = host;

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
                video.Owner = outer;
                return;
            }

            var stream = ResourceLoader.Load<VideoStream>(path);
            if (stream == null)
            {
                RitsuLibFramework.Logger.Error($"[AncientStage] Could not load VideoStream: '{path}'");
                outer.AddChild(video);
                video.Owner = outer;
                return;
            }

            video.Stream = stream;
            video.Autoplay = true;
            outer.AddChild(video);
            video.Owner = outer;
        }

        private static void MountBackgroundCues(Control outer, AncientEventStageProceduralVisualSet stage)
        {
            var bgLayer = CreateSpriteLayer("RitsuAncientStageBg", outer);
            outer.AddChild(bgLayer);
            bgLayer.Owner = outer;

            var bgCue = string.IsNullOrWhiteSpace(stage.BackgroundLoopCueName) ? "loop" : stage.BackgroundLoopCueName!;
            ModCreatureVisualPlayback.TryPlayOnVisualRoot(bgLayer, null, bgCue, true, stage.BackgroundCueSet);
        }

        private static Control CreateSpriteLayer(string layerName, Control outer)
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
            sprite.Owner = outer;

            layer.Resized += () => sprite.Position = layer.Size * 0.5f;
            Callable.From(() => sprite.Position = layer.Size * 0.5f).CallDeferred();

            return layer;
        }
    }
}
