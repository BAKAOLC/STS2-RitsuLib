using Godot;
using MegaCrit.Sts2.addons.mega_text;
using Timer = Godot.Timer;

namespace STS2RitsuLib.Settings
{
    internal static partial class ModSettingsUiFactory
    {
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
                CustomMinimumSize = new(0f, 2f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Color = new(0.909804f, 0.862745f, 0.745098f, 0.25098f),
            };
        }

        /// <summary>
        ///     Rule above the sidebar mod scroll: same white/subtle language as ModGroup row bottoms, but thicker.
        /// </summary>
        public static ColorRect CreateSidebarScrollTopDivider()
        {
            var a = ModSettingsUiMetrics.SidebarModListSubtleAlpha;
            // 2px line: slightly higher alpha than 1px row border so it still reads, without looking heavy.
            var alpha = Mathf.Clamp(a * 2.15f, 0.052f, 0.10f);
            return new ColorRect
            {
                CustomMinimumSize = new(0f, ModSettingsUiMetrics.SidebarScrollTopDividerHeight),
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Color = new Color(1f, 1f, 1f, alpha),
            };
        }

        /// <summary>
        ///     Flat rectangular tag behind the mod version (reference: stencil / inventory label).
        /// </summary>
        internal static StyleBoxFlat CreateSidebarModVersionBadgeStyle()
        {
            return new()
            {
                BgColor = new Color(0.14f, 0.15f, 0.17f, 0.78f),
                BorderColor = Colors.Transparent,
                BorderWidthLeft = 0,
                BorderWidthTop = 0,
                BorderWidthRight = 0,
                BorderWidthBottom = 0,
                CornerRadiusTopLeft = 0,
                CornerRadiusTopRight = 0,
                CornerRadiusBottomRight = 0,
                CornerRadiusBottomLeft = 0,
                ContentMarginLeft = 6,
                ContentMarginTop = 3,
                ContentMarginRight = 6,
                ContentMarginBottom = 3,
            };
        }

        private static MarginContainer CreateSettingLine<TValue>(ModSettingsUiContext context,
            Func<string> labelProvider,
            Func<string> descriptionBodyProvider, Control valueControl, IModSettingsValueBinding<TValue> binding)
        {
            return CreateSettingLine(context, labelProvider, descriptionBodyProvider, valueControl,
                CreateEntryActionsButton(context, binding), binding);
        }

        private static MarginContainer CreateSettingLine<TValue>(ModSettingsUiContext context,
            Func<string> labelProvider,
            Func<string> descriptionBodyProvider, Control valueControl, IModSettingsValueBinding<TValue> binding,
            ModSettingsMenuCapabilities capabilities)
        {
            return CreateSettingLine(context, labelProvider, descriptionBodyProvider, valueControl,
                CreateEntryActionsButton(context, binding, capabilities), binding);
        }

