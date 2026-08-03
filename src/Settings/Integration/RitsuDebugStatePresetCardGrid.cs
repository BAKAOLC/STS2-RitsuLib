using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    // ReSharper disable once Godot.MissingParameterlessConstructor
    internal sealed partial class RitsuDebugStatePresetCardGrid : ScrollContainer
    {
        private const float TileWidth = 232f;
        private const float TileHeight = 324f;
        private const int ContentEdgePadding = 18;
        private readonly HFlowContainer _flow;
        private readonly MarginContainer _frame;
        private readonly Action<int> _selected;
        private IReadOnlyList<RitsuDebugStatePresetCard> _cards = [];
        private int _selectedIndex = -1;

        internal RitsuDebugStatePresetCardGrid(Action<int> selected)
        {
            ArgumentNullException.ThrowIfNull(selected);
            _selected = selected;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;
            HorizontalScrollMode = ScrollMode.Disabled;
            ModSettingsUiControlTheming.ApplySettingsScrollContainerThemeForDropdownList(this);
            _frame = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _frame.AddThemeConstantOverride("margin_left", ContentEdgePadding);
            _frame.AddThemeConstantOverride("margin_top", ContentEdgePadding);
            _frame.AddThemeConstantOverride("margin_right", ContentEdgePadding);
            _frame.AddThemeConstantOverride("margin_bottom", ContentEdgePadding);
            AddChild(_frame);
            _flow = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                Alignment = FlowContainer.AlignmentMode.Begin,
            };
            _flow.AddThemeConstantOverride("h_separation", 14);
            _flow.AddThemeConstantOverride("v_separation", 18);
            _frame.AddChild(_flow);
            GetVScrollBar().VisibilityChanged += SyncScrollGutter;
            SyncScrollGutter();
        }

        internal void SetCards(IReadOnlyList<RitsuDebugStatePresetCard> cards, int selectedIndex)
        {
            ArgumentNullException.ThrowIfNull(cards);
            _cards = cards;
            _selectedIndex = selectedIndex;
            Rebuild();
        }

        internal void SetSelectedIndex(int selectedIndex)
        {
            if (_selectedIndex == selectedIndex)
                return;
            var previous = _selectedIndex;
            _selectedIndex = selectedIndex;
            ApplyTileSelection(previous);
            ApplyTileSelection(selectedIndex);
        }

        internal void RefreshCard(int index)
        {
            if (index < 0 || index >= _cards.Count || index >= _flow.GetChildCount())
                return;
            var current = (Control)_flow.GetChild(index);
            var replacement = CreateTile(_cards[index], index);
            _flow.RemoveChild(current);
            ReleaseTile(current);
            _flow.AddChild(replacement);
            _flow.MoveChild(replacement, index);
        }

        private void Rebuild()
        {
            foreach (var child in _flow.GetChildren())
            {
                _flow.RemoveChild(child);
                ReleaseTile((Control)child);
            }

            for (var index = 0; index < _cards.Count; index++)
                _flow.AddChild(CreateTile(_cards[index], index));
        }

        private Control CreateTile(RitsuDebugStatePresetCard saved, int index)
        {
            var tile = new PanelContainer
            {
                CustomMinimumSize = new(TileWidth, TileHeight),
                MouseFilter = MouseFilterEnum.Stop,
                TooltipText = BuildTooltip(saved),
            };
            tile.AddThemeStyleboxOverride(
                "panel",
                index == _selectedIndex
                    ? RitsuShellChromeStyles.CreateSelectedListItemCardStyle()
                    : RitsuShellChromeStyles.CreateListItemCardStyle());
            var canvas = new Control { MouseFilter = MouseFilterEnum.Pass };
            tile.AddChild(canvas);
            if (!RitsuDebugCardActions.TryResolveCanonicalCard(saved.CardId, out var canonical, out _))
                return tile;

            var preview = canonical.ToMutable();
            RitsuDebugCardActions.ApplyAvailableUpgradeLevels(preview, saved.UpgradeLevels);
            RitsuDebugCardActions.ApplyCardState(preview, saved.ToCardState());
            var card = NCard.Create(preview);
            if (card == null)
                return tile;
            var holder = NGridCardHolder.Create(card);
            if (holder == null)
            {
                card.QueueFree();
                return tile;
            }

            holder.SetMeta(RitsuDebugCardCatalog.HolderMetaKey, true);
            holder.Scale = holder.SmallScale;
            var visualCenter = RitsuDebugCardCatalog.HolderVisualBounds.GetCenter() * holder.Scale;
            holder.Position = new Vector2(TileWidth * 0.5f, TileHeight * 0.5f) - visualCenter;
            holder.MouseFilter = MouseFilterEnum.Pass;
            holder.Pressed += _ => _selected(index);
            canvas.AddChild(holder);
            card.UpdateVisuals(PileType.None, CardPreviewMode.Normal);

            var count = new Label
            {
                Text = $"×{saved.Count}",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
                Position = new(TileWidth - 54f, 8f),
                Size = new(44f, 28f),
            };
            count.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            count.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
            count.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            canvas.AddChild(count);
            return tile;
        }

        private void ApplyTileSelection(int index)
        {
            if (index < 0 || index >= _flow.GetChildCount() || _flow.GetChild(index) is not PanelContainer tile)
                return;
            tile.AddThemeStyleboxOverride(
                "panel",
                index == _selectedIndex
                    ? RitsuShellChromeStyles.CreateSelectedListItemCardStyle()
                    : RitsuShellChromeStyles.CreateListItemCardStyle());
        }

        private static void ReleaseTile(Control tile)
        {
            var holder = FindCardHolder(tile);
            if (holder != null && IsInstanceValid(holder))
            {
                holder.RemoveMeta(RitsuDebugCardCatalog.HolderMetaKey);
                holder.GetParent()?.RemoveChildSafely(holder);
                holder.QueueFreeSafely();
            }

            tile.QueueFree();
        }

        private static NGridCardHolder? FindCardHolder(Node parent)
        {
            foreach (var child in parent.GetChildren())
            {
                if (child is NGridCardHolder holder)
                    return holder;
                if (FindCardHolder(child) is { } descendant)
                    return descendant;
            }

            return null;
        }

        private static string BuildTooltip(RitsuDebugStatePresetCard card)
        {
            var lines = new List<string> { card.CardId, $"×{card.Count}" };
            if (card.UpgradeLevels > 0)
                lines.Add($"+{card.UpgradeLevels}");
            if (card.BaseCost.HasValue)
                lines.Add(string.Format(
                    L("ritsulib.debugTools.statePresets.tooltipCost", "Cost {0}"),
                    card.BaseCost.Value));
            if (card.ReplayCount.HasValue)
                lines.Add(string.Format(
                    L("ritsulib.debugTools.statePresets.tooltipReplay", "Replay {0}"),
                    card.ReplayCount.Value));
            if (card.DynamicVars is { Count: > 0 })
                lines.Add(string.Join(", ", card.DynamicVars.Select(static pair => $"{pair.Key}={pair.Value}")));
            return string.Join('\n', lines);
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }

        private void SyncScrollGutter()
        {
            var gutter = ContentEdgePadding;
            if (GetVScrollBar().Visible)
                gutter += ModSettingsUiControlTheming.ResolveSettingsScrollContentRightGutter(this);
            if (_frame.GetThemeConstant("margin_right") == gutter)
                return;
            _frame.AddThemeConstantOverride("margin_right", gutter);
            _frame.QueueSort();
        }
    }
}
