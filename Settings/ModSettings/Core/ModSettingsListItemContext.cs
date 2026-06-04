using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Per-row API when building a custom list editor: mutate the item, clipboard, nested entries, and list chrome.
    ///     构建自定义列表编辑器时的逐行 API：可修改条目、访问剪贴板、创建嵌套条目并操作列表 chrome。
    /// </summary>
    public sealed class ModSettingsListItemContext<TItem>
    {
        private readonly Action? _duplicate;
        private readonly ListRowLiveIndex _liveIndex;
        private readonly Action? _moveDown;
        private readonly Action? _moveUp;
        private readonly Action _remove;
        private readonly Action _requestRefresh;
        private readonly ModSettingsUiContext _uiContext;
        private readonly Action<TItem> _update;

        internal ModSettingsListItemContext(
            ModSettingsUiContext uiContext,
            IModSettingsValueBinding<TItem> binding,
            string rowStateKey,
            ListRowLiveIndex liveIndex,
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
            RowStateKey = rowStateKey;
            _liveIndex = liveIndex;
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
        ///     Stable state key for transient per-row UI state.
        ///     临时逐行 UI 状态使用的稳定状态键。
        /// </summary>
        public string RowStateKey { get; }

        /// <summary>
        ///     Zero-based index of this row in the list.
        ///     此行在列表中的从零开始索引。
        /// </summary>
        public int Index => _liveIndex.Value;

        /// <summary>
        ///     Total number of rows in the list.
        ///     列表中的总行数。
        /// </summary>
        public int ItemCount { get; private set; }

        /// <summary>
        ///     Current item snapshot for this row.
        ///     此行当前条目的快照。
        /// </summary>
        public TItem Item { get; private set; }

        /// <summary>
        ///     True when the row can move toward the start.
        ///     当此行可以向列表开头移动时为 true。
        /// </summary>
        public bool CanMoveUp => Index > 0;

        /// <summary>
        ///     True when the row can move toward the end.
        ///     当此行可以向列表末尾移动时为 true。
        /// </summary>
        public bool CanMoveDown => Index < ItemCount - 1;

        /// <summary>
        ///     Binding scoped to this row’s value (structured clipboard when implemented).
        ///     作用域限定到此行值的绑定（实现时支持结构化剪贴板）。
        /// </summary>
        public IModSettingsValueBinding<TItem> Binding { get; }

        /// <summary>
        ///     True when <see cref="Binding" /> exposes structured copy/paste.
        ///     当 <see cref="Binding" /> 公开结构化复制 / 粘贴时为 true。
        /// </summary>
        public bool SupportsStructuredClipboard => Binding is IStructuredModSettingsValueBinding<TItem>;

        internal void SyncRowListState(int index, int itemCount, TItem item)
        {
            _liveIndex.Value = index;
            ItemCount = itemCount;
            Item = item;
        }

        /// <summary>
        ///     Writes <paramref name="item" /> back into the list at <see cref="Index" />.
        ///     将 <paramref name="item" /> 写回列表的 <see cref="Index" /> 位置。
        /// </summary>
        public void Update(TItem item)
        {
            _update(item);
        }

        /// <summary>
        ///     Removes this row from the list.
        ///     从列表中移除此行。
        /// </summary>
        public void Remove()
        {
            _remove();
        }

        /// <summary>
        ///     Moves the row up when <see cref="CanMoveUp" />.
        ///     当 <see cref="CanMoveUp" /> 时将该行上移。
        /// </summary>
        public void MoveUp()
        {
            _moveUp?.Invoke();
        }

        /// <summary>
        ///     Moves the row down when <see cref="CanMoveDown" />.
        ///     当 <see cref="CanMoveDown" /> 时将该行下移。
        /// </summary>
        public void MoveDown()
        {
            _moveDown?.Invoke();
        }

        /// <summary>
        ///     Duplicates the row when supported by the list host.
        ///     当列表宿主支持时复制此行。
        /// </summary>
        public void Duplicate()
        {
            _duplicate?.Invoke();
        }

        /// <summary>
        ///     Requests a deferred rebuild of the list UI.
        ///     请求延迟重建列表 UI。
        /// </summary>
        public void RequestRefresh()
        {
            _requestRefresh();
        }

        /// <summary>
        ///     Reads transient row UI state for the current settings session.
        ///     读取当前设置会话中的临时行 UI 状态。
        /// </summary>
        public TValue GetRowState<TValue>(string key, TValue fallback = default!)
        {
            if (_uiContext.TryGetRowState(RowStateKey, key, out TValue? value) && value is not null)
                return value;
            return fallback;
        }

        /// <summary>
        ///     Stores transient row UI state for the current settings session.
        ///     存储当前设置会话中的临时行 UI 状态。
        /// </summary>
        public void SetRowState<TValue>(string key, TValue value)
        {
            _uiContext.SetRowState(RowStateKey, key, value);
        }

        /// <summary>
        ///     Copies <see cref="Item" /> using structured clipboard when available.
        ///     在可用时使用结构化剪贴板复制 <see cref="Item" />。
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
        ///     返回当前剪贴板内容对该行类型和适配器是否可粘贴。
        /// </summary>
        public bool CanPasteFromClipboard()
        {
            return Binding is IStructuredModSettingsValueBinding<TItem> structured &&
                   ModSettingsClipboardOperations.CanPasteBindingValue(Binding, structured.Adapter);
        }

        /// <summary>
        ///     Pastes into this row and calls <see cref="Update" /> on success; shows UI feedback on failure.
        ///     粘贴到此行，并在成功时调用 <see cref="Update" />；失败时显示 UI 反馈。
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
        ///     将 <typeparamref name="TItem" /> 的子字段投影为自己的绑定（嵌套编辑器）。
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
        ///     在此行的 UI 上下文下实例化任意 <see cref="ModSettingsEntryDefinition" />。
        /// </summary>
        public Control CreateEntry(ModSettingsEntryDefinition entry)
        {
            return entry.CreateControl(_uiContext);
        }

        /// <summary>
        ///     Convenience wrapper that builds a nested list entry for <typeparamref name="TChild" />.
        ///     用于为 <typeparamref name="TChild" /> 构建嵌套列表条目的便捷 wrapper。
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
                addButtonText ?? ModSettingsLocalization.Text("button.add", "Add"),
                description,
                false,
                false,
                null));
        }

        internal sealed class ListRowLiveIndex
        {
            public int Value;
        }
    }
}
