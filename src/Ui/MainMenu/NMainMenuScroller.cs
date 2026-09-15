using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using STS2RitsuLib.Data;

namespace STS2RitsuLib.Ui.MainMenu
{
    internal sealed partial class NMainMenuScroller : Control
    {
        private const float EdgePadding = 16f;
        private const float ContentPadding = 6f;
        private const float HorizontalPadding = 96f;
        private const float EdgeZone = 80f;
        private const float EdgeMinScale = 0.78f;
        private const float EdgeMinAlpha = 0.4f;
        private const float SceneBoxTop = 69f;
        private const float SceneBoxHeight = 450f;
        private const float ScrollOverflowTolerance = 40f;
        private readonly List<NMainMenuTextButton> _buttons = [];
        private readonly List<Control> _items = [];
        private readonly List<Control> _measured = [];
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
        private float _separation;
        internal bool Initialized { get; private set; }


        private float ScrollLimit { get; set; }

        private bool ScrollEngaged => ScrollLimit > 0f;

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
                _designOffsets = new(box.OffsetLeft, SceneBoxTop,
                    box.OffsetRight - box.OffsetLeft, SceneBoxHeight),
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
            _measured.Clear();
            _measurements.Clear();
            _rowTops.Clear();
            ScrollLimit = 0f;
            _visualScrollTween?.Kill();
            _visualScrollTween = null;
            _visualScroll = 0f;
            _heightStepIndex = 0;
            DisposeDecorations();
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
                _measured.Clear();
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
                    _measured.Add(item);
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
                var overflow = _contentHeight + ContentPadding * 2f - Size.Y;
                ScrollLimit = overflow > ScrollOverflowTolerance ? overflow : 0f;
                ClipContents = ScrollEngaged;
                _columnCenter = Mathf.Clamp(centerX - left, 0f, width);
                _scroll = Mathf.Clamp(_scroll, 0f, ScrollLimit);
                SyncHeightSteps(instant: true);
                PositionItems();
                RebuildNavigation();
                if (FollowsFocus && GetViewport().GuiGetFocusOwner() is { } focused && _rowTops.ContainsKey(focused))
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
            ResetEdgeScale(item);
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
            var (topWeight, bottomWeight, start) = GetEdgeScaleContext();
            foreach (var item in _measured)
            {
                if (!IsInstanceValid(item))
                    continue;
                var minimum = _measurements[item];
                var width = item.SizeFlagsHorizontal.HasFlag(SizeFlags.Expand) ||
                            item.SizeFlagsHorizontal.HasFlag(SizeFlags.Fill)
                    ? Mathf.Max(minimum.X, _designOffsets.Size.X)
                    : minimum.X;
                item.Size = new(width, minimum.Y);
                item.Position = new(_columnCenter - width / 2f, start + _rowTops[item]);
                ApplyEdgeScale(item, start + _rowTops[item], minimum.Y, topWeight, bottomWeight);
            }

            UpdateDecorations();
        }

        private void SetScroll(float value)
        {
            _scroll = Mathf.Clamp(value, 0f, ScrollLimit);
            SyncHeightSteps(instant: false);
            PositionItems();
        }

        private void EnsureVisible(Control item)
        {
            if (!_measurements.ContainsKey(item))
                return;
            var index = _measured.IndexOf(item);
            var step = _heightStepIndex;
            if (index >= 0 && index <= _heightStepIndex)
                step = Math.Max(0, index - 1);
            else if (index > _heightStepIndex)
            {
                step = index;
                for (var candidate = _heightStepIndex; candidate <= index; candidate++)
                {
                    var top = ContentPadding + (candidate > 0 ? _separation : 0f);
                    var fits = false;
                    for (var i = candidate; i < _measured.Count; i++)
                    {
                        var height = _measurements[_measured[i]].Y;
                        if (i == index)
                        {
                            fits = top + height <= Size.Y - ContentPadding + 0.5f;
                            break;
                        }

                        top += height + _separation;
                    }

                    if (!fits)
                        continue;
                    step = candidate;
                    break;
                }
            }

            SetScroll(ScrollPixelsForStep(step));
        }
    }
}
