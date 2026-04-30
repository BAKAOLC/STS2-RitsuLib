using Godot;
using MegaCrit.Sts2.addons.mega_text;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;
using Timer = Godot.Timer;

namespace STS2RitsuLib.Settings
{
    internal static partial class ModSettingsUiFactory
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

        public static ModSettingsSidebarButton CreateSidebarButton(string text, Action onPressed,
            ModSettingsSidebarItemKind kind = ModSettingsSidebarItemKind.Page,
            string? prefix = null,
            int indentLevel = 0)
        {
            return new(text, onPressed, kind, prefix, indentLevel);
        }

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

        private static MarginContainer CreateSettingLine<TValue>(ModSettingsUiContext context,
            Func<string> labelProvider,
            Func<string> descriptionBodyProvider, Control valueControl, IModSettingsValueBinding<TValue> binding,
            ModSettingsText? labelRefreshSource = null,
            ModSettingsText? descriptionRefreshSource = null)
        {
            return CreateSettingLine(context, labelProvider, descriptionBodyProvider, valueControl,
                CreateEntryActionsButton(context, binding),
                labelRefreshSource, descriptionRefreshSource);
        }

        private static MarginContainer CreateSettingLine<TValue>(ModSettingsUiContext context,
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

        private static MarginContainer CreateSettingLine(ModSettingsUiContext context, Func<string> labelProvider,
            Func<string> descriptionBodyProvider, Control valueControl, Control? actionControl = null,
            ModSettingsText? labelRefreshSource = null,
            ModSettingsText? descriptionRefreshSource = null)
        {
            var descriptionText = descriptionBodyProvider();
            var line = new MarginContainer();

            var lineMargins = RitsuShellThemeLayoutResolver.ResolveEdges("components.entryLine.layout.margin", 8);
            lineMargins = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.margin.left", lineMargins.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.margin.top", 4),
                RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.margin.right", lineMargins.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.margin.bottom", 4));
            line.AddThemeConstantOverride("margin_left", lineMargins.Left);
            line.AddThemeConstantOverride("margin_right", lineMargins.Right);
            line.AddThemeConstantOverride("margin_top", lineMargins.Top);
            line.AddThemeConstantOverride("margin_bottom", lineMargins.Bottom);

            var surface = new PanelContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ClipContents = false,
            };
            surface.AddThemeStyleboxOverride("panel", CreateEntrySurfaceStyle());
            line.AddChild(surface);

            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            row.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.rowSeparation", 20));
            surface.AddChild(row);

            var leftColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            leftColumn.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.leftColumnSeparation", 5));

            var label = CreateRefreshableHeaderLabel(context, labelRefreshSource, ResolveLabelText,
                RitsuShellTheme.Current.Metric.FontSize.SettingLineTitle,
                HorizontalAlignment.Left,
                RitsuShellTheme.Current.Text.RichTitle);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            leftColumn.AddChild(label);

            var descriptionLabel =
                CreateRefreshableDescriptionLabel(context, descriptionRefreshSource, descriptionBodyProvider);
            descriptionLabel.Visible = !string.IsNullOrWhiteSpace(descriptionText);
            leftColumn.AddChild(descriptionLabel);

            row.AddChild(leftColumn);

            valueControl.CustomMinimumSize = new(Math.Max(EntryControlWidth, valueControl.CustomMinimumSize.X),
                Mathf.Max(valueControl.CustomMinimumSize.Y, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));
            valueControl.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
            valueControl.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            row.AddChild(valueControl);

            if (actionControl == null)
            {
                AttachHostSurfaceReadOnlySync(context, valueControl, null);
                return line;
            }

            actionControl.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            row.AddChild(actionControl);
            if (actionControl is ModSettingsActionsButton actionsButton)
                AttachContextMenuTargets(line, valueControl, actionsButton);

            AttachHostSurfaceReadOnlySync(context, valueControl, actionControl);
            return line;

            string ResolveLabelText()
            {
                var s = labelProvider();
                return string.IsNullOrWhiteSpace(s)
                    ? ModSettingsLocalization.Get("entry.label.empty", "—")
                    : s;
            }
        }

        private static void AttachHostSurfaceReadOnlySync(ModSettingsUiContext context, Control valueControl,
            Control? actionControl)
        {
            var readOnlyMask = context.GetSectionHostReadOnlyMask();

            Sync();
            RegisterRefreshWhenAlive(context, valueControl, Sync, ModSettingsUiRefreshSpec.Always);
            if (actionControl != null)
                RegisterRefreshWhenAlive(context, actionControl, Sync, ModSettingsUiRefreshSpec.Always);
            return;

            void Sync()
            {
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
            ModSettingsActionsButton button)
        {
            AttachContextMenuRecursively(line, button);
            AttachContextMenuRecursively(valueControl, button);
        }

        private static void AttachContextMenuRecursively(Control target, ModSettingsActionsButton button)
        {
            AttachContextMenu(target, button);
            foreach (var child in target.GetChildren())
                if (child is Control childControl)
                    AttachContextMenuRecursively(childControl, button);
        }

        internal static void AttachContextMenu(Control target, ModSettingsActionsButton button)
        {
            if (target.HasMeta(ContextMenuAttachedMetaKey))
                return;

            target.SetMeta(ContextMenuAttachedMetaKey, true);

            if (target.MouseFilter == Control.MouseFilterEnum.Ignore)
                target.MouseFilter = Control.MouseFilterEnum.Pass;

            var longPressTimer = new Timer
            {
                OneShot = true,
                WaitTime = 0.55f,
                Autostart = false,
                ProcessCallback = Timer.TimerProcessCallback.Idle,
            };
            target.AddChild(longPressTimer);
            var pendingTouchPosition = Vector2.Zero;
            longPressTimer.Timeout += () =>
            {
                if (!CanOpenContextMenu(target, button))
                {
                    button.ForceCloseDropdown();
                    return;
                }

                button.OpenAt(pendingTouchPosition);
            };

            target.GuiInput += @event =>
            {
                switch (@event)
                {
                    case InputEventScreenTouch touch:
                    {
                        if (touch.Pressed)
                        {
                            if (!CanOpenContextMenu(target, button))
                            {
                                button.ForceCloseDropdown();
                                longPressTimer.Stop();
                                return;
                            }

                            pendingTouchPosition = target.GetGlobalTransformWithCanvas().Origin + touch.Position;
                            longPressTimer.Start();
                        }
                        else
                        {
                            longPressTimer.Stop();
                        }

                        return;
                    }
                    case InputEventScreenDrag:
                        longPressTimer.Stop();
                        return;
                }

                if (@event is not InputEventMouseButton
                    {
                        Pressed: true,
                        ButtonIndex: MouseButton.Right,
                    })
                    return;

                if (!CanOpenContextMenu(target, button))
                {
                    button.ForceCloseDropdown();
                    return;
                }

                button.OpenAt(target.GetGlobalMousePosition());
                target.GetViewport().SetInputAsHandled();
            };
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
            ModSettingsUiActionRegistry.AppendSectionActions(context, sectionContext, actions);
            return actions;
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
        /// </summary>
        internal static Control MaybeWrapDynamicVisibility(ModSettingsUiContext context, Control inner,
            Func<bool>? predicate)
        {
            if (predicate == null)
                return inner;

            var host = new MarginContainer
            {
                Name = "DynamicVisibilityHost",
                MouseFilter = Control.MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            host.AddChild(inner);

            Apply();
            RegisterRefreshWhenAlive(context, host, Apply, ModSettingsUiRefreshSpec.Always);
            return host;

            void Apply()
            {
                if (!GodotObject.IsInstanceValid(host))
                    return;
                try
                {
                    host.Visible = predicate();
                }
                catch
                {
                    host.Visible = true;
                }
            }
        }

        private static Control CreateSection(ModSettingsUiContext context, ModSettingsPage page,
            ModSettingsSection section)
        {
            var sectionUiContext = new ModSettingsSectionUiContext(page, section, context);
            var sectionMenuActions = BuildSectionMenuActions(context, sectionUiContext);
            var sectionActionsButton = sectionMenuActions.Count == 0
                ? null
                : new ModSettingsActionsButton(sectionMenuActions, context.RequestRefresh);
            if (sectionActionsButton != null)
                sectionActionsButton.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

            var wrappedEntries = new List<Control>(section.Entries.Count);
            context.BeginSectionSurfaceScope(page, section);
            try
            {
                foreach (var entry in section.Entries)
                    try
                    {
                        var control = entry.CreateControl(context);
                        control = MaybeWrapDynamicVisibility(context, control, entry.VisibilityPredicate);
                        control = MaybeWrapDynamicEnabled(context, control, entry.EnabledPredicate);
                        wrappedEntries.Add(control);
                    }
                    catch (Exception ex)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[Settings] Failed to build entry '{page.ModId}:{page.Id}:{section.Id}:{entry.Id}': {ex.Message}");
                        wrappedEntries.Add(CreateBuildErrorPlaceholder(
                            ModSettingsLocalization.Get("entry.failed.title", "Setting failed to load"),
                            string.Format(
                                ModSettingsLocalization.Get("entry.failed.body", "Failed to build setting '{0}'."),
                                entry.Id)));
                    }
            }
            finally
            {
                context.EndSectionSurfaceScope();
            }

            Control built;
            if (section.IsCollapsible)
            {
                var collapsible = new ModSettingsCollapsibleSection(
                    ResolveSectionTitleText(section),
                    section.Id,
                    section.Description != null ? ModSettingsUiContext.Resolve(section.Description) : null,
                    section.StartCollapsed,
                    wrappedEntries.ToArray(),
                    sectionActionsButton);
                if (sectionActionsButton != null)
                    AttachContextMenuTargets(collapsible, collapsible, sectionActionsButton);
                built = collapsible;
            }
            else
            {
                var container = new VBoxContainer
                {
                    Name = $"Section_{section.Id}",
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                };
                container.AddThemeConstantOverride("separation",
                    RitsuShellThemeLayoutResolver.ResolveInt("components.section.layout.separation", 8));

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
                foreach (var wrapped in wrappedEntries)
                    container.AddChild(wrapped);
                if (sectionActionsButton != null)
                    AttachContextMenuTargets(container, container, sectionActionsButton);
                built = container;
            }

            var hostCombined = ModSettingsHostSurfaceResolver.CombineVisibility(section.VisibleWhen,
                () => ModSettingsHostSurfaceResolver.IsVisibleOnCurrentHost(section.VisibleOnHostSurfaces));
            var visibleHost = MaybeWrapDynamicVisibility(context, built, hostCombined);

            // For collapsible sections, keep the collapse toggle operable while disabling the content/actions.
            if (section.EnabledWhen == null || built is not ModSettingsCollapsibleSection collapsibleHost)
                return MaybeWrapDynamicEnabled(context, visibleHost, section.EnabledWhen);
            Apply();
            RegisterRefreshWhenAlive(context, visibleHost, Apply, ModSettingsUiRefreshSpec.Always);
            return visibleHost;

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

        internal static Control MaybeWrapDynamicEnabled(ModSettingsUiContext context, Control host,
            Func<bool>? predicate)
        {
            if (predicate == null)
                return host;
            if (host.HasMeta(EnabledSyncMetaKey))
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

        private static MegaRichTextLabel CreateHeaderLabel(string text, int fontSize, HorizontalAlignment alignment,
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

        internal static MegaRichTextLabel CreateInlineDescription(string text)
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

        internal static StyleBoxFlat CreateSurfaceStyle()
        {
            return RitsuShellChromeStyles.CreateSurfaceStyle();
        }

        internal static StyleBoxFlat CreateEntryFieldFrameStyle(bool emphasized)
        {
            return RitsuShellChromeStyles.CreateEntryFieldFrameStyle(emphasized);
        }

        /// <summary>
        ///     Frame for <see cref="ColorPickerButton" />: same border/bg language as <see cref="CreateSurfaceStyle" />,
        ///     but <b>equal</b> content margins so the inner color swatch stays square inside a square button.
        /// </summary>
        internal static StyleBoxFlat CreateColorPickerSwatchFrameStyle()
        {
            return RitsuShellChromeStyles.CreateColorPickerSwatchFrameStyle();
        }

        private static StyleBoxFlat CreateEntrySurfaceStyle()
        {
            return CreateSurfaceStyle();
        }

        internal static StyleBoxFlat CreateInsetSurfaceStyle()
        {
            return RitsuShellChromeStyles.CreateInsetSurfaceStyle();
        }

        internal static StyleBoxFlat CreateChromeActionsMenuStyle(bool highlighted)
        {
            return RitsuShellChromeStyles.CreateChromeActionsMenuStyle(highlighted);
        }

        internal static StyleBoxFlat CreatePageToolbarTrayStyle()
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
            const float sideSlotMin = 104f;
            var pageTitle = ModSettingsLocalization.ResolvePageDisplayName(page);

            var tray = new PanelContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ClipContents = false,
            };
            tray.AddThemeStyleboxOverride("panel", CreatePageToolbarTrayStyle());

            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            row.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.pageToolbar.layout.rowSeparation", 10));

            var left = new HBoxContainer
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.pageToolbar.layout.sideSlot.minSize",
                    new(sideSlotMin, 44f)),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Begin,
            };
            if (showBack)
            {
                var back = new ModSettingsMiniButton(ModSettingsLocalization.Get("button.back", "Back"), onBack)
                {
                    SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                    CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                        "components.pageToolbar.layout.backButton.minSize",
                        new(88f, 38f)),
                };
                left.AddChild(back);
            }

            var center = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            center.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.pageToolbar.layout.centerSeparation", 5));

            var titleLabel = CreatePageToolbarTitleLabel(pageTitle, page.Id);
            center.AddChild(titleLabel);

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
            center.AddChild(pageDescription);

            var right = new HBoxContainer
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.pageToolbar.layout.sideSlot.minSize",
                    new(sideSlotMin, 44f)),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.End,
            };
            if (trailingMenu != null)
                right.AddChild(trailingMenu);

            row.AddChild(left);
            row.AddChild(center);
            row.AddChild(right);
            tray.AddChild(row);
            return tray;
        }

        internal static StyleBoxFlat CreateListShellStyle()
        {
            return RitsuShellChromeStyles.CreateListShellStyle();
        }

        internal static StyleBoxFlat CreateListItemCardStyle(bool accent = false)
        {
            return RitsuShellChromeStyles.CreateListItemCardStyle(accent);
        }

        internal static StyleBoxFlat CreateListEditorSurfaceStyle()
        {
            return RitsuShellChromeStyles.CreateListEditorSurfaceStyle();
        }

        internal static StyleBoxFlat CreatePillStyle(bool highlighted = false)
        {
            return RitsuShellChromeStyles.CreatePillStyle(highlighted);
        }
    }
}
