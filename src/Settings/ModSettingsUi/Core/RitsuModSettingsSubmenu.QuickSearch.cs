using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Ui.Overlay;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Ui.Windows;

namespace STS2RitsuLib.Settings
{
    public partial class RitsuModSettingsSubmenu
    {
        private const ulong QuickSearchDoubleShiftIntervalMsec = 420;
        private const int QuickSearchResultLimit = 40;
        private readonly List<ModSettingsSidebarButton> _quickSearchResultButtons = [];
        private CancellationTokenSource? _quickSearchCancellation;
        private ModSettingsMiniButton? _quickSearchButton;
        private LineEdit? _quickSearchEdit;
        private IReadOnlyList<ModSettingsSearchResult> _quickSearchIndex = [];
        private ulong _quickSearchLastShiftTapMsec;
        private bool _quickSearchOpen;

        private Control? _quickSearchOverlay;
        private Control? _quickSearchPreviousFocus;
        private int _quickSearchRefreshEpoch;
        private IReadOnlyList<ModSettingsSearchResult> _quickSearchResults = [];
        private VBoxContainer? _quickSearchResultsHost;
        private ScrollContainer? _quickSearchScroll;
        private int _quickSearchSelectedIndex = -1;
        private RitsuFloatingWindow? _quickSearchWindow;

        private bool TryHandleQuickSearchInput(InputEvent inputEvent)
        {
            if (_quickSearchOpen)
                return TryHandleOpenQuickSearchInput(inputEvent);
            if (!Visible || !IsInsideTree() || !ActiveScreenContext.Instance.IsCurrent(this))
                return false;

            if (inputEvent is InputEventMouseButton { Pressed: true })
            {
                _quickSearchLastShiftTapMsec = 0;
                return false;
            }

            if (inputEvent is not InputEventKey { Pressed: true } keyEvent || keyEvent.IsEcho())
                return false;
            if (!IsBareShiftPress(keyEvent) || IsFocusNavigationBlocked() ||
                IsQuickSearchShortcutClaimedByFocusedControl())
            {
                _quickSearchLastShiftTapMsec = 0;
                return false;
            }

            var now = Time.GetTicksMsec();
            if (_quickSearchLastShiftTapMsec == 0 ||
                now - _quickSearchLastShiftTapMsec > QuickSearchDoubleShiftIntervalMsec)
            {
                _quickSearchLastShiftTapMsec = now;
                return false;
            }

            _quickSearchLastShiftTapMsec = 0;
            OpenQuickSearch();
            return true;
        }

        private bool TryHandleOpenQuickSearchInput(InputEvent inputEvent)
        {
            if (inputEvent.IsEcho())
                return false;
            if (IsQuickSearchCancelInput(inputEvent))
            {
                CloseQuickSearch(true);
                return true;
            }

            var selectionDelta = inputEvent.IsActionPressed("ui_up")
                ? -1
                : inputEvent.IsActionPressed("ui_down")
                    ? 1
                    : 0;
            if (selectionDelta != 0)
            {
                MoveQuickSearchSelection(selectionDelta);
                return true;
            }

            if (IsQuickSearchAcceptInput(inputEvent))
            {
                OpenSelectedQuickSearchResult();
                return true;
            }

            return false;
        }

        private void OpenQuickSearch()
        {
            EnsureQuickSearchOverlay();
            if (_quickSearchOverlay == null || _quickSearchWindow == null || _quickSearchEdit == null)
                return;

            _quickSearchPreviousFocus = GetViewport()?.GuiGetFocusOwner();
            _quickSearchIndex = ModSettingsSearchIndex.BuildVisible();
            _quickSearchOpen = true;
            _quickSearchEdit.Text = string.Empty;
            RefreshQuickSearchResults();
            _quickSearchWindow.Show();
            _quickSearchOverlay.Show();
            Callable.From(() =>
            {
                if (!_quickSearchOpen || !IsInstanceValid(_quickSearchEdit) || !_quickSearchEdit.IsVisibleInTree())
                    return;
                _quickSearchEdit.GrabFocus();
            }).CallDeferred();
        }

