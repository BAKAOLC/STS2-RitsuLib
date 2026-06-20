using Godot;
using MegaCrit.Sts2.addons.mega_text;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Factory for reusable RitsuLib mod-settings UI chrome and controls.
    ///     可复用的 RitsuLib Mod 设置 UI chrome 与控件工厂。
    /// </summary>
    public static partial class ModSettingsUiFactory
    {
        private const string EnabledSyncMetaKey = "__ritsu_enabled_sync_attached";
        private const string EnabledSyncOriginalMouseFilterMetaKey = "__ritsu_enabled_orig_mouse_filter";
        private const string EnabledSyncOriginalProcessModeMetaKey = "__ritsu_enabled_orig_process_mode";
        private const string EnabledSyncOriginalModulateMetaKey = "__ritsu_enabled_orig_modulate";
        private const string EnabledSyncOriginalDisabledMetaKey = "__ritsu_enabled_orig_disabled";
        private const string EnabledSyncOriginalLineEditEditableMetaKey = "__ritsu_enabled_orig_line_edit_editable";
        private const string EnabledSyncOriginalTextEditEditableMetaKey = "__ritsu_enabled_orig_text_edit_editable";
        private const float DisabledOpacityFactorFallback = 0.78f;
        private const string DisabledOpacityTokenPath = "semantic.state.disabled.opacity";
        private const string DisabledTintTokenPath = "semantic.state.disabled.tint";
        private const string DisabledOverlayTokenPath = "semantic.state.disabled.overlay";
        private const string DisabledFixedTokenPath = "semantic.state.disabled.fixed";

        private const string DisabledStylePathMetaKey = "__ritsu_disabled_style_path";

        private const double ContextMenuLongPressSeconds = 0.55;
        private const string ContextMenuButtonMetaKey = "_ritsulib_context_menu_button";

        /// <summary>
        ///     Creates a themed settings sidebar navigation button.
        ///     创建主题化设置侧栏导航按钮。
        /// </summary>
        public static ModSettingsSidebarButton CreateSidebarButton(string text, Action onPressed,
            ModSettingsSidebarItemKind kind = ModSettingsSidebarItemKind.Page,
            string? prefix = null,
            int indentLevel = 0)
        {
            return new(text, onPressed, kind, prefix, indentLevel);
        }

        /// <summary>
        ///     Creates a themed divider line.
        ///     创建主题化分割线。
        /// </summary>
        public static ColorRect CreateDivider()
        {
            return new()
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.divider.layout.minSize",
                    new(0f, 2f)),
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Color = RitsuShellTheme.Current.Color.Divider,
            };
        }

        private static Control CreateSettingLine<TValue>(ModSettingsUiContext context,
            Func<string> labelProvider,
            Func<string> descriptionBodyProvider, Control valueControl, IModSettingsValueBinding<TValue> binding,
            ModSettingsText? labelRefreshSource = null,
            ModSettingsText? descriptionRefreshSource = null)
        {
            return CreateSettingLine(context, labelProvider, descriptionBodyProvider, valueControl,
                CreateEntryActionsButton(context, binding),
                labelRefreshSource, descriptionRefreshSource);
        }

        private static Control CreateSettingLine<TValue>(ModSettingsUiContext context,
            Func<string> labelProvider,
            Func<string> descriptionBodyProvider, Control valueControl, IModSettingsValueBinding<TValue> binding,
            ModSettingsMenuCapabilities capabilities,
            ModSettingsText? labelRefreshSource = null,
            ModSettingsText? descriptionRefreshSource = null)
        {
            return CreateSettingLine(context, labelProvider, descriptionBodyProvider, valueControl,
                CreateEntryActionsButton(context, binding, capabilities),
                labelRefreshSource, descriptionRefreshSource);
        }

        private static Control CreateSettingLine(ModSettingsUiContext context, Func<string> labelProvider,
            Func<string> descriptionBodyProvider, Control valueControl, Control? actionControl = null,
            ModSettingsText? labelRefreshSource = null,
            ModSettingsText? descriptionRefreshSource = null)
        {
            var line = new FastSettingLine(valueControl);
            line.Bind(context, ResolveLabelText, descriptionBodyProvider, actionControl,
                labelRefreshSource, descriptionRefreshSource);
            return line;

            string ResolveLabelText()
            {
                var s = labelProvider();
                return string.IsNullOrWhiteSpace(s)
                    ? ModSettingsLocalization.Get("entry.label.empty", "-")
                    : s;
            }
        }

        private static void AttachHostSurfaceReadOnlySync(ModSettingsUiContext context, Control valueControl,
            Control? actionControl, Func<bool>? canApply = null)
        {
            var readOnlyMask = context.GetSectionHostReadOnlyMask();
            if (readOnlyMask == ModSettingsHostSurface.None)
                return;

            Sync();
            RegisterRefreshWhenAlive(context, valueControl, Sync, ModSettingsUiRefreshSpec.Always);
            if (actionControl != null)
                RegisterRefreshWhenAlive(context, actionControl, Sync, ModSettingsUiRefreshSpec.Always);
            return;

            void Sync()
            {
                if (canApply != null && !canApply())
                    return;
                if (!GodotObject.IsInstanceValid(valueControl))
                    return;

                var locked = ModSettingsHostSurfaceResolver.IsReadOnlyOnCurrentHost(readOnlyMask);
                valueControl.ProcessMode = locked ? Node.ProcessModeEnum.Disabled : Node.ProcessModeEnum.Inherit;
                valueControl.Modulate = locked ? new(1f, 1f, 1f, 0.58f) : Colors.White;
                if (actionControl == null || !GodotObject.IsInstanceValid(actionControl)) return;
                actionControl.ProcessMode = locked ? Node.ProcessModeEnum.Disabled : Node.ProcessModeEnum.Inherit;
                actionControl.Modulate = locked ? new(1f, 1f, 1f, 0.58f) : Colors.White;
            }
        }

        internal static void AttachContextMenuTargets(Control line, Control valueControl,
            ModSettingsActionsButton button, bool overwriteExisting = true)
        {
            AttachContextMenuSurface(line, button, true, overwriteExisting);
            if (!ReferenceEquals(line, valueControl) && !line.IsAncestorOf(valueControl))
                AttachContextMenuSurface(valueControl, button, true, overwriteExisting);
        }

        private static void AttachContextMenuSurface(Control target, ModSettingsActionsButton button,
            bool attachRoot = false, bool overwriteExisting = true)
        {
            if (attachRoot || ShouldAttachContextMenuDescendant(target))
                AttachContextMenu(target, button, overwriteExisting);

            foreach (var child in target.GetChildren())
                if (child is Control childControl)
                    AttachContextMenuSurface(childControl, button, false, overwriteExisting);
        }

        private static bool ShouldAttachContextMenuDescendant(Control control)
        {
            if (control.MouseFilter != Control.MouseFilterEnum.Ignore)
                return true;

            return control is BaseButton or LineEdit or TextEdit or HSlider or OptionButton or ColorPickerButton
                or MenuButton;
        }

        internal static void AttachContextMenu(Control target, ModSettingsActionsButton button,
            bool overwriteExisting = true)
        {
            if (!overwriteExisting && target.HasMeta(ContextMenuButtonMetaKey))
                return;

            target.SetMeta(ContextMenuButtonMetaKey, button);

            if (target.HasMeta(ContextMenuAttachedMetaKey))
                return;

            target.SetMeta(ContextMenuAttachedMetaKey, true);

            if (target.MouseFilter == Control.MouseFilterEnum.Ignore)
                target.MouseFilter = Control.MouseFilterEnum.Pass;

            object? activeLongPressToken;

            target.GuiInput += @event =>
            {
                switch (@event)
                {
                    case InputEventScreenTouch touch:
                    {
                        if (touch.Pressed)
                        {
                            var currentButton = ResolveContextMenuButton(target);
                            if (currentButton == null || !CanOpenContextMenu(target, currentButton))
                            {
                                ResolveContextMenuButton(target)?.ForceCloseDropdown();
                                activeLongPressToken = null;
                                return;
                            }

                            var pendingTouchPosition =
                                target.GetGlobalTransformWithCanvas().Origin + touch.Position;
                            var token = new object();
                            activeLongPressToken = token;
                            var tree = target.GetTree();
                            var timer = tree?.CreateTimer(ContextMenuLongPressSeconds);
                            if (timer != null)
                                timer.Timeout += () =>
                                {
                                    if (!GodotObject.IsInstanceValid(target))
                                    {
                                        activeLongPressToken = null;
                                        return;
                                    }

                                    if (!ReferenceEquals(activeLongPressToken, token))
                                        return;
                                    activeLongPressToken = null;
                                    var resolveContextMenuButton = ResolveContextMenuButton(target);
                                    if (resolveContextMenuButton == null ||
                                        !CanOpenContextMenu(target, resolveContextMenuButton))
                                    {
                                        resolveContextMenuButton?.ForceCloseDropdown();
                                        return;
                                    }

                                    resolveContextMenuButton.OpenAt(pendingTouchPosition);
                                };
                        }
                        else
                        {
                            activeLongPressToken = null;
                        }

                        return;
                    }
                    case InputEventScreenDrag:
                        activeLongPressToken = null;
                        return;
                }

                if (@event is not InputEventMouseButton
                    {
                        Pressed: true,
                        ButtonIndex: MouseButton.Right,
                    })
                    return;

                var menuButton = ResolveContextMenuButton(target);
                if (menuButton == null || !CanOpenContextMenu(target, menuButton))
                {
                    menuButton?.ForceCloseDropdown();
                    return;
                }

                menuButton.OpenAt(target.GetGlobalMousePosition());
                target.GetViewport().SetInputAsHandled();
            };
        }

        private static ModSettingsActionsButton? ResolveContextMenuButton(Control target)
        {
            if (!target.HasMeta(ContextMenuButtonMetaKey))
                return null;

            return target.GetMeta(ContextMenuButtonMetaKey).AsGodotObject() as ModSettingsActionsButton;
        }

        private static bool CanOpenContextMenu(Control target, ModSettingsActionsButton button)
        {
            if (!GodotObject.IsInstanceValid(target) || !GodotObject.IsInstanceValid(button))
                return false;
            if (!button.IsVisibleInTree() || button.Disabled || button.ProcessMode == Node.ProcessModeEnum.Disabled)
                return false;
            return !IsControlEffectivelyDisabled(target);
        }

        private static bool IsControlEffectivelyDisabled(Control control)
        {
            for (Node? n = control; n != null; n = n.GetParent())
            {
                if (n is not Control c)
                    continue;
                if (c.ProcessMode == Node.ProcessModeEnum.Disabled)
                    return true;
                if (c is BaseButton { Disabled: true })
                    return true;
            }

            return false;
        }

        internal static Control? CreateEntryActionsButton<TValue>(ModSettingsUiContext context,
            IModSettingsValueBinding<TValue> binding,
            ModSettingsMenuCapabilities capabilities = ModSettingsMenuCapabilities.All)
        {
            var actions = BuildBindingActions(context, binding, capabilities);
            return actions.Count == 0 ? null : new ModSettingsActionsButton(actions, context.RequestRefresh);
        }

        private static List<ModSettingsMenuAction> BuildBindingActions<TValue>(ModSettingsUiContext context,
            IModSettingsValueBinding<TValue> binding, ModSettingsMenuCapabilities capabilities)
        {
            var actions = new List<ModSettingsMenuAction>();
            if (capabilities.HasFlag(ModSettingsMenuCapabilities.ResetToDefault) &&
                binding is IDefaultModSettingsValueBinding<TValue> defaults)
                actions.Add(new(
                    ModSettingsStandardActionIds.ResetToDefault,
                    ModSettingsLocalization.Get("button.resetDefault", "Reset to default"),
                    true,
                    () =>
                    {
                        binding.Write(defaults.CreateDefaultValue());
                        context.MarkDirty(binding);
                        context.RequestRefresh();
                    }));

            if (capabilities.HasFlag(ModSettingsMenuCapabilities.Copy))
                actions.Add(new(
                    ModSettingsStandardActionIds.Copy,
                    ModSettingsLocalization.Get("button.copy", "Copy data"),
                    true,
                    () =>
                    {
                        CopyBindingValueToClipboard(binding);
                        context.RequestRefresh();
                    }));
            if (capabilities.HasFlag(ModSettingsMenuCapabilities.Paste))
                actions.Add(new(
                    ModSettingsStandardActionIds.Paste,
                    ModSettingsLocalization.Get("button.paste", "Paste data"),
                    () => CanPasteBindingValueFromClipboard(binding),
                    () =>
                    {
                        if (!TryPasteBindingValueFromClipboard(context, binding)) return;
                        context.MarkDirty(binding);
                        context.RequestRefresh();
                    }));
            ModSettingsUiActionRegistry.AppendBindingActions(context, binding, actions);
            return actions;
        }

        internal static List<ModSettingsMenuAction> BuildListItemMenuActions<TItem>(ModSettingsUiContext context,
            ModSettingsListItemContext<TItem> itemContext)
        {
            var actions = new List<ModSettingsMenuAction>
            {
                new(ModSettingsStandardActionIds.MoveUp, ModSettingsLocalization.Get("button.moveUp", "Move up"),
                    itemContext.CanMoveUp,
                    itemContext.MoveUp),
                new(ModSettingsStandardActionIds.MoveDown, ModSettingsLocalization.Get("button.moveDown", "Move down"),
                    itemContext.CanMoveDown,
                    itemContext.MoveDown),
                new(ModSettingsStandardActionIds.Duplicate,
                    ModSettingsLocalization.Get("button.duplicate", "Duplicate"),
                    itemContext.SupportsStructuredClipboard,
                    itemContext.Duplicate),
                new(ModSettingsStandardActionIds.Copy, ModSettingsLocalization.Get("button.copy", "Copy data"),
                    itemContext.SupportsStructuredClipboard,
                    () => { itemContext.TryCopyToClipboard(); }),
                new(ModSettingsStandardActionIds.Paste, ModSettingsLocalization.Get("button.paste", "Paste data"),
                    itemContext.CanPasteFromClipboard,
                    () => { itemContext.TryPasteFromClipboard(); }),
                new(ModSettingsStandardActionIds.Remove, ModSettingsLocalization.Get("button.remove", "Remove"), true,
                    itemContext.Remove),
            };
            ModSettingsUiActionRegistry.AppendListItemActions(context, itemContext, actions);
            return actions;
        }

        internal static List<ModSettingsMenuAction> BuildPageMenuActions(ModSettingsUiContext context,
            ModSettingsPageUiContext pageContext)
        {
            var actions = new List<ModSettingsMenuAction>();
            if (pageContext.Page.MenuCapabilities.HasFlag(ModSettingsMenuCapabilities.Copy))
                actions.Add(new(ModSettingsStandardActionIds.PageCopy,
                    ModSettingsLocalization.Get("button.copy", "Copy data"),
                    true,
                    () =>
                    {
                        ModSettingsUiChromeClipboard.TryCopyPage(pageContext);
                        context.RequestRefresh();
                    }));
            if (pageContext.Page.MenuCapabilities.HasFlag(ModSettingsMenuCapabilities.Paste))
                actions.Add(new(ModSettingsStandardActionIds.PagePaste,
                    ModSettingsLocalization.Get("button.paste", "Paste data"),
                    () => ModSettingsUiChromeClipboard.CanPastePage(pageContext),
                    () =>
                    {
                        ModSettingsUiChromeClipboard.TryPastePage(pageContext);
                        context.RequestRefresh();
                    }));
            if (pageContext.Page.MenuCapabilities.HasFlag(ModSettingsMenuCapabilities.ResetToDefault) &&
                PageCanResetToDefault(pageContext.Page))
                actions.Add(new(ModSettingsStandardActionIds.PageResetToDefault,
                    ModSettingsLocalization.Get("button.resetPageDefaults", "Reset page to defaults"),
                    () => PageCanResetToDefault(pageContext.Page),
                    () => { ResetPageToDefaults(pageContext); }));
            ModSettingsUiActionRegistry.AppendPageActions(context, pageContext, actions);
            return actions;
        }

        internal static List<ModSettingsMenuAction> BuildSectionMenuActions(ModSettingsUiContext context,
            ModSettingsSectionUiContext sectionContext)
        {
            var actions = new List<ModSettingsMenuAction>();
            if (sectionContext.Section.MenuCapabilities.HasFlag(ModSettingsMenuCapabilities.Copy))
                actions.Add(new(ModSettingsStandardActionIds.SectionCopy,
                    ModSettingsLocalization.Get("button.copy", "Copy data"),
                    true,
                    () =>
                    {
                        ModSettingsUiChromeClipboard.TryCopySection(sectionContext);
                        context.RequestRefresh();
                    }));
            if (sectionContext.Section.MenuCapabilities.HasFlag(ModSettingsMenuCapabilities.Paste))
                actions.Add(new(ModSettingsStandardActionIds.SectionPaste,
                    ModSettingsLocalization.Get("button.paste", "Paste data"),
                    () => ModSettingsUiChromeClipboard.CanPasteSection(sectionContext),
                    () =>
                    {
                        ModSettingsUiChromeClipboard.TryPasteSection(sectionContext);
                        context.RequestRefresh();
                    }));
            if (sectionContext.Section.MenuCapabilities.HasFlag(ModSettingsMenuCapabilities.ResetToDefault) &&
                SectionCanResetToDefault(sectionContext.Section))
                actions.Add(new(ModSettingsStandardActionIds.SectionResetToDefault,
                    ModSettingsLocalization.Get("button.resetSectionDefaults", "Reset section to defaults"),
                    () => SectionCanResetToDefault(sectionContext.Section),
                    () => { ResetSectionToDefaults(sectionContext); }));
            ModSettingsUiActionRegistry.AppendSectionActions(context, sectionContext, actions);
            return actions;
        }

        private static bool PageCanResetToDefault(ModSettingsPage page)
        {
            return page.Sections.Any(SectionCanResetToDefault);
        }

        private static bool SectionCanResetToDefault(ModSettingsSection section)
        {
            return section.Entries.Any(entry => entry.CanResetToDefault);
        }

        private static void ResetPageToDefaults(ModSettingsPageUiContext pageContext)
        {
            var count = pageContext.Page.Sections.Sum(section =>
                ResetSectionEntriesToDefaults(pageContext.Host, section));

            if (count > 0)
                pageContext.Host.RequestRefreshAfterDataModelBatchChange();
        }

        private static void ResetSectionToDefaults(ModSettingsSectionUiContext sectionContext)
        {
            if (ResetSectionEntriesToDefaults(sectionContext.Host, sectionContext.Section) > 0)
                sectionContext.Host.RequestRefreshAfterDataModelBatchChange();
        }

        private static int ResetSectionEntriesToDefaults(IModSettingsUiActionHost host, ModSettingsSection section)
        {
            return section.Entries.Count(entry => entry.TryResetToDefault(host));
        }

        private static void CopyBindingValueToClipboard<TValue>(IModSettingsValueBinding<TValue> binding)
        {
            var adapter = ResolveClipboardAdapter(binding);
            ModSettingsClipboardOperations.InvokeCopy(binding, ModSettingsClipboardScope.Self, adapter, binding.Read());
        }

        private static bool CanPasteBindingValueFromClipboard<TValue>(IModSettingsValueBinding<TValue> binding)
        {
            var adapter = ResolveClipboardAdapter(binding);
            return ModSettingsClipboardOperations.CanPasteBindingValue(binding, adapter);
        }

        private static bool TryPasteBindingValueFromClipboard<TValue>(ModSettingsUiContext context,
            IModSettingsValueBinding<TValue> binding)
        {
            var adapter = ResolveClipboardAdapter(binding);
            if (!ModSettingsClipboardOperations.TryPasteBindingValue(binding, adapter, out var value,
                    out var failureReason))
            {
                context.NotifyPasteFailure(failureReason);
                return false;
            }

            binding.Write(value);
            return true;
        }

        internal static IStructuredModSettingsValueAdapter<TValue> ResolveClipboardAdapter<TValue>(
            IModSettingsValueBinding<TValue> binding)
        {
            return binding is IStructuredModSettingsValueBinding<TValue> structured
                ? structured.Adapter
                : ModSettingsStructuredData.Json<TValue>();
        }

        internal static string ResolveEntryLabelDisplay(ModSettingsText? label)
        {
            var s = ModSettingsUiContext.Resolve(label);
            return string.IsNullOrWhiteSpace(s)
                ? ModSettingsLocalization.Get("entry.label.empty", "—")
                : s;
        }

        private static string ResolveSectionTitleText(ModSettingsSection section)
        {
            return section.Title != null
                ? ResolveEntryLabelDisplay(section.Title)
                : ModSettingsLocalization.Get("section.default", "Section");
        }

        /// <summary>
        ///     Wraps <paramref name="inner" /> so Godot <c>Control.Visible</c> tracks <paramref name="predicate" /> on
        ///     each settings UI refresh.
        ///     包装 <paramref name="inner" />，使 Godot <c>Control.Visible</c> 在每次设置 UI 刷新时跟随 <paramref name="predicate" />。
        /// </summary>
        internal static Control MaybeWrapDynamicVisibility(ModSettingsUiContext context, Control inner,
            Func<bool>? predicate)
        {
            if (predicate == null)
                return inner;

            Apply();
            context.RegisterDynamicVisibility(inner, predicate);
            return inner;

            void Apply()
            {
                if (!GodotObject.IsInstanceValid(inner))
                    return;
                try
                {
                    var visible = predicate();
                    if (inner.Visible == visible)
                        return;
                    inner.Visible = visible;
                    FastVerticalStack.RequestAncestorLayouts(inner);
                }
                catch
                {
                    inner.Visible = true;
                    FastVerticalStack.RequestAncestorLayouts(inner);
                }
            }
        }

        private static SectionBuildPlan CreateSectionShell(ModSettingsUiContext context, ModSettingsPage page,
            ModSettingsSection section)
        {
            var sectionUiContext = new ModSettingsSectionUiContext(page, section, context);
            var sectionMenuActions = BuildSectionMenuActions(context, sectionUiContext);
            var sectionActionsButton = sectionMenuActions.Count == 0
                ? null
                : new ModSettingsActionsButton(sectionMenuActions, context.RequestRefresh);
            if (sectionActionsButton != null)
                sectionActionsButton.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

            Control built;
            Control entryHost;
            if (section.IsCollapsible)
            {
                var collapsible = new ModSettingsCollapsibleSection(
                    ResolveSectionTitleText(section),
                    section.Id,
                    section.Description != null ? ModSettingsUiContext.Resolve(section.Description) : null,
                    section.StartCollapsed,
                    [],
                    sectionActionsButton);
                if (sectionActionsButton != null)
                    AttachContextMenuTargets(collapsible, collapsible, sectionActionsButton);
                built = collapsible;
                entryHost = collapsible.ContentHost;
            }
            else
            {
                var container = new FastVerticalStack(
                    RitsuShellThemeLayoutResolver.ResolveInt("components.section.layout.separation", 8))
                {
                    Name = $"Section_{section.Id}",
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                };

                if (section.Title != null || sectionActionsButton != null)
                {
                    var headerRow = new HBoxContainer
                    {
                        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                        MouseFilter = Control.MouseFilterEnum.Ignore,
                        Alignment = BoxContainer.AlignmentMode.Center,
                    };
                    headerRow.AddThemeConstantOverride("separation",
                        RitsuShellThemeLayoutResolver.ResolveInt("components.section.layout.headerSeparation", 10));
                    if (section.Title != null)
                    {
                        var title = CreateRefreshableSectionTitle(context, section.Title,
                            () => ResolveEntryLabelDisplay(section.Title));
                        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                        headerRow.AddChild(title);
                    }
                    else
                    {
                        headerRow.AddChild(new Control
                        {
                            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                            MouseFilter = Control.MouseFilterEnum.Ignore,
                        });
                    }

                    if (sectionActionsButton != null)
                        headerRow.AddChild(sectionActionsButton);
                    container.AddChild(headerRow);
                }

                if (section.Description != null)
                    container.AddChild(CreateRefreshableDescriptionLabel(context, section.Description,
                        () => ModSettingsUiContext.Resolve(section.Description)));
                if (sectionActionsButton != null)
                    AttachContextMenuTargets(container, container, sectionActionsButton);
                built = container;
                entryHost = container;
            }

            var visibleHost = MaybeWrapDynamicVisibility(context, built, CreateSectionVisibilityPredicate(section));

            // For collapsible sections, keep the collapse toggle operable while disabling the content/actions.
            if (section.EnabledWhen == null || built is not ModSettingsCollapsibleSection collapsibleHost)
                return new(visibleHost, entryHost, sectionActionsButton,
                    built is ModSettingsCollapsibleSection lazyCollapsible && section.StartCollapsed
                        ? lazyCollapsible
                        : null);
            Apply();
            RegisterRefreshWhenAlive(context, visibleHost, Apply, ModSettingsUiRefreshSpec.Always);
            return new(visibleHost, entryHost, sectionActionsButton,
                section.StartCollapsed ? collapsibleHost : null);

            void Apply()
            {
                bool enabled;
                try
                {
                    enabled = section.EnabledWhen();
                }
                catch
                {
                    enabled = true;
                }

                collapsibleHost.SetContentEnabled(enabled);
            }
        }

        private static Func<bool>? CreateSectionVisibilityPredicate(ModSettingsSection section)
        {
            if (section.VisibleWhen == null && section.VisibleOnHostSurfaces == ModSettingsHostSurface.All)
                return null;

            if (section.VisibleOnHostSurfaces == ModSettingsHostSurface.All)
                return section.VisibleWhen;

            return ModSettingsHostSurfaceResolver.CombineVisibility(section.VisibleWhen,
                () => ModSettingsHostSurfaceResolver.IsVisibleOnCurrentHost(section.VisibleOnHostSurfaces));
        }

        private static PageBuildItem CreateEntryBuildItem(ModSettingsUiContext context, ModSettingsPage page,
            ModSettingsSection section, ModSettingsEntryDefinition entry, SectionBuildPlan sectionPlan,
            ModSettingsReusableEntryNodePool? entryNodePool)
        {
            Control control;
            context.BeginSectionSurfaceScope(page, section);
            context.BeginEntrySurfaceScope(entry);
            try
            {
                control = TryCreatePooledStandardEntry(context, entry, entryNodePool, out var pooled)
                    ? pooled
                    : entry.CreateControl(context);
                control = MaybeWrapDynamicVisibility(context, control, entry.VisibilityPredicate);
                control = MaybeWrapDynamicEnabled(context, control, entry.EnabledPredicate);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Settings] Failed to build entry '{page.ModId}:{page.Id}:{section.Id}:{entry.Id}': {ex.Message}");
                control = CreateBuildErrorPlaceholder(
                    ModSettingsLocalization.Get("entry.failed.title", "Setting failed to load"),
                    string.Format(ModSettingsLocalization.Get("entry.failed.body", "Failed to build setting '{0}'."),
                        entry.Id));
            }
            finally
            {
                context.EndEntrySurfaceScope();
                context.EndSectionSurfaceScope();
            }

            return new(control, true, sectionPlan.EntryHost, added =>
            {
                context.RegisterEntryAnchor(page, section, entry, added);
                if (sectionPlan.SectionActionsButton != null)
                    AttachContextMenuTargets(added, added, sectionPlan.SectionActionsButton, false);
            });
        }

        internal static Control MaybeWrapDynamicEnabled(ModSettingsUiContext context, Control host,
            Func<bool>? predicate)
        {
            if (predicate == null || host.HasMeta(EnabledSyncMetaKey))
                return host;
            host.SetMeta(EnabledSyncMetaKey, true);

            Apply();
            RegisterRefreshWhenAlive(context, host, Apply, ModSettingsUiRefreshSpec.Always);
            return host;

            void Apply()
            {
                if (!GodotObject.IsInstanceValid(host))
                    return;
                bool enabled;
                try
                {
                    enabled = predicate();
                }
                catch
                {
                    enabled = true;
                }

                ApplyEnabledRecursive(host, enabled);
            }
        }

        internal static void ApplyEnabledRecursive(Node node, bool enabled)
        {
            if (node is Control control)
                ApplyEnabledToControl(control, enabled);

            foreach (var child in node.GetChildren())
                if (child != null)
                    ApplyEnabledRecursive(child, enabled);
        }

        internal static void ApplyEnabledToControl(Control control, bool enabled)
        {
            if (!GodotObject.IsInstanceValid(control))
                return;

            if (!control.HasMeta(EnabledSyncOriginalMouseFilterMetaKey))
                control.SetMeta(EnabledSyncOriginalMouseFilterMetaKey, (int)control.MouseFilter);
            if (!control.HasMeta(EnabledSyncOriginalProcessModeMetaKey))
                control.SetMeta(EnabledSyncOriginalProcessModeMetaKey, (int)control.ProcessMode);
            if (!control.HasMeta(EnabledSyncOriginalModulateMetaKey))
                control.SetMeta(EnabledSyncOriginalModulateMetaKey, control.Modulate);

            switch (control)
            {
                case BaseButton button when !button.HasMeta(EnabledSyncOriginalDisabledMetaKey):
                    button.SetMeta(EnabledSyncOriginalDisabledMetaKey, button.Disabled);
                    break;
                case LineEdit lineEdit when !lineEdit.HasMeta(EnabledSyncOriginalLineEditEditableMetaKey):
                    lineEdit.SetMeta(EnabledSyncOriginalLineEditEditableMetaKey, lineEdit.Editable);
                    break;
                case TextEdit textEdit when !textEdit.HasMeta(EnabledSyncOriginalTextEditEditableMetaKey):
                    textEdit.SetMeta(EnabledSyncOriginalTextEditEditableMetaKey, textEdit.Editable);
                    break;
            }

            if (enabled)
            {
                control.MouseFilter =
                    (Control.MouseFilterEnum)(int)control.GetMeta(EnabledSyncOriginalMouseFilterMetaKey);
                control.ProcessMode = (Node.ProcessModeEnum)(int)control.GetMeta(EnabledSyncOriginalProcessModeMetaKey);
                control.Modulate = (Color)control.GetMeta(EnabledSyncOriginalModulateMetaKey);
                switch (control)
                {
                    case BaseButton btn:
                        btn.Disabled = btn.HasMeta(EnabledSyncOriginalDisabledMetaKey) &&
                                       (bool)btn.GetMeta(EnabledSyncOriginalDisabledMetaKey);
                        break;
                    case LineEdit line when line.HasMeta(EnabledSyncOriginalLineEditEditableMetaKey):
                        line.Editable = (bool)line.GetMeta(EnabledSyncOriginalLineEditEditableMetaKey);
                        break;
                    case TextEdit text when text.HasMeta(EnabledSyncOriginalTextEditEditableMetaKey):
                        text.Editable = (bool)text.GetMeta(EnabledSyncOriginalTextEditEditableMetaKey);
                        break;
                }

                return;
            }

            if (control is ModSettingsActionsButton actions)
                actions.ForceCloseDropdown();
            if (control is IModSettingsTransientPopupOwner popupOwner)
                popupOwner.ForceCloseTransientUi();

            control.ProcessMode = Node.ProcessModeEnum.Disabled;
            control.MouseFilter = Control.MouseFilterEnum.Ignore;
            var orig = (Color)control.GetMeta(EnabledSyncOriginalModulateMetaKey);
            control.Modulate = ResolveDisabledModulate(control, orig);
            switch (control)
            {
                case BaseButton b:
                    b.Disabled = true;
                    break;
                case LineEdit lineEditNow:
                    lineEditNow.Editable = false;
                    break;
                case TextEdit textEditNow:
                    textEditNow.Editable = false;
                    break;
            }
        }

        internal static void SetDisabledStylePath(Control control, string tokenBasePath)
        {
            control.SetMeta(DisabledStylePathMetaKey, tokenBasePath);
        }

        private static Color ResolveDisabledModulate(Control control, Color original)
        {
            var basePath = control.HasMeta(DisabledStylePathMetaKey)
                ? control.GetMeta(DisabledStylePathMetaKey).AsString()
                : string.Empty;

            var opacity = ResolveDisabledNumber(basePath, ".opacity", DisabledOpacityTokenPath,
                DisabledOpacityFactorFallback);

            // 1) Fixed ARGB override (explicit, strongest).
            if (TryResolveDisabledColor(basePath, ".fixed", DisabledFixedTokenPath, out var fixedArgb))
                return new(fixedArgb.R, fixedArgb.G, fixedArgb.B, fixedArgb.A * opacity);

            // 2) Overlay: lerp original -> overlay by overlay alpha.
            if (TryResolveDisabledColor(basePath, ".overlay", DisabledOverlayTokenPath, out var overlay) &&
                overlay.A > 0.001f)
            {
                var t = overlay.A;
                var r = Mathf.Lerp(original.R, overlay.R, t);
                var g = Mathf.Lerp(original.G, overlay.G, t);
                var b = Mathf.Lerp(original.B, overlay.B, t);
                return new(r, g, b, original.A * opacity);
            }

            // 3) Tint: multiply RGB.
            if (TryResolveDisabledColor(basePath, ".tint", DisabledTintTokenPath, out var tint))
                return new(original.R * tint.R, original.G * tint.G, original.B * tint.B, original.A * opacity);

            // 4) Opacity only.
            return new(original.R, original.G, original.B, original.A * opacity);
        }

        private static bool TryResolveDisabledColor(string basePath, string suffix, string fallbackPath,
            out Color color)
        {
            var t = RitsuShellTheme.Current;
            if (!string.IsNullOrWhiteSpace(basePath) &&
                t.TryGetColor(basePath + suffix, out color))
                return true;
            return t.TryGetColor(fallbackPath, out color);
        }

        private static float ResolveDisabledNumber(string basePath, string suffix, string fallbackPath, float fallback)
        {
            var t = RitsuShellTheme.Current;
            if (!string.IsNullOrWhiteSpace(basePath) &&
                t.TryGetNumber(basePath + suffix, out var v1) &&
                v1 is > 0.05 and <= 1.0)
                return (float)v1;

            if (t.TryGetNumber(fallbackPath, out var v) && v is > 0.05 and <= 1.0)
                return (float)v;

            return fallback;
        }

        internal static MegaRichTextLabel CreateSectionTitle(string text)
        {
            var label = CreateHeaderLabel(text, 22, HorizontalAlignment.Left, null,
                RitsuShellTheme.Current.Text.RichTitle);
            label.CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.section.layout.title.minSize",
                new(0f, 34f));
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            return label;
        }

        internal static MegaRichTextLabel CreateRefreshableSectionTitle(ModSettingsUiContext context,
            ModSettingsText? refreshSource,
            Func<string> textProvider)
        {
            var label = CreateSectionTitle(textProvider());
            var spec = refreshSource?.GetUiRefreshSpec() ?? ModSettingsUiRefreshSpec.StaticDisplay;
            RegisterRefreshWhenAlive(context, label, () => label.SetTextAutoSize(textProvider()), spec);
            return label;
        }

        private static MegaRichTextLabel CreateRefreshableHeaderLabel(ModSettingsUiContext context,
            ModSettingsText? refreshSource,
            Func<string> textProvider,
            int fontSize, HorizontalAlignment alignment, Color? textModulate = null)
        {
            var label = CreateHeaderLabel(textProvider(), fontSize, alignment, null, textModulate);
            var spec = refreshSource?.GetUiRefreshSpec() ?? ModSettingsUiRefreshSpec.StaticDisplay;
            RegisterRefreshWhenAlive(context, label, () => label.SetTextAutoSize(textProvider()), spec);
            return label;
        }

        /// <summary>
        ///     Creates a themed rich-text header label.
        ///     创建主题化富文本标题标签。
        /// </summary>
        public static MegaRichTextLabel CreateHeaderLabel(string text, int fontSize, HorizontalAlignment alignment,
            float? scrollViewportHeight = null, Color? textModulate = null)
        {
            var boundedScroll = scrollViewportHeight is > 0f;
            var label = new MegaRichTextLabel
            {
                BbcodeEnabled = true,
                AutoSizeEnabled = false,
                FitContent = !boundedScroll,
                ScrollActive = boundedScroll,
                ClipContents = boundedScroll,
                FocusMode = Control.FocusModeEnum.None,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = alignment,
                Theme = ModSettingsUiResources.SettingsLineTheme,
                IsHorizontallyBound = true,
                Modulate = textModulate ?? Colors.White,
            };

            if (boundedScroll)
                label.CustomMinimumSize = new(0f, scrollViewportHeight!.Value);

            label.AddThemeFontOverride("normal_font", RitsuShellTheme.Current.Font.Body);
            label.AddThemeFontOverride("bold_font", RitsuShellTheme.Current.Font.BodyBold);
            label.AddThemeFontSizeOverride("normal_font_size", fontSize);
            label.AddThemeFontSizeOverride("bold_font_size", fontSize);
            label.AddThemeFontSizeOverride("italics_font_size", fontSize);
            label.AddThemeFontSizeOverride("bold_italics_font_size", fontSize);
            label.AddThemeFontSizeOverride("mono_font_size", fontSize);
            label.MinFontSize = Math.Max(14, fontSize - 3);
            label.MaxFontSize = fontSize;
            label.SetTextAutoSize(text);
            return label;
        }

        private static MegaRichTextLabel CreatePageToolbarTitleLabel(string primaryTitle, string fallbackId)
        {
            var text = !string.IsNullOrWhiteSpace(primaryTitle)
                ? primaryTitle
                : !string.IsNullOrWhiteSpace(fallbackId)
                    ? fallbackId
                    : ModSettingsLocalization.Get("page.untitled", "Untitled");
            var label = CreateHeaderLabel(text, 24, HorizontalAlignment.Center, null,
                RitsuShellTheme.Current.Text.RichTitle);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            label.CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.pageToolbar.layout.title.minSize",
                new(0f, 30f));
            return label;
        }

        internal static Control CreateRefreshableParagraphBlock(ModSettingsUiContext context,
            ModSettingsText? refreshSource,
            Func<string> textProvider, float? maxViewportHeight)
        {
            var wrap = new MarginContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            wrap.AddThemeConstantOverride("margin_top",
                RitsuShellThemeLayoutResolver.ResolveInt("components.paragraph.layout.margin.top", 0));
            wrap.AddThemeConstantOverride("margin_bottom",
                RitsuShellThemeLayoutResolver.ResolveInt("components.paragraph.layout.margin.bottom", 0));

            var initial = ResolvedText();
            wrap.Visible = initial.Length > 0;

            var useCap = maxViewportHeight is > 0f;
            var label = CreateHeaderLabel(
                initial.Length == 0 ? "\u200b" : initial,
                16,
                HorizontalAlignment.Left,
                useCap ? maxViewportHeight : null,
                RitsuShellTheme.Current.Text.RichBody);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            wrap.AddChild(label);

            var spec = refreshSource?.GetUiRefreshSpec() ?? ModSettingsUiRefreshSpec.StaticDisplay;
            RegisterRefreshWhenAlive(context, wrap, () =>
            {
                var t = ResolvedText();
                wrap.Visible = t.Length > 0;
                label.SetTextAutoSize(t.Length == 0 ? "\u200b" : t);
            }, spec);

            return wrap;

            string ResolvedText()
            {
                return textProvider()?.Trim() ?? string.Empty;
            }
        }

        /// <summary>
        ///     Creates a themed inline description label.
        ///     创建主题化行内说明标签。
        /// </summary>
        public static MegaRichTextLabel CreateInlineDescription(string text)
        {
            var label = CreateHeaderLabel(text, 16, HorizontalAlignment.Left, null,
                RitsuShellTheme.Current.Text.RichSecondary);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            return label;
        }

        internal static Control CreateBuildErrorPlaceholder(string title, string body)
        {
            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            panel.AddThemeStyleboxOverride("panel", CreateChromeActionsMenuStyle(true));

            var stack = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            stack.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.errorPlaceholder.layout.separation", 4));
            panel.AddChild(stack);

            stack.AddChild(CreateSectionTitle(title));
            stack.AddChild(CreateInlineDescription(body));
            return panel;
        }

        private static MegaRichTextLabel CreateDescriptionLabel(string text)
        {
            return CreateInlineDescription(text);
        }

        internal static MegaRichTextLabel CreateRefreshableDescriptionLabel(ModSettingsUiContext context,
            ModSettingsText? refreshSource,
            Func<string> textProvider)
        {
            var initial = textProvider();
            var label = CreateDescriptionLabel(initial);
            label.Visible = !string.IsNullOrWhiteSpace(initial);
            var spec = refreshSource?.GetUiRefreshSpec() ?? ModSettingsUiRefreshSpec.StaticDisplay;
            RegisterRefreshWhenAlive(context, label, () =>
            {
                var text = textProvider();
                label.SetTextAutoSize(text);
                label.Visible = !string.IsNullOrWhiteSpace(text);
            }, spec);
            return label;
        }

        private static string SanitizeName(string text)
        {
            return string.Join("_", text.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        ///     Creates the standard settings surface style.
        ///     创建标准设置表面样式。
        /// </summary>
        public static StyleBoxFlat CreateSurfaceStyle()
        {
            return RitsuShellChromeStyles.CreateSurfaceStyle();
        }

        /// <summary>
        ///     Creates the standard settings value-field frame style.
        ///     创建标准设置值字段边框样式。
        /// </summary>
        public static StyleBoxFlat CreateEntryFieldFrameStyle(bool emphasized)
        {
            return RitsuShellChromeStyles.CreateEntryFieldFrameStyle(emphasized);
        }

        /// <summary>
        ///     Frame for <see cref="ColorPickerButton" />: same border/bg language as <see cref="CreateSurfaceStyle" />,
        ///     but <b>equal</b> content margins so the inner color swatch stays square inside a square button.
        ///     <see cref="ColorPickerButton" /> 的边框：边框/bg 语言与 <see cref="CreateSurfaceStyle" /> 相同，但内容边距<b>相等</b>
        ///     ，让内部色块在方形按钮内保持正方形。
        /// </summary>
        public static StyleBoxFlat CreateColorPickerSwatchFrameStyle()
        {
            return RitsuShellChromeStyles.CreateColorPickerSwatchFrameStyle();
        }

        private static StyleBoxFlat CreateEntrySurfaceStyle()
        {
            return CreateSurfaceStyle();
        }

        /// <summary>
        ///     Creates the standard inset settings surface style.
        ///     创建标准内嵌设置表面样式。
        /// </summary>
        public static StyleBoxFlat CreateInsetSurfaceStyle()
        {
            return RitsuShellChromeStyles.CreateInsetSurfaceStyle();
        }

        /// <summary>
        ///     Creates the standard chrome actions menu style.
        ///     创建标准 chrome 操作菜单样式。
        /// </summary>
        public static StyleBoxFlat CreateChromeActionsMenuStyle(bool highlighted)
        {
            return RitsuShellChromeStyles.CreateChromeActionsMenuStyle(highlighted);
        }

        /// <summary>
        ///     Creates the standard page toolbar tray style.
        ///     创建标准页面工具栏托盘样式。
        /// </summary>
        public static StyleBoxFlat CreatePageToolbarTrayStyle()
        {
            return RitsuShellChromeStyles.CreatePageToolbarTrayStyle();
        }

        internal static Control CreateModSettingsPageHeaderBar(ModSettingsUiContext context, ModSettingsPage page,
            bool showBack, Action onBack)
        {
            var pageUiContext = new ModSettingsPageUiContext(page, context);
            var pageActions = BuildPageMenuActions(context, pageUiContext);
            var pageBtn = pageActions.Count == 0
                ? null
                : new ModSettingsActionsButton(pageActions, context.RequestRefresh);
            if (pageBtn != null)
                pageBtn.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

            var bar = CreatePageHeaderBar(context, page, showBack, onBack, pageBtn);
            if (pageBtn != null)
                AttachContextMenuTargets(bar, bar, pageBtn);
            return bar;
        }

        private static Control CreatePageHeaderBar(ModSettingsUiContext context, ModSettingsPage page, bool showBack,
            Action onBack,
            ModSettingsActionsButton? trailingMenu)
        {
            var pageTitle = ModSettingsLocalization.ResolvePageDisplayName(page);
            Control? back = null;
            if (showBack)
                back = new ModSettingsMiniButton(ModSettingsLocalization.Get("button.back", "Back"), onBack)
                {
                    SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                    CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                        "components.pageToolbar.layout.backButton.minSize",
                        new(88f, 38f)),
                };

            var titleLabel = CreatePageToolbarTitleLabel(pageTitle, page.Id);

            var pageDescription = CreateRefreshableDescriptionLabel(context, page.Description,
                () => ModSettingsUiContext.ResolvePageDescription(page) ?? string.Empty);
            pageDescription.HorizontalAlignment = HorizontalAlignment.Center;
            pageDescription.AddThemeFontSizeOverride("normal_font_size",
                RitsuShellTheme.Current.Metric.FontSize.PageDescription);
            pageDescription.AddThemeFontSizeOverride("bold_font_size",
                RitsuShellTheme.Current.Metric.FontSize.PageDescription);
            pageDescription.AddThemeFontSizeOverride("italics_font_size",
                RitsuShellTheme.Current.Metric.FontSize.PageDescription);
            pageDescription.AddThemeFontSizeOverride("bold_italics_font_size",
                RitsuShellTheme.Current.Metric.FontSize.PageDescription);
            pageDescription.AddThemeFontSizeOverride("mono_font_size",
                RitsuShellTheme.Current.Metric.FontSize.PageDescription);
            pageDescription.MinFontSize = 16;
            pageDescription.MaxFontSize = RitsuShellTheme.Current.Metric.FontSize.PageDescription;
            pageDescription.Modulate = RitsuShellTheme.Current.Text.RichSecondary;
            if (trailingMenu != null)
                trailingMenu.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

            return new FastPageHeaderBar(titleLabel, pageDescription, back, trailingMenu);
        }

        /// <summary>
        ///     Creates the standard scrollable list shell style.
        ///     创建标准可滚动列表外壳样式。
        /// </summary>
        public static StyleBoxFlat CreateListShellStyle()
        {
            return RitsuShellChromeStyles.CreateListShellStyle();
        }

        /// <summary>
        ///     Creates the standard list item card style.
        ///     创建标准列表项卡片样式。
        /// </summary>
        public static StyleBoxFlat CreateListItemCardStyle(bool accent = false)
        {
            return RitsuShellChromeStyles.CreateListItemCardStyle(accent);
        }

        /// <summary>
        ///     Creates the standard inline list-editor surface style.
        ///     创建标准行内列表编辑器表面样式。
        /// </summary>
        public static StyleBoxFlat CreateListEditorSurfaceStyle()
        {
            return RitsuShellChromeStyles.CreateListEditorSurfaceStyle();
        }

        /// <summary>
        ///     Creates the standard compact pill style.
        ///     创建标准紧凑胶囊样式。
        /// </summary>
        public static StyleBoxFlat CreatePillStyle(bool highlighted = false)
        {
            return RitsuShellChromeStyles.CreatePillStyle(highlighted);
        }

        private sealed record SectionBuildPlan(
            Control Control,
            Control EntryHost,
            ModSettingsActionsButton? SectionActionsButton,
            ModSettingsCollapsibleSection? LazyContentSection);
    }
}
