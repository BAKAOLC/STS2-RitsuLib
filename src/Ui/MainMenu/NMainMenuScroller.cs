using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using STS2RitsuLib.Data;

namespace STS2RitsuLib.Ui.MainMenu
{
    internal sealed partial class NMainMenuScroller : Control
    {
        private const float EdgePadding = 16f;
        private const float ContentPadding = 6f;
        private const float HorizontalPadding = 96f;
        private const float ScrollBarScale = 0.5f;
        private const float ScrollBarInset = 24f;
        private readonly List<NMainMenuTextButton> _buttons = [];
        private readonly List<Control> _items = [];
        private readonly Dictionary<Control, Vector2> _measurements = [];
        private readonly Dictionary<Control, float> _rowTops = [];
        private float _columnCenter;
        private float _contentHeight;
        private Vector2 _designAnchor;
        private Rect2 _designOffsets;
        private bool _initializeOnEnter;
        private bool _layingOut;
        private bool _layoutDirty = true;
        private NMainMenu _mainMenu = null!;
        private float _scroll;
        private NScrollbar _scrollBar = null!;
        private float _separation;
        internal bool Initialized { get; private set; }


        private float ScrollLimit => Mathf.Max(0f, _contentHeight + ContentPadding * 2f - Size.Y);

        internal static NMainMenuScroller? Find(NMainMenu menu)
        {
            return menu.GetNodeOrNull<NMainMenuScroller>("MainMenuTextButtons");
        }

        internal static void Install(NMainMenu menu)
        {
            if (!RitsuLibSettingsStore.IsMainMenuScrollingEnabled())
                return;
            var original = menu.GetNodeOrNull<Control>("MainMenuTextButtons");
            if (original is NMainMenuScroller)
                return;
            if (original is not VBoxContainer box || box.GetScript().VariantType != Variant.Type.Nil)
            {
                RitsuLibFramework.Logger.Warn(
                    "[MainMenu] Unsupported menu button container; leaving its layout intact.");
                return;
            }

            var owners = CollectOwners(box);
            var index = box.GetIndex();
            var owner = box.Owner;
            var unique = box.UniqueNameInOwner;
            var host = new NMainMenuScroller
            {
                Name = box.Name,
                _mainMenu = menu,
                _designAnchor = new(box.AnchorLeft, box.AnchorTop),
                _designOffsets = new(box.OffsetLeft, box.OffsetTop,
                    box.OffsetRight - box.OffsetLeft, box.OffsetBottom - box.OffsetTop),
                _separation = box.GetThemeConstant("separation"),
                Theme = box.Theme,
                Visible = box.Visible,
                Modulate = box.Modulate,
                SelfModulate = box.SelfModulate,
                MouseFilter = MouseFilterEnum.Stop,
                ClipContents = true,
                FocusMode = FocusModeEnum.None,
            };
            foreach (var (node, _) in owners)
                node.Owner = null;
            box.Owner = null;
            box.UniqueNameInOwner = false;
            menu.RemoveChild(box);
            menu.AddChild(host);
            menu.MoveChild(host, index);
            host.Owner = owner;
            host.UniqueNameInOwner = unique;
            foreach (var child in box.GetChildren())
            {
                box.RemoveChild(child);
                host.AddChild(child);
            }

            foreach (var (node, nodeOwner) in owners)
            {
                var restoredOwner = nodeOwner == box ? host : nodeOwner;
                if (restoredOwner.IsAncestorOf(node))
                    node.Owner = restoredOwner;
            }

            foreach (var group in box.GetGroups())
                host.AddToGroup(group);
            foreach (var key in box.GetMetaList())
                host.SetMeta(key, box.GetMeta(key));
            box.QueueFree();
        }

        private static List<(Node Node, Node Owner)> CollectOwners(Node root)
        {
            List<(Node, Node)> owners = [];
            foreach (var child in root.GetChildren())
            {
                if (child.Owner is { } owner)
                    owners.Add((child, owner));
                owners.AddRange(CollectOwners(child));
            }

            return owners;
        }