        private void CloseQuickSearch(bool restoreFocus)
        {
            _quickSearchLastShiftTapMsec = 0;
            _quickSearchRefreshEpoch++;
            CancelQuickSearch();
            if (!_quickSearchOpen)
                return;

            _quickSearchOpen = false;
            _quickSearchOverlay?.Hide();
            _quickSearchWindow?.Hide();
            if (restoreFocus)
                RestoreQuickSearchFocus();
            else
                _quickSearchPreviousFocus = null;
        }

        private void ResetQuickSearchOverlay()
        {
            CloseQuickSearch(false);
            if (IsInstanceValid(_quickSearchOverlay))
                _quickSearchOverlay.QueueFree();
            _quickSearchOverlay = null;
            _quickSearchWindow = null;
            _quickSearchEdit = null;
            _quickSearchResultsHost = null;
            _quickSearchScroll = null;
            _quickSearchIndex = [];
            _quickSearchResults = [];
            _quickSearchResultButtons.Clear();
        }

        private void EnsureQuickSearchOverlay()
        {
            if (IsInstanceValid(_quickSearchOverlay))
                return;

            var overlay = new Control
            {
                Name = "SettingsQuickSearchOverlay",
                MouseFilter = MouseFilterEnum.Stop,
                Visible = false,
                ZIndex = 500,
            };
            overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(overlay);
            _quickSearchOverlay = overlay;

            var backdrop = new ColorRect
            {
                Color = RitsuShellTheme.Current.Color.ModalBackdrop,
                MouseFilter = MouseFilterEnum.Stop,
            };
            backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            backdrop.GuiInput += inputEvent =>
            {
                if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                    CloseQuickSearch(true);
            };
            overlay.AddChild(backdrop);

            var window = new RitsuFloatingWindow(new()
            {
                Title = ModSettingsLocalization.Get("search.title", "Search settings"),
                InitialSize = new(780f, 560f),
                MinimumSize = new(560f, 360f),
                MaximumSize = new(900f, 680f),
                Movable = false,
                Resizable = false,
                Closable = true,
                StartCentered = true,
                ConstrainToViewport = true,
                FitInitialSizeToContent = false,
            })
            {
                ZIndex = 1,
            };
            window.Closed += (_, _) => CloseQuickSearch(true);
            window.SetContent(CreateQuickSearchContent());
            overlay.AddChild(window);
            _quickSearchWindow = window;
        }

        private Control CreateQuickSearchContent()
        {
            var content = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            content.AddThemeConstantOverride("separation", 10);

            var edit = ModSettingsUiControlTheming.CreateStyledLineEdit(
                ModSettingsLocalization.Get("search.placeholder", "Search mods, pages, sections, and settings"),
                0f,
                48f,
                18);
            edit.ClearButtonEnabled = true;
            edit.TextChanged += _ => QueueQuickSearchRefresh();
            edit.TextSubmitted += _ => OpenSelectedQuickSearchResult();
            content.AddChild(edit);
            _quickSearchEdit = edit;

            var hint = new Label
            {
                Text = ModSettingsLocalization.Get("search.hint",
                    "Double-tap Shift to open · ↑/↓ to select · Enter to navigate · Esc to close"),
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            hint.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            hint.AddThemeFontSizeOverride("font_size", 14);
            hint.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.RichMuted);
            content.AddChild(hint);

            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            };
            ModSettingsUiControlTheming.ApplySettingsScrollContainerTheme(scroll);
            content.AddChild(scroll);
            _quickSearchScroll = scroll;

            var scrollFrame = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            scrollFrame.AddThemeConstantOverride("margin_right",
                ModSettingsUiControlTheming.ResolveSettingsScrollContentRightGutter(scroll));
            scroll.AddChild(scrollFrame);

            var resultsHost = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            resultsHost.AddThemeConstantOverride("separation", 5);
            scrollFrame.AddChild(resultsHost);
            _quickSearchResultsHost = resultsHost;
            return content;
        }

        private void QueueQuickSearchRefresh()
        {
            var epoch = ++_quickSearchRefreshEpoch;
            Callable.From(() =>
            {
                if (!_quickSearchOpen || epoch != _quickSearchRefreshEpoch)
                    return;
                RefreshQuickSearchResults();
            }).CallDeferred();
        }

