using System.Diagnostics;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Search;
using STS2RitsuLib.Search.Pinyin;
using STS2RitsuLib.Ui.Catalog;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
    // ReSharper disable once Godot.MissingParameterlessConstructor
    internal sealed partial class RitsuCatalogSearchMenu : HBoxContainer, IModSettingsTransientPopupOwner
    {
        private readonly string _catalogId;
        private readonly RitsuCatalogFilter[] _filters;
        private readonly Dictionary<string, int> _selections;
        private readonly Dictionary<string, int> _defaults;
        private readonly string? _primaryFilterId;
        private readonly Action _changed;
        private readonly Action<string> _applyQuery;
        private readonly VBoxContainer _body;
        private readonly Control _backdrop;
        private readonly PanelContainer _panel;
        private readonly Button _toggle;
        private readonly Button _modeSwitch;
        private readonly Label _summary;
        private readonly HBoxContainer _normalToolbar;
        private readonly HBoxContainer _advancedToolbar;
        private readonly LineEdit _advancedInput;
        private readonly Button _edit;
        private readonly Dictionary<string, ModSettingsDropdownChoiceControl<int>> _normalFilterEditors = [];
        private readonly Button _reset;
        private Control? _sidebarHost;
        private LineEdit? _input;
        private string _normalQuery = string.Empty;
        private string _advancedQuery = string.Empty;
        private RitsuSearchOptions _searchOptions = RitsuSearchOptions.Default;
        private RitsuDebugSearchPreferences _preferences = new();
        private RitsuCatalogSearchFields _availableFields = RitsuCatalogSearchFields.All;
        private bool _expanded;
        private ulong? _nestedPopupFrame;
        private ulong? _closedFrame;

        internal RitsuCatalogSearchMenu(
            string catalogId,
            RitsuCatalogFilter[] filters,
            Dictionary<string, int> selections,
            Action changed,
            Action<string> applyQuery,
            string? primaryFilterId = null)
        {
            _catalogId = catalogId;
            _filters = filters;
            _selections = selections;
            _defaults = new(selections, StringComparer.Ordinal);
            _changed = changed;
            _applyQuery = applyQuery;
            _primaryFilterId = primaryFilterId;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            AddThemeConstantOverride("separation", 6);
            _modeSwitch = new ModSettingsMiniButton(string.Empty, () => SwitchMode(!IsAdvanced))
            {
                CustomMinimumSize = new(140f, 44f),
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                ClipText = true,
            };
            AddChild(_modeSwitch);
            _normalToolbar = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _normalToolbar.AddThemeConstantOverride("separation", 8);
            _advancedToolbar = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _advancedToolbar.AddThemeConstantOverride("separation", 8);
            AddChild(_normalToolbar);
            AddChild(_advancedToolbar);
            _advancedInput = ModSettingsUiControlTheming.CreateStyledLineEdit(string.Empty,
                L("mode.expressionPlaceholder", "Expression, e.g. name:Strike AND NOT keyword:Exhaust"), 0f);
            _advancedInput.MaxLength = RitsuCatalogQuery.MaximumLength;
            _advancedInput.ClearButtonEnabled = true;
            _advancedInput.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _advancedInput.TextChanged += value =>
            {
                if (_input == null)
                    return;
                _input.Text = value;
                _input.EmitSignal(LineEdit.SignalName.TextChanged, value);
            };
            _advancedToolbar.AddChild(_advancedInput);
            _edit = new ModSettingsMiniButton(L("editor.open", "Edit"), Open)
            {
                CustomMinimumSize = new(120f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
            };
            _advancedToolbar.AddChild(_edit);
            _body = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _body.AddThemeConstantOverride("separation", 14);
            _toggle = new ModSettingsMiniButton(L("filters", "Filters"), () =>
            {
                if (_expanded)
                    Close();
                else
                    Open();
            });
            _toggle.CustomMinimumSize = new(80f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight);
            _toggle.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            _normalToolbar.AddChild(_toggle);
            _backdrop = new() { Visible = false, TopLevel = true, ZIndex = 850, MouseFilter = MouseFilterEnum.Stop };
            _backdrop.GuiInput += @event =>
            {
                if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                    Close();
            };
            AddChild(_backdrop);
            _panel = new() { Visible = false, TopLevel = true, ZIndex = 851, MouseFilter = MouseFilterEnum.Stop };
            _panel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListShellStyle());
            AddChild(_panel);
            var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            content.AddThemeConstantOverride("separation", 14);
            _panel.AddChild(content);
            var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            content.AddChild(header);
            _summary = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
            };
            _summary.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            _summary.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.Hint);
            header.AddChild(_summary);
            var close = new ModSettingsMiniButton("×", Close) { CustomMinimumSize = new(40f, 40f) };
            header.AddChild(close);
            var scroll = new ScrollContainer
            {
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            ModSettingsUiControlTheming.ApplySettingsScrollContainerThemeForDropdownList(scroll);
            scroll.AddThemeConstantOverride("scrollbar_v_separation", 10);
            content.AddChild(scroll);
            var bodyMargin = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            bodyMargin.AddThemeConstantOverride("margin_right", 12);
            scroll.AddChild(bodyMargin);
            bodyMargin.AddChild(_body);
            _reset = new ModSettingsMiniButton(L("reset", "Restore defaults"), () =>
            {
                _preferences = new();
                foreach (var (id, value) in _defaults)
                    _selections[id] = value;
                RebuildControls();
                SaveAndRefresh();
            });
            content.AddChild(_reset);
            Restore(false);
            SetProcess(false);
            SetProcessUnhandledInput(false);
        }

        internal bool IsAdvanced => _preferences.AdvancedMode;

        internal void BindSearch(LineEdit input, Control sidebarHost)
        {
            _input = input;
            _sidebarHost = sidebarHost;
            input.CustomMinimumSize = new(240f, input.CustomMinimumSize.Y);
            input.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            input.SizeFlagsStretchRatio = 2f;
            if (input.GetParent() != null)
                input.Reparent(_normalToolbar);
            else
                _normalToolbar.AddChild(input);
            _normalToolbar.MoveChild(input, 0);
            AddNormalFilterControls();
            UpdateSearchMode();
        }

        private void AddNormalFilterControls()
        {
            foreach (var filter in _filters.Where(filter => filter.Id != _primaryFilterId))
            {
                var group = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                group.AddThemeConstantOverride("separation", 6);
                var label = TextLabel(filter.Label);
                label.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
                label.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
                group.AddChild(label);
                var options = new List<(int Value, string Label)> { (-1, filter.AllLabel) };
                options.AddRange(filter.Options.Select((option, index) => (index, option.Label)));
                var editor = new ModSettingsDropdownChoiceControl<int>(options, _selections[filter.Id], value =>
                {
                    _selections[filter.Id] = value;
                    SaveAndRefresh();
                });
                editor.CustomMinimumSize = new(140f, editor.CustomMinimumSize.Y);
                editor.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                foreach (var face in editor.GetChildren().OfType<Button>())
                    face.CustomMinimumSize = new(140f, face.CustomMinimumSize.Y);
                group.AddChild(editor);
                _normalToolbar.AddChild(group);
                _normalFilterEditors.Add(filter.Id, editor);
            }
        }

        private void SwitchMode(bool advanced)
        {
            if (IsAdvanced == advanced)
                return;
            Close();
            if (IsAdvanced)
                _advancedQuery = _input?.Text ?? string.Empty;
            else
                _normalQuery = _input?.Text ?? string.Empty;
            _preferences.AdvancedMode = advanced;
            UpdateSearchMode();
            RememberFilters();
            _changed();
            (advanced ? _advancedInput : _input)?.GrabFocus();
        }

        private void UpdateSearchMode()
        {
            _modeSwitch.Text = IsAdvanced ? L("mode.normal", "Normal search") : L("mode.advanced", "Advanced search");
            _normalToolbar.Visible = !IsAdvanced;
            _advancedToolbar.Visible = IsAdvanced;
            _advancedInput.Text = _advancedQuery;
            foreach (var (id, editor) in _normalFilterEditors)
                editor.SetValue(_selections[id]);
            if (_input != null)
            {
                _input.Text = IsAdvanced ? _advancedQuery : _normalQuery;
            }

            RefreshSummary();
        }

        public override void _EnterTree()
        {
            RitsuDebugToolsInterfaceStateStore.StateRestored += Restore;
        }

        public override void _ExitTree()
        {
            RitsuDebugToolsInterfaceStateStore.StateRestored -= Restore;
            Close();
        }

        public override void _Process(double delta)
        {
            if (!_expanded)
            {
                if (_closedFrame is not { } frame || Engine.GetProcessFrames() > frame + 1)
                {
                    SetProcess(false);
                    SetProcessUnhandledInput(false);
                }

                return;
            }

            ObserveNestedPopup();
            if (!IsVisibleInTree())
                Close();
            else
                LayoutPanel();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            ObserveNestedPopup();
            if (GetViewport().IsInputHandled() || @event.IsEcho() ||
                !(@event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack)))
                return;
            if (_closedFrame is { } closed && Engine.GetProcessFrames() <= closed + 1)
            {
                GetViewport().SetInputAsHandled();
                return;
            }

            if (!_expanded)
                return;
            // One physical press can emit both cancel and back actions after a nested dropdown closes.
            if (_nestedPopupFrame is { } frame && Engine.GetProcessFrames() <= frame + 1)
            {
                CloseNestedPopups();
                GetViewport().SetInputAsHandled();
                return;
            }

            Close();
            GetViewport().SetInputAsHandled();
        }

        private void ObserveNestedPopup()
        {
            if (_expanded && Descendants(_body).OfType<IModSettingsDirectionalInputClaimant>()
                    .Any(static owner => owner.ClaimsDirectionalInput))
                _nestedPopupFrame = Engine.GetProcessFrames();
        }

        void IModSettingsTransientPopupOwner.ForceCloseTransientUi()
        {
            Close();
        }

        private void Open()
        {
            if (_sidebarHost == null)
                return;
            RebuildControls();
            _nestedPopupFrame = null;
            _closedFrame = null;
            _expanded = true;
            LayoutPanel();
            _backdrop.Show();
            _panel.Show();
            SetProcess(true);
            SetProcessUnhandledInput(true);
        }

        private void Close()
        {
            if (!_expanded)
                return;
            CloseNestedPopups();
            _expanded = false;
            _closedFrame = Engine.GetProcessFrames();
            _panel.Hide();
            _backdrop.Hide();
            var focus = IsAdvanced ? _edit : _toggle;
            if (focus.IsVisibleInTree())
                focus.GrabFocus();
        }

        private void CloseNestedPopups()
        {
            foreach (var owner in Descendants(_body).OfType<IModSettingsTransientPopupOwner>())
                owner.ForceCloseTransientUi();
        }

        private void LayoutPanel()
        {
            if (_sidebarHost == null)
                return;
            var bounds = _sidebarHost.GetGlobalRect();
            _backdrop.GlobalPosition = bounds.Position;
            _backdrop.Size = bounds.Size;
            _panel.Size = new(Mathf.Min(IsAdvanced ? 700f : 660f, bounds.Size.X), bounds.Size.Y);
            _panel.GlobalPosition = new(bounds.End.X - _panel.Size.X, bounds.Position.Y);
        }

        internal void SetAvailableFields(IEnumerable<RitsuCatalogItem> items)
        {
            var available = RitsuCatalogSearchFields.Default | RitsuCatalogSearchFields.Metadata;
            foreach (var item in items)
                available |= item.SearchDocument?.AvailableFields ?? RitsuCatalogSearchFields.None;
            if (_availableFields == available)
                return;
            _availableFields = available;
            RebuildControls();
        }

        internal void RememberFilters()
        {
            foreach (var filter in _filters)
            {
                var index = _selections.GetValueOrDefault(filter.Id, -1);
                _preferences.Filters[filter.Id] = index >= 0 && index < filter.Options.Count
                    ? filter.Options[index].Id
                    : null;
            }

            RitsuDebugToolsInterfaceStateStore.RememberSearchPreferences(_catalogId, _preferences);
            foreach (var (id, editor) in _normalFilterEditors)
                editor.SetValue(_selections[id]);
            RefreshSummary();
        }

        internal async Task<T[]> FilterAsync<T>(
            T[] source,
            Func<T, RitsuCatalogItem> itemSelector,
            Func<RitsuCatalogItem, bool> matchesFilters,
            string query,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            QueryError = null;
            if (!RitsuCatalogSearchRequest.TryCreate(IsAdvanced, query, _preferences.Fields, _availableFields,
                    _searchOptions, out var request, out var error, out var position))
            {
                QueryError = FormatQueryError(error, position);
                return [];
            }

            var results = new List<T>();
            var slice = Stopwatch.GetTimestamp();
            foreach (var value in source)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = itemSelector(value);
                if (request!.Matches(item, matchesFilters, cancellationToken))
                    results.Add(value);
                if (Stopwatch.GetElapsedTime(slice).TotalMilliseconds < 4)
                    continue;
                await RitsuGodotAwaitSafety.AwaitProcessFrameAsync(GetTree(), this, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsInsideTree())
                    throw new OperationCanceledException(cancellationToken);
                slice = Stopwatch.GetTimestamp();
            }

            return [.. results];
        }

        internal string? QueryError { get; private set; }

        private static string FormatQueryError(string? error, int position)
        {
            var reason = error switch
            {
                "tooLong" => L("query.tooLong", "Use at most 2048 characters."),
                "tooComplex" => L("query.tooComplex", "Use at most 128 terms and 16 nesting levels."),
                "unknownField" => L("query.unknownField", "Unknown field. Use name, id, desc, keyword, or attr."),
                "unavailableField" => L("query.unavailableField", "This field is unavailable in this catalog."),
                "closingParenthesis" => L("query.closingParenthesis", "Missing closing parenthesis."),
                "closingQuote" => L("query.closingQuote", "Missing closing quote."),
                "escape" => L("query.escape", "Inside quotes, only quotes and backslashes can be escaped."),
                "expectedTerm" => L("query.expectedTerm", "Enter a search term."),
                "fieldRequired" => L("query.fieldRequired",
                    "Each condition needs a field: name, id, desc, keyword, or attr."),
                _ => L("query.unexpected", "Unexpected operator or parenthesis."),
            };
            return string.Format(L("query.error", "Search syntax error at character {0}: {1}"), position, reason);
        }

        private void Restore(bool _)
        {
            _preferences = RitsuDebugToolsInterfaceStateStore.GetSearchPreferences(_catalogId);
            foreach (var filter in _filters)
            {
                _selections[filter.Id] = _defaults.GetValueOrDefault(filter.Id, -1);
                if (!_preferences.Filters.TryGetValue(filter.Id, out var id))
                    continue;
                if (id == null)
                    _selections[filter.Id] = -1;
                else
                {
                    var index = filter.Options.ToList().FindIndex(option => option.Id == id);
                    if (index >= 0)
                        _selections[filter.Id] = index;
                }
            }

            RebuildControls();
            UpdateSearchMode();
            if (IsInsideTree())
                _changed();
        }

        private void RebuildControls()
        {
            foreach (var child in _body.GetChildren())
            {
                _body.RemoveChild(child);
                child.QueueFreeSafely();
            }

            _reset.Visible = !IsAdvanced;
            if (IsAdvanced)
            {
                AddExpressionEditor(_body);
                RefreshSummary();
                return;
            }

            var fields = Section(_body, "fields", L("fields", "Search fields"), true);
            var fieldHint = Paragraph(L("fieldsHint", "Choose which text normal search matches."));
            fields.AddChild(fieldHint);
            var choices = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            choices.AddThemeConstantOverride("h_separation", 6);
            choices.AddThemeConstantOverride("v_separation", 6);
            fields.AddChild(choices);
            foreach (var field in new[]
                     {
                         RitsuCatalogSearchFields.Name, RitsuCatalogSearchFields.Id,
                         RitsuCatalogSearchFields.Description, RitsuCatalogSearchFields.Keywords,
                         RitsuCatalogSearchFields.Metadata,
                     })
            {
                if ((_availableFields & field) == 0)
                    continue;
                var selected = (_preferences.Fields & field) != 0;
                var button = ModSettingsUiControlTheming.CreateCompactSettingsToggleButton(FieldLabel(field), selected);
                button.TooltipText = field switch
                {
                    RitsuCatalogSearchFields.Keywords => L("keywordsHint",
                        "Keywords and related terms used by this object"),
                    RitsuCatalogSearchFields.Metadata => L("metadataHint",
                        "Search attribute text such as type, rarity, and source"),
                    _ => FieldLabel(field),
                };
                button.Pressed += () =>
                {
                    _preferences.Fields ^= field;
                    ModSettingsUiControlTheming.ApplySettingsToggleButtonStyle(button,
                        (_preferences.Fields & field) != 0, false);
                    SaveAndRefresh();
                };
                choices.AddChild(button);
            }

            var providers = RitsuSearchExpansionRegistry.GetProviderSnapshots();
            if (providers.Count > 0)
            {
                foreach (var provider in providers)
                {
                    if (provider.Id.Equals(PinyinSearchExpansionProvider.ProviderId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        var providerOptions = Section(_body, $"provider:{provider.Id}", provider.DisplayName);
                        AddMode(providerOptions, L("pinyin", "Full spelling"), _preferences.Pinyin,
                            provider.Enabled, value => _preferences.Pinyin = value);
                        AddMode(providerOptions, L("initials", "Initials"), _preferences.PinyinInitials,
                            provider.Enabled, value => _preferences.PinyinInitials = value);
                    }
                    else
                    {
                        var selected = _preferences.Providers.GetValueOrDefault(provider.Id);
                        var toggle = ModSettingsUiControlTheming.CreateCompactSettingsToggleButton(
                            selected ? L("on", "On") : L("off", "Off"), selected);
                        toggle.Pressed += () =>
                        {
                            var enabled = !_preferences.Providers.GetValueOrDefault(provider.Id);
                            _preferences.Providers[provider.Id] = enabled;
                            toggle.Text = enabled ? L("on", "On") : L("off", "Off");
                            ModSettingsUiControlTheming.ApplySettingsToggleButtonStyle(toggle, enabled, false);
                            SaveAndRefresh();
                        };
                        AddRow(_body, provider.DisplayName, toggle);
                    }
                }
            }

            RefreshSummary();
        }

        private VBoxContainer Section(VBoxContainer parent, string id, string title, bool defaultExpanded = false)
        {
            var section = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            section.AddThemeConstantOverride("separation", 6);
            var body = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Visible = _preferences.ExpandedSections.GetValueOrDefault(id, defaultExpanded),
            };
            body.AddThemeConstantOverride("separation", 6);
            var heading = new ModSettingsMiniButton((body.Visible ? "▾ " : "▸ ") + title, Toggle)
            {
                Alignment = HorizontalAlignment.Left,
                CustomMinimumSize = new(0f, 34f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            heading.Pressed += () => heading.Text = (body.Visible ? "▾ " : "▸ ") + title;
            section.AddChild(heading);
            section.AddChild(body);
            parent.AddChild(section);
            return body;

            void Toggle()
            {
                CloseNestedPopups();
                body.Visible = !body.Visible;
                _preferences.ExpandedSections[id] = body.Visible;
                RitsuDebugToolsInterfaceStateStore.RememberSearchPreferences(_catalogId, _preferences);
            }
        }

        private static Label TextLabel(string text)
        {
            var label = new Label
            {
                Text = text,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            label.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            return label;
        }

        private static Label Paragraph(string text)
        {
            var label = TextLabel(text);
            label.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            label.CustomMinimumSize = new(0f, 32f);
            return label;
        }

        private static void AddRow(VBoxContainer section, string label, Control control)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("separation", 12);
            row.AddChild(TextLabel(label));
            row.AddChild(control);
            section.AddChild(row);
        }

        private void AddMode(VBoxContainer section, string label, bool? selected, bool defaultValue, Action<bool?> set)
        {
            var defaultLabel = defaultValue ? L("on", "On") : L("off", "Off");
            AddRow(section, label, new ModSettingsDropdownChoiceControl<int>(
            [
                (-1, $"{L("inherit", "Default")} ({defaultLabel})"),
                (1, L("on", "On")),
                (0, L("off", "Off")),
            ], selected.HasValue ? selected.Value ? 1 : 0 : -1, value =>
            {
                set(value < 0 ? null : value == 1);
                SaveAndRefresh();
            }));
        }

        private static IEnumerable<Node> Descendants(Node root)
        {
            foreach (var child in root.GetChildren())
            {
                yield return child;
                foreach (var descendant in Descendants(child))
                    yield return descendant;
            }
        }

        private void SaveAndRefresh()
        {
            RememberFilters();
            _changed();
        }

        private void RefreshSummary()
        {
            var selected = _preferences.Fields & _availableFields;
            var labels = Enum.GetValues<RitsuCatalogSearchFields>()
                .Where(field => field is RitsuCatalogSearchFields.Name or RitsuCatalogSearchFields.Id or
                    RitsuCatalogSearchFields.Description or RitsuCatalogSearchFields.Keywords or
                    RitsuCatalogSearchFields.Metadata)
                .Where(field => (selected & field) != 0)
                .Select(FieldLabel);
            _summary.Text = IsAdvanced ? L("editor.title", "Expression editor") : L("title", "Normal search settings");
            _toggle.TooltipText = IsAdvanced
                ? L("editor.title", "Expression editor")
                : selected == RitsuCatalogSearchFields.None
                    ? L("noFields", "Select at least one search field")
                    : string.Join(" · ", labels);
            _toggle.Text = L("filters", "Filters");
            var active = !IsAdvanced && (selected != RitsuCatalogSearchFields.Default ||
                                         _selections.Any(pair =>
                                             pair.Value != _defaults.GetValueOrDefault(pair.Key, -1)) ||
                                         _preferences.Pinyin.HasValue || _preferences.PinyinInitials.HasValue ||
                                         _preferences.Providers.Values.Any(static enabled => enabled));
            ModSettingsUiControlTheming.ApplySettingsToggleButtonStyle(_toggle, active, false);
            ModSettingsUiControlTheming.RefreshAdaptiveButtonText(_toggle);
            _searchOptions = new()
            {
                Pinyin = _preferences.Pinyin,
                PinyinInitials = _preferences.PinyinInitials,
                UseOtherProviders = false,
                ProviderOverrides = _preferences.Providers,
            };
        }

        private static string FieldLabel(RitsuCatalogSearchFields field)
        {
            return field switch
            {
                RitsuCatalogSearchFields.Name => L("name", "Name"),
                RitsuCatalogSearchFields.Id => "ID",
                RitsuCatalogSearchFields.Description => L("description", "Description"),
                RitsuCatalogSearchFields.Keywords => L("keywords", "Keywords"),
                RitsuCatalogSearchFields.Metadata => L("metadata", "Attributes"),
                _ => string.Empty,
            };
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get($"ritsulib.catalog.search.{key}", fallback);
        }
    }
}
