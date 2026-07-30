using System.Collections.Concurrent;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the refresh and persistence operations available to settings UI actions registered through
    ///         <see cref="ModSettingsUiActionRegistry" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供通过 <see cref="ModSettingsUiActionRegistry" /> 注册的设置界面操作可用的刷新与持久化功能。
    ///     </para>
    /// </summary>
    public interface IModSettingsUiActionHost
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Requests a deferred rebuild of the settings UI, such as after changing a list's structure.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         请求延迟重建设置界面，例如在改变列表结构后使用。
        ///     </para>
        /// </summary>
        void RequestRefresh();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Marks a binding as changed so it is saved during the next persistence flush.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将绑定标记为已更改，使其在下一次持久化刷新时保存。
        ///     </para>
        /// </summary>
        /// <param name="binding">
        ///     <para xml:lang="en">The binding whose persisted value changed.</para>
        ///     <para xml:lang="zh-CN">持久化值已更改的绑定。</para>
        /// </param>
        void MarkDirty(IModSettingsBinding binding);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Requests a deferred UI rebuild that also invokes every registered refresh callback on the current
        ///         page once. Use this after changing several fields or a shared data model in one operation.
        ///     </para>
        ///     <para xml:lang="en">
        ///         This does not schedule persistence. Call <see cref="MarkDirty" /> for each changed binding that must
        ///         be saved.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         请求延迟重建界面，并在重建时调用当前页面上每个已注册的刷新回调一次。适用于一次操作更改多个字段
        ///         或共享数据模型之后。
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         此方法不会安排持久化；需要保存的每个已更改绑定仍须调用 <see cref="MarkDirty" />。
        ///     </para>
        /// </summary>
        void RequestRefreshAfterDataModelBatchChange()
        {
            RequestRefresh();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines stable IDs for built-in settings menu actions. Extensions may use these IDs by convention, but
    ///         the registry does not enforce uniqueness.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义内置设置菜单操作的稳定 ID。扩展可依约定使用这些 ID，但注册表不会强制其唯一。
    ///     </para>
    /// </summary>
    public static class ModSettingsStandardActionIds
    {
        /// <summary>
        ///     <para xml:lang="en">Resets a binding to its default value.</para>
        ///     <para xml:lang="zh-CN">将绑定重置为默认值。</para>
        /// </summary>
        public const string ResetToDefault = "ritsulib.settings.resetDefault";

        /// <summary>
        ///     <para xml:lang="en">Copies the current value to a clipboard envelope.</para>
        ///     <para xml:lang="zh-CN">将当前值复制到剪贴板信封。</para>
        /// </summary>
        public const string Copy = "ritsulib.settings.copy";

        /// <summary>
        ///     <para xml:lang="en">Pastes a value from a clipboard envelope into a binding.</para>
        ///     <para xml:lang="zh-CN">将剪贴板信封中的值粘贴到绑定。</para>
        /// </summary>
        public const string Paste = "ritsulib.settings.paste";

        /// <summary>
        ///     <para xml:lang="en">Moves a list item up.</para>
        ///     <para xml:lang="zh-CN">向上移动列表项。</para>
        /// </summary>
        public const string MoveUp = "ritsulib.settings.moveUp";

        /// <summary>
        ///     <para xml:lang="en">Moves a list item down.</para>
        ///     <para xml:lang="zh-CN">向下移动列表项。</para>
        /// </summary>
        public const string MoveDown = "ritsulib.settings.moveDown";

        /// <summary>
        ///     <para xml:lang="en">Duplicates a list item.</para>
        ///     <para xml:lang="zh-CN">复制列表项。</para>
        /// </summary>
        public const string Duplicate = "ritsulib.settings.duplicate";

        /// <summary>
        ///     <para xml:lang="en">Removes a list item.</para>
        ///     <para xml:lang="zh-CN">移除列表项。</para>
        /// </summary>
        public const string Remove = "ritsulib.settings.remove";

        /// <summary>
        ///     <para xml:lang="en">Copies all setting snapshots on a page.</para>
        ///     <para xml:lang="zh-CN">复制页面中的全部设置快照。</para>
        /// </summary>
        public const string PageCopy = "ritsulib.settings.page.copy";

        /// <summary>
        ///     <para xml:lang="en">Pastes setting snapshots into an entire page.</para>
        ///     <para xml:lang="zh-CN">将设置快照粘贴到整个页面。</para>
        /// </summary>
        public const string PagePaste = "ritsulib.settings.page.paste";

        /// <summary>
        ///     <para xml:lang="en">Resets every binding with a default value on a settings page.</para>
        ///     <para xml:lang="zh-CN">重置设置页面中每个具有默认值的绑定。</para>
        /// </summary>
        public const string PageResetToDefault = "ritsulib.settings.page.resetDefault";

        /// <summary>
        ///     <para xml:lang="en">Copies all setting snapshots in one section.</para>
        ///     <para xml:lang="zh-CN">复制一个节中的全部设置快照。</para>
        /// </summary>
        public const string SectionCopy = "ritsulib.settings.section.copy";

        /// <summary>
        ///     <para xml:lang="en">Pastes setting snapshots into one section.</para>
        ///     <para xml:lang="zh-CN">将设置快照粘贴到一个节。</para>
        /// </summary>
        public const string SectionPaste = "ritsulib.settings.section.paste";

        /// <summary>
        ///     <para xml:lang="en">Resets every binding with a default value in a settings section.</para>
        ///     <para xml:lang="zh-CN">重置设置节中每个具有默认值的绑定。</para>
        /// </summary>
        public const string SectionResetToDefault = "ritsulib.settings.section.resetDefault";
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the target page and host for a page-level settings UI action.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供页面级设置界面操作的目标页面与宿主。
    ///     </para>
    /// </summary>
    /// <param name="page">
    ///     <para xml:lang="en">The target settings page.</para>
    ///     <para xml:lang="zh-CN">目标设置页面。</para>
    /// </param>
    /// <param name="host">
    ///     <para xml:lang="en">The host used to request refresh and persistence operations.</para>
    ///     <para xml:lang="zh-CN">用于请求刷新与持久化操作的宿主。</para>
    /// </param>
    public sealed class ModSettingsPageUiContext(ModSettingsPage page, IModSettingsUiActionHost host)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the page targeted by the action.</para>
        ///     <para xml:lang="zh-CN">获取操作的目标页面。</para>
        /// </summary>
        public ModSettingsPage Page { get; } = page ?? throw new ArgumentNullException(nameof(page));

        /// <summary>
        ///     <para xml:lang="en">Gets the host for refresh and persistence operations.</para>
        ///     <para xml:lang="zh-CN">获取用于刷新与持久化操作的宿主。</para>
        /// </summary>
        public IModSettingsUiActionHost Host { get; } = host ?? throw new ArgumentNullException(nameof(host));
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the target page, section, and host for a section-level settings UI action.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供节级设置界面操作的目标页面、节与宿主。
    ///     </para>
    /// </summary>
    /// <param name="page">
    ///     <para xml:lang="en">The page that owns the target section.</para>
    ///     <para xml:lang="zh-CN">目标节所属的页面。</para>
    /// </param>
    /// <param name="section">
    ///     <para xml:lang="en">The target settings section.</para>
    ///     <para xml:lang="zh-CN">目标设置节。</para>
    /// </param>
    /// <param name="host">
    ///     <para xml:lang="en">The host used to request refresh and persistence operations.</para>
    ///     <para xml:lang="zh-CN">用于请求刷新与持久化操作的宿主。</para>
    /// </param>
    public sealed class ModSettingsSectionUiContext(
        ModSettingsPage page,
        ModSettingsSection section,
        IModSettingsUiActionHost host)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the page that owns the target section.</para>
        ///     <para xml:lang="zh-CN">获取目标节所属的页面。</para>
        /// </summary>
        public ModSettingsPage Page { get; } = page ?? throw new ArgumentNullException(nameof(page));

        /// <summary>
        ///     <para xml:lang="en">Gets the section targeted by the action.</para>
        ///     <para xml:lang="zh-CN">获取操作的目标节。</para>
        /// </summary>
        public ModSettingsSection Section { get; } = section ?? throw new ArgumentNullException(nameof(section));

        /// <summary>
        ///     <para xml:lang="en">Gets the host for refresh and persistence operations.</para>
        ///     <para xml:lang="zh-CN">获取用于刷新与持久化操作的宿主。</para>
        /// </summary>
        public IModSettingsUiActionHost Host { get; } = host ?? throw new ArgumentNullException(nameof(host));
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Specifies whether a clipboard operation covers only a binding's immediate value or its supported nested
    ///         data as well.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         指定剪贴板操作仅包含绑定的直接值，还是同时包含其支持的嵌套数据。
    ///     </para>
    /// </summary>
    public enum ModSettingsClipboardScope
    {
        /// <summary>
        ///     <para xml:lang="en">Includes only the binding's immediate value.</para>
        ///     <para xml:lang="zh-CN">仅包含绑定的直接值。</para>
        /// </summary>
        Self = 0,

        /// <summary>
        ///     <para xml:lang="en">Includes supported nested structured data.</para>
        ///     <para xml:lang="zh-CN">包含受支持的嵌套结构化数据。</para>
        /// </summary>
        Subtree = 1,
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes one command displayed in a settings action menu or context menu. An enablement failure is
    ///         logged and disables the command; an exception from the selected action propagates after the menu closes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述设置操作菜单或上下文菜单中显示的一项命令。启用状态计算失败时会记录异常并禁用该命令；
    ///         所选操作抛出的异常会在菜单关闭后继续传播。
    ///     </para>
    /// </summary>
    /// <param name="Id">
    ///     <para xml:lang="en">An optional stable action ID.</para>
    ///     <para xml:lang="zh-CN">可选的稳定操作 ID。</para>
    /// </param>
    /// <param name="Label">
    ///     <para xml:lang="en">The text displayed for the action.</para>
    ///     <para xml:lang="zh-CN">为操作显示的文本。</para>
    /// </param>
    /// <param name="IsEnabled">
    ///     <para xml:lang="en">A function that determines whether the action is currently enabled.</para>
    ///     <para xml:lang="zh-CN">确定操作当前是否启用的函数。</para>
    /// </param>
    /// <param name="Action">
    ///     <para xml:lang="en">The callback invoked when the action is selected.</para>
    ///     <para xml:lang="zh-CN">选择该操作时调用的回调。</para>
    /// </param>
    public sealed record ModSettingsMenuAction(string? Id, string Label, Func<bool> IsEnabled, Action Action)
    {
        /// <summary>
        ///     <para xml:lang="en">Creates an action without an ID and with a fixed enabled state.</para>
        ///     <para xml:lang="zh-CN">创建不带 ID 且启用状态固定的操作。</para>
        /// </summary>
        /// <param name="label">
        ///     <para xml:lang="en">The text displayed for the action.</para>
        ///     <para xml:lang="zh-CN">为操作显示的文本。</para>
        /// </param>
        /// <param name="enabled">
        ///     <para xml:lang="en">Whether the action is enabled.</para>
        ///     <para xml:lang="zh-CN">操作是否启用。</para>
        /// </param>
        /// <param name="action">
        ///     <para xml:lang="en">The callback invoked when the action is selected.</para>
        ///     <para xml:lang="zh-CN">选择该操作时调用的回调。</para>
        /// </param>
        public ModSettingsMenuAction(string label, bool enabled, Action action)
            : this(null, label, () => enabled, action)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an action without an ID and with a dynamically evaluated enabled state.</para>
        ///     <para xml:lang="zh-CN">创建不带 ID 且启用状态动态计算的操作。</para>
        /// </summary>
        /// <param name="label">
        ///     <para xml:lang="en">The text displayed for the action.</para>
        ///     <para xml:lang="zh-CN">为操作显示的文本。</para>
        /// </param>
        /// <param name="isEnabled">
        ///     <para xml:lang="en">A function that determines whether the action is currently enabled.</para>
        ///     <para xml:lang="zh-CN">确定操作当前是否启用的函数。</para>
        /// </param>
        /// <param name="action">
        ///     <para xml:lang="en">The callback invoked when the action is selected.</para>
        ///     <para xml:lang="zh-CN">选择该操作时调用的回调。</para>
        /// </param>
        public ModSettingsMenuAction(string label, Func<bool> isEnabled, Action action)
            : this(null, label, isEnabled, action)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an action with an optional stable ID and a fixed enabled state.</para>
        ///     <para xml:lang="zh-CN">创建带可选稳定 ID 且启用状态固定的操作。</para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">An optional stable action ID.</para>
        ///     <para xml:lang="zh-CN">可选的稳定操作 ID。</para>
        /// </param>
        /// <param name="label">
        ///     <para xml:lang="en">The text displayed for the action.</para>
        ///     <para xml:lang="zh-CN">为操作显示的文本。</para>
        /// </param>
        /// <param name="enabled">
        ///     <para xml:lang="en">Whether the action is enabled.</para>
        ///     <para xml:lang="zh-CN">操作是否启用。</para>
        /// </param>
        /// <param name="action">
        ///     <para xml:lang="en">The callback invoked when the action is selected.</para>
        ///     <para xml:lang="zh-CN">选择该操作时调用的回调。</para>
        /// </param>
        public ModSettingsMenuAction(string? id, string label, bool enabled, Action action)
            : this(id, label, () => enabled, action)
        {
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers callbacks that append custom actions to menus for setting rows, list items, pages, and
    ///         sections. Each menu build invokes a stable registration snapshot outside the registry lock; callbacks
    ///         registered during that invocation participate in later builds. Appender exceptions propagate.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         注册向设置行、列表项、页面与节菜单追加自定义操作的回调。每次构建菜单时都会在注册表锁外调用稳定的注册快照；
    ///         调用期间新增的回调从后续构建开始生效。追加回调抛出的异常会继续传播。
    ///     </para>
    /// </summary>
    public static class ModSettingsUiActionRegistry
    {
        private static readonly ConcurrentDictionary<Type, BindingAppenderBag> BindingAppenders = new();
        private static readonly ConcurrentDictionary<Type, ListItemAppenderBag> ListItemAppenders = new();
        private static readonly PageAppenderBag PageAppenders = new();
        private static readonly SectionAppenderBag SectionAppenders = new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a callback that appends actions for setting bindings whose value type is
        ///         <typeparamref name="TValue" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个回调，为值类型为 <typeparamref name="TValue" /> 的设置绑定追加操作。
        ///     </para>
        /// </summary>
        /// <typeparam name="TValue">
        ///     <para xml:lang="en">The exact value type handled by the callback.</para>
        ///     <para xml:lang="zh-CN">回调处理的确切值类型。</para>
        /// </typeparam>
        /// <param name="append">
        ///     <para xml:lang="en">
        ///         The callback that receives the host, target binding, and mutable action list when a menu is built.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         构建菜单时接收宿主、目标绑定与可变操作列表的回调。
        ///     </para>
        /// </param>
        public static void RegisterBindingActionAppender<TValue>(
            Action<IModSettingsUiActionHost, IModSettingsValueBinding<TValue>, List<ModSettingsMenuAction>> append)
        {
            ArgumentNullException.ThrowIfNull(append);
            BindingAppenders.GetOrAdd(typeof(TValue), _ => new()).Add(append);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a callback that appends actions for list rows whose item type is
        ///         <typeparamref name="TItem" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个回调，为元素类型为 <typeparamref name="TItem" /> 的列表行追加操作。
        ///     </para>
        /// </summary>
        /// <typeparam name="TItem">
        ///     <para xml:lang="en">The exact list item type handled by the callback.</para>
        ///     <para xml:lang="zh-CN">回调处理的确切列表元素类型。</para>
        /// </typeparam>
        /// <param name="append">
        ///     <para xml:lang="en">
        ///         The callback that receives the host, target item context, and mutable action list when a menu is
        ///         built.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         构建菜单时接收宿主、目标元素上下文与可变操作列表的回调。
        ///     </para>
        /// </param>
        public static void RegisterListItemActionAppender<TItem>(
            Action<IModSettingsUiActionHost, ModSettingsListItemContext<TItem>, List<ModSettingsMenuAction>> append)
        {
            ArgumentNullException.ThrowIfNull(append);
            ListItemAppenders.GetOrAdd(typeof(TItem), _ => new()).Add(append);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a callback that appends page-level actions.</para>
        ///     <para xml:lang="zh-CN">注册追加页面级操作的回调。</para>
        /// </summary>
        /// <param name="append">
        ///     <para xml:lang="en">
        ///         The callback that receives the host, target page context, and mutable action list when a menu is
        ///         built.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         构建菜单时接收宿主、目标页面上下文与可变操作列表的回调。
        ///     </para>
        /// </param>
        public static void RegisterPageActionAppender(
            Action<IModSettingsUiActionHost, ModSettingsPageUiContext, List<ModSettingsMenuAction>> append)
        {
            ArgumentNullException.ThrowIfNull(append);
            PageAppenders.Add(append);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a callback that appends section-level actions.</para>
        ///     <para xml:lang="zh-CN">注册追加节级操作的回调。</para>
        /// </summary>
        /// <param name="append">
        ///     <para xml:lang="en">
        ///         The callback that receives the host, target section context, and mutable action list when a menu is
        ///         built.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         构建菜单时接收宿主、目标节上下文与可变操作列表的回调。
        ///     </para>
        /// </param>
        public static void RegisterSectionActionAppender(
            Action<IModSettingsUiActionHost, ModSettingsSectionUiContext, List<ModSettingsMenuAction>> append)
        {
            ArgumentNullException.ThrowIfNull(append);
            SectionAppenders.Add(append);
        }

        internal static void AppendBindingActions<TValue>(IModSettingsUiActionHost host,
            IModSettingsValueBinding<TValue> binding, List<ModSettingsMenuAction> list)
        {
            if (BindingAppenders.TryGetValue(typeof(TValue), out var bag))
                bag.Invoke(host, binding, list);
        }

        internal static bool HasBindingActionAppender<TValue>()
        {
            return BindingAppenders.ContainsKey(typeof(TValue));
        }

        internal static void AppendListItemActions<TItem>(IModSettingsUiActionHost host,
            ModSettingsListItemContext<TItem> itemContext, List<ModSettingsMenuAction> list)
        {
            if (ListItemAppenders.TryGetValue(typeof(TItem), out var bag))
                bag.Invoke(host, itemContext, list);
        }

        internal static void AppendPageActions(IModSettingsUiActionHost host, ModSettingsPageUiContext pageContext,
            List<ModSettingsMenuAction> list)
        {
            PageAppenders.Invoke(host, pageContext, list);
        }

        internal static void AppendSectionActions(IModSettingsUiActionHost host,
            ModSettingsSectionUiContext sectionContext,
            List<ModSettingsMenuAction> list)
        {
            SectionAppenders.Invoke(host, sectionContext, list);
        }

        private sealed class BindingAppenderBag
        {
            private readonly List<Delegate> _delegates = [];
            private readonly Lock _lock = new();

            public void Add<TValue>(
                Action<IModSettingsUiActionHost, IModSettingsValueBinding<TValue>, List<ModSettingsMenuAction>> d)
            {
                lock (_lock)
                {
                    _delegates.Add(d);
                }
            }

            public void Invoke<TValue>(IModSettingsUiActionHost host, IModSettingsValueBinding<TValue> binding,
                List<ModSettingsMenuAction> sink)
            {
                Delegate[] snapshot;
                lock (_lock)
                {
                    snapshot = [.. _delegates];
                }

                foreach (var d in snapshot)
                    ((Action<IModSettingsUiActionHost, IModSettingsValueBinding<TValue>, List<ModSettingsMenuAction>>)d)
                        (host, binding, sink);
            }
        }

        private sealed class ListItemAppenderBag
        {
            private readonly List<Delegate> _delegates = [];
            private readonly Lock _lock = new();

            public void Add<TItem>(
                Action<IModSettingsUiActionHost, ModSettingsListItemContext<TItem>, List<ModSettingsMenuAction>> d)
            {
                lock (_lock)
                {
                    _delegates.Add(d);
                }
            }

            public void Invoke<TItem>(IModSettingsUiActionHost host, ModSettingsListItemContext<TItem> itemContext,
                List<ModSettingsMenuAction> sink)
            {
                Delegate[] snapshot;
                lock (_lock)
                {
                    snapshot = [.. _delegates];
                }

                foreach (var d in snapshot)
                    ((Action<IModSettingsUiActionHost, ModSettingsListItemContext<TItem>, List<ModSettingsMenuAction>>)
                            d)
                        (host, itemContext, sink);
            }
        }

        private sealed class PageAppenderBag
        {
            private readonly
                List<Action<IModSettingsUiActionHost, ModSettingsPageUiContext, List<ModSettingsMenuAction>>>
                _delegates = [];

            private readonly Lock _lock = new();

            public void Add(Action<IModSettingsUiActionHost, ModSettingsPageUiContext, List<ModSettingsMenuAction>> d)
            {
                lock (_lock)
                {
                    _delegates.Add(d);
                }
            }

            public void Invoke(IModSettingsUiActionHost host, ModSettingsPageUiContext pageContext,
                List<ModSettingsMenuAction> sink)
            {
                Action<IModSettingsUiActionHost, ModSettingsPageUiContext, List<ModSettingsMenuAction>>[] snapshot;
                lock (_lock)
                {
                    snapshot = [.. _delegates];
                }

                foreach (var d in snapshot)
                    d(host, pageContext, sink);
            }
        }

        private sealed class SectionAppenderBag
        {
            private readonly List<Action<IModSettingsUiActionHost, ModSettingsSectionUiContext,
                    List<ModSettingsMenuAction>>>
                _delegates = [];

            private readonly Lock _lock = new();

            public void Add(
                Action<IModSettingsUiActionHost, ModSettingsSectionUiContext, List<ModSettingsMenuAction>> d)
            {
                lock (_lock)
                {
                    _delegates.Add(d);
                }
            }

            public void Invoke(IModSettingsUiActionHost host, ModSettingsSectionUiContext sectionContext,
                List<ModSettingsMenuAction> sink)
            {
                Action<IModSettingsUiActionHost, ModSettingsSectionUiContext,
                    List<ModSettingsMenuAction>>[] snapshot;
                lock (_lock)
                {
                    snapshot = [.. _delegates];
                }

                foreach (var d in snapshot)
                    d(host, sectionContext, sink);
            }
        }
    }
}
