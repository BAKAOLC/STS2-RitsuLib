using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the current item, list operations, structured clipboard access, transient UI state, and nested
    ///         entry helpers to a custom list-item editor.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为自定义列表项编辑器提供当前列表项、列表操作、结构化剪贴板、临时界面状态及嵌套条目辅助方法。
    ///     </para>
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
        ///     <para xml:lang="en">Gets the key assigned to this row's transient UI state.</para>
        ///     <para xml:lang="zh-CN">获取分配给此行临时界面状态的键。</para>
        /// </summary>
        public string RowStateKey { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the row's current zero-based list index.</para>
        ///     <para xml:lang="zh-CN">获取此行当前在列表中的从零开始索引。</para>
        /// </summary>
        public int Index => _liveIndex.Value;

        /// <summary>
        ///     <para xml:lang="en">Gets the current number of items in the list.</para>
        ///     <para xml:lang="zh-CN">获取列表当前包含的列表项数量。</para>
        /// </summary>
        public int ItemCount { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Gets the item snapshot currently represented by this row.</para>
        ///     <para xml:lang="zh-CN">获取此行当前表示的列表项快照。</para>
        /// </summary>
        public TItem Item { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the row can move one position toward the start of the list.</para>
        ///     <para xml:lang="zh-CN">获取此行是否可以向列表开头移动一个位置。</para>
        /// </summary>
        public bool CanMoveUp => Index > 0;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the row can move one position toward the end of the list.</para>
        ///     <para xml:lang="zh-CN">获取此行是否可以向列表末尾移动一个位置。</para>
        /// </summary>
        public bool CanMoveDown => Index < ItemCount - 1;

        /// <summary>
        ///     <para xml:lang="en">Gets the binding scoped to this row's current list item.</para>
        ///     <para xml:lang="zh-CN">获取作用域限定为此行当前列表项的绑定。</para>
        /// </summary>
        public IModSettingsValueBinding<TItem> Binding { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether <see cref="Binding" /> supports structured copy and paste.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="Binding" /> 是否支持结构化复制与粘贴。</para>
        /// </summary>
        public bool SupportsStructuredClipboard => Binding is IStructuredModSettingsValueBinding<TItem>;

        internal void SyncRowListState(int index, int itemCount, TItem item)
        {
            _liveIndex.Value = index;
            ItemCount = itemCount;
            Item = item;
        }

        /// <summary>
        ///     <para xml:lang="en">Writes <paramref name="item" /> to the row's current <see cref="Index" />.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="item" /> 写入此行当前的 <see cref="Index" /> 位置。</para>
        /// </summary>
        public void Update(TItem item)
        {
            _update(item);
        }

        /// <summary>
        ///     <para xml:lang="en">Removes this row's current item from the list.</para>
        ///     <para xml:lang="zh-CN">从列表中移除此行当前表示的列表项。</para>
        /// </summary>
        public void Remove()
        {
            _remove();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Moves the row one position toward the start when <see cref="CanMoveUp" /> is true.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当 <see cref="CanMoveUp" /> 为 <see langword="true" /> 时，将此行向列表开头移动一个位置。
        ///     </para>
        /// </summary>
        public void MoveUp()
        {
            _moveUp?.Invoke();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Moves the row one position toward the end when <see cref="CanMoveDown" /> is true.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当 <see cref="CanMoveDown" /> 为 <see langword="true" /> 时，将此行向列表末尾移动一个位置。
        ///     </para>
        /// </summary>
        public void MoveDown()
        {
            _moveDown?.Invoke();
        }

        /// <summary>
        ///     <para xml:lang="en">Duplicates this row when the list host provides duplication support.</para>
        ///     <para xml:lang="zh-CN">当列表宿主提供复制支持时复制此行。</para>
        /// </summary>
        public void Duplicate()
        {
            _duplicate?.Invoke();
        }

        /// <summary>
        ///     <para xml:lang="en">Requests a deferred rebuild of the list UI.</para>
        ///     <para xml:lang="zh-CN">请求延迟重建列表界面。</para>
        /// </summary>
        public void RequestRefresh()
        {
            _requestRefresh();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Reads a typed value from this row's transient state for the current settings UI session.
        ///     </para>
        ///     <para xml:lang="zh-CN">从当前设置界面会话中此行的临时状态读取类型化值。</para>
        /// </summary>
        public TValue GetRowState<TValue>(string key, TValue fallback = default!)
        {
            if (_uiContext.TryGetRowState(RowStateKey, key, out TValue? value) && value is not null)
                return value;
            return fallback;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Stores a typed value in this row's transient state for the current settings UI session.
        ///     </para>
        ///     <para xml:lang="zh-CN">在当前设置界面会话中此行的临时状态内存储类型化值。</para>
        /// </summary>
        public void SetRowState<TValue>(string key, TValue value)
        {
            _uiContext.SetRowState(RowStateKey, key, value);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Copies <see cref="Item" /> through the binding's structured adapter when one is available.
        ///     </para>
        ///     <para xml:lang="zh-CN">当绑定提供结构化适配器时，通过该适配器复制 <see cref="Item" />。</para>
        /// </summary>
        public bool TryCopyToClipboard(ModSettingsClipboardScope scope = ModSettingsClipboardScope.Self)
        {
            if (Binding is not IStructuredModSettingsValueBinding<TItem> structured)
                return false;

            ModSettingsClipboardOperations.InvokeCopy(Binding, scope, structured.Adapter, Item);
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Determines whether the current clipboard payload can be pasted through this row's structured
        ///         adapter.
        ///     </para>
        ///     <para xml:lang="zh-CN">确定当前剪贴板载荷是否可通过此行的结构化适配器粘贴。</para>
        /// </summary>
        public bool CanPasteFromClipboard()
        {
            return Binding is IStructuredModSettingsValueBinding<TItem> structured &&
                   ModSettingsClipboardOperations.CanPasteBindingValue(Binding, structured.Adapter);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to paste a structured value into this row, calls <see cref="Update" /> on success, and
        ///         reports a rejected payload through the settings UI.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试将结构化值粘贴到此行；成功时调用 <see cref="Update" />，载荷被拒绝时通过设置界面报告原因。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Projects a value within <typeparamref name="TItem" /> as a child binding for a nested editor.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <typeparamref name="TItem" /> 内的值投影为供嵌套编辑器使用的子绑定。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Creates the control for <paramref name="entry" /> within this row's settings UI context.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在此行的设置界面上下文中为 <paramref name="entry" /> 创建控件。
        ///     </para>
        /// </summary>
        public Control CreateEntry(ModSettingsEntryDefinition entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return entry.CreateControl(_uiContext);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds and creates a nested list editor for <typeparamref name="TChild" /> values.
        ///     </para>
        ///     <para xml:lang="zh-CN">构建并创建用于编辑 <typeparamref name="TChild" /> 值的嵌套列表编辑器。</para>
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