        internal void Initialize()
        {
            if (Initialized)
                return;
            _scrollBar = ResourceLoader.Load<PackedScene>("res://scenes/ui/scrollbar.tscn").Instantiate<NScrollbar>();
            _scrollBar.Name = "ScrollBar";
            _scrollBar.FocusMode = FocusModeEnum.None;
            _scrollBar.MouseFilter = MouseFilterEnum.Stop;
            _scrollBar.PivotOffset = Vector2.Zero;
            _scrollBar.Scale = Vector2.One * ScrollBarScale;
            AddChild(_scrollBar, false, InternalMode.Back);
            _scrollBar.ValueChanged += OnScrollBarChanged;
            ChildOrderChanged += RequestLayout;
            _mainMenu.Resized += RequestLayout;
            VisibilityChanged += OnVisibilityChanged;
            ConnectInputEvents();
            Initialized = true;
            InitializeDecorations();
            RefreshLayout();
            RestoreFocusIfMissing();
            SetProcess(true);
        }

        public override void _EnterTree()
        {
            SetProcess(false);
            if (_initializeOnEnter)
                Callable.From(Initialize).CallDeferred();
        }

        public override void _ExitTree()
        {
            if (!Initialized)
                return;
            DisconnectInputEvents();
            ChildOrderChanged -= RequestLayout;
            VisibilityChanged -= OnVisibilityChanged;
            _mainMenu.Resized -= RequestLayout;
            foreach (var item in _items)
                UntrackItem(item);
            _items.Clear();
            _buttons.Clear();
            DisposeDecorations();
            _scrollBar.ValueChanged -= OnScrollBarChanged;
            _scrollBar.QueueFree();
            Initialized = false;
            _initializeOnEnter = true;
            _layoutDirty = true;
        }

        public override void _Process(double delta)
        {
            if (!Initialized)
                return;
            if (_layoutDirty)
                RefreshLayout();
            RefreshInteraction();
            UpdateDecorations();
        }

        private void RequestLayout()
        {
            if (!_layingOut)
                _layoutDirty = true;
        }

        private void RefreshLayout()
        {
            if (!Initialized || _layingOut)
                return;
            _layingOut = true;
            try
            {
                SynchronizeItems();
                _measurements.Clear();
                _rowTops.Clear();
                _contentHeight = 0f;
                var columnWidth = _designOffsets.Size.X;
                foreach (var item in _items.Where(static item => item.Visible))
                {
                    var minimum = item.GetCombinedMinimumSize();
                    if (item is NMainMenuTextButton { label: { } label })
                    {
                        minimum.X = Mathf.Max(minimum.X, label.Size.X * 1.05f);
                        minimum.Y = Mathf.Max(minimum.Y, label.Size.Y * 1.05f);
                    }

                    if (_measurements.Count > 0)
                        _contentHeight += _separation;
                    _rowTops[item] = _contentHeight;
                    _measurements[item] = minimum;
                    _contentHeight += minimum.Y;
                    columnWidth = Mathf.Max(columnWidth, minimum.X);
                }

                var menuSize = _mainMenu.Size;
                var designPosition = menuSize * _designAnchor + _designOffsets.Position;
                var centerX = designPosition.X + _designOffsets.Size.X / 2f;
                var top = Mathf.Clamp(designPosition.Y, EdgePadding, Mathf.Max(EdgePadding, menuSize.Y - 50f));
                var bottom = Mathf.Clamp(designPosition.Y + _designOffsets.Size.Y, top,
                    Mathf.Max(top, menuSize.Y - EdgePadding));
                var width = Mathf.Min(columnWidth + HorizontalPadding * 2f,
                    Mathf.Max(1f, menuSize.X - EdgePadding * 2f));
                var left = Mathf.Clamp(centerX - width / 2f, EdgePadding,
                    Mathf.Max(EdgePadding, menuSize.X - EdgePadding - width));
                Position = new(left, top);
                Size = new(width, Mathf.Max(1f, bottom - top));
                _columnCenter = Mathf.Clamp(centerX - left, 0f, width);
                _scroll = Mathf.Clamp(_scroll, 0f, ScrollLimit);
                _scrollBar.Position = new(Size.X - ScrollBarInset - 12f, ContentPadding + ScrollBarInset);
                _scrollBar.Size = new(48f,
                    Mathf.Max(1f, Size.Y - (ContentPadding + ScrollBarInset) * 2f) / ScrollBarScale);
                _scrollBar.MaxValue = Mathf.Max(1f, ScrollLimit);
                _scrollBar.Visible = ScrollLimit > 0f;
                _scrollBar.SetValueNoSignal(_scroll);
                PositionItems();
                RebuildNavigation();
                if (IsDirectional && GetViewport().GuiGetFocusOwner() is { } focused && _rowTops.ContainsKey(focused))
                    EnsureVisible(focused);
                _layoutDirty = false;
            }
            finally
            {
                _layingOut = false;
            }
        }