        private void RefreshQuickSearchResults()
        {
            if (_quickSearchResultsHost == null || _quickSearchEdit == null)
                return;

            CancelQuickSearch();
            ClearQuickSearchResults();
            var query = _quickSearchEdit.Text.Trim();
            if (query.Length == 0)
            {
                _quickSearchResults = [];
                _quickSearchSelectedIndex = -1;
                AddQuickSearchStatus(ModSettingsLocalization.Get("search.start",
                    "Type a setting name or description to find its destination."));
                return;
            }

            var epoch = _quickSearchRefreshEpoch;
            _quickSearchCancellation = new();
            AddQuickSearchStatus(FormatQuickSearchProgress(0, _quickSearchIndex.Count));
            ObserveBackgroundUiTask(
                RefreshQuickSearchResultsAsync(query, epoch, _quickSearchCancellation.Token),
                $"quick_search:{query}");
        }

        private async Task RefreshQuickSearchResultsAsync(
            string query,
            int epoch,
            CancellationToken cancellationToken)
        {
            await foreach (var batch in ModSettingsSearchIndex.SearchStreamAsync(
                               _quickSearchIndex,
                               query,
                               QuickSearchResultLimit,
                               cancellationToken))
            {
                if (!_quickSearchOpen || epoch != _quickSearchRefreshEpoch || cancellationToken.IsCancellationRequested)
                    break;
                RenderQuickSearchBatch(batch);
            }
        }

        private void RenderQuickSearchBatch(ModSettingsSearchBatch batch)
        {
            if (_quickSearchResultsHost == null)
                return;
            ClearQuickSearchResults();
            _quickSearchResults = batch.Results;
            if (_quickSearchResults.Count == 0)
            {
                _quickSearchSelectedIndex = -1;
                AddQuickSearchStatus(batch.IsComplete
                    ? ModSettingsLocalization.Get("search.empty", "No matching settings found.")
                    : FormatQuickSearchProgress(batch.ProcessedCount, batch.TotalCount));
                return;
            }

            for (var index = 0; index < _quickSearchResults.Count; index++)
            {
                var capturedIndex = index;
                var result = _quickSearchResults[index];
                var button = new ModSettingsSidebarButton(
                    $"{result.Title}\n{result.Path}",
                    () => OpenQuickSearchResult(capturedIndex),
                    ModSettingsSidebarItemKind.Utility)
                {
                    CustomMinimumSize = new(0f, 62f),
                    TooltipText = $"{result.Title}\n{result.Path}",
                };
                button.MouseEntered += () => SetQuickSearchSelection(capturedIndex, false);
                _quickSearchResultsHost.AddChild(button);
                _quickSearchResultButtons.Add(button);
            }

            SetQuickSearchSelection(0, false);
            if (!batch.IsComplete)
                AddQuickSearchStatus(FormatQuickSearchProgress(batch.ProcessedCount, batch.TotalCount));
            else if (batch.HasMore)
                AddQuickSearchStatus(string.Format(
                    ModSettingsLocalization.Get("search.more", "Showing the first {0} matches. More are available."),
                    QuickSearchResultLimit));
        }

        private void ClearQuickSearchResults()
        {
            if (_quickSearchResultsHost == null)
                return;
            foreach (var child in _quickSearchResultsHost.GetChildren())
            {
                _quickSearchResultsHost.RemoveChild(child);
                child.QueueFree();
            }

            _quickSearchResultButtons.Clear();
        }

        private void CancelQuickSearch()
        {
            _quickSearchCancellation?.Cancel();
            _quickSearchCancellation?.Dispose();
            _quickSearchCancellation = null;
        }

        private static string FormatQuickSearchProgress(int processedCount, int totalCount)
        {
            return string.Format(
                ModSettingsLocalization.Get("search.searching", "Searching… {0}/{1}"),
                processedCount,
                totalCount);
        }

        private void AddQuickSearchStatus(string text)
        {
            if (_quickSearchResultsHost == null)
                return;
            var label = new Label
            {
                Text = text,
                CustomMinimumSize = new(0f, 96f),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            label.AddThemeFontSizeOverride("font_size", 17);
            label.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.RichMuted);
            _quickSearchResultsHost.AddChild(label);
        }

        private void MoveQuickSearchSelection(int delta)
        {
            if (_quickSearchResultButtons.Count == 0)
                return;
            var next = _quickSearchSelectedIndex < 0
                ? delta > 0 ? 0 : _quickSearchResultButtons.Count - 1
                : Math.Clamp(_quickSearchSelectedIndex + delta, 0, _quickSearchResultButtons.Count - 1);
            SetQuickSearchSelection(next, true);
        }

