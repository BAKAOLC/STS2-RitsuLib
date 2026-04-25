using Godot;
using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Diagnostics
{
    internal sealed partial class ModDataCloudProgressOverlay : CanvasLayer
    {
        private readonly Label? _countLabel;
        private readonly Label? _pathLabel;
        private readonly ProgressBar? _progressBar;
        private readonly Label? _titleLabel;

        private ModDataCloudProgressOverlay(int totalSteps, string title)
        {
            Layer = 128;
            Name = "RitsuModDataCloudProgress";

            var dim = new ColorRect
            {
                Color = new(0f, 0f, 0f, 0.55f),
                MouseFilter = Control.MouseFilterEnum.Stop,
            };
            dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            AddChild(dim);

            var center = new CenterContainer
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            AddChild(center);

            var panel = new PanelContainer
            {
                CustomMinimumSize = new(440f, 0f),
            };
            center.AddChild(panel);
            panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());

            var v = new VBoxContainer();
            v.AddThemeConstantOverride("separation", 12);
            panel.AddChild(v);

            _titleLabel = new()
            {
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Text = title,
            };
            _titleLabel.AddThemeFontSizeOverride("font_size", 22);
            v.AddChild(_titleLabel);

            _progressBar = new()
            {
                MinValue = 0,
                MaxValue = Math.Max(1, totalSteps),
                Value = 0,
                ShowPercentage = false,
                CustomMinimumSize = new(420f, 28f),
            };
            v.AddChild(_progressBar);

            var detailCol = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            detailCol.AddThemeConstantOverride("separation", 6);
            v.AddChild(detailCol);

            _countLabel = new()
            {
                AutowrapMode = TextServer.AutowrapMode.Off,
                Text = string.Empty,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            _countLabel.AddThemeFontSizeOverride("font_size", 16);
            _countLabel.AddThemeColorOverride("font_color", new(0.85f, 0.88f, 0.92f));
            detailCol.AddChild(_countLabel);

            _pathLabel = new()
            {
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Text = string.Empty,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            _pathLabel.AddThemeFontSizeOverride("font_size", 14);
            _pathLabel.AddThemeColorOverride("font_color", new(0.72f, 0.78f, 0.86f));
            detailCol.AddChild(_pathLabel);
        }

        public ModDataCloudProgressOverlay()
        {
        }

        public static ModDataCloudProgressOverlay Attach(Node root, int totalSteps, string title)
        {
            var overlay = new ModDataCloudProgressOverlay(totalSteps, title);
            root.AddChild(overlay);
            return overlay;
        }

        public void SetProgress(int completedSteps, int totalSteps, string? currentRelativePath)
        {
            if (_progressBar != null)
            {
                _progressBar.MaxValue = Math.Max(1, totalSteps);
                _progressBar.Value = Math.Clamp(completedSteps, 0, (int)_progressBar.MaxValue);
            }

            var total = Math.Max(1, totalSteps);
            var countFmt = ModSettingsLocalization.Get("ritsulib.modCloud.progress.count", "{0} / {1}");
            if (_countLabel != null)
                _countLabel.Text = string.Format(countFmt, Math.Min(completedSteps, total), total);

            if (_pathLabel == null) return;
            var id = string.IsNullOrWhiteSpace(currentRelativePath) ? "…" : currentRelativePath;
            _pathLabel.Text = id;
        }

        public void Detach()
        {
            if (IsInstanceValid(this))
                QueueFree();
        }

        private static StyleBoxFlat CreatePanelStyle()
        {
            return new()
            {
                BgColor = new(0.12f, 0.15f, 0.19f, 0.97f),
                BorderColor = new(0.45f, 0.58f, 0.68f, 0.65f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomRight = 8,
                CornerRadiusBottomLeft = 8,
                ContentMarginLeft = 20,
                ContentMarginTop = 18,
                ContentMarginRight = 20,
                ContentMarginBottom = 18,
            };
        }
    }
}
