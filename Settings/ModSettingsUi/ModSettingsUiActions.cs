using System.Collections.Concurrent;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Minimal host exposed when registering actions with <see cref="ModSettingsUiActionRegistry" /> (refresh and dirty
    ///     marking).
    /// </summary>
    public interface IModSettingsUiActionHost
    {
        /// <summary>
        ///     Requests a deferred UI rebuild (e.g. after list mutation).
        /// </summary>
        void RequestRefresh();

        /// <summary>
        ///     Marks <paramref name="binding" /> dirty so persistence runs on the next flush.
        /// </summary>
        void MarkDirty(IModSettingsBinding binding);

        /// <summary>
        ///     Schedules the next deferred refresh as a full binding pass for the current page: every
        ///     <c>RegisterRefresh</c> callback on that page runs once, without requiring <see cref="MarkDirty" /> per
        ///     binding. Use after batch-mutating many fields or a shared backing object. Persisted keys still need
        ///     <see cref="MarkDirty" /> on each binding you changed so <see cref="IModSettingsBinding.Save" /> runs.
        /// </summary>
        void RequestRefreshAfterDataModelBatchChange()
        {
            RequestRefresh();
        }
    }

    /// <summary>
    ///     Stable ids for built-in settings menu items (convention for extensions; not guaranteed unique).
    /// </summary>
    public static class ModSettingsStandardActionIds
    {
        /// <summary>
        ///     Reset binding to its default value.
        /// </summary>
        public const string ResetToDefault = "ritsulib.settings.resetDefault";

        /// <summary>
        ///     Copy current value to the clipboard envelope.
        /// </summary>
        public const string Copy = "ritsulib.settings.copy";

        /// <summary>
        ///     Paste from the clipboard envelope into the binding.
        /// </summary>
        public const string Paste = "ritsulib.settings.paste";

        /// <summary>
        ///     Move a list item up.
        /// </summary>
        public const string MoveUp = "ritsulib.settings.moveUp";

        /// <summary>
        ///     Move a list item down.
        /// </summary>
        public const string MoveDown = "ritsulib.settings.moveDown";

        /// <summary>
        ///     Duplicate a list item.
        /// </summary>
        public const string Duplicate = "ritsulib.settings.duplicate";

        /// <summary>
        ///     Remove a list item.
        /// </summary>
        public const string Remove = "ritsulib.settings.remove";

        /// <summary>
        ///     Copy an entire settings page (chrome clipboard).
        /// </summary>
        public const string PageCopy = "ritsulib.settings.page.copy";

        /// <summary>
        ///     Paste an entire settings page (chrome clipboard).
        /// </summary>
        public const string PagePaste = "ritsulib.settings.page.paste";

        /// <summary>
        ///     Copy a single section (chrome clipboard).
        /// </summary>
        public const string SectionCopy = "ritsulib.settings.section.copy";

        /// <summary>
        ///     Paste into a single section (chrome clipboard).
        /// </summary>
        public const string SectionPaste = "ritsulib.settings.section.paste";
    }

    /// <summary>
    ///     Action context for a settings page (no binding).
    /// </summary>
    public sealed class ModSettingsPageUiContext(ModSettingsPage page, IModSettingsUiActionHost host)
    {
        /// <summary>
        ///     Page being targeted by page-level actions.
        /// </summary>
        public ModSettingsPage Page { get; } = page;

        /// <summary>
        ///     Host for refresh and dirty propagation.
        /// </summary>
        public IModSettingsUiActionHost Host { get; } = host;
    }

    /// <summary>
    ///     Action context for a settings section (no standalone binding).
    /// </summary>
    public sealed class ModSettingsSectionUiContext(
        ModSettingsPage page,
        ModSettingsSection section,
        IModSettingsUiActionHost host)
    {
        /// <summary>
        ///     Owning page.
        /// </summary>
        public ModSettingsPage Page { get; } = page;

        /// <summary>
        ///     Section receiving section-level actions.
        /// </summary>
        public ModSettingsSection Section { get; } = section;

        /// <summary>
        ///     Host for refresh and dirty propagation.
        /// </summary>
        public IModSettingsUiActionHost Host { get; } = host;
    }

    /// <summary>
    ///     How much of a value subtree copy/paste should include (binding self vs. nested structure).
    /// </summary>
    public enum ModSettingsClipboardScope
    {
        /// <summary>
        ///     Only the immediate binding value.
        /// </summary>
        Self = 0,

        /// <summary>
        ///     Include nested structured data where supported.
        /// </summary>
        Subtree = 1,
    }

    /// <summary>
    ///     One command in the settings Actions menu or context menu.
    /// </summary>
    public sealed record ModSettingsMenuAction(string? Id, string Label, Func<bool> IsEnabled, Action Action)
    {
        /// <summary>
        ///     Creates an action with a fixed enabled flag.
        /// </summary>
        public ModSettingsMenuAction(string label, bool enabled, Action action)
            : this(null, label, () => enabled, action)
        {
        }

        /// <summary>
        ///     Creates an action with a dynamic enabled predicate.
        /// </summary>
        public ModSettingsMenuAction(string label, Func<bool> isEnabled, Action action)
            : this(null, label, isEnabled, action)
        {
        }

        /// <summary>
        ///     Creates an action with optional stable <paramref name="id" /> and fixed enabled flag.
        /// </summary>
        public ModSettingsMenuAction(string? id, string label, bool enabled, Action action)
            : this(id, label, () => enabled, action)
        {
        }
    }

    /// <summary>
    ///     Appends custom menu items for binding rows, list items, pages, and sections.
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
        /// </summary>
        public static void RegisterBindingActionAppender<TValue>(
            Action<IModSettingsUiActionHost, IModSettingsValueBinding<TValue>, List<ModSettingsMenuAction>> append)
        {
            BindingAppenders.GetOrAdd(typeof(TValue), _ => new()).Add(append);
        }

        /// <summary>
        ///     Registers a callback that appends menu items for list rows of item type
        ///     <typeparamref name="TItem" />.
        /// </summary>
        public static void RegisterListItemActionAppender<TItem>(
            Action<IModSettingsUiActionHost, ModSettingsListItemContext<TItem>, List<ModSettingsMenuAction>> append)
        {
            ListItemAppenders.GetOrAdd(typeof(TItem), _ => new()).Add(append);
        }

        /// <summary>
        ///     Registers a callback that appends page-level menu items.
        /// </summary>
        public static void RegisterPageActionAppender(
            Action<IModSettingsUiActionHost, ModSettingsPageUiContext, List<ModSettingsMenuAction>> append)
        {
            ArgumentNullException.ThrowIfNull(append);
            PageAppenders.Add(append);
        }

        /// <summary>
        ///     Registers a callback that appends section-level menu items.
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