        private void SetQuickSearchSelection(int index, bool ensureVisible)
        {
            if (index < 0 || index >= _quickSearchResultButtons.Count)
                return;
            _quickSearchSelectedIndex = index;
            for (var i = 0; i < _quickSearchResultButtons.Count; i++)
                _quickSearchResultButtons[i].SetSelected(i == index);
            if (ensureVisible && IsInstanceValid(_quickSearchScroll))
                _quickSearchScroll.EnsureControlVisible(_quickSearchResultButtons[index]);
        }

        private void OpenSelectedQuickSearchResult()
        {
            OpenQuickSearchResult(_quickSearchSelectedIndex);
        }

        private void OpenQuickSearchResult(int index)
        {
            if (index < 0 || index >= _quickSearchResults.Count)
                return;
            var result = _quickSearchResults[index];
            CloseQuickSearch(false);
            ObserveBackgroundUiTask(OpenQuickSearchResultAsync(result),
                $"quick_search:{result.Location.ModId}:{result.Location.PageId}");
        }

        private async Task OpenQuickSearchResultAsync(ModSettingsSearchResult result)
        {
            var openResult = await OpenToAsync(result.Location, new()
            {
                ExpandCollapsedSection = true,
                Focus = true,
                Highlight = true,
            });
            if (!openResult.Success)
                RitsuLibFramework.Logger.Warn(
                    $"[Settings] Quick search could not open '{result.Title}': {openResult.Message}");
        }

        private void RestoreQuickSearchFocus()
        {
            var target = _quickSearchPreviousFocus;
            _quickSearchPreviousFocus = null;
            if (!IsInstanceValid(target) || !target.IsVisibleInTree())
                return;
            Callable.From(() =>
            {
                if (IsInstanceValid(target) && target.IsVisibleInTree())
                    target.GrabFocus();
            }).CallDeferred();
        }

        private void ApplyQuickSearchButtonPresentation()
        {
            if (!IsInstanceValid(_quickSearchButton))
                return;
            _quickSearchButton.Text = string.Empty;
            _quickSearchButton.Icon = RitsuDebugToolsIcons.Get(
                RitsuDebugToolsGlyph.Search,
                18,
                RitsuShellTheme.Current.Text.LabelPrimary);
            _quickSearchButton.TooltipText =
                $"{ModSettingsLocalization.Get("search.buttonTooltip", "Search all settings")}\n" +
                ModSettingsLocalization.Get("search.buttonShortcut",
                    "Shortcut: double-tap Shift");
        }

        private bool IsQuickSearchShortcutClaimedByFocusedControl()
        {
            var owner = GetViewport()?.GuiGetFocusOwner();
            for (Node? node = owner; node != null && !ReferenceEquals(node, this); node = node.GetParent())
                if (node is IModSettingsDirectionalInputClaimant { ClaimsDirectionalInput: true })
                    return true;
            return false;
        }

        private static bool IsBareShiftPress(InputEventKey keyEvent)
        {
            var keycode = keyEvent.Keycode != Key.None ? keyEvent.Keycode : keyEvent.PhysicalKeycode;
            return keycode == Key.Shift &&
                   keyEvent is { CtrlPressed: false, AltPressed: false, MetaPressed: false };
        }

        private static bool IsQuickSearchCancelInput(InputEvent inputEvent)
        {
            return inputEvent is InputEventKey { Pressed: true, Keycode: Key.Escape } ||
                   inputEvent.IsActionPressed("ui_cancel") ||
                   inputEvent.IsActionPressed(MegaInput.cancel) ||
                   inputEvent.IsActionPressed(MegaInput.pauseAndBack);
        }

        private static bool IsQuickSearchAcceptInput(InputEvent inputEvent)
        {
            return inputEvent is InputEventKey { Pressed: true, Keycode: Key.Enter or Key.KpEnter } ||
                   inputEvent.IsActionPressed("ui_accept") ||
                   inputEvent.IsActionPressed(MegaInput.select) ||
                   inputEvent.IsActionPressed(Sts2InputCompat.ConfirmAction);
        }
    }
}
