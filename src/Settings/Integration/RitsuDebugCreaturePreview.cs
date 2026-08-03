using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    // ReSharper disable once Godot.MissingParameterlessConstructor
    internal sealed partial class RitsuDebugCreaturePreview : PanelContainer
    {
        private const int MaximumVisualCount = 3;
        private readonly MonsterModel[] _monsters;
        private readonly List<NCreatureVisuals> _visuals = [];
        private SubViewport _viewport = null!;
        private SubViewportContainer _viewportContainer = null!;

        internal RitsuDebugCreaturePreview(IEnumerable<MonsterModel> monsters)
        {
            ArgumentNullException.ThrowIfNull(monsters);
            _monsters =
            [
                .. monsters
                    .Where(static monster => monster != null)
                    .DistinctBy(static monster => monster.Id)
                    .Take(MaximumVisualCount),
            ];
        }

        public override void _Ready()
        {
            CustomMinimumSize = new(0f, 172f);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListShellStyle());

            var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            column.AddThemeConstantOverride("separation", 4);
            AddChild(column);
            _viewportContainer = new()
            {
                CustomMinimumSize = new(0f, 136f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Stretch = true,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            column.AddChild(_viewportContainer);
            _viewport = new()
            {
                Size = new(480, 136),
                TransparentBg = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            };
            _viewportContainer.AddChild(_viewport);

            var names = new Label
            {
                Text = _monsters.Length == 0
                    ? ModSettingsLocalization.Get("ritsulib.debugTools.previewUnavailable", "Preview unavailable")
                    : string.Join(" · ", _monsters.Select(ResolveTitle)),
                HorizontalAlignment = HorizontalAlignment.Center,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                TooltipText = string.Join('\n', _monsters.Select(monster => $"{ResolveTitle(monster)}\n{monster.Id}")),
                MouseFilter = MouseFilterEnum.Pass,
            };
            names.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            names.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
            names.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            column.AddChild(names);

            BuildVisuals();
            _viewportContainer.Resized += QueueVisualLayout;
            QueueVisualLayout();
        }

        public override void _ExitTree()
        {
            _visuals.Clear();
            base._ExitTree();
        }

        private void BuildVisuals()
        {
            foreach (var monster in _monsters)
                try
                {
                    var visuals = monster.CreateVisuals();
                    if (visuals == null || !IsInstanceValid(visuals))
                        continue;
                    _viewport.AddChild(visuals);
                    _visuals.Add(visuals);
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[DebugToolsUi] Could not create preview visuals for monster '{monster.Id}': {ex}");
                }
        }

        private void QueueVisualLayout()
        {
            if (!IsInsideTree())
                return;
            Callable.From(LayoutVisuals).CallDeferred();
        }

        private void LayoutVisuals()
        {
            if (!IsInstanceValid(_viewport) || !IsInstanceValid(_viewportContainer))
                return;
            var size = _viewportContainer.Size;
            if (size.X < 1f || size.Y < 1f)
                return;
            _viewport.Size = new(Math.Max(1, Mathf.RoundToInt(size.X)), Math.Max(1, Mathf.RoundToInt(size.Y)));
            if (_visuals.Count == 0)
                return;

            var spacing = size.X / (_visuals.Count + 1f);
            var scale = _visuals.Count switch
            {
                1 => 0.42f,
                2 => 0.34f,
                _ => 0.28f,
            };
            for (var index = 0; index < _visuals.Count; index++)
            {
                var visuals = _visuals[index];
                if (!IsInstanceValid(visuals))
                    continue;
                visuals.Scale = Vector2.One * scale;
                visuals.Position = new(spacing * (index + 1), size.Y * 0.76f);
            }
        }

        private static string ResolveTitle(MonsterModel monster)
        {
            try
            {
                var title = monster.Title?.GetFormattedText()?.Trim();
                return string.IsNullOrWhiteSpace(title) ? monster.Id.Entry : title;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugToolsUi] Could not resolve preview title for monster '{monster.Id}': {ex.Message}");
                return monster.Id.Entry;
            }
        }
    }
}