        private void SynchronizeItems()
        {
            var current = GetChildren().OfType<Control>().ToList();
            foreach (var removed in _items.Except(current))
                UntrackItem(removed);
            foreach (var added in current.Except(_items))
                TrackItem(added);
            _items.Clear();
            _items.AddRange(current);
            _buttons.Clear();
            _buttons.AddRange(current.OfType<NMainMenuTextButton>());
        }

        private void TrackItem(Control item)
        {
            item.MinimumSizeChanged += RequestLayout;
            item.VisibilityChanged += RequestLayout;
            if (item is not NMainMenuTextButton button)
                return;
            button.Focused += OnButtonFocused;
            button.Unfocused += OnButtonUnfocused;
            if (button.label is not { } label)
                return;
            label.Resized += RequestLayout;
            label.MinimumSizeChanged += RequestLayout;
        }

        private void UntrackItem(Control item)
        {
            if (!IsInstanceValid(item))
                return;
            item.MinimumSizeChanged -= RequestLayout;
            item.VisibilityChanged -= RequestLayout;
            if (item is not NMainMenuTextButton button)
                return;
            CancelPress(button);
            button.Focused -= OnButtonFocused;
            button.Unfocused -= OnButtonUnfocused;
            if (button.label is not { } label || !IsInstanceValid(label))
                return;
            label.Resized -= RequestLayout;
            label.MinimumSizeChanged -= RequestLayout;
        }

        private void PositionItems()
        {
            var start = Mathf.Max(ContentPadding, (Size.Y - _contentHeight) / 2f) - _scroll;
            foreach (var (item, minimum) in _measurements)
            {
                var width = item.SizeFlagsHorizontal.HasFlag(SizeFlags.Expand) ||
                            item.SizeFlagsHorizontal.HasFlag(SizeFlags.Fill)
                    ? Mathf.Max(minimum.X, _designOffsets.Size.X)
                    : minimum.X;
                item.Size = new(width, minimum.Y);
                item.Position = new(_columnCenter - width / 2f, start + _rowTops[item]);
            }

            UpdateDecorations();
        }

        private void OnScrollBarChanged(double value)
        {
            if (IsActive)
            {
                CancelPress();
                SetScroll((float)value);
            }
        }

        private void SetScroll(float value)
        {
            _scroll = Mathf.Clamp(value, 0f, ScrollLimit);
            _scrollBar.SetValueNoSignal(_scroll);
            PositionItems();
        }

        private void EnsureVisible(Control item)
        {
            if (!_rowTops.TryGetValue(item, out var top))
                return;
            var height = _measurements[item].Y;
            var margin = Mathf.Min(ContentPadding, Mathf.Max(0f, (Size.Y - height) / 2f));
            var start = Mathf.Max(ContentPadding, (Size.Y - _contentHeight) / 2f);
            var offset = _scroll;
            if (start + top - offset < margin)
                offset = start + top - margin;
            else if (start + top + height - offset > Size.Y - margin)
                offset = start + top + height - Size.Y + margin;
            SetScroll(offset);
        }
    }
}
