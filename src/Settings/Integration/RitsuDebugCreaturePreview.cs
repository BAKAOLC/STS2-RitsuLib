using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    // ReSharper disable once Godot.MissingParameterlessConstructor
    internal sealed partial class RitsuDebugCreaturePreview : PanelContainer
    {
        private const int MaximumVisualCount = 5;
        private const float ViewportHeight = 260f;
        private const float HorizontalPadding = 18f;
        private const float VerticalPadding = 14f;
        private const float CreatureSpacing = 12f;
        private const float MaximumSingleScale = 0.78f;
        private const float MaximumGroupScale = 0.58f;
        private readonly List<NCreature> _creatures = [];
        private readonly MonsterModel[] _monsters;
        private SubViewport _viewport = null!;
        private SubViewportContainer _viewportContainer = null!;

        internal RitsuDebugCreaturePreview(IEnumerable<MonsterModel> monsters)
        {
            ArgumentNullException.ThrowIfNull(monsters);
            _monsters =
            [
                .. monsters
                    .Where(static monster => monster != null)
                    .Take(MaximumVisualCount),
            ];
        }

        public override void _Ready()
        {
            CustomMinimumSize = new(0f, ViewportHeight + 36f);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListShellStyle());

            var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            column.AddThemeConstantOverride("separation", 4);
            AddChild(column);
            _viewportContainer = new()
            {
                CustomMinimumSize = new(0f, ViewportHeight),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Stretch = true,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            column.AddChild(_viewportContainer);
            _viewport = new()
            {
                Size = new(480, Mathf.RoundToInt(ViewportHeight)),
                TransparentBg = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            };
            _viewportContainer.AddChild(_viewport);

            var names = new RitsuShellTooltipLabel
            {
                Text = _monsters.Length == 0
                    ? ModSettingsLocalization.Get("ritsulib.debugTools.previewUnavailable", "Preview unavailable")
                    : string.Join(" · ", _monsters.Select(ResolveTitle)),
                HorizontalAlignment = HorizontalAlignment.Center,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                TooltipText = BuildTooltip(_monsters),
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
            _creatures.Clear();
            base._ExitTree();
        }

        private static string BuildTooltip(IReadOnlyList<MonsterModel> monsters)
        {
            if (monsters.Count == 0)
                return ModSettingsLocalization.Get("ritsulib.debugTools.previewUnavailable", "Preview unavailable");

            var title = string.Join(" · ", monsters.Select(ResolveTitle));
            var identities = monsters.Select(monster => $"{ResolveTitle(monster)} · {monster.Id}");
            return $"{title}\n{string.Join('\n', identities)}";
        }

        private void BuildVisuals()
        {
            foreach (var monster in _monsters)
                try
                {
                    var previewMonster = monster.IsMutable
                        ? (MonsterModel)monster.MutableClone()
                        : monster.ToMutable();
                    previewMonster.Rng = Rng.Chaotic;
                    previewMonster.RunRng = new(string.Empty);
                    previewMonster.SetUpForCombat();
                    var entity = new Creature(previewMonster, CombatSide.Enemy, null)
                    {
                        CombatState = new NullCombatState(),
                    };
                    var creature = NCreature.Create(entity);
                    if (creature == null || !IsInstanceValid(creature))
                        continue;
                    _viewport.AddChild(creature);
                    creature.SetupForBestiary();
                    creature.Hitbox.Resized += QueueVisualLayout;
                    _creatures.Add(creature);
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
            var layouts = _creatures
                .Where(IsInstanceValid)
                .Select(static creature => new CreatureLayout(creature, GetCreatureBounds(creature)))
                .Where(static layout => layout.Bounds.Size is { X: > 0f, Y: > 0f })
                .ToArray();
            if (layouts.Length == 0)
                return;

            var gapWidth = CreatureSpacing * (layouts.Length - 1);
            var availableWidth = Math.Max(1f, size.X - HorizontalPadding * 2f - gapWidth);
            var availableHeight = Math.Max(1f, size.Y - VerticalPadding * 2f);
            var contentWidth = layouts.Sum(static layout => layout.Bounds.Size.X);
            var contentHeight = layouts.Max(static layout => layout.Bounds.Size.Y);
            var maximumScale = layouts.Length == 1 ? MaximumSingleScale : MaximumGroupScale;
            var scale = Math.Min(maximumScale,
                Math.Min(availableWidth / contentWidth, availableHeight / contentHeight));
            scale = Math.Max(0.05f, scale);

            var scaledWidth = contentWidth * scale + gapWidth;
            var cursorX = (size.X - scaledWidth) * 0.5f;
            var floorY = size.Y - VerticalPadding;
            foreach (var layout in layouts)
            {
                layout.Creature.Scale = Vector2.One * scale;
                layout.Creature.Position = new(
                    cursorX - layout.Bounds.Position.X * scale,
                    floorY - layout.Bounds.End.Y * scale);
                cursorX += layout.Bounds.Size.X * scale + CreatureSpacing;
            }
        }

        private static Rect2 GetCreatureBounds(NCreature creature)
        {
            var inverseTransform = creature.GetGlobalTransformWithCanvas().AffineInverse();
            return new(inverseTransform * creature.Hitbox.GlobalPosition, creature.Hitbox.Size);
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

        private readonly record struct CreatureLayout(NCreature Creature, Rect2 Bounds);
    }
}
