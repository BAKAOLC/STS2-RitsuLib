using Godot;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Diagnostics.CompendiumExport
{
    internal sealed partial class CompendiumPngExportProgressOverlay : CanvasLayer
    {
        private readonly Label? _countLabel;
        private readonly Label? _nameLabel;
        private readonly ProgressBar? _progressBar;
        private readonly Label? _titleLabel;

        private CompendiumPngExportProgressOverlay(int totalSteps, string title)
        {
            Layer = 128;
            Name = "RitsuCompendiumPngExportProgress";

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
            _titleLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.OverlayTitle);
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

            var stopRow = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                Alignment = BoxContainer.AlignmentMode.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            stopRow.AddChild(new ModSettingsTextButton(
                ModSettingsLocalization.Get("ritsulib.compendiumPngExport.stop.button", "Stop export"),
                ModSettingsButtonTone.Normal,
                CompendiumPngExportSession.RequestStop));
            v.AddChild(stopRow);

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
            _countLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.OverlayBody);
            _countLabel.AddThemeColorOverride("font_color", new(0.85f, 0.88f, 0.92f));
            detailCol.AddChild(_countLabel);

            _nameLabel = new()
            {
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Text = string.Empty,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            _nameLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.OverlayBody);
            _nameLabel.AddThemeColorOverride("font_color", new(0.72f, 0.78f, 0.86f));
            detailCol.AddChild(_nameLabel);
        }

        public CompendiumPngExportProgressOverlay()
        {
        }

        public static CompendiumPngExportProgressOverlay Attach(Node root, int totalSteps, string title)
        {
            var overlay = new CompendiumPngExportProgressOverlay(totalSteps, title);
            root.AddChild(overlay);
            return overlay;
        }

        public void SetProgress(int completedSteps, string? currentId)
        {
            if (_progressBar == null) return;
            _progressBar.Value = completedSteps;
            var total = (int)_progressBar.MaxValue;
            var id = string.IsNullOrWhiteSpace(currentId) ? "…" : currentId;
            var countFmt = ModSettingsLocalization.Get("ritsulib.compendiumPngExport.progress.count",
                "{0} / {1}");
            if (_countLabel != null)
                _countLabel.Text = string.Format(countFmt, completedSteps, total);
            if (_nameLabel != null)
                _nameLabel.Text = id;
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
                BgColor = RitsuShellTheme.Current.Component.OverlayPanel.Bg,
                BorderColor = RitsuShellTheme.Current.Component.OverlayPanel.Border,
                BorderWidthLeft = RitsuShellTheme.Current.Metric.BorderWidth.Overlay,
                BorderWidthTop = RitsuShellTheme.Current.Metric.BorderWidth.Overlay,
                BorderWidthRight = RitsuShellTheme.Current.Metric.BorderWidth.Overlay,
                BorderWidthBottom = RitsuShellTheme.Current.Metric.BorderWidth.Overlay,
                CornerRadiusTopLeft = RitsuShellTheme.Current.Metric.Radius.Overlay,
                CornerRadiusTopRight = RitsuShellTheme.Current.Metric.Radius.Overlay,
                CornerRadiusBottomRight = RitsuShellTheme.Current.Metric.Radius.Overlay,
                CornerRadiusBottomLeft = RitsuShellTheme.Current.Metric.Radius.Overlay,
                ContentMarginLeft = RitsuShellTheme.Current.Metric.Overlay.PaddingH,
                ContentMarginTop = RitsuShellTheme.Current.Metric.Overlay.PaddingV,
                ContentMarginRight = RitsuShellTheme.Current.Metric.Overlay.PaddingH,
                ContentMarginBottom = RitsuShellTheme.Current.Metric.Overlay.PaddingV,
            };
        }
    }
}
