using Godot;
using Godot.Collections;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class ModSettingsListControl<TItem> : VBoxContainer
    {
        private readonly string _dragToken = Guid.NewGuid().ToString("N");
        private readonly System.Collections.Generic.Dictionary<int, ModSettingsListDropSlot<TItem>> _dropSlots = [];
        private readonly ListModSettingsEntryDefinition<TItem> _entry;
        private readonly System.Collections.Generic.Dictionary<int, Control> _rowCards = [];
        private ModSettingsListDropSlot<TItem>? _activeDropSlot;
        private Label? _countLabel;
        private int _currentDragIndex = -1;
        private bool _dropCommitted;
        private PanelContainer? _emptyState;
        private List<TItem>? _listStructuralBaseline;
        private VBoxContainer? _rows;

        public ModSettingsListControl(ModSettingsUiContext context, ListModSettingsEntryDefinition<TItem> entry)
        {
            UiContext = context;
            _entry = entry;

            MouseFilter = MouseFilterEnum.Ignore;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.list.layout.rootSeparation", 10));
        }

        public ModSettingsListControl()
        {
            UiContext = null!;
            _entry = null!;
        }

        internal ModSettingsUiContext UiContext { get; }

        internal ModSettingsMenuCapabilities EntryMenuCapabilities => _entry.MenuCapabilities;

        public override void _Notification(int what)
        {
            if (what != NotificationDragEnd) return;
            if (!_dropCommitted && _activeDropSlot != null && _currentDragIndex >= 0)
                MoveDraggedItemTo(_activeDropSlot.TargetIndex);

            _currentDragIndex = -1;
            _dropCommitted = false;
            ClearActiveDropSlot();
        }

        public override void _Process(double delta)
        {
            if (_currentDragIndex < 0 || !Input.IsMouseButtonPressed(MouseButton.Left) || _rows == null)
                return;

            var mouse = GetViewport().GetMousePosition();
            var nearestTargetIndex = -1;
            var nearestDistance = float.MaxValue;

            foreach (var pair in _dropSlots)
            {
                var rect = pair.Value.GetGlobalRect();
                var center = rect.Position + rect.Size * 0.5f;
                var dx = mouse.X < rect.Position.X
                    ? rect.Position.X - mouse.X
                    : mouse.X > rect.End.X
                        ? mouse.X - rect.End.X
                        : 0f;
                var dy = MathF.Abs(mouse.Y - center.Y);
                var distance = dx * 0.25f + dy;
                if (!(distance < nearestDistance)) continue;
                nearestDistance = distance;
                nearestTargetIndex = pair.Key;
            }

            if (nearestTargetIndex >= 0)
                PreviewDropAtIndex(nearestTargetIndex);
        }

        public override void _Ready()
        {
            var shell = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            shell.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListShellStyle());
            AddChild(shell);

            var root = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.list.layout.shellSeparation", 10));
            shell.AddChild(root);

            var header = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = AlignmentMode.Center,
            };
            header.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.list.layout.headerSeparation", 10));
            root.AddChild(header);

            var textColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            textColumn.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.list.layout.textColumnSeparation", 3));
            header.AddChild(textColumn);

            textColumn.AddChild(ModSettingsUiFactory.CreateRefreshableSectionTitle(UiContext, _entry.Label,
                () => ModSettingsUiFactory.ResolveEntryLabelDisplay(_entry.Label)));

            var descriptionLabel = ModSettingsUiFactory.CreateRefreshableDescriptionLabel(UiContext, _entry.Description,
                () => ModSettingsUiControlFactoryHelper.ResolveDescription(_entry.Description));
            textColumn.AddChild(descriptionLabel);

            var summary = new PanelContainer
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.list.layout.summaryPill.minSize",
                    new(96f, 32f)),
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            summary.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreatePillStyle());
            header.AddChild(summary);

            var countLabel = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            countLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            countLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.PillCount);
            countLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            summary.AddChild(countLabel);
            _countLabel = countLabel;

            var addButton = new ModSettingsTextButton(ModSettingsUiContext.Resolve(_entry.AddButtonText),
                ModSettingsButtonTone.Accent,
                () => Mutate(items => items.Add(_entry.CreateItem())))
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.list.layout.addButton.minSize",
                    new(152f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            header.AddChild(addButton);

            if (ModSettingsUiFactory.CreateEntryActionsButton(UiContext, _entry.Binding, _entry.MenuCapabilities) is
                ModSettingsActionsButton actionsButton)
            {
                header.AddChild(actionsButton);
                ModSettingsUiFactory.AttachContextMenuTargets(this, shell, actionsButton);
            }

            var body = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            body.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateInsetSurfaceStyle());
            root.AddChild(body);

            var bodyContent = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            bodyContent.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.list.layout.bodySeparation", 6));
            body.AddChild(bodyContent);

            _rows = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _rows.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.list.layout.rowsSeparation", 6));
            bodyContent.AddChild(_rows);

            var emptyState = new PanelContainer
            {
                Visible = false,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            emptyState.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreatePillStyle());
            bodyContent.AddChild(emptyState);

            var emptyLabel = new Label
            {
                Text = ModSettingsLocalization.Get("list.empty", "No items yet. Add one to start editing."),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            emptyLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            emptyLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
            emptyLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            emptyState.AddChild(emptyLabel);
            _emptyState = emptyState;

            ModSettingsUiFactory.RegisterRefreshWhenAlive(UiContext, this, SyncRows,
                ModSettingsUiRefreshSpec.ForBinding(_entry.Binding));
            SyncRows();
        }

        private void SyncRows()
        {
            if (_rows == null || !IsInstanceValid(this))
                return;

            ClearActiveDropSlot();
            var items = _entry.Binding.Read();
            UpdateListHeaderChrome(items);

            if (!TryIncrementalListSync(items))
                FullRebuildRows(items);

            PruneTrailingDropSlots(items);
            LayoutRowsChildOrder(items);
            UpdateStructuralBaseline(items);
            _rows?.ResetSize();
            _rows?.QueueSort();
        }

        private void UpdateListHeaderChrome(List<TItem> items)
        {
            if (_countLabel != null)
                _countLabel.Text = ModSettingsLocalizedFormatting.Format(
                    ModSettingsLocalization.Get("list.count", "{0} items"),
                    items.Count);

            if (_emptyState != null)
                _emptyState.Visible = items.Count == 0;
        }

        private void UpdateStructuralBaseline(List<TItem> items)
        {
            _listStructuralBaseline = CloneBindingValue(items);
        }

        private bool BaselinePrefixMatches(List<TItem> items, int prefixLength)
        {
            if (_listStructuralBaseline == null || prefixLength > _listStructuralBaseline.Count ||
                prefixLength > items.Count)
                return false;

            var comparer = EqualityComparer<TItem>.Default;
            for (var i = 0; i < prefixLength; i++)
                if (!comparer.Equals(items[i], _listStructuralBaseline[i]))
                    return false;

            return true;
        }

        private static bool RowKeysCoverRange(System.Collections.Generic.Dictionary<int, Control> rowCards, int count)
        {
            for (var i = 0; i < count; i++)
                if (!rowCards.ContainsKey(i))
                    return false;

            return true;
        }

        private bool TryIncrementalListSync(List<TItem> items)
        {
            var n = items.Count;

            if (n == _rowCards.Count && RowKeysCoverRange(_rowCards, n))
            {
                var comparer = EqualityComparer<TItem>.Default;
                List<int>? dirty = null;
                for (var i = 0; i < n; i++)
                {
                    if (!_rowCards.TryGetValue(i, out var c) || c is not ModSettingsListItemCard<TItem> card)
                        return false;
                    if (comparer.Equals(items[i], card.ItemContext.Item))
                        continue;
                    dirty ??= [];
                    dirty.Add(i);
                }

                if (dirty == null)
                {
                    for (var i = 0; i < n; i++)
                        ResyncExistingRow(i, items);
                    return true;
                }

                if (dirty.Count == n)
                    return false;

                var dirtySet = new HashSet<int>(dirty);
                foreach (var i in dirtySet)
                    ReplaceRowCardAt(i, items);

                for (var i = 0; i < n; i++)
                {
                    if (dirtySet.Contains(i))
                        continue;
                    ResyncExistingRow(i, items);
                }

                return true;
            }

            if (n == _rowCards.Count + 1
                && RowKeysCoverRange(_rowCards, n - 1)
                && !_rowCards.ContainsKey(n - 1)
                && _listStructuralBaseline != null
                && _listStructuralBaseline.Count == n - 1
                && BaselinePrefixMatches(items, n - 1))
            {
                for (var i = 0; i < n - 1; i++)
                    ResyncExistingRow(i, items);
                AppendRow(items, n - 1, n);
                return true;
            }

            if (n != _rowCards.Count - 1
                || !_rowCards.ContainsKey(n)
                || _listStructuralBaseline == null
                || _listStructuralBaseline.Count != n + 1
                || !BaselinePrefixMatches(items, n))
                return false;

            if (_rowCards.TryGetValue(n, out var trailing) && IsInstanceValid(trailing))
                DetachAndQueueFree(trailing);
            _rowCards.Remove(n);

            for (var i = 0; i < n; i++)
                ResyncExistingRow(i, items);
            return true;
        }

        private static void DetachAndQueueFree(Control node)
        {
            if (!IsInstanceValid(node))
                return;
            node.GetParent()?.RemoveChild(node);
            node.QueueFree();
        }

        private void ReplaceRowCardAt(int i, List<TItem> items)
        {
            if (_rows == null)
                return;

            if (_rowCards.TryGetValue(i, out var oldRow) && IsInstanceValid(oldRow))
            {
                DetachAndQueueFree(oldRow);
                _rowCards.Remove(i);
            }

            var row = CreateRow(i, items[i], items.Count);
            _rowCards[i] = row;
            _rows.AddChild(row);
        }

        private void ResyncExistingRow(int i, List<TItem> items)
        {
            if (!_rowCards.TryGetValue(i, out var control) ||
                control is not ModSettingsListItemCard<TItem> card) return;
            var item = items[i];
            card.ItemContext.SyncRowListState(i, items.Count, item);
            var title = ModSettingsUiFactory.ResolveEntryLabelDisplay(SafeResolveItemLabel(item));
            var subtitle = SafeResolveItemDescription(item) is { } d
                ? ModSettingsUiContext.Resolve(d)
                : null;
            card.SyncRowChrome(i, title, subtitle, i == 0);
            card.QueueSort();
        }

        private void AppendRow(List<TItem> items, int index, int itemCount)
        {
            var row = CreateRow(index, items[index], itemCount);
            _rowCards[index] = row;
            if (_rows != null && row.GetParent() != _rows)
                _rows.AddChild(row);
        }

        private void PruneTrailingDropSlots(List<TItem> items)
        {
            foreach (var staleSlot in _dropSlots.Keys.Where(index => index > items.Count).ToArray())
            {
                if (_dropSlots.TryGetValue(staleSlot, out var slot) && IsInstanceValid(slot))
                    DetachAndQueueFree(slot);
                _dropSlots.Remove(staleSlot);
            }
        }

        private void LayoutRowsChildOrder(List<TItem> items)
        {
            if (_rows == null)
                return;

            var childOrder = 0;
            for (var slotIndex = 0; slotIndex <= items.Count; slotIndex++)
            {
                var dropSlot = EnsureDropSlot(slotIndex);
                if (dropSlot.GetParent() != _rows)
                    _rows.AddChild(dropSlot);
                _rows.MoveChild(dropSlot, childOrder++);

                if (slotIndex >= items.Count)
                    continue;

                if (!_rowCards.TryGetValue(slotIndex, out var row))
                    continue;

                if (row.GetParent() != _rows)
                    _rows.AddChild(row);
                _rows.MoveChild(row, childOrder++);
            }
        }

        private void FullRebuildRows(List<TItem> items)
        {
            var liveIndexes = Enumerable.Range(0, items.Count).ToHashSet();
            foreach (var staleIndex in _rowCards.Keys.Where(index => !liveIndexes.Contains(index)).ToArray())
            {
                if (_rowCards.TryGetValue(staleIndex, out var staleRow) && IsInstanceValid(staleRow))
                    DetachAndQueueFree(staleRow);
                _rowCards.Remove(staleIndex);
            }

            for (var slotIndex = 0; slotIndex < items.Count; slotIndex++)
            {
                if (_rowCards.TryGetValue(slotIndex, out var existing) && IsInstanceValid(existing))
                    DetachAndQueueFree(existing);

                var row = CreateRow(slotIndex, items[slotIndex], items.Count);
                _rowCards[slotIndex] = row;
                if (_rows != null && row.GetParent() != _rows)
                    _rows.AddChild(row);
            }
        }

        private Control CreateRow(int index, TItem item, int itemCount)
        {
            var liveIndex = new ModSettingsListItemContext<TItem>.ListRowLiveIndex { Value = index };
            var itemContext = new ModSettingsListItemContext<TItem>(
                UiContext,
                CreateItemBinding(index),
                $"{_entry.Id}[{index}]",
                liveIndex,
                itemCount,
                item,
                updatedItem => Mutate(items => items[liveIndex.Value] = updatedItem),
                () => Mutate(items =>
                {
                    if (liveIndex.Value <= 0)
                        return;
                    MoveItem(items, liveIndex.Value, liveIndex.Value - 1);
                }),
                () => Mutate(items =>
                {
                    if (liveIndex.Value >= items.Count - 1)
                        return;
                    MoveItem(items, liveIndex.Value, liveIndex.Value + 1);
                }),
                () => Mutate(items => DuplicateItem(items, liveIndex.Value)),
                () => Mutate(items => items.RemoveAt(liveIndex.Value)),
                UiContext.RequestRefresh);

            return new ModSettingsListItemCard<TItem>(
                this,
                index,
                ModSettingsUiFactory.ResolveEntryLabelDisplay(SafeResolveItemLabel(item)),
                SafeResolveItemDescription(item) is { } description
                    ? ModSettingsUiContext.Resolve(description)
                    : null,
                itemContext,
                SafeCreateItemEditor(itemContext),
                _entry.CollapsibleItems,
                _entry.StartItemsCollapsed,
                SafeCreateItemHeaderAccessory(itemContext));
        }

        private ModSettingsText SafeResolveItemLabel(TItem item)
        {
            try
            {
                return _entry.ItemLabel(item);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModSettingsListControl] ItemLabel failed for '{_entry.Id}': {ex}");
                return ModSettingsText.Literal(_entry.Id);
            }
        }

        private ModSettingsText? SafeResolveItemDescription(TItem item)
        {
            if (_entry.ItemDescription == null)
                return null;

            try
            {
                return _entry.ItemDescription(item);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModSettingsListControl] ItemDescription failed for '{_entry.Id}': {ex}");
                return null;
            }
        }

        private Control? SafeCreateItemEditor(ModSettingsListItemContext<TItem> itemContext)
        {
            if (_entry.ItemEditorFactory == null)
                return null;

            try
            {
                return _entry.ItemEditorFactory(itemContext);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModSettingsListControl] ItemEditorFactory failed for '{_entry.Id}': {ex}");
                return null;
            }
        }

        private Control? SafeCreateItemHeaderAccessory(ModSettingsListItemContext<TItem> itemContext)
        {
            if (_entry.ItemHeaderAccessoryFactory == null)
                return null;

            try
            {
                return _entry.ItemHeaderAccessoryFactory(itemContext);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModSettingsListControl] ItemHeaderAccessoryFactory failed for '{_entry.Id}': {ex}");
                return null;
            }
        }

        private void Mutate(Action<List<TItem>> mutate)
        {
            var clone = CloneBindingValue(_entry.Binding.Read());
            mutate(clone);
            _entry.Binding.Write(clone);
            UiContext.MarkDirty(_entry.Binding);
            UiContext.RequestRefresh();
        }

        private IModSettingsValueBinding<TItem> CreateItemBinding(int index)
        {
            var itemAdapter = _entry.ItemDataAdapter;
            return ModSettingsBindings.Project(
                _entry.Binding,
                $"items[{index}]",
                items => items[index],
                (items, item) => ReplaceAt(items, index, item),
                itemAdapter);
        }

        internal Dictionary CreateDragData(int index)
        {
            _currentDragIndex = index;
            _dropCommitted = false;
            return new()
            {
                ["token"] = _dragToken,
                ["index"] = index,
            };
        }

        internal bool CanAcceptDrop(Variant data)
        {
            return data.VariantType == Variant.Type.Dictionary
                   && data.AsGodotDictionary().TryGetValue("token", out var token)
                   && token.AsString() == _dragToken;
        }

        internal void HandleDrop(Variant data, int targetIndex)
        {
            if (!CanAcceptDrop(data))
                return;

            var dragIndex = data.AsGodotDictionary()["index"].AsInt32();
            _dropCommitted = true;
            ClearActiveDropSlot();
            Mutate(items => MoveItemToSlot(items, dragIndex, targetIndex));
        }

        internal void SetActiveDropSlot(ModSettingsListDropSlot<TItem>? slot, bool active)
        {
            if (!active)
            {
                if (_activeDropSlot == slot)
                    ClearActiveDropSlot();
                else
                    slot?.SetHighlighted(false);
                return;
            }

            if (_activeDropSlot != null && _activeDropSlot != slot)
                _activeDropSlot.SetHighlighted(false);

            _activeDropSlot = slot;
            _activeDropSlot?.SetHighlighted(true);
        }

        internal void ClearActiveDropSlot()
        {
            _activeDropSlot?.SetHighlighted(false);
            _activeDropSlot = null;
        }

        internal void PreviewDropAtIndex(int targetIndex)
        {
            if (_dropSlots.TryGetValue(targetIndex, out var slot))
                SetActiveDropSlot(slot, true);
        }

        internal void DropAtIndex(Variant data, int targetIndex)
        {
            HandleDrop(data, targetIndex);
        }

        private void MoveDraggedItemTo(int targetIndex)
        {
            var dragIndex = _currentDragIndex;
            if (dragIndex < 0)
                return;

            _dropCommitted = true;
            Mutate(items => MoveItemToSlot(items, dragIndex, targetIndex));
        }

        private ModSettingsListDropSlot<TItem> EnsureDropSlot(int index)
        {
            if (_dropSlots.TryGetValue(index, out var slot) && IsInstanceValid(slot))
                return slot;

            slot = new(this, index);
            _dropSlots[index] = slot;
            return slot;
        }

        private List<TItem> CloneBindingValue(List<TItem> items)
        {
            return _entry.Binding is IStructuredModSettingsValueBinding<List<TItem>> structured
                ? structured.Adapter.Clone(items)
                : [.. items];
        }

        private static List<TItem> ReplaceAt(List<TItem> items, int index, TItem item)
        {
            var clone = items.ToList();
            clone[index] = item;
            return clone;
        }

        private void DuplicateItem(List<TItem> items, int index)
        {
            if (index < 0 || index >= items.Count)
                return;

            var item = items[index];
            if (_entry.ItemDataAdapter != null)
                item = _entry.ItemDataAdapter.Clone(item);
            items.Insert(index + 1, item);
        }

        private static void MoveItem(List<TItem> items, int from, int to)
        {
            if (from < 0 || from >= items.Count || to < 0 || to >= items.Count || from == to)
                return;

            var item = items[from];
            items.RemoveAt(from);
            items.Insert(to, item);
        }

        private static void MoveItemToSlot(List<TItem> items, int from, int slotIndex)
        {
            if (from < 0 || from >= items.Count)
                return;

            slotIndex = Mathf.Clamp(slotIndex, 0, items.Count);
            var normalizedIndex = slotIndex;
            if (from < normalizedIndex)
                normalizedIndex--;

            if (normalizedIndex == from)
                return;

            var item = items[from];
            items.RemoveAt(from);
            items.Insert(normalizedIndex, item);
        }
    }

    internal sealed partial class ModSettingsListDropSlot<TItem> : PanelContainer
    {
        private readonly ModSettingsListControl<TItem> _owner;
        private NControllerManager? _hookedDropSlotController;

        public ModSettingsListDropSlot(ModSettingsListControl<TItem> owner, int targetIndex)
        {
            _owner = owner;
            TargetIndex = targetIndex;

            FocusMode = FocusModeEnum.None;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            MouseFilter = MouseFilterEnum.Stop;
            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.list.layout.dropSlot.minSize",
                new(0f, 8f));
            AddThemeStyleboxOverride("panel", CreateStyle(false));
        }

        public ModSettingsListDropSlot()
        {
            _owner = null!;
        }

        internal int TargetIndex { get; }

        public override void _EnterTree()
        {
            base._EnterTree();
            _hookedDropSlotController = NControllerManager.Instance;
            if (_hookedDropSlotController != null)
            {
                _hookedDropSlotController.ControllerDetected += ApplyDropSlotInputPolicy;
                _hookedDropSlotController.MouseDetected += ApplyDropSlotInputPolicy;
            }

            ApplyDropSlotInputPolicy();
        }

        public override void _ExitTree()
        {
            if (_hookedDropSlotController != null)
            {
                _hookedDropSlotController.ControllerDetected -= ApplyDropSlotInputPolicy;
                _hookedDropSlotController.MouseDetected -= ApplyDropSlotInputPolicy;
                _hookedDropSlotController = null;
            }

            base._ExitTree();
        }

        private void ApplyDropSlotInputPolicy()
        {
            var directionalNavigation = Sts2InputCompat.IsUsingDirectionalNavigation;
            MouseFilter = directionalNavigation ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
        }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            var canDrop = _owner.CanAcceptDrop(data);
            _owner.SetActiveDropSlot(this, canDrop);
            return canDrop;
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            _owner.HandleDrop(data, TargetIndex);
        }

        public override void _Notification(int what)
        {
            if (what == NotificationDragEnd)
                _owner.ClearActiveDropSlot();
        }

        internal void SetHighlighted(bool highlighted)
        {
            AddThemeStyleboxOverride("panel", CreateStyle(highlighted));
        }

        private static StyleBoxFlat CreateStyle(bool highlighted)
        {
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.choiceCenter.layout.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            BoxEdges border = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.choiceCenter.layout.borderWidth.left", 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.choiceCenter.layout.borderWidth.top",
                    highlighted ? 1 : 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.choiceCenter.layout.borderWidth.right", 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.choiceCenter.layout.borderWidth.bottom",
                    highlighted ? 1 : 0));
            BoxEdges padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.choiceCenter.layout.padding.left", 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.choiceCenter.layout.padding.top",
                    highlighted ? 1 : 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.choiceCenter.layout.padding.right", 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.choiceCenter.layout.padding.bottom",
                    highlighted ? 1 : 0));
            return new()
            {
                BgColor = highlighted
                    ? RitsuShellTheme.Current.Component.ChoiceCenter.HighlightTop
                    : RitsuShellTheme.Current.Color.Transparent,
                BorderColor = highlighted
                    ? RitsuShellTheme.Current.Component.ChoiceCenter.HighlightBottom
                    : RitsuShellTheme.Current.Color.Transparent,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }
    }

    internal sealed partial class ModSettingsListItemCard<TItem> : PanelContainer
    {
        private readonly bool _isCollapsible;
        private readonly ModSettingsListControl<TItem> _owner;
        private bool _collapsed;
        private ModSettingsDragHandle? _dragHandle;
        private PanelContainer? _editorSurface;
        private MegaRichTextLabel? _plainSubtitleLabel;
        private MegaRichTextLabel? _plainTitleLabel;
        private ModSettingsCollapsibleHeaderButton? _toggleButton;

        public ModSettingsListItemCard(
            ModSettingsListControl<TItem> owner,
            int index,
            string title,
            string? subtitle,
            ModSettingsListItemContext<TItem> itemContext,
            Control? editorContent,
            bool collapsible,
            bool startCollapsed,
            Control? headerAccessory)
        {
            _owner = owner;
            ItemContext = itemContext;
            _isCollapsible = collapsible && editorContent != null;
            _collapsed = _isCollapsible && itemContext.GetRowState("collapsed", startCollapsed);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            MouseFilter = MouseFilterEnum.Stop;
            AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListItemCardStyle(index == 0));

            var outer = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Begin,
            };
            outer.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.listItem.layout.outerSeparation", 8));
            AddChild(outer);

            var drag = new ModSettingsDragHandle(() => itemContext.Index,
                () => owner.CreateDragData(itemContext.Index));
            _dragHandle = drag;
            outer.AddChild(drag);

            var root = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.listItem.layout.rootSeparation", 8));
            outer.AddChild(root);

            var headerRow = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            headerRow.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.listItem.layout.headerSeparation", 8));
            root.AddChild(headerRow);

            if (_isCollapsible)
            {
                _toggleButton = new(title, subtitle, ToggleCollapsed)
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };
                headerRow.AddChild(_toggleButton);
            }
            else
            {
                var header = new HBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                    Alignment = BoxContainer.AlignmentMode.Center,
                };
                header.AddThemeConstantOverride("separation",
                    RitsuShellThemeLayoutResolver.ResolveInt("components.listItem.layout.headerInnerSeparation", 8));
                headerRow.AddChild(header);

                var textColumn = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                textColumn.AddThemeConstantOverride("separation",
                    RitsuShellThemeLayoutResolver.ResolveInt("components.listItem.layout.textSeparation", 2));
                header.AddChild(textColumn);

                var titleLabel = ModSettingsUiFactory.CreateSectionTitle(title);
                textColumn.AddChild(titleLabel);
                _plainTitleLabel = titleLabel;

                var subtitleLabel = ModSettingsUiFactory.CreateInlineDescription(subtitle ?? string.Empty);
                subtitleLabel.Visible = !string.IsNullOrWhiteSpace(subtitle);
                textColumn.AddChild(subtitleLabel);
                _plainSubtitleLabel = subtitleLabel;
            }

            var actions = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            actions.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.listItem.layout.actionsSeparation", 8));
            headerRow.AddChild(actions);

            if (headerAccessory != null)
                actions.AddChild(headerAccessory);

            var actionsButton = new ModSettingsActionsButton(
                ModSettingsUiFactory.BuildListItemMenuActions(owner.UiContext, itemContext,
                    owner.EntryMenuCapabilities),
                itemContext.RequestRefresh)
            {
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            actions.AddChild(actionsButton);
            ModSettingsUiFactory.AttachContextMenuTargets(this, outer, actionsButton);

            if (editorContent == null) return;
            _editorSurface = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Visible = !_collapsed,
            };
            _editorSurface.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListEditorSurfaceStyle());
            root.AddChild(_editorSurface);
            _editorSurface.AddChild(editorContent);
            ApplyCollapsedState();
        }

        public ModSettingsListItemCard()
        {
            _owner = null!;
            ItemContext = null!;
        }

        internal ModSettingsListItemContext<TItem> ItemContext { get; }

        internal void SyncRowChrome(int logicalIndex, string title, string? subtitle, bool isFirstRow)
        {
            AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListItemCardStyle(isFirstRow));
            _dragHandle?.RefreshIndexDisplay();

            if (_isCollapsible)
            {
                _toggleButton?.SetTexts(title, subtitle);
            }
            else
            {
                _plainTitleLabel?.SetTextAutoSize(title);
                if (_plainSubtitleLabel == null) return;
                _plainSubtitleLabel.SetTextAutoSize(subtitle ?? string.Empty);
                _plainSubtitleLabel.Visible = !string.IsNullOrWhiteSpace(subtitle);
            }
        }

        private void ToggleCollapsed()
        {
            if (!_isCollapsible)
                return;
            _collapsed = !_collapsed;
            ItemContext.SetRowState("collapsed", _collapsed);
            ApplyCollapsedState();
        }

        private void ApplyCollapsedState()
        {
            _editorSurface?.SetDeferred(CanvasItem.PropertyName.Visible, !_collapsed);
            _toggleButton?.SetSelected(!_collapsed);
        }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            if (!_owner.CanAcceptDrop(data))
                return false;

            var i = ItemContext.Index;
            _owner.PreviewDropAtIndex(atPosition.Y < Size.Y * 0.5f ? i : i + 1);
            return true;
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            var i = ItemContext.Index;
            _owner.DropAtIndex(data, atPosition.Y < Size.Y * 0.5f ? i : i + 1);
        }
    }
}