        private static MarginContainer CreateSettingLine(ModSettingsUiContext context, Func<string> labelProvider,
            Func<string> descriptionBodyProvider, Control valueControl, Control? actionControl = null,
            IModSettingsBinding? scopeBinding = null)
        {
            var descriptionText = descriptionBodyProvider();
            var line = new MarginContainer();

            line.AddThemeConstantOverride("margin_left", 8);
            line.AddThemeConstantOverride("margin_right", 8);
            line.AddThemeConstantOverride("margin_top", 6);
            line.AddThemeConstantOverride("margin_bottom", 6);

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
            row.AddThemeConstantOverride("separation", 20);
            surface.AddChild(row);

            var leftColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            leftColumn.AddThemeConstantOverride("separation", 5);

            var label = CreateRefreshableHeaderLabel(context, ResolveLabelText, 24, HorizontalAlignment.Left,
                ModSettingsUiPalette.RichTextTitle);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            leftColumn.AddChild(label);

            var descriptionLabel = CreateRefreshableDescriptionLabel(context, descriptionBodyProvider);
            descriptionLabel.Visible = !string.IsNullOrWhiteSpace(descriptionText);
            leftColumn.AddChild(descriptionLabel);

            if (scopeBinding != null)
                leftColumn.AddChild(CreatePersistenceScopeTag(scopeBinding));

            row.AddChild(leftColumn);

            valueControl.CustomMinimumSize = new(Math.Max(EntryControlWidth, valueControl.CustomMinimumSize.X),
                Mathf.Max(valueControl.CustomMinimumSize.Y, ModSettingsUiMetrics.EntryValueMinHeight));
            valueControl.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
            valueControl.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            row.AddChild(valueControl);

            if (actionControl == null) return line;
            actionControl.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            row.AddChild(actionControl);
            if (actionControl is ModSettingsActionsButton actionsButton)
                AttachContextMenuTargets(line, valueControl, actionsButton);

            return line;

            string ResolveLabelText()
            {
                var s = labelProvider();
                return string.IsNullOrWhiteSpace(s)
                    ? ModSettingsLocalization.Get("entry.label.empty", "—")
                    : s;
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
            longPressTimer.Timeout += () => button.OpenAt(pendingTouchPosition);

            target.GuiInput += @event =>
            {
                switch (@event)
                {
                    case InputEventScreenTouch touch:
                    {
                        if (touch.Pressed)
                        {
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

                button.OpenAt(target.GetGlobalMousePosition());
                target.GetViewport().SetInputAsHandled();
            };
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
            RegisterRefreshWhenAlive(context, host, Apply);
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
            foreach (var entry in section.Entries)
                try
                {
                    wrappedEntries.Add(MaybeWrapDynamicVisibility(context, entry.CreateControl(context),
                        entry.VisibilityPredicate));
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
                container.AddThemeConstantOverride("separation", 8);

                if (section.Title != null || sectionActionsButton != null)
                {
                    var headerRow = new HBoxContainer
                    {
                        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                        MouseFilter = Control.MouseFilterEnum.Ignore,
                        Alignment = BoxContainer.AlignmentMode.Center,
                    };
                    headerRow.AddThemeConstantOverride("separation", 10);
                    if (section.Title != null)
                    {
                        var title = CreateRefreshableSectionTitle(context,
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
                    container.AddChild(CreateRefreshableDescriptionLabel(context,
                        () => ModSettingsUiContext.Resolve(section.Description)));
                foreach (var wrapped in wrappedEntries)
                    container.AddChild(wrapped);
                if (sectionActionsButton != null)
                    AttachContextMenuTargets(container, container, sectionActionsButton);
                built = container;
            }

            return MaybeWrapDynamicVisibility(context, built, section.VisibleWhen);
        }

        internal static MegaRichTextLabel CreateSectionTitle(string text)
        {
            var label = CreateHeaderLabel(text, 22, HorizontalAlignment.Left, null, ModSettingsUiPalette.RichTextTitle);
            label.CustomMinimumSize = new(0f, 34f);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            return label;
        }

        internal static MegaRichTextLabel CreateRefreshableSectionTitle(ModSettingsUiContext context,
            Func<string> textProvider)
        {
            var label = CreateSectionTitle(textProvider());
            RegisterRefreshWhenAlive(context, label, () => label.SetTextAutoSize(textProvider()));
            return label;
        }

        private static MegaRichTextLabel CreateRefreshableHeaderLabel(ModSettingsUiContext context,
            Func<string> textProvider,
            int fontSize, HorizontalAlignment alignment, Color? textModulate = null)
        {
            var label = CreateHeaderLabel(textProvider(), fontSize, alignment, null, textModulate);
            RegisterRefreshWhenAlive(context, label, () => label.SetTextAutoSize(textProvider()));
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

            label.AddThemeFontOverride("normal_font", ModSettingsUiResources.KreonRegular);
            label.AddThemeFontOverride("bold_font", ModSettingsUiResources.KreonBold);
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

        /// <summary>
        ///     Large left-aligned page title (uppercase), vanilla-style header line.
        /// </summary>
        private static MegaRichTextLabel CreatePageMainTitleLabel(string primaryTitle, string fallbackId)
        {
            var text = !string.IsNullOrWhiteSpace(primaryTitle)
                ? primaryTitle
                : !string.IsNullOrWhiteSpace(fallbackId)
                    ? fallbackId
                    : ModSettingsLocalization.Get("page.untitled", "Untitled");
            var upper = text.ToUpperInvariant();
            var label = CreateHeaderLabel(upper, 28, HorizontalAlignment.Left, null, ModSettingsUiPalette.RichTextTitle);
            label.AddThemeFontOverride("normal_font", ModSettingsUiResources.KreonBold);
            label.AddThemeFontOverride("bold_font", ModSettingsUiResources.KreonBold);
            label.AddThemeFontSizeOverride("normal_font_size", 28);
            label.AddThemeFontSizeOverride("bold_font_size", 28);
            label.AddThemeFontSizeOverride("italics_font_size", 28);
            label.AddThemeFontSizeOverride("bold_italics_font_size", 28);
            label.AddThemeFontSizeOverride("mono_font_size", 28);
            label.MinFontSize = 18;
            label.MaxFontSize = 28;
            // FitContent + narrow column can collapse width; fill row and disable wrap for one-line title.
            label.FitContent = false;
            label.AutowrapMode = TextServer.AutowrapMode.Off;
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            label.SizeFlagsStretchRatio = 1f;
            label.CustomMinimumSize = new(0f, 36f);
            return label;
        }

        private static string EscapeBbcodeUserText(string? s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            return s.Replace("[", "[lb]");
        }

        /// <summary>
        ///     Content page header: mod name + current page (breadcrumb row).
        /// </summary>
        private static string BuildPageBreadcrumbBbcode(ModSettingsPage page)
        {
            var mod = EscapeBbcodeUserText(ModSettingsLocalization.ResolveModName(page.ModId, page.ModId));
            var pageName = EscapeBbcodeUserText(ModSettingsLocalization.ResolvePageDisplayName(page));
            const string sep = "[color=#4a5058] // [/color]";
            const string muted = "#7a8088";
            const string accentHex = "#ea9104";
            return $"[font_size=12][color={muted}]{mod}[/color]{sep}[color={accentHex}]{pageName}[/color][/font_size]";
        }

        private static MegaRichTextLabel CreateRefreshableBreadcrumbLabel(ModSettingsUiContext context,
            ModSettingsPage page)
        {
            var label = new MegaRichTextLabel
            {
                BbcodeEnabled = true,
                AutoSizeEnabled = false,
                FitContent = true,
                ScrollActive = false,
                ClipContents = false,
                FocusMode = Control.FocusModeEnum.None,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Theme = ModSettingsUiResources.SettingsLineTheme,
                IsHorizontallyBound = true,
            };
            label.AddThemeFontOverride("normal_font", ModSettingsUiResources.KreonBold);
            label.AddThemeFontOverride("bold_font", ModSettingsUiResources.KreonBold);
            label.AddThemeFontSizeOverride("normal_font_size", 12);
            label.AddThemeFontSizeOverride("bold_font_size", 12);
            label.AddThemeFontSizeOverride("italics_font_size", 12);
            label.AddThemeFontSizeOverride("bold_italics_font_size", 12);
            label.AddThemeFontSizeOverride("mono_font_size", 12);
            label.MinFontSize = 10;
            label.MaxFontSize = 12;
            label.SetTextAutoSize(BuildPageBreadcrumbBbcode(page));
            RegisterRefreshWhenAlive(context, label, () => label.SetTextAutoSize(BuildPageBreadcrumbBbcode(page)));
            return label;
        }

        internal static Control CreateRefreshableParagraphBlock(ModSettingsUiContext context,
            Func<string> textProvider, float? maxViewportHeight)
        {
            var wrap = new MarginContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            wrap.AddThemeConstantOverride("margin_top", 0);
            wrap.AddThemeConstantOverride("margin_bottom", 0);

            var initial = ResolvedText();
            wrap.Visible = initial.Length > 0;

            var useCap = maxViewportHeight is > 0f;
            var label = CreateHeaderLabel(
                initial.Length == 0 ? "\u200b" : initial,
                16,
                HorizontalAlignment.Left,
                useCap ? maxViewportHeight : null,
                ModSettingsUiPalette.RichTextBody);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            wrap.AddChild(label);

            RegisterRefreshWhenAlive(context, wrap, () =>
            {
                var t = ResolvedText();
                wrap.Visible = t.Length > 0;
                label.SetTextAutoSize(t.Length == 0 ? "\u200b" : t);
            });

            return wrap;

            string ResolvedText()
            {
                return textProvider()?.Trim() ?? string.Empty;
            }
        }

        internal static MegaRichTextLabel CreateInlineDescription(string text)
        {
            var label = CreateHeaderLabel(text, 16, HorizontalAlignment.Left, null,
                ModSettingsUiPalette.RichTextSecondary);
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
            stack.AddThemeConstantOverride("separation", 4);
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
            Func<string> textProvider)
        {
            var initial = textProvider();
            var label = CreateDescriptionLabel(initial);
            label.Visible = !string.IsNullOrWhiteSpace(initial);
            RegisterRefreshWhenAlive(context, label, () =>
            {
                var text = textProvider();
                label.SetTextAutoSize(text);
                label.Visible = !string.IsNullOrWhiteSpace(text);
            });
            return label;
        }

        internal static Control CreatePersistenceScopeTag(IModSettingsBinding binding)
        {
            var panel = new PanelContainer
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            };
            panel.AddThemeStyleboxOverride("panel", CreateScopeTagStyle());
            var label = new Label
            {
                Text = ModSettingsUiContext.GetPersistenceScopeChipText(binding),
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            label.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
            label.AddThemeFontSizeOverride("font_size", 13);
            label.AddThemeColorOverride("font_color", ModSettingsUiPalette.RichTextMuted);
            panel.AddChild(label);
            return panel;
        }

        private static StyleBoxFlat CreateScopeTagStyle()
        {
            return new()
            {
                BgColor = new(0.085f, 0.108f, 0.138f, 0.97f),
                BorderColor = new(0.32f, 0.48f, 0.58f, 0.5f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ContentMarginLeft = 8,
                ContentMarginTop = 3,
                ContentMarginRight = 8,
                ContentMarginBottom = 3,
            };
        }

        private static string SanitizeName(string text)
        {
            return string.Join("_", text.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        }

        internal static StyleBoxFlat CreateSurfaceStyle()
        {
            return new()
            {
                BgColor = new(0.095f, 0.115f, 0.15f, 0.965f),
                BorderColor = new(0.38f, 0.58f, 0.70f, 0.42f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ShadowColor = new(0f, 0f, 0f, 0.14f),
                ShadowSize = 2,
                ContentMarginLeft = 12,
                ContentMarginTop = 8,
                ContentMarginRight = 12,
                ContentMarginBottom = 8,
            };
        }

        internal static StyleBoxFlat CreateEntryFieldFrameStyle(bool emphasized)
        {
            var borderColor = new Color(0.38f, 0.58f, 0.70f, 0.42f);
            var borderW = emphasized ? 2 : 1;
            return new()
            {
                BgColor = new(0.095f, 0.115f, 0.15f, 0.965f),
                BorderColor = borderColor,
                BorderWidthLeft = borderW,
                BorderWidthTop = borderW,
                BorderWidthRight = borderW,
                BorderWidthBottom = borderW,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ShadowColor = emphasized
                    ? new(borderColor.R, borderColor.G, borderColor.B, 0.42f)
                    : new Color(0f, 0f, 0f, 0.14f),
                ShadowSize = emphasized ? 7 : 2,
                ContentMarginLeft = 12,
                ContentMarginTop = 8,
                ContentMarginRight = 12,
                ContentMarginBottom = 8,
            };
        }

        /// <summary>
        ///     Frame for <see cref="ColorPickerButton" />: same border/bg language as <see cref="CreateSurfaceStyle" />,
        ///     but <b>equal</b> content margins so the inner color swatch stays square inside a square button.
        /// </summary>
        internal static StyleBoxFlat CreateColorPickerSwatchFrameStyle()
        {
            const int inset = 5;
            return new()
            {
                BgColor = new(0.095f, 0.115f, 0.15f, 0.965f),
                BorderColor = new(0.38f, 0.58f, 0.70f, 0.42f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ShadowSize = 0,
                ContentMarginLeft = inset,
                ContentMarginTop = inset,
                ContentMarginRight = inset,
                ContentMarginBottom = inset,
            };
        }

        private static StyleBoxFlat CreateEntrySurfaceStyle()
        {
            return CreateSurfaceStyle();
        }

        internal static StyleBoxFlat CreateInsetSurfaceStyle()
        {
            return new()
            {
                BgColor = new(0.07f, 0.085f, 0.11f, 0.98f),
                BorderColor = new(0.30f, 0.44f, 0.56f, 0.34f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ContentMarginLeft = 10,
                ContentMarginTop = 8,
                ContentMarginRight = 10,
                ContentMarginBottom = 8,
            };
        }

        /// <summary>
        ///     Sidebar mod cover: square clip, no border (image or placeholder draws edge-to-edge).
        /// </summary>
        internal static StyleBoxFlat CreateModSidebarPreviewFrameStyle()
        {
            return new()
            {
                BgColor = Colors.Transparent,
                BorderColor = Colors.Transparent,
                BorderWidthLeft = 0,
                BorderWidthTop = 0,
                BorderWidthRight = 0,
                BorderWidthBottom = 0,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ContentMarginLeft = 0,
                ContentMarginTop = 0,
                ContentMarginRight = 0,
                ContentMarginBottom = 0,
            };
        }

        internal static StyleBoxFlat CreateChromeActionsMenuStyle(bool highlighted)
        {
            return new()
            {
                BgColor = highlighted
                    ? new(0.15f, 0.20f, 0.26f, 0.96f)
                    : new Color(0.09f, 0.115f, 0.15f, 0.90f),
                BorderColor = highlighted
                    ? new(0.50f, 0.70f, 0.84f, 0.62f)
                    : new Color(0.30f, 0.44f, 0.56f, 0.42f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ContentMarginLeft = 10,
                ContentMarginTop = 6,
                ContentMarginRight = 10,
                ContentMarginBottom = 6,
            };
        }

        /// <summary>
        ///     Vanilla-style page header: no panel fill; single bottom hairline separator.
        /// </summary>
        internal static StyleBoxFlat CreatePageHeaderTrayStyle()
        {
            return new()
            {
                BgColor = Colors.Transparent,
                BorderColor = new Color(0.42f, 0.46f, 0.52f, 0.55f),
                BorderWidthLeft = 0,
                BorderWidthTop = 0,
                BorderWidthRight = 0,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 0,
                CornerRadiusTopRight = 0,
                CornerRadiusBottomRight = 0,
                CornerRadiusBottomLeft = 0,
                ShadowSize = 0,
                ContentMarginLeft = 4,
                ContentMarginTop = 8,
                ContentMarginRight = 4,
                ContentMarginBottom = 12,
            };
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
            tray.AddThemeStyleboxOverride("panel", CreatePageHeaderTrayStyle());

            var column = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            column.AddThemeConstantOverride("separation", 6);

            var crumbRow = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            crumbRow.AddThemeConstantOverride("separation", 10);

            if (showBack)
            {
                var back = new ModSettingsMiniButton(ModSettingsLocalization.Get("button.back", "Back"), onBack)
                {
                    SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                    CustomMinimumSize = new(88f, 38f),
                };
                crumbRow.AddChild(back);
            }

            var breadcrumb = CreateRefreshableBreadcrumbLabel(context, page);
            breadcrumb.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            crumbRow.AddChild(breadcrumb);

            var titleRow = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            titleRow.AddThemeConstantOverride("separation", 10);

            var titleLabel = CreatePageMainTitleLabel(pageTitle, page.Id);
            titleLabel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

            var right = new HBoxContainer
            {
                CustomMinimumSize = new(sideSlotMin, 44f),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.End,
            };
            if (trailingMenu != null)
                right.AddChild(trailingMenu);

            titleRow.AddChild(titleLabel);
            titleRow.AddChild(right);

            var pageDescription = CreateRefreshableDescriptionLabel(context,
                () => ModSettingsUiContext.ResolvePageDescription(page) ?? string.Empty);
            pageDescription.HorizontalAlignment = HorizontalAlignment.Left;
            pageDescription.AddThemeFontSizeOverride("normal_font_size", 16);
            pageDescription.AddThemeFontSizeOverride("bold_font_size", 16);
            pageDescription.AddThemeFontSizeOverride("italics_font_size", 16);
            pageDescription.AddThemeFontSizeOverride("bold_italics_font_size", 16);
            pageDescription.AddThemeFontSizeOverride("mono_font_size", 16);
            pageDescription.MinFontSize = 14;
            pageDescription.MaxFontSize = 16;
            pageDescription.Modulate = ModSettingsUiPalette.RichTextMuted;

            column.AddChild(crumbRow);
            column.AddChild(titleRow);
            column.AddChild(pageDescription);
            tray.AddChild(column);
            return tray;
        }

        internal static StyleBoxFlat CreateListShellStyle()
        {
            return new()
            {
                BgColor = new(0.06f, 0.075f, 0.098f, 0.98f),
                BorderColor = new(0.34f, 0.52f, 0.64f, 0.38f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ShadowColor = new(0f, 0f, 0f, 0.16f),
                ShadowSize = 3,
                ContentMarginLeft = 12,
                ContentMarginTop = 12,
                ContentMarginRight = 12,
                ContentMarginBottom = 12,
            };
        }

        internal static StyleBoxFlat CreateListItemCardStyle(bool accent = false)
        {
            return new()
            {
                BgColor = accent
                    ? new(0.115f, 0.16f, 0.205f, 0.985f)
                    : new Color(0.09f, 0.11f, 0.145f, 0.975f),
                BorderColor = accent
                    ? new(0.52f, 0.77f, 0.90f, 0.70f)
                    : new Color(0.33f, 0.50f, 0.62f, 0.34f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ShadowColor = new(0f, 0f, 0f, 0.12f),
                ShadowSize = 2,
                ContentMarginLeft = 10,
                ContentMarginTop = 10,
                ContentMarginRight = 10,
                ContentMarginBottom = 10,
            };
        }

        internal static StyleBoxFlat CreateListEditorSurfaceStyle()
        {
            return new()
            {
                BgColor = new(0.055f, 0.068f, 0.09f, 0.985f),
                BorderColor = new(0.30f, 0.46f, 0.58f, 0.42f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ShadowColor = new(0f, 0f, 0f, 0.12f),
                ShadowSize = 2,
                ContentMarginLeft = 10,
                ContentMarginTop = 8,
                ContentMarginRight = 10,
                ContentMarginBottom = 8,
            };
        }

        internal static StyleBoxFlat CreatePillStyle(bool highlighted = false)
        {
            return new()
            {
                BgColor = highlighted
                    ? new(0.17f, 0.28f, 0.34f, 0.98f)
                    : new Color(0.12f, 0.16f, 0.21f, 0.96f),
                BorderColor = highlighted
                    ? new(0.60f, 0.82f, 0.92f, 0.78f)
                    : new Color(0.38f, 0.54f, 0.66f, 0.40f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ContentMarginLeft = 10,
                ContentMarginTop = 5,
                ContentMarginRight = 10,
                ContentMarginBottom = 5,
            };
        }
    }
}
