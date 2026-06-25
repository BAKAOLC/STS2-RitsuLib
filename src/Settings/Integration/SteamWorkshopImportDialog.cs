using Godot;
using STS2RitsuLib.Platform.Steam;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class SteamWorkshopImportDialog : CanvasLayer
    {
        private const int ModalLayer = 132;

        private readonly IReadOnlyList<ulong> _itemIds = null!;
        private readonly Dictionary<ulong, RitsuSteamWorkshopItem> _itemsById = [];
        private readonly Action? _onSubscriptionsChanged;
        private ModSettingsTextButton? _confirmButton;
        private WorkshopImportItemCanvas? _itemCanvas;
        private Control? _previousFocus;
        private Label? _statusLabel;
        private bool _submitting;

        private SteamWorkshopImportDialog(IReadOnlyList<ulong> itemIds, Action? onSubscriptionsChanged)
        {
            _itemIds = itemIds;
            _onSubscriptionsChanged = onSubscriptionsChanged;
            Layer = ModalLayer;
            Name = "SteamWorkshopImportDialog";
        }

        public SteamWorkshopImportDialog()
        {
        }

        internal static bool Show(string workshopItemIdText, Node? attachParent = null,
            Action? onSubscriptionsChanged = null)
        {
            var itemIds = SteamWorkshopManager.ParseWorkshopItemIds(workshopItemIdText);
            if (itemIds.Count == 0)
                return false;

            var parent = ResolveAttachParent(attachParent);
            if (parent == null)
                return false;

            var dialog = new SteamWorkshopImportDialog(itemIds, onSubscriptionsChanged);
            parent.AddChild(dialog);
            return true;
        }

        public override void _Ready()
        {
            base._Ready();
            _previousFocus = GetViewport()?.GuiGetFocusOwner();
            Build();
            _ = LoadItemsAsync();
        }

        private static Node? ResolveAttachParent(Node? attachParent)
        {
            if (attachParent != null && IsInstanceValid(attachParent))
                return attachParent.IsInsideTree() ? attachParent.GetTree()?.Root : null;

            return Engine.GetMainLoop() is SceneTree tree ? tree.Root : null;
        }

        private void Build()
        {
            var root = new ModalRoot(CloseDialog)
            {
                MouseFilter = Control.MouseFilterEnum.Stop,
            };
            root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            AddChild(root);

            var dim = new ColorRect
            {
                Color = RitsuShellTheme.Current.Color.ModalBackdrop,
                MouseFilter = Control.MouseFilterEnum.Stop,
            };
            dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            root.AddChild(dim);

            var viewportMargin = new MarginContainer
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            viewportMargin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            viewportMargin.AddThemeConstantOverride("margin_left", 32);
            viewportMargin.AddThemeConstantOverride("margin_top", 28);
            viewportMargin.AddThemeConstantOverride("margin_right", 32);
            viewportMargin.AddThemeConstantOverride("margin_bottom", 28);
            root.AddChild(viewportMargin);

            var panel = new PanelContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Stop,
            };
            panel.AddThemeStyleboxOverride("panel",
                RitsuShellPanelStyles.CreateFramedSurface(
                    RitsuShellTheme.Current.Surface.Content,
                    RitsuShellTheme.Current.Metric.Radius.Default));
            viewportMargin.AddChild(panel);

            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 22);
            margin.AddThemeConstantOverride("margin_top", 20);
            margin.AddThemeConstantOverride("margin_right", 22);
            margin.AddThemeConstantOverride("margin_bottom", 20);
            panel.AddChild(margin);

            var box = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            box.AddThemeConstantOverride("separation", 12);
            margin.AddChild(box);

            box.AddChild(CreateLabel(
                L("ritsulib.steamWorkshopManagement.import.title", "Import Workshop subscriptions"),
                RitsuShellTheme.Current.Text.RichTitle,
                28,
                true));

            _statusLabel = CreateLabel(
                string.Format(
                    L("ritsulib.steamWorkshopManagement.import.loading",
                        "Loading details for {0} Workshop item(s)..."),
                    _itemIds.Count),
                RitsuShellTheme.Current.Text.RichMuted,
                15);
            box.AddChild(_statusLabel);

            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                MouseFilter = Control.MouseFilterEnum.Stop,
                FocusMode = Control.FocusModeEnum.None,
            };
            ModSettingsUiControlTheming.ApplySettingsScrollContainerTheme(scroll);
            box.AddChild(scroll);

            var marginHost = new MarginContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            marginHost.AddThemeConstantOverride(
                "margin_right",
                ModSettingsUiControlTheming.ResolveSettingsScrollContentRightGutter(scroll));
            scroll.AddChild(marginHost);

            _itemCanvas = new(UpdateSelectionStatus)
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            marginHost.AddChild(_itemCanvas);
            _itemCanvas.SetMessage(L("ritsulib.steamWorkshopManagement.import.loadingRows", "Loading..."));

            box.AddChild(BuildButtonRow());
            Callable.From(GrabInitialFocus).CallDeferred();
        }

        private Control BuildButtonRow()
        {
            var row = new HBoxContainer
            {
                Alignment = BoxContainer.AlignmentMode.End,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 10);

            row.AddChild(new ModSettingsTextButton(
                L("baselib.restoreDefaults.cancel", "Cancel"),
                ModSettingsButtonTone.Normal,
                CloseDialog)
            {
                CustomMinimumSize = new(140f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
            });

            _confirmButton = new(
                L("ritsulib.steamWorkshopManagement.import.subscribeSelected", "Subscribe selected"),
                ModSettingsButtonTone.Accent,
                SubscribeSelected)
            {
                CustomMinimumSize = new(190f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
            };
            row.AddChild(_confirmButton);
            return row;
        }

        private async Task LoadItemsAsync()
        {
            IReadOnlyList<RitsuSteamWorkshopItem> items = [];
            string? error = null;
            try
            {
                items = await SteamWorkshopManager.Instance.QueryItemsAsync(_itemIds).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            Callable.From(() =>
            {
                if (!IsInsideTree())
                    return;

                _itemsById.Clear();
                foreach (var item in items)
                    if (item.Id != 0)
                        _itemsById[item.Id] = item;

                RebuildRows();
                if (error == null)
                    UpdateSelectionStatus();
                else
                    SetStatus(
                        string.Format(
                            L("ritsulib.steamWorkshopManagement.import.detailsFailed",
                                "Could not load Workshop details: {0}. Showing item ids only."),
                            error),
                        true);
            }).CallDeferred();
        }

        private void RebuildRows()
        {
            if (_itemCanvas == null)
                return;

            var items = _itemIds
                .Select(itemId => _itemsById.GetValueOrDefault(itemId) ?? CreatePlaceholderItem(itemId))
                .OrderBy(static item => item.Id)
                .ToArray();
            _itemCanvas.SetItems(items);

            Callable.From(GrabInitialFocus).CallDeferred();
        }

        private void SubscribeSelected()
        {
            if (_submitting)
                return;

            var selected = _itemCanvas?.GetSelectedItems() ?? [];
            if (selected.Length == 0)
            {
                SetStatus(
                    L("ritsulib.steamWorkshopManagement.import.noneSelected",
                        "Select at least one Workshop item to subscribe."),
                    true);
                return;
            }

            _submitting = true;
            if (_confirmButton != null)
                _confirmButton.Disabled = true;

            var accepted = selected
                .Select(item => SteamWorkshopUpdateCoordinator.SubscribeItemFromUi(item.Id, item.DisplayName))
                .Count(result => result.Accepted);

            if (accepted == 0)
            {
                _submitting = false;
                if (_confirmButton != null)
                    _confirmButton.Disabled = false;
                SetStatus(
                    L("ritsulib.steamWorkshopManagement.import.noneAccepted",
                        "Steam did not accept any subscription requests."),
                    true);
                return;
            }

            _onSubscriptionsChanged?.Invoke();
            CloseDialog();
        }

        private void UpdateSelectionStatus()
        {
            var selected = _itemCanvas?.SelectedCount ?? _itemIds.Count;
            var pending = _itemCanvas?.PendingCount ?? 0;
            if (_itemCanvas is { TotalCount: > 0 } && pending == 0)
            {
                SetStatus(
                    L("ritsulib.steamWorkshopManagement.import.allSubscribed",
                        "All imported Workshop items are already subscribed."),
                    false);
                _confirmButton?.Configure(
                    L("ritsulib.steamWorkshopManagement.import.allSubscribedButton", "All subscribed"),
                    ModSettingsButtonTone.Normal,
                    null);
                if (_confirmButton != null)
                    _confirmButton.Disabled = true;
                return;
            }

            if (_confirmButton != null && !_submitting)
                _confirmButton.Disabled = false;
            SetStatus(
                string.Format(
                    L("ritsulib.steamWorkshopManagement.import.previewCount",
                        "Previewing {0} Workshop item(s). Selected for subscription: {1}."),
                    _itemIds.Count,
                    selected),
                false);
        }

        private void SetStatus(string text, bool warning)
        {
            if (_statusLabel == null)
                return;

            _statusLabel.Text = text;
            _statusLabel.AddThemeColorOverride(
                "font_color",
                warning ? RitsuShellTheme.Current.Text.HoverHighlight : RitsuShellTheme.Current.Text.RichMuted);
        }

        private void GrabInitialFocus()
        {
            if (!IsInsideTree())
                return;

            if (_itemCanvas?.GrabFirstInteractiveFocus() == true) return;

            if (_confirmButton is { } confirmButton && IsInstanceValid(confirmButton))
                confirmButton.GrabFocus();
        }

        private void CloseDialog()
        {
            QueueFree();
            RestorePreviousFocus();
        }

        private void RestorePreviousFocus()
        {
            var target = _previousFocus;
            if (target == null || !IsInstanceValid(target) || !target.IsVisibleInTree())
                return;

            Callable.From(() =>
            {
                if (IsInstanceValid(target) && target.IsVisibleInTree())
                    target.GrabFocus();
            }).CallDeferred();
        }

        private static RitsuSteamWorkshopItem CreatePlaceholderItem(ulong itemId)
        {
            return new(
                itemId,
                string.Format(
                    L("ritsulib.steamWorkshopManagement.import.placeholderName", "Workshop item {0}"),
                    itemId),
                null,
                null,
                null,
                null,
                null,
                false,
                false,
                false,
                false,
                false,
                null,
                null);
        }

        private static Label CreateLabel(string text, Color color, int fontSize, bool bold = false)
        {
            var label = new Label
            {
                Text = text,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            label.AddThemeFontOverride(
                "font",
                bold ? RitsuShellTheme.Current.Font.BodyBold : RitsuShellTheme.Current.Font.Body);
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeColorOverride("font_color", color);
            return label;
        }

        private static string FormatSubscriptionState(RitsuSteamWorkshopItem item)
        {
            return string.Format(
                L("ritsulib.steamWorkshopManagement.import.subscriptionState", "ID: {0} | {1}"),
                item.Id,
                item.IsSubscribed
                    ? L("ritsulib.steamWorkshopManagement.state.subscribed", "subscribed")
                    : L("ritsulib.steamWorkshopManagement.import.notSubscribed", "not subscribed"));
        }

        private static string FormatDescription(RitsuSteamWorkshopItem item)
        {
            return string.IsNullOrWhiteSpace(item.Description)
                ? L("ritsulib.steamWorkshopManagement.import.noDescription", "No description was provided.")
                : item.Description.Trim();
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }

        private sealed partial class WorkshopImportItemCanvas(Action selectionChanged) : Control
        {
            private const float CardHeight = 188f;
            private const float CardGap = 10f;
            private const float ButtonHeight = 36f;
            private const float ButtonGap = 8f;
            private const float DividerHeight = 44f;
            private const float Padding = 10f;
            private const float PreviewSize = 96f;
            private readonly List<CardButtonBinding> _buttonBindings = [];
            private readonly List<CardState> _cards = [];
            private readonly HashSet<string> _loadingPreviewUrls = new(StringComparer.Ordinal);
            private readonly Dictionary<string, Texture2D?> _previewTextures = new(StringComparer.Ordinal);
            private string? _message;

            public int TotalCount => _cards.Count;
            public int PendingCount => _cards.Count(static card => !card.Item.IsSubscribed);
            public int SelectedCount => _cards.Count(static card => card.Selected && !card.Item.IsSubscribed);

            private bool AllSubscribed => _cards.Count > 0 && _cards.All(static card => card.Item.IsSubscribed);

            public RitsuSteamWorkshopItem[] GetSelectedItems()
            {
                return _cards
                    .Where(static card => card.Selected && !card.Item.IsSubscribed)
                    .Select(static card => card.Item)
                    .ToArray();
            }

            public void SetMessage(string? message)
            {
                _message = message;
                _cards.Clear();
                HideButtons(0);
                UpdateMinimumHeight();
                QueueRedraw();
            }

            public void SetItems(IReadOnlyList<RitsuSteamWorkshopItem> items)
            {
                _message = null;
                _cards.Clear();
                foreach (var item in items
                             .OrderBy(static item => item.IsSubscribed)
                             .ThenBy(static item => item.Id))
                    _cards.Add(new(item, !item.IsSubscribed));

                UpdateMinimumHeight();
                SyncButtons();
                QueueRedraw();
            }

            public bool GrabFirstInteractiveFocus()
            {
                var button = _buttonBindings
                    .Select(static binding => binding.ActionButton)
                    .FirstOrDefault(static button => button.Visible && !button.Disabled) ?? _buttonBindings
                    .Select(static binding => binding.OpenButton)
                    .FirstOrDefault(static button => button.Visible && !button.Disabled);
                if (button == null)
                    return false;

                button.GrabFocus();
                return true;
            }

            public override void _Ready()
            {
                base._Ready();
                SyncButtons();
            }

            public override void _Notification(int what)
            {
                base._Notification(what);
                if (what != (int)NotificationResized) return;
                UpdateMinimumHeight();
                SyncButtons();
            }

            public override void _Draw()
            {
                if (!string.IsNullOrWhiteSpace(_message))
                {
                    DrawString(
                        RitsuShellTheme.Current.Font.Body,
                        new(Padding, 30f),
                        _message,
                        HorizontalAlignment.Left,
                        -1f,
                        15,
                        RitsuShellTheme.Current.Text.RichMuted);
                    return;
                }

                var layout = GetLayout();
                for (var i = 0; i < _cards.Count; i++)
                    DrawCard(_cards[i], GetCardRect(i, layout));

                if (FindDividerY(layout) is { } dividerY)
                    DrawDivider(dividerY, AllSubscribed
                        ? L("ritsulib.steamWorkshopManagement.import.allSubscribed",
                            "All imported Workshop items are already subscribed.")
                        : L("ritsulib.steamWorkshopManagement.import.subscribedDivider",
                            "Already subscribed"));
            }

            private void DrawCard(CardState state, Rect2 rect)
            {
                DrawRect(rect, RitsuShellTheme.Current.Surface.Content);
                DrawRect(rect, state.Item.IsSubscribed
                    ? RitsuShellTheme.Current.Surface.Inset.Border
                    : RitsuShellTheme.Current.Text.HoverHighlight, false, 1.2f);

                var preview = new Rect2(rect.Position + new Vector2(Padding, Padding), new(PreviewSize, PreviewSize));
                DrawRect(preview, RitsuShellTheme.Current.Surface.Inset.Bg);
                DrawRect(preview, RitsuShellTheme.Current.Surface.Inset.Border, false, 1f);
                DrawPreview(state.Item, preview);

                var textX = preview.End.X + 10f;
                var textWidth = Math.Max(80f, rect.End.X - textX - Padding);
                var y = rect.Position.Y + Padding + 18f;
                DrawTextLine(state.Item.DisplayName, textX, y, textWidth, RitsuShellTheme.Current.Text.RichTitle,
                    RitsuShellTheme.Current.Font.BodyBold, 16);
                y += 22f;
                DrawTextLine(FormatSubscriptionState(state.Item), textX, y, textWidth,
                    RitsuShellTheme.Current.Text.RichMuted, RitsuShellTheme.Current.Font.Body, 13);
                y += 20f;
                DrawWrappedText(FormatDescription(state.Item), textX, y, textWidth, 2,
                    RitsuShellTheme.Current.Text.RichBody, RitsuShellTheme.Current.Font.Body, 13);
            }

            private void DrawPreview(RitsuSteamWorkshopItem item, Rect2 rect)
            {
                if (string.IsNullOrWhiteSpace(item.PreviewUrl))
                {
                    DrawCenteredText(
                        L("ritsulib.steamWorkshopManagement.preview.empty", "No preview"),
                        rect,
                        RitsuShellTheme.Current.Text.RichMuted,
                        12);
                    return;
                }

                if (_previewTextures.TryGetValue(item.PreviewUrl, out var texture) && texture != null)
                {
                    DrawTextureRect(texture, rect.Grow(-4f), false);
                    return;
                }

                DrawCenteredText("...", rect, RitsuShellTheme.Current.Text.RichMuted, 14);
                if (_loadingPreviewUrls.Add(item.PreviewUrl))
                    _ = LoadPreviewAsync(item.PreviewUrl);
            }

            private async Task LoadPreviewAsync(string previewUrl)
            {
                var texture = await SteamWorkshopManager.Instance.LoadPreviewTextureAsync(previewUrl)
                    .ConfigureAwait(false);
                Callable.From(() =>
                {
                    if (!IsInstanceValid(this))
                        return;

                    _previewTextures[previewUrl] = texture;
                    _loadingPreviewUrls.Remove(previewUrl);
                    QueueRedraw();
                }).CallDeferred();
            }

            private void SyncButtons()
            {
                if (!IsInsideTree() || !string.IsNullOrWhiteSpace(_message))
                {
                    HideButtons(0);
                    return;
                }

                var layout = GetLayout();
                for (var i = 0; i < _cards.Count; i++)
                {
                    var rect = GetCardRect(i, layout);
                    var (openRect, actionRect) = GetButtonRects(rect);
                    var state = _cards[i];
                    var binding = EnsureButtonBinding(i);
                    ConfigureButton(binding.OpenButton, openRect,
                        L("ritsulib.steamWorkshopManagement.open.button", "Open"),
                        ModSettingsButtonTone.Normal,
                        false,
                        () => SteamWorkshopManager.Instance.TryOpenWorkshopPage(state.Item.Id));
                    ConfigureButton(binding.ActionButton, actionRect,
                        ResolveSelectionButtonText(state),
                        state.Selected ? ModSettingsButtonTone.Accent : ModSettingsButtonTone.Normal,
                        state.Item.IsSubscribed,
                        () => ToggleItemSelection(state));
                    binding.ActionButton.SetSelected(state.Selected && !state.Item.IsSubscribed);
                }

                HideButtons(_cards.Count);
            }

            private void ToggleItemSelection(CardState state)
            {
                if (state.Item.IsSubscribed)
                    return;

                state.Selected = !state.Selected;
                SyncButtons();
                selectionChanged();
                QueueRedraw();
            }

            private CardButtonBinding EnsureButtonBinding(int index)
            {
                while (_buttonBindings.Count <= index)
                {
                    var binding = new CardButtonBinding(new(), new());
                    AddChild(binding.OpenButton);
                    AddChild(binding.ActionButton);
                    _buttonBindings.Add(binding);
                }

                return _buttonBindings[index];
            }

            private static void ConfigureButton(
                ModSettingsTextButton button,
                Rect2 rect,
                string text,
                ModSettingsButtonTone tone,
                bool disabled,
                Action action)
            {
                button.Configure(text, tone, action);
                button.Position = rect.Position;
                button.Size = rect.Size;
                button.CustomMinimumSize = rect.Size;
                button.Visible = true;
                button.Disabled = disabled;
                button.MouseFilter = MouseFilterEnum.Stop;
            }

            private void HideButtons(int firstUnused)
            {
                for (var i = firstUnused; i < _buttonBindings.Count; i++)
                {
                    _buttonBindings[i].OpenButton.Visible = false;
                    _buttonBindings[i].ActionButton.Visible = false;
                }
            }

            private (Rect2 open, Rect2 action) GetButtonRects(Rect2 card)
            {
                var width = (card.Size.X - Padding * 2f - ButtonGap) / 2f;
                var y = card.End.Y - Padding - ButtonHeight - 6f;
                var open = new Rect2(card.Position.X + Padding, y, width, ButtonHeight);
                var action = new Rect2(open.End.X + ButtonGap, y, width, ButtonHeight);
                return (open, action);
            }

            private Rect2 GetCardRect(int index, CanvasLayout layout)
            {
                var subscribedStart = _cards.FindIndex(static card => card.Item.IsSubscribed);
                if (subscribedStart < 0 || index < subscribedStart)
                    return GetCardRectInSection(index, 0f, layout);

                var pendingRows = Mathf.CeilToInt((float)subscribedStart / layout.Columns);
                var sectionTop = pendingRows * (CardHeight + CardGap) + DividerHeight;
                if (subscribedStart == 0)
                    sectionTop = DividerHeight;
                return GetCardRectInSection(index - subscribedStart, sectionTop, layout);
            }

            private Rect2 GetCardRectInSection(int sectionIndex, float sectionTop, CanvasLayout layout)
            {
                var row = sectionIndex / layout.Columns;
                var column = sectionIndex % layout.Columns;
                return new(
                    column * (layout.CardWidth + CardGap),
                    sectionTop + row * (CardHeight + CardGap),
                    layout.CardWidth,
                    CardHeight);
            }

            private float? FindDividerY(CanvasLayout layout)
            {
                var subscribedStart = _cards.FindIndex(static card => card.Item.IsSubscribed);
                switch (subscribedStart)
                {
                    case < 0:
                        return null;
                    case 0:
                        return 0f;
                    default:
                    {
                        var pendingRows = Mathf.CeilToInt((float)subscribedStart / layout.Columns);
                        return pendingRows * (CardHeight + CardGap) - CardGap + 6f;
                    }
                }
            }

            private void DrawDivider(float y, string text)
            {
                var font = RitsuShellTheme.Current.Font.BodyBold;
                var size = font.GetStringSize(text);
                var x = Math.Max(Padding, (Size.X - size.X) / 2f);
                DrawString(font, new(x, y + 28f), text, HorizontalAlignment.Left, -1f, 16,
                    RitsuShellTheme.Current.Text.RichTitle);
            }

            private CanvasLayout GetLayout()
            {
                var width = Math.Max(520f, Size.X);
                const int columns = 3;
                var cardWidth = (width - CardGap * (columns - 1)) / columns;
                return new(columns, cardWidth);
            }

            private void UpdateMinimumHeight()
            {
                if (!string.IsNullOrWhiteSpace(_message))
                {
                    CustomMinimumSize = new(680f, CardHeight);
                    UpdateMinimumSize();
                    return;
                }

                var layout = GetLayout();
                var subscribedStart = _cards.FindIndex(static card => card.Item.IsSubscribed);
                var rows = subscribedStart < 0
                    ? Mathf.CeilToInt((float)_cards.Count / layout.Columns)
                    : Mathf.CeilToInt((float)subscribedStart / layout.Columns) +
                      Mathf.CeilToInt((float)(_cards.Count - subscribedStart) / layout.Columns);
                var divider = subscribedStart >= 0 ? DividerHeight : 0f;
                CustomMinimumSize = new(680f, rows * CardHeight + Math.Max(0, rows - 1) * CardGap + divider);
                UpdateMinimumSize();
            }

            private void DrawCenteredText(string text, Rect2 rect, Color color, int fontSize)
            {
                var font = RitsuShellTheme.Current.Font.Body;
                var trimmed = TrimToWidth(font, text, fontSize, rect.Size.X - 8f);
                var size = font.GetStringSize(trimmed, HorizontalAlignment.Left, -1f, fontSize);
                DrawString(
                    font,
                    new(rect.Position.X + (rect.Size.X - size.X) / 2f, rect.Position.Y + (rect.Size.Y + size.Y) / 2f),
                    trimmed,
                    HorizontalAlignment.Left,
                    -1f,
                    fontSize,
                    color);
            }

            private void DrawTextLine(string text, float x, float baseline, float width, Color color, Font font,
                int fontSize)
            {
                DrawString(
                    font,
                    new(x, baseline),
                    TrimToWidth(font, text, fontSize, width),
                    HorizontalAlignment.Left,
                    -1f,
                    fontSize,
                    color);
            }

            private void DrawWrappedText(string text, float x, float baseline, float width, int maxLines, Color color,
                Font font, int fontSize)
            {
                var remaining = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
                for (var line = 0; line < maxLines && !string.IsNullOrWhiteSpace(remaining); line++)
                {
                    var lastLine = line == maxLines - 1;
                    var trimmed = lastLine
                        ? TrimToWidth(font, remaining, fontSize, width)
                        : TakeLine(font, remaining, fontSize, width, out remaining);
                    DrawString(font, new(x, baseline + line * 17f), trimmed, HorizontalAlignment.Left, -1f, fontSize,
                        color);
                }
            }

            private static string TakeLine(Font font, string text, int fontSize, float width, out string remaining)
            {
                remaining = string.Empty;
                if (font.GetStringSize(text, HorizontalAlignment.Left, -1f, fontSize).X <= width)
                    return text;

                var low = 0;
                var high = text.Length;
                while (low < high)
                {
                    var mid = (low + high + 1) / 2;
                    if (font.GetStringSize(text[..mid], HorizontalAlignment.Left, -1f, fontSize).X <= width)
                        low = mid;
                    else
                        high = mid - 1;
                }

                var cut = Math.Max(1, low);
                while (cut > 1 && !char.IsWhiteSpace(text[cut - 1]))
                    cut--;
                if (cut <= 1)
                    cut = Math.Max(1, low);

                remaining = text[cut..].TrimStart();
                return text[..cut].TrimEnd();
            }

            private static string TrimToWidth(Font font, string text, int fontSize, float width)
            {
                if (string.IsNullOrEmpty(text) ||
                    font.GetStringSize(text, HorizontalAlignment.Left, -1f, fontSize).X <= width)
                    return text;

                const string ellipsis = "...";
                var low = 0;
                var high = text.Length;
                while (low < high)
                {
                    var mid = (low + high + 1) / 2;
                    var candidate = text[..mid] + ellipsis;
                    if (font.GetStringSize(candidate, HorizontalAlignment.Left, -1f, fontSize).X <= width)
                        low = mid;
                    else
                        high = mid - 1;
                }

                return text[..Math.Max(0, low)] + ellipsis;
            }

            private static string ResolveSelectionButtonText(CardState state)
            {
                if (state.Item.IsSubscribed)
                    return L("ritsulib.steamWorkshopManagement.state.subscribed", "subscribed");

                return state.Selected
                    ? L("ritsulib.steamWorkshopManagement.import.willSubscribe", "Will subscribe")
                    : L("ritsulib.steamWorkshopManagement.import.skipSubscribe", "Skip");
            }

            private sealed record CardButtonBinding(
                ModSettingsTextButton OpenButton,
                ModSettingsTextButton ActionButton);

            private sealed class CardState(RitsuSteamWorkshopItem item, bool selected)
            {
                public RitsuSteamWorkshopItem Item { get; } = item;
                public bool Selected { get; set; } = selected;
            }

            private readonly record struct CanvasLayout(int Columns, float CardWidth);
        }

        private sealed partial class ModalRoot(Action onClose) : Control
        {
            public override void _Ready()
            {
                SetProcessUnhandledInput(true);
            }

            public override void _UnhandledInput(InputEvent @event)
            {
                if (@event.IsEcho())
                    return;

                if (@event.IsActionPressed("ui_cancel") ||
                    @event.IsActionPressed("cancel") ||
                    @event.IsActionPressed("pauseAndBack"))
                {
                    onClose();
                    GetViewport()?.SetInputAsHandled();
                    return;
                }

                if (ShouldConsumeModalInput(@event))
                {
                    GetViewport()?.SetInputAsHandled();
                    return;
                }

                base._UnhandledInput(@event);
            }

            private static bool ShouldConsumeModalInput(InputEvent @event)
            {
                return @event.IsActionPressed("ui_up") ||
                       @event.IsActionPressed("ui_down") ||
                       @event.IsActionPressed("ui_left") ||
                       @event.IsActionPressed("ui_right") ||
                       @event.IsActionPressed("ui_accept") ||
                       @event.IsActionPressed("ui_cancel") ||
                       @event.IsActionPressed("accept") ||
                       @event.IsActionPressed("select");
            }
        }
    }
}
