using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Per-row API when building a custom list editor: mutate the item, clipboard, nested entries, and list chrome.
    /// </summary>
    public sealed class ModSettingsListItemContext<TItem>
    {
        private readonly Action? _duplicate;
        private readonly Action? _moveDown;
        private readonly Action? _moveUp;
        private readonly Action _remove;
        private readonly Action _requestRefresh;
        private readonly ModSettingsUiContext _uiContext;
        private readonly Action<TItem> _update;

        internal ModSettingsListItemContext(
            ModSettingsUiContext uiContext,
            IModSettingsValueBinding<TItem> binding,
            int index,
            int itemCount,
            TItem item,
            Action<TItem> update,
            Action? moveUp,
            Action? moveDown,
            Action? duplicate,
            Action remove,
            Action requestRefresh)
        {
            _uiContext = uiContext;
            Binding = binding;
            Index = index;
            ItemCount = itemCount;
            Item = item;
            _update = update;
            _moveUp = moveUp;
            _moveDown = moveDown;
            _duplicate = duplicate;
            _remove = remove;
            _requestRefresh = requestRefresh;
        }

        /// <summary>
        ///     Zero-based index of this row in the list.
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     Total number of rows in the list.
        /// </summary>
        public int ItemCount { get; }

        /// <summary>
        ///     Current item snapshot for this row.
        /// </summary>
        public TItem Item { get; }

        /// <summary>
        ///     True when the row can move toward the start.
        /// </summary>
        public bool CanMoveUp => Index > 0;

        /// <summary>
        ///     True when the row can move toward the end.
        /// </summary>
        public bool CanMoveDown => Index < ItemCount - 1;

        /// <summary>
        ///     Binding scoped to this row’s value (structured clipboard when implemented).
        /// </summary>
        public IModSettingsValueBinding<TItem> Binding { get; }

        /// <summary>
        ///     True when <see cref="Binding" /> exposes structured copy/paste.
        /// </summary>
        public bool SupportsStructuredClipboard => Binding is IStructuredModSettingsValueBinding<TItem>;

        /// <summary>
        ///     Writes <paramref name="item" /> back into the list at <see cref="Index" />.
        /// </summary>
        public void Update(TItem item)
        {
            _update(item);
        }

        /// <summary>
        ///     Removes this row from the list.
        /// </summary>
        public void Remove()
        {
            _remove();
        }

        /// <summary>
        ///     Moves the row up when <see cref="CanMoveUp" />.
        /// </summary>
        public void MoveUp()
        {
            _moveUp?.Invoke();
        }

        /// <summary>
        ///     Moves the row down when <see cref="CanMoveDown" />.
        /// </summary>
        public void MoveDown()
        {
            _moveDown?.Invoke();
        }

        /// <summary>
        ///     Duplicates the row when supported by the list host.
        /// </summary>
        public void Duplicate()
        {
            _duplicate?.Invoke();
        }

        /// <summary>
        ///     Requests a deferred rebuild of the list UI.
        /// </summary>
        public void RequestRefresh()
        {
            _requestRefresh();
        }

        /// <summary>
        ///     Copies <see cref="Item" /> using structured clipboard when available.
        /// </summary>
        public bool TryCopyToClipboard(ModSettingsClipboardScope scope = ModSettingsClipboardScope.Self)
        {
            if (Binding is not IStructuredModSettingsValueBinding<TItem> structured)
                return false;

            ModSettingsClipboardOperations.InvokeCopy(Binding, scope, structured.Adapter, Item);
            return true;
        }

        /// <summary>
        ///     Returns whether paste from clipboard is valid for this row’s type and adapter.
        /// </summary>
        public bool CanPasteFromClipboard()
        {
            return Binding is IStructuredModSettingsValueBinding<TItem> structured &&
                   ModSettingsClipboardOperations.CanPasteBindingValue(Binding, structured.Adapter);
        }

        /// <summary>
        ///     Pastes into this row and calls <see cref="Update" /> on success; shows UI feedback on failure.
        /// </summary>
        public bool TryPasteFromClipboard()
        {
            if (Binding is not IStructuredModSettingsValueBinding<TItem> structured)
                return false;

            if (!ModSettingsClipboardOperations.TryPasteBindingValue(Binding, structured.Adapter, out var value,
                    out var failureReason))
            {
                _uiContext.NotifyPasteFailure(failureReason);
                return false;
            }

            Update(value);
            return true;
        }

        /// <summary>
        ///     Projects a child field of <typeparamref name="TItem" /> as its own binding (nested editors).
        /// </summary>
        public IModSettingsValueBinding<TValue> Project<TValue>(
            string dataKey,
            Func<TItem, TValue> getter,
            Func<TItem, TValue, TItem> setter,
            IStructuredModSettingsValueAdapter<TValue>? adapter = null)
        {
            return ModSettingsBindings.Project(Binding, dataKey, getter, setter, adapter);
        }

        /// <summary>
        ///     Instantiates any <see cref="ModSettingsEntryDefinition" /> under this row’s UI context.
        /// </summary>
        public Control CreateEntry(ModSettingsEntryDefinition entry)
        {
            return entry.CreateControl(_uiContext);
        }

        /// <summary>
        ///     Convenience wrapper that builds a nested list entry for <typeparamref name="TChild" />.
        /// </summary>
        public Control CreateListEditor<TChild>(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<List<TChild>> binding,
            Func<TChild> createItem,
            Func<TChild, ModSettingsText> itemLabel,
            Func<TChild, ModSettingsText?>? itemDescription = null,
            Func<ModSettingsListItemContext<TChild>, Control>? itemEditorFactory = null,
            ModSettingsText? addButtonText = null,
            ModSettingsText? description = null)
        {
            return CreateEntry(new ListModSettingsEntryDefinition<TChild>(
                id,
                label,
                binding,
                createItem,
                itemLabel,
                itemDescription,
                itemEditorFactory,
                null,
                addButtonText ?? ModSettingsText.I18N(ModSettingsLocalization.Instance, "button.add", "Add"),
                description));
        }
    }
}
