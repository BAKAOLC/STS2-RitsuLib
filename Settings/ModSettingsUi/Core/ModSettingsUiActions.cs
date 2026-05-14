using System.Collections.Concurrent;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Minimal host exposed when registering actions with <see cref="ModSettingsUiActionRegistry" /> (refresh and dirty
    ///     marking).
    ///     使用 <see cref="ModSettingsUiActionRegistry" /> 注册动作时公开的最小 host（刷新和脏标记）。
    /// </summary>
    public interface IModSettingsUiActionHost
    {
        /// <summary>
        ///     Requests a deferred UI rebuild (e.g. after list mutation).
        ///     请求延迟 UI 重建（例如列表变更后）。
        /// </summary>
        void RequestRefresh();

        /// <summary>
        ///     Marks <paramref name="binding" /> dirty so persistence runs on the next flush.
        ///     将 <paramref name="binding" /> 标记为脏，使持久化在下一次 flush 时运行。
        /// </summary>
        void MarkDirty(IModSettingsBinding binding);

        /// <summary>
        ///     Schedules the next deferred refresh as a full binding pass for the current page: every
        ///     <c>RegisterRefresh</c> callback on that page runs once, without requiring <see cref="MarkDirty" /> per
        ///     binding. Use after batch-mutating many fields or a shared backing object. Persisted keys still need
        ///     <see cref="MarkDirty" /> on each binding you changed so <see cref="IModSettingsBinding.Save" /> runs.
        ///     将下一次延迟刷新安排为当前页面的完整 binding 遍历：该页面上的每个
        ///     <c>RegisterRefresh</c> 回调都会运行一次，不需要对每个 binding 调用 <see cref="MarkDirty" />。
        ///     在批量变更多个字段或共享后备对象后使用。持久化键仍需要
        ///     在你更改的每个 binding 上调用 <see cref="MarkDirty" />，以便 <see cref="IModSettingsBinding.Save" /> 运行。
        /// </summary>
        void RequestRefreshAfterDataModelBatchChange()
        {
            RequestRefresh();
        }
    }

    /// <summary>
    ///     Stable ids for built-in settings menu items (convention for extensions; not guaranteed unique).
    ///     内置设置菜单项的稳定 id（供扩展遵循的约定；不保证唯一）。
    /// </summary>
    public static class ModSettingsStandardActionIds
    {
        /// <summary>
        ///     Reset binding to its default value.
        ///     将 binding 重置为默认值。
        /// </summary>
        public const string ResetToDefault = "ritsulib.settings.resetDefault";

        /// <summary>
        ///     Copy current value to the clipboard envelope.
        ///     将当前值复制到剪贴板信封。
        /// </summary>
        public const string Copy = "ritsulib.settings.copy";

        /// <summary>
        ///     Paste from the clipboard envelope into the binding.
        ///     从剪贴板信封粘贴到 binding。
        /// </summary>
        public const string Paste = "ritsulib.settings.paste";

        /// <summary>
        ///     Move a list item up.
        ///     Move a list item up.
        /// </summary>
        public const string MoveUp = "ritsulib.settings.moveUp";

        /// <summary>
        ///     Move a list item down.
        ///     Move a list item down.
        /// </summary>
        public const string MoveDown = "ritsulib.settings.moveDown";

        /// <summary>
        ///     Duplicate a list item.
        ///     Duplicate a list item.
        /// </summary>
        public const string Duplicate = "ritsulib.settings.duplicate";

        /// <summary>
        ///     Remove a list item.
        ///     移除列表项。
        /// </summary>
        public const string Remove = "ritsulib.settings.remove";

        /// <summary>
        ///     Copy an entire settings page (chrome clipboard).
        ///     Copy an entire 设置 页面 (chrome 剪贴板).
        /// </summary>
        public const string PageCopy = "ritsulib.settings.page.copy";

        /// <summary>
        ///     Paste an entire settings page (chrome clipboard).
        ///     Paste an entire 设置 页面 (chrome 剪贴板).
        /// </summary>
        public const string PagePaste = "ritsulib.settings.page.paste";

        /// <summary>
        ///     Copy a single section (chrome clipboard).
        ///     Copy a single section (chrome 剪贴板).
        /// </summary>
        public const string SectionCopy = "ritsulib.settings.section.copy";

        /// <summary>
        ///     Paste into a single section (chrome clipboard).
        ///     粘贴到单个 section（chrome 剪贴板）。
        /// </summary>
        public const string SectionPaste = "ritsulib.settings.section.paste";
    }

    /// <summary>
    ///     Action context for a settings page (no binding).
    ///     设置页面的动作上下文（无 binding）。
    /// </summary>
    public sealed class ModSettingsPageUiContext(ModSettingsPage page, IModSettingsUiActionHost host)
    {
        /// <summary>
        ///     Page being targeted by page-level actions.
        ///     页面级动作的目标页面。
        /// </summary>
        public ModSettingsPage Page { get; } = page;

        /// <summary>
        ///     Host for refresh and dirty propagation.
        ///     刷新和脏传播的宿主。
        /// </summary>
        public IModSettingsUiActionHost Host { get; } = host;
    }

    /// <summary>
    ///     Action context for a settings section (no standalone binding).
    ///     设置 section 的动作上下文（无独立 binding）。
    /// </summary>
    public sealed class ModSettingsSectionUiContext(
        ModSettingsPage page,
        ModSettingsSection section,
        IModSettingsUiActionHost host)
    {
        /// <summary>
        ///     Owning page.
        ///     Owning 页面.
        /// </summary>
        public ModSettingsPage Page { get; } = page;

        /// <summary>
        ///     Section receiving section-level actions.
        /// </summary>
        public ModSettingsSection Section { get; } = section;

        /// <summary>
        ///     Host for refresh and dirty propagation.
        ///     刷新和脏传播的宿主。
        /// </summary>
        public IModSettingsUiActionHost Host { get; } = host;
    }

    /// <summary>
    ///     How much of a value subtree copy/paste should include (binding self vs. nested structure).
    ///     值子树复制/粘贴应包含的范围（binding 自身或嵌套结构）。
    /// </summary>
    public enum ModSettingsClipboardScope
    {
        /// <summary>
        ///     Only the immediate binding value.
        ///     仅直接 binding 值。
        /// </summary>
        Self = 0,

        /// <summary>
        ///     Include nested structured data where supported.
        ///     在支持时包含嵌套结构化数据。
        /// </summary>
        Subtree = 1,
    }

    /// <summary>
    ///     One command in the settings Actions menu or context menu.
    ///     设置 Actions 菜单或上下文菜单中的一个命令。
    /// </summary>
    public sealed record ModSettingsMenuAction(string? Id, string Label, Func<bool> IsEnabled, Action Action)
    {
        /// <summary>
        ///     Creates an action with a fixed enabled flag.
        ///     创建带固定启用标志的动作。
        /// </summary>
        public ModSettingsMenuAction(string label, bool enabled, Action action)
            : this(null, label, () => enabled, action)
        {
        }

        /// <summary>
        ///     Creates an action with a dynamic enabled predicate.
        ///     创建带动态启用谓词的动作。
        /// </summary>
        public ModSettingsMenuAction(string label, Func<bool> isEnabled, Action action)
            : this(null, label, isEnabled, action)
        {
        }

        /// <summary>
        ///     Creates an action with optional stable <paramref name="id" /> and fixed enabled flag.
        ///     创建带可选稳定 <paramref name="id" /> 和固定启用标志的动作。
        /// </summary>
        public ModSettingsMenuAction(string? id, string label, bool enabled, Action action)
            : this(id, label, () => enabled, action)
        {
        }
    }

    /// <summary>
    ///     Appends custom menu items for binding rows, list items, pages, and sections.
    ///     为 binding 行、列表项、页面和 section 追加自定义菜单项。
    /// </summary>
    public static class ModSettingsUiActionRegistry
    {
        private static readonly ConcurrentDictionary<Type, BindingAppenderBag> BindingAppenders = new();
        private static readonly ConcurrentDictionary<Type, ListItemAppenderBag> ListItemAppenders = new();
        private static readonly PageAppenderBag PageAppenders = new();
        private static readonly SectionAppenderBag SectionAppenders = new();

        /// <summary>
        ///     Registers a callback that appends menu items for value bindings of type
        ///     <typeparamref name="TValue" />.
        ///     注册一个回调，为 <typeparamref name="TValue" /> 类型的值 binding
        ///     追加菜单项。
        /// </summary>
        public static void RegisterBindingActionAppender<TValue>(
            Action<IModSettingsUiActionHost, IModSettingsValueBinding<TValue>, List<ModSettingsMenuAction>> append)
        {
            BindingAppenders.GetOrAdd(typeof(TValue), _ => new()).Add(append);
        }

        /// <summary>
        ///     Registers a callback that appends menu items for list rows of item type
        ///     <typeparamref name="TItem" />.
        ///     注册一个回调，为 <typeparamref name="TItem" /> 项类型的列表行追加菜单项。
        /// </summary>
        public static void RegisterListItemActionAppender<TItem>(
            Action<IModSettingsUiActionHost, ModSettingsListItemContext<TItem>, List<ModSettingsMenuAction>> append)
        {
            ListItemAppenders.GetOrAdd(typeof(TItem), _ => new()).Add(append);
        }

        /// <summary>
        ///     Registers a callback that appends page-level menu items.
        ///     注册一个追加页面级菜单项的回调。
        /// </summary>
        public static void RegisterPageActionAppender(
            Action<IModSettingsUiActionHost, ModSettingsPageUiContext, List<ModSettingsMenuAction>> append)
        {
            ArgumentNullException.ThrowIfNull(append);
            PageAppenders.Add(append);
        }

        /// <summary>
        ///     Registers a callback that appends section-level menu items.
        ///     注册一个追加 section 级菜单项的回调。
        /// </summary>
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

            public void Add<TValue>(
                Action<IModSettingsUiActionHost, IModSettingsValueBinding<TValue>, List<ModSettingsMenuAction>> d)
            {
                _delegates.Add(d);
            }

            public void Invoke<TValue>(IModSettingsUiActionHost host, IModSettingsValueBinding<TValue> binding,
                List<ModSettingsMenuAction> sink)
            {
                foreach (var d in _delegates)
                    ((Action<IModSettingsUiActionHost, IModSettingsValueBinding<TValue>, List<ModSettingsMenuAction>>)d)
                        (host, binding, sink);
            }
        }

        private sealed class ListItemAppenderBag
        {
            private readonly List<Delegate> _delegates = [];

            public void Add<TItem>(
                Action<IModSettingsUiActionHost, ModSettingsListItemContext<TItem>, List<ModSettingsMenuAction>> d)
            {
                _delegates.Add(d);
            }

            public void Invoke<TItem>(IModSettingsUiActionHost host, ModSettingsListItemContext<TItem> itemContext,
                List<ModSettingsMenuAction> sink)
            {
                foreach (var d in _delegates)
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

            public void Add(Action<IModSettingsUiActionHost, ModSettingsPageUiContext, List<ModSettingsMenuAction>> d)
            {
                _delegates.Add(d);
            }

            public void Invoke(IModSettingsUiActionHost host, ModSettingsPageUiContext pageContext,
                List<ModSettingsMenuAction> sink)
            {
                foreach (var d in _delegates)
                    d(host, pageContext, sink);
            }
        }

        private sealed class SectionAppenderBag
        {
            private readonly List<Action<IModSettingsUiActionHost, ModSettingsSectionUiContext,
                    List<ModSettingsMenuAction>>>
                _delegates = [];

            public void Add(
                Action<IModSettingsUiActionHost, ModSettingsSectionUiContext, List<ModSettingsMenuAction>> d)
            {
                _delegates.Add(d);
            }

            public void Invoke(IModSettingsUiActionHost host, ModSettingsSectionUiContext sectionContext,
                List<ModSettingsMenuAction> sink)
            {
                foreach (var d in _delegates)
                    d(host, sectionContext, sink);
            }
        }
    }
}
