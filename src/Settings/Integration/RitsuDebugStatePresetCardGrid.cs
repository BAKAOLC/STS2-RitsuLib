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
        private readonly Func<bool> _isReorderAllowed;
        private readonly Action<int, int> _moved;
        private readonly PresetCardReorderController _reorderController;
        private readonly Action<int> _selected;
        private IReadOnlyList<RitsuDebugStatePresetCard> _cards = [];
        private int _selectedIndex = -1;

        internal RitsuDebugStatePresetCardGrid(
            Control dragLayer,
            Func<bool> isReorderAllowed,
            Action<int> selected,
            Action<int, int> moved)
        {
            ArgumentNullException.ThrowIfNull(dragLayer);
            ArgumentNullException.ThrowIfNull(isReorderAllowed);
            ArgumentNullException.ThrowIfNull(selected);
            ArgumentNullException.ThrowIfNull(moved);
            _isReorderAllowed = isReorderAllowed;
            _selected = selected;
            _moved = moved;
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
            _reorderController = PresetCardReorderController.Attach(dragLayer, this);
            GetVScrollBar().VisibilityChanged += SyncScrollGutter;
            SyncScrollGutter();
        }

        internal void SetCards(IReadOnlyList<RitsuDebugStatePresetCard> cards, int selectedIndex)
        {
            ArgumentNullException.ThrowIfNull(cards);
            _reorderController.Cancel();
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

        public override void _ExitTree()
        {
            _reorderController.Cancel();
            ReleaseCards();
            base._ExitTree();
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
            ReleaseCards();

            for (var index = 0; index < _cards.Count; index++)
                _flow.AddChild(CreateTile(_cards[index], index));
        }

        private void ReleaseCards()
        {
            foreach (var child in _flow.GetChildren())
            {
                _flow.RemoveChild(child);
                ReleaseTile((Control)child);
            }
        }

        private Control CreateTile(RitsuDebugStatePresetCard saved, int index)
        {
            var tile = new RitsuShellTooltipPanelContainer
            {
                CustomMinimumSize = new(TileWidth, TileHeight),
                MouseFilter = MouseFilterEnum.Stop,
                MouseDefaultCursorShape = CursorShape.Drag,
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
                card.QueueFreeSafely();
                return tile;
            }

            holder.SetMeta(RitsuDebugCardCatalog.HolderMetaKey, true);
            holder.Scale = holder.SmallScale;
            var visualCenter = RitsuDebugCardCatalog.HolderVisualBounds.GetCenter() * holder.Scale;
            holder.Position = new Vector2(TileWidth * 0.5f, TileHeight * 0.5f) - visualCenter;
            holder.MouseFilter = MouseFilterEnum.Pass;
            holder.Pressed += _ =>
            {
                if (_reorderController.ShouldSuppressClick() || _reorderController.IsDragging)
                    return;
                _selected(index);
            };
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
            var lines = new List<string>
            {
                card.CardId,
                $"×{card.Count}",
                L("ritsulib.debugTools.statePresets.dragCardHint", "Drag cards to reorder."),
            };
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

        private sealed class PresetCardReorderController
        {
            private const float DragStartThreshold = 4f;
            private readonly Control _dragLayer;
            private readonly List<Vector2> _dropSlotCenters = [];
            private readonly RitsuDebugStatePresetCardGrid _owner;
            private Control? _ghost;
            private Vector2 _ghostOffset;
            private int _originalIndex = -1;
            private Control? _pendingTile;
            private Vector2? _pendingGlobalPosition;
            private Control? _sourceTile;
            private bool _suppressNextClick;
            private bool _wasMousePressed;

            private PresetCardReorderController(
                Control dragLayer,
                RitsuDebugStatePresetCardGrid owner)
            {
                _dragLayer = dragLayer;
                _owner = owner;
            }

            internal bool IsDragging { get; private set; }

            internal static PresetCardReorderController Attach(
                Control dragLayer,
                RitsuDebugStatePresetCardGrid owner)
            {
                var controller = new PresetCardReorderController(dragLayer, owner);
                var timer = new Godot.Timer
                {
                    Name = "PresetCardReorderDragPoll",
                    WaitTime = 0.016d,
                    Autostart = true,
                    ProcessMode = ProcessModeEnum.Always,
                };
                timer.Timeout += controller.Poll;
                owner.AddChild(timer);
                return controller;
            }

            internal void Cancel()
            {
                FinishVisuals();
                _pendingTile = null;
                _pendingGlobalPosition = null;
            }

            internal bool ShouldSuppressClick()
            {
                if (!_suppressNextClick)
                    return false;
                _suppressNextClick = false;
                return true;
            }

            private Vector2 MouseCanvas => _owner._flow.GetGlobalMousePosition();

            private void Poll()
            {
                if (!IsInstanceValid(_owner) || !IsInstanceValid(_dragLayer))
                    return;
                var mousePressed = Input.IsMouseButtonPressed(MouseButton.Left);
                if (!_owner.IsVisibleInTree() || !_owner._isReorderAllowed())
                {
                    Cancel();
                    _pendingTile = null;
                    _pendingGlobalPosition = null;
                    _wasMousePressed = mousePressed;
                    return;
                }

                var mouse = MouseCanvas;
                if (IsDragging)
                {
                    UpdateGhostPosition(mouse);
                    UpdateTarget(mouse);
                    if (!mousePressed)
                        CompleteDrop();
                    _wasMousePressed = mousePressed;
                    return;
                }

                if (mousePressed)
                {
                    if (!_wasMousePressed)
                    {
                        _pendingTile = FindTileAt(mouse);
                        _pendingGlobalPosition = _pendingTile == null ? null : mouse;
                    }

                    if (_pendingTile != null &&
                        _pendingGlobalPosition is { } start &&
                        start.DistanceTo(mouse) >= DragStartThreshold)
                        TryBeginDrag(_pendingTile, mouse);
                }
                else if (_wasMousePressed)
                {
                    _pendingTile = null;
                    _pendingGlobalPosition = null;
                }

                _wasMousePressed = mousePressed;
            }

            private void TryBeginDrag(Control tile, Vector2 globalMouse)
            {
                if (IsDragging)
                    return;
                CaptureDropSlots();
                if (_dropSlotCenters.Count < 2 ||
                    FindCardHolder(tile) is not { } holder ||
                    CreateCardGhost(holder, globalMouse) is not { } ghost)
                {
                    _dropSlotCenters.Clear();
                    return;
                }

                _ghost = ghost;
                _sourceTile = tile;
                _originalIndex = tile.GetIndex();
                IsDragging = true;
                _suppressNextClick = true;
                _pendingTile = null;
                _pendingGlobalPosition = null;
                tile.Modulate = new(1f, 1f, 0.95f, 0.22f);
            }

            private Control? CreateCardGhost(NGridCardHolder holder, Vector2 globalMouse)
            {
                if (holder.CardNode is not Control source || source.Duplicate(14) is not Control duplicate)
                    return null;
                duplicate.Name = "PresetCardReorderGhost";
                duplicate.ZIndex = 90;
                IgnoreMouseRecursively(duplicate);
                _dragLayer.AddChild(duplicate);
                duplicate.Scale = holder.Scale * source.Scale;
                duplicate.Rotation = holder.Rotation + source.Rotation;
                duplicate.Position = _dragLayer.GetGlobalTransformWithCanvas().AffineInverse() *
                                     source.GetGlobalTransformWithCanvas().Origin;
                var localMouse = _dragLayer.GetGlobalTransformWithCanvas().AffineInverse() * globalMouse;
                _ghostOffset = duplicate.Position - localMouse;
                return duplicate;
            }

            private static void IgnoreMouseRecursively(Node node)
            {
                if (node is Control control)
                    control.MouseFilter = MouseFilterEnum.Ignore;
                foreach (var child in node.GetChildren())
                    IgnoreMouseRecursively(child);
            }

            private void UpdateGhostPosition(Vector2 globalMouse)
            {
                if (_ghost == null || !IsInstanceValid(_ghost))
                    return;
                var localMouse = _dragLayer.GetGlobalTransformWithCanvas().AffineInverse() * globalMouse;
                _ghost.Position = localMouse + _ghostOffset;
            }

            private void UpdateTarget(Vector2 globalMouse)
            {
                if (_sourceTile == null ||
                    !IsInstanceValid(_sourceTile) ||
                    _dropSlotCenters.Count == 0)
                    return;

                var destinationIndex = 0;
                var targetDistanceSquared = float.MaxValue;
                for (var index = 0; index < _dropSlotCenters.Count; index++)
                {
                    var distanceSquared = (globalMouse - _dropSlotCenters[index]).LengthSquared();
                    if (distanceSquared >= targetDistanceSquared)
                        continue;
                    targetDistanceSquared = distanceSquared;
                    destinationIndex = index;
                }

                var sourceIndex = _sourceTile.GetIndex();
                if (destinationIndex == sourceIndex)
                    return;
                _owner._flow.MoveChild(_sourceTile, destinationIndex);
                _owner._flow.QueueSort();
            }

            private void CaptureDropSlots()
            {
                _dropSlotCenters.Clear();
                foreach (var child in _owner._flow.GetChildren())
                {
                    if (child is Control { Visible: true } tile)
                        _dropSlotCenters.Add(tile.GetGlobalRect().GetCenter());
                }
            }

            private Control? FindTileAt(Vector2 globalMouse)
            {
                Control? fallback = null;
                var fallbackDistanceSquared = float.MaxValue;
                foreach (var child in _owner._flow.GetChildren())
                {
                    if (child is not Control { Visible: true } tile)
                        continue;
                    var rect = tile.GetGlobalRect();
                    if (rect.HasPoint(globalMouse))
                        return tile;
                    var distanceSquared = (globalMouse - rect.GetCenter()).LengthSquared();
                    if (distanceSquared >= fallbackDistanceSquared)
                        continue;
                    fallbackDistanceSquared = distanceSquared;
                    fallback = tile;
                }

                var maximumDistance = new Vector2(TileWidth, TileHeight).LengthSquared() * 0.35f;
                return fallbackDistanceSquared <= maximumDistance ? fallback : null;
            }

            private void CompleteDrop()
            {
                var destinationIndex = _sourceTile is { } source && IsInstanceValid(source)
                    ? source.GetIndex()
                    : _originalIndex;
                var originalIndex = _originalIndex;
                FinishVisuals();
                if (originalIndex >= 0 && destinationIndex >= 0 && originalIndex != destinationIndex)
                    _owner._moved(originalIndex, destinationIndex);
            }

            private void FinishVisuals()
            {
                if (_sourceTile != null && IsInstanceValid(_sourceTile))
                    _sourceTile.Modulate = Colors.White;
                if (_ghost != null && IsInstanceValid(_ghost))
                    _ghost.QueueFreeSafely();
                _ghost = null;
                _sourceTile = null;
                _originalIndex = -1;
                _dropSlotCenters.Clear();
                IsDragging = false;
            }
        }
    }
}
