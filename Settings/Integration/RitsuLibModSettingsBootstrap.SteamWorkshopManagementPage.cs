using Godot;
using STS2RitsuLib.Platform.Steam;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal static partial class RitsuLibModSettingsBootstrap
    {
        private static void RegisterSteamWorkshopManagementPage()
        {
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .AsChildOf("updates")
                    .WithSortOrder(-550)
                    .WithTitle(T("ritsulib.page.steamWorkshopManagement.title", "Steam Workshop management"))
                    .WithDescription(T("ritsulib.page.steamWorkshopManagement.description",
                        "Search, subscribe, unsubscribe, and inspect Steam Workshop items."))
                    .AddSection("steam_workshop_management", section => section
                        .WithTitle(T("ritsulib.section.steamWorkshopManagement.title", "Workshop items"))
                        .WithDescription(T("ritsulib.section.steamWorkshopManagement.description",
                            "Subscription changes are handled by Steam. Restart the game after downloads or removals finish."))
                        .AddCustom(
                            "steam_workshop_management_control",
                            T("ritsulib.steamWorkshopManagement.control.label", "Items"),
                            _ => new SteamWorkshopManagementControl())),
                "steam-workshop-management");
        }

        private sealed partial class SteamWorkshopManagementControl : VBoxContainer
        {
            private const float ResultsScrollHeight = 520f;

            private int _refreshGeneration;
            private bool _searchContentBuilt;
            private LineEdit? _searchEdit;
            private int _searchGeneration;
            private WorkshopItemCanvas? _searchList;
            private Label? _searchStatusLabel;
            private bool _subscribedContentBuilt;
            private WorkshopItemCanvas? _subscribedList;
            private Label? _subscribedStatusLabel;

            public SteamWorkshopManagementControl()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill;
                MouseFilter = MouseFilterEnum.Ignore;
                AddThemeConstantOverride("separation", 12);
                Build();
            }

            private void Build()
            {
                AddChild(BuildSearchSection());
                AddChild(BuildSubscribedSection());
            }

            private Control BuildSearchSection()
            {
                var section = new ModSettingsCollapsibleSection(
                    L("ritsulib.steamWorkshopManagement.search.title", "Search Workshop"),
                    "steam_workshop_search",
                    L("ritsulib.steamWorkshopManagement.search.description",
                        "Find Workshop items by ID, Steam link, or text, then subscribe from the result cards."),
                    true,
                    []);
                section.SetLazyContentBuilder(() =>
                {
                    if (_searchContentBuilt)
                        return;

                    _searchContentBuilt = true;
                    var box = new VBoxContainer
                    {
                        SizeFlagsHorizontal = SizeFlags.ExpandFill,
                        MouseFilter = MouseFilterEnum.Ignore,
                    };
                    box.AddThemeConstantOverride("separation", 10);
                    box.AddChild(BuildSearchRow());

                    _searchStatusLabel = CreateLabel(
                        L("ritsulib.steamWorkshopManagement.search.idle",
                            "Enter a Workshop ID, Steam Workshop link, or search text."),
                        RitsuShellTheme.Current.Text.RichMuted,
                        14);
                    box.AddChild(_searchStatusLabel);

                    var scroll = CreateResultsScroll();
                    box.AddChild(scroll);
                    _searchList = CreateItemCanvas();
                    AddCanvasToScroll(scroll, _searchList);
                    section.ContentHost.AddChild(box);
                });
                return section;
            }

            private Control BuildSearchRow()
            {
                var row = new HBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                row.AddThemeConstantOverride("separation", 10);

                _searchEdit = ModSettingsUiControlTheming.CreateStyledLineEdit(
                    L("ritsulib.steamWorkshopManagement.search.placeholder", "Workshop ID, link, or search text"),
                    460f);
                _searchEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                _searchEdit.TextSubmitted += _ => SearchFromInput();
                row.AddChild(_searchEdit);

                row.AddChild(new ModSettingsTextButton(
                    L("ritsulib.steamWorkshopManagement.search.button", "Search"),
                    ModSettingsButtonTone.Accent,
                    SearchFromInput)
                {
                    CustomMinimumSize = new(150f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
                    SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                });

                row.AddChild(new ModSettingsTextButton(
                    L("ritsulib.steamWorkshopManagement.subscribe.button", "Subscribe"),
                    ModSettingsButtonTone.Normal,
                    SubscribeFromInput)
                {
                    CustomMinimumSize = new(150f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
                    SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                });

                return row;
            }

            private Control BuildSubscribedSection()
            {
                var section = new ModSettingsCollapsibleSection(
                    L("ritsulib.steamWorkshopManagement.subscribed.title", "Subscribed items"),
                    "steam_workshop_subscribed",
                    L("ritsulib.steamWorkshopManagement.subscribed.description",
                        "Inspect current Steam Workshop subscriptions and unsubscribe from item cards."),
                    true,
                    []);
                section.SetLazyContentBuilder(() =>
                {
                    if (_subscribedContentBuilt)
                        return;

                    _subscribedContentBuilt = true;
                    var box = new VBoxContainer
                    {
                        SizeFlagsHorizontal = SizeFlags.ExpandFill,
                        MouseFilter = MouseFilterEnum.Ignore,
                    };
                    box.AddThemeConstantOverride("separation", 10);

                    var row = new HBoxContainer
                    {
                        SizeFlagsHorizontal = SizeFlags.ExpandFill,
                        MouseFilter = MouseFilterEnum.Ignore,
                    };
                    row.AddThemeConstantOverride("separation", 10);
                    _subscribedStatusLabel = CreateLabel(
                        L("ritsulib.steamWorkshopManagement.status.loading", "Loading Workshop items..."),
                        RitsuShellTheme.Current.Text.RichMuted,
                        14);
                    _subscribedStatusLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                    row.AddChild(_subscribedStatusLabel);
                    row.AddChild(new ModSettingsTextButton(
                        L("button.refresh", "Refresh"),
                        ModSettingsButtonTone.Normal,
                        RefreshSubscribedItems)
                    {
                        CustomMinimumSize = new(150f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
                        SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                    });
                    box.AddChild(row);

                    var scroll = CreateResultsScroll();
                    box.AddChild(scroll);
                    _subscribedList = CreateItemCanvas();
                    AddCanvasToScroll(scroll, _subscribedList);
                    section.ContentHost.AddChild(box);
                    RefreshSubscribedItems();
                });
                return section;
            }

            private WorkshopItemCanvas CreateItemCanvas()
            {
                return new(OnOpenItem, OnToggleSubscription)
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Stop,
                };
            }

            private void SearchFromInput()
            {
                var query = _searchEdit?.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(query))
                {
                    SetSearchStatus(
                        L("ritsulib.steamWorkshopManagement.search.empty",
                            "Enter a Workshop ID, link, or search text."),
                        true);
                    return;
                }

                var generation = ++_searchGeneration;
                _searchList?.SetMessage(null);
                _searchList?.SetItems([]);
                SetSearchStatus(
                    L("ritsulib.steamWorkshopManagement.search.loading", "Searching Workshop..."),
                    false);
                _ = SearchFromInputAsync(query, generation);
            }

            private async Task SearchFromInputAsync(string query, int generation)
            {
                IReadOnlyList<RitsuSteamWorkshopItem> items;
                try
                {
                    if (SteamWorkshopManager.TryExtractWorkshopItemId(query, out var itemId))
                        items = await SteamWorkshopManager.Instance.QueryItemsAsync([itemId])
                            .ConfigureAwait(false);
                    else
                        items = await SteamWorkshopManager.Instance.SearchItemsAsync(query)
                            .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Post(() =>
                    {
                        if (generation == _searchGeneration)
                            SetSearchStatus(
                                string.Format(
                                    L("ritsulib.steamWorkshopManagement.search.failed",
                                        "Workshop search failed: {0}"),
                                    ex.Message),
                                true);
                    });
                    return;
                }

                Post(() =>
                {
                    if (generation != _searchGeneration)
                        return;

                    PrepareSearchResults(items);
                });
            }

            private void PrepareSearchResults(IReadOnlyList<RitsuSteamWorkshopItem> items)
            {
                if (!SteamWorkshopManager.Instance.IsAvailable)
                {
                    _searchList?.SetItems([]);
                    SetSearchStatus(
                        L("ritsulib.steamWorkshopManagement.status.unavailable",
                            "Steam Workshop is not available in this session."),
                        true);
                    return;
                }

                if (!SteamWorkshopManager.Instance.IsSearchAvailable &&
                    !SteamWorkshopManager.TryExtractWorkshopItemId(_searchEdit?.Text ?? string.Empty, out _))
                {
                    _searchList?.SetItems([]);
                    SetSearchStatus(
                        L("ritsulib.steamWorkshopManagement.search.unavailable",
                            "Workshop text search is not available with the current Steamworks runtime."),
                        true);
                    return;
                }

                SetSearchStatus(
                    string.Format(
                        L("ritsulib.steamWorkshopManagement.search.count", "Search results: {0}"),
                        items.Count),
                    false);

                if (items.Count == 0)
                {
                    _searchList?.SetItems([]);
                    _searchList?.SetMessage(
                        L("ritsulib.steamWorkshopManagement.search.noResults", "No Workshop items were found."));
                    return;
                }

                _searchList?.SetMessage(null);
                _searchList?.SetItems(items);
            }

            private void SubscribeFromInput()
            {
                var text = _searchEdit?.Text?.Trim() ?? string.Empty;
                if (!SteamWorkshopManager.TryExtractWorkshopItemId(text, out var itemId))
                {
                    SetSearchStatus(
                        L("ritsulib.steamWorkshopManagement.status.invalidId",
                            "Enter a positive numeric Workshop item id or Steam Workshop link."),
                        true);
                    return;
                }

                var result = SteamWorkshopUpdateCoordinator.SubscribeItemFromUi(itemId);
                if (result.Accepted)
                    RefreshSubscribedItemsDelayed();
            }

            private void RefreshSubscribedItems()
            {
                if (!_subscribedContentBuilt)
                    return;

                var generation = ++_refreshGeneration;
                _subscribedList?.SetMessage(null);
                _subscribedList?.SetItems([]);
                SetSubscribedStatus(
                    L("ritsulib.steamWorkshopManagement.status.loading", "Loading Workshop items..."),
                    false);
                _ = RefreshSubscribedItemsAsync(generation);
            }

            private async Task RefreshSubscribedItemsAsync(int generation)
            {
                try
                {
                    var cachedItems = await SteamWorkshopManager.Instance.ListSubscribedItemsFromCacheAsync()
                        .ConfigureAwait(false);
                    Post(() =>
                    {
                        if (generation == _refreshGeneration)
                            PrepareSubscribedItems(cachedItems);
                    });

                    var items = await SteamWorkshopManager.Instance.ListSubscribedItemsAsync().ConfigureAwait(false);
                    Post(() =>
                    {
                        if (generation == _refreshGeneration)
                            PrepareSubscribedItems(items);
                    });
                }
                catch (Exception ex)
                {
                    Post(() =>
                    {
                        if (generation == _refreshGeneration)
                            SetSubscribedStatus(
                                string.Format(
                                    L("ritsulib.steamWorkshopManagement.status.failed",
                                        "Could not read Workshop subscriptions: {0}"),
                                    ex.Message),
                                true);
                    });
                }
            }

            private void PrepareSubscribedItems(IReadOnlyList<RitsuSteamWorkshopItem> items)
            {
                if (!SteamWorkshopManager.Instance.IsAvailable)
                {
                    _subscribedList?.SetItems([]);
                    SetSubscribedStatus(
                        L("ritsulib.steamWorkshopManagement.status.unavailable",
                            "Steam Workshop is not available in this session."),
                        true);
                    return;
                }

                SetSubscribedStatus(
                    string.Format(
                        L("ritsulib.steamWorkshopManagement.status.count",
                            "Subscribed Workshop items: {0}"),
                        items.Count),
                    false);

                if (items.Count == 0)
                {
                    _subscribedList?.SetItems([]);
                    _subscribedList?.SetMessage(
                        L("ritsulib.steamWorkshopManagement.empty", "No subscribed Workshop items were found."));
                    return;
                }

                _subscribedList?.SetMessage(null);
                _subscribedList?.SetItems(items);
            }

            private void OnOpenItem(RitsuSteamWorkshopItem item)
            {
                SteamWorkshopManager.Instance.TryOpenWorkshopPage(item.Id);
            }

            private void OnToggleSubscription(RitsuSteamWorkshopItem item)
            {
                var result = item.IsSubscribed
                    ? SteamWorkshopUpdateCoordinator.UnsubscribeItemFromUi(item.Id, item.DisplayName)
                    : SteamWorkshopUpdateCoordinator.SubscribeItemFromUi(item.Id, item.DisplayName);
                if (result.Accepted)
                    RefreshSubscribedItemsDelayed();
            }

            private void RefreshSubscribedItemsDelayed()
            {
                _ = RefreshSubscribedItemsDelayedAsync();
            }

            private async Task RefreshSubscribedItemsDelayedAsync()
            {
                await Task.Delay(1200).ConfigureAwait(false);
                Post(RefreshSubscribedItems);
            }

            private static ScrollContainer CreateResultsScroll()
            {
                var scroll = new ScrollContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    CustomMinimumSize = new(0f, ResultsScrollHeight),
                    HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                    MouseFilter = MouseFilterEnum.Pass,
                };
                ModSettingsUiControlTheming.ApplySettingsScrollContainerTheme(scroll);
                return scroll;
            }

            private static void AddCanvasToScroll(ScrollContainer scroll, WorkshopItemCanvas canvas)
            {
                var margin = new MarginContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                margin.AddThemeConstantOverride(
                    "margin_right",
                    ModSettingsUiControlTheming.ResolveSettingsScrollContentRightGutter(scroll));
                scroll.AddChild(margin);
                margin.AddChild(canvas);
            }

            private void SetSearchStatus(string text, bool warning)
            {
                SetStatus(_searchStatusLabel, text, warning);
            }

            private void SetSubscribedStatus(string text, bool warning)
            {
                SetStatus(_subscribedStatusLabel, text, warning);
            }

            private static void SetStatus(Label? label, string text, bool warning)
            {
                if (label == null)
                    return;

                label.Text = text;
                label.AddThemeColorOverride(
                    "font_color",
                    warning ? RitsuShellTheme.Current.Text.HoverHighlight : RitsuShellTheme.Current.Text.RichMuted);
            }

            private static Label CreateLabel(string text, Color color, int fontSize, bool bold = false)
            {
                var label = new Label
                {
                    Text = text,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
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

            private void Post(Action action)
            {
                if (!IsInsideTree())
                    return;

                Callable.From(() =>
                {
                    if (IsInsideTree())
                        action();
                }).CallDeferred();
            }
        }

        private sealed partial class WorkshopItemCanvas(
            Action<RitsuSteamWorkshopItem> openItem,
            Action<RitsuSteamWorkshopItem> toggleSubscription)
            : Control
        {
            private const float CardHeight = 188f;
            private const float CardGap = 10f;
            private const float ButtonHeight = 36f;
            private const float ButtonGap = 8f;
            private const float Padding = 10f;
            private const float PreviewSize = 96f;
            private readonly List<CardButtonBinding> _buttonBindings = [];
            private readonly HashSet<string> _loadingPreviewUrls = new(StringComparer.Ordinal);
            private readonly Dictionary<string, Texture2D?> _previewTextures = new(StringComparer.Ordinal);
            private IReadOnlyList<RitsuSteamWorkshopItem> _items = [];
            private double _lastScroll = -1d;
            private Vector2 _lastSize = new(-1f, -1f);
            private string? _message;

            public void SetItems(IReadOnlyList<RitsuSteamWorkshopItem> items)
            {
                _items = items;
                UpdateMinimumHeight();
                SyncButtons();
                QueueRedraw();
            }

            public void SetMessage(string? message)
            {
                _message = message;
                UpdateMinimumHeight();
                SyncButtons();
                QueueRedraw();
            }

            public override void _Ready()
            {
                base._Ready();
                SetProcess(true);
                SyncButtons();
            }

            public override void _Notification(int what)
            {
                base._Notification(what);
                if (what == (int)NotificationResized)
                {
                    UpdateMinimumHeight();
                    SyncButtons();
                }
            }

            public override void _Process(double delta)
            {
                base._Process(delta);
                var scroll = FindScrollContainer();
                var currentScroll = scroll?.ScrollVertical ?? 0d;
                if (Math.Abs(currentScroll - _lastScroll) <= 0.1d && Size == _lastSize)
                    return;

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
                var visible = GetVisibleYRange();
                for (var i = 0; i < _items.Count; i++)
                {
                    var rect = GetCardRect(i, layout);
                    if (rect.Position.Y > visible.end || rect.End.Y < visible.start)
                        continue;

                    DrawCard(_items[i], rect);
                }
            }

            private void DrawCard(RitsuSteamWorkshopItem item, Rect2 rect)
            {
                DrawRect(rect, RitsuShellTheme.Current.Surface.Content);
                DrawRect(rect, item.NeedsUpdate
                    ? RitsuShellTheme.Current.Text.HoverHighlight
                    : RitsuShellTheme.Current.Surface.Inset.Border, false, 1.2f);

                var preview = new Rect2(rect.Position + new Vector2(Padding, Padding), new(PreviewSize, PreviewSize));
                DrawRect(preview, RitsuShellTheme.Current.Surface.Inset.Bg);
                DrawRect(preview, RitsuShellTheme.Current.Surface.Inset.Border, false, 1f);
                DrawPreview(item, preview);

                var textX = preview.End.X + 10f;
                var textWidth = Math.Max(48f, rect.End.X - textX - Padding);
                var y = rect.Position.Y + Padding + 18f;
                DrawTextLine(item.DisplayName, textX, y, textWidth, RitsuShellTheme.Current.Text.RichTitle,
                    RitsuShellTheme.Current.Font.BodyBold, 16);
                y += 22f;
                DrawTextLine(FormatItemSubtitle(item), textX, y, textWidth, RitsuShellTheme.Current.Text.RichMuted,
                    RitsuShellTheme.Current.Font.Body, 13);
                y += 18f;
                if (FormatItemMetadata(item) is { } metadata)
                {
                    DrawTextLine(metadata, textX, y, textWidth, RitsuShellTheme.Current.Text.RichMuted,
                        RitsuShellTheme.Current.Font.Body, 13);
                    y += 18f;
                }

                if (!string.IsNullOrWhiteSpace(item.Description))
                    DrawTextLine(item.Description, textX, y, textWidth, RitsuShellTheme.Current.Text.RichBody,
                        RitsuShellTheme.Current.Font.Body, 13);
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
                    _previewTextures[previewUrl] = texture;
                    _loadingPreviewUrls.Remove(previewUrl);
                    QueueRedraw();
                }).CallDeferred();
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

            private void SyncButtons()
            {
                _lastSize = Size;
                _lastScroll = FindScrollContainer()?.ScrollVertical ?? 0d;

                if (!string.IsNullOrWhiteSpace(_message) || _items.Count == 0 || !IsInsideTree())
                {
                    HideButtons(0);
                    return;
                }

                var layout = GetLayout();
                var visible = GetVisibleYRange();
                var bindingIndex = 0;
                for (var i = 0; i < _items.Count; i++)
                {
                    var rect = GetCardRect(i, layout);
                    if (rect.Position.Y > visible.end || rect.End.Y < visible.start)
                        continue;

                    var (openRect, actionRect) = GetButtonRects(rect);
                    var item = _items[i];
                    var binding = EnsureButtonBinding(bindingIndex++);
                    ConfigureButton(binding.OpenButton, openRect,
                        L("ritsulib.steamWorkshopManagement.open.button", "Open"),
                        ModSettingsButtonTone.Normal,
                        () => openItem(item));
                    ConfigureButton(binding.ActionButton, actionRect,
                        item.IsSubscribed
                            ? L("ritsulib.steamWorkshopManagement.unsubscribe.button", "Unsubscribe")
                            : L("ritsulib.steamWorkshopManagement.subscribe.button", "Subscribe"),
                        item.IsSubscribed ? ModSettingsButtonTone.Normal : ModSettingsButtonTone.Accent,
                        () => toggleSubscription(item));
                }

                HideButtons(bindingIndex);
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
                Action action)
            {
                button.Configure(text, tone, action);
                button.Position = rect.Position;
                button.Size = rect.Size;
                button.CustomMinimumSize = rect.Size;
                button.Visible = true;
                button.Disabled = false;
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
                var y = card.End.Y - Padding - ButtonHeight;
                var open = new Rect2(card.Position.X + Padding, y, width, ButtonHeight);
                var action = new Rect2(open.End.X + ButtonGap, y, width, ButtonHeight);
                return (open, action);
            }

            private Rect2 GetCardRect(int index, CanvasLayout layout)
            {
                var row = index / layout.Columns;
                var column = index % layout.Columns;
                return new(
                    column * (layout.CardWidth + CardGap),
                    row * (CardHeight + CardGap),
                    layout.CardWidth,
                    CardHeight);
            }

            private CanvasLayout GetLayout()
            {
                var width = Math.Max(320f, Size.X);
                var columns = width >= 680f ? 2 : 1;
                var cardWidth = (width - CardGap * (columns - 1)) / columns;
                return new(columns, cardWidth);
            }

            private void UpdateMinimumHeight()
            {
                var rows = !string.IsNullOrWhiteSpace(_message)
                    ? 1
                    : Math.Max(1, Mathf.CeilToInt((float)_items.Count / GetLayout().Columns));
                CustomMinimumSize = new(680f, rows * CardHeight + Math.Max(0, rows - 1) * CardGap);
                UpdateMinimumSize();
            }

            private (float start, float end) GetVisibleYRange()
            {
                var scroll = FindScrollContainer();
                return scroll == null
                    ? (0f, Size.Y)
                    : (scroll.ScrollVertical - CardHeight, scroll.ScrollVertical + scroll.Size.Y + CardHeight);
            }

            private ScrollContainer? FindScrollContainer()
            {
                for (var node = GetParent(); node != null; node = node.GetParent())
                    if (node is ScrollContainer scroll)
                        return scroll;
                return null;
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

            private static string FormatItemSubtitle(RitsuSteamWorkshopItem item)
            {
                var state = item.IsDownloading || item.IsDownloadPending
                    ? L("ritsulib.steamWorkshopManagement.state.downloading", "downloading")
                    : item.NeedsUpdate
                        ? L("ritsulib.steamWorkshopManagement.state.needsUpdate", "needs update")
                        : item.IsInstalled
                            ? L("ritsulib.steamWorkshopManagement.state.installed", "installed")
                            : item.IsSubscribed
                                ? L("ritsulib.steamWorkshopManagement.state.subscribed", "subscribed")
                                : L("ritsulib.steamWorkshopManagement.state.notInstalled", "not installed");

                return string.Format(
                    L("ritsulib.steamWorkshopManagement.item.subtitle", "ID: {0} | {1}"),
                    item.Id,
                    state);
            }

            private static string? FormatItemMetadata(RitsuSteamWorkshopItem item)
            {
                List<string> parts = [];
                if (!string.IsNullOrWhiteSpace(item.ModId))
                    parts.Add(string.Format(
                        L("ritsulib.steamWorkshopManagement.item.modId", "Mod ID: {0}"),
                        item.ModId));
                if (!string.IsNullOrWhiteSpace(item.Author))
                    parts.Add(string.Format(
                        L("ritsulib.steamWorkshopManagement.item.author", "Author: {0}"),
                        item.Author));
                return parts.Count == 0 ? null : string.Join(" | ", parts);
            }

            private sealed record CardButtonBinding(
                ModSettingsTextButton OpenButton,
                ModSettingsTextButton ActionButton);

            private readonly record struct CanvasLayout(int Columns, float CardWidth);
        }
    }
}
