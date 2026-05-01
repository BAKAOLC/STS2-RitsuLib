using Godot;

namespace STS2RitsuLib.Ui.Toast
{
    internal sealed partial class RitsuToastHost : CanvasLayer
    {
        private readonly List<VisibleToast> _closing = [];
        private readonly Stack<RitsuToastEntry> _entryPool = [];
        private readonly Queue<RitsuToastRequest> _pending = [];
        private readonly List<VisibleToast> _pendingEnter = [];
        private readonly List<VisibleToast> _prewarming = [];
        private readonly List<VisibleToast> _visible = [];
        private bool _hasDeferredReflowQueued;
        private bool _hasPendingEnterDispatchQueued;
        private bool _hasPrewarmCommitQueued;
        private int _hoveringCount;
        private Control? _root;
        private RitsuToastSettings _settings = RitsuToastSettings.Default;
        private Control? _warmupRoot;

        public RitsuToastHost()
        {
            Layer = 160;
            Name = "RitsuToastHost";
            ProcessMode = ProcessModeEnum.Always;
        }

        public override void _Ready()
        {
            _root = new()
            {
                Name = "Root",
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            AddChild(_root);
            _warmupRoot = new()
            {
                Name = "WarmupRoot",
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Position = new(-100000f, -100000f),
                Modulate = new(1f, 1f, 1f, 0f),
            };
            AddChild(_warmupRoot);
            var viewport = GetViewport();
            if (viewport != null)
                viewport.SizeChanged += () =>
                {
                    Reflow(false);
                    QueueDeferredReflow();
                };
            Visible = _settings.Enabled;
            TryDequeue();
            QueueDeferredReflow();
        }

        public override void _Process(double delta)
        {
            if (_visible.Count == 0 && _closing.Count == 0)
                return;

            StepAnimations(delta);
            if (_hoveringCount > 0)
                return;

            for (var i = _visible.Count - 1; i >= 0; i--)
            {
                var item = _visible[i];
                if (item.IsClosing || item.Entering || item.RemainingSeconds <= 0d)
                    continue;
                item.RemainingSeconds -= delta;
                if (item.RemainingSeconds > 0d)
                    continue;
                RequestClose(item, false);
            }
        }

        public void ApplySettings(RitsuToastSettings settings)
        {
            _settings = settings;
            Visible = settings.Enabled;
            if (!settings.Enabled)
            {
                for (var i = _visible.Count - 1; i >= 0; i--)
                    RequestClose(_visible[i], true);
                for (var i = _closing.Count - 1; i >= 0; i--)
                    FinalizeClose(_closing[i]);
                for (var i = _prewarming.Count - 1; i >= 0; i--)
                    RecyclePrewarming(_prewarming[i]);
                _prewarming.Clear();
                _pendingEnter.Clear();
                _pending.Clear();
                _hoveringCount = 0;
                return;
            }

            Reflow(false);
            TryDequeue();
        }

        public void Enqueue(RitsuToastRequest request)
        {
            if (!_settings.Enabled)
                return;
            if (_root == null)
            {
                _pending.Enqueue(request);
                return;
            }

            if (GetOccupiedSlotsCount() >= Math.Max(1, _settings.QueuePolicy.MaxVisible))
            {
                _pending.Enqueue(request);
                return;
            }

            ShowNow(request);
        }

        public void RefreshTheme()
        {
            foreach (var toast in _visible)
            {
                var style = toast.Request.StyleOverride ?? RitsuToastThemeResolver.Resolve(toast.Request.Level);
                toast.Style = style;
                toast.Entry.ApplyStyle(style);
            }

            Reflow(false);
        }

        private void ShowNow(RitsuToastRequest request)
        {
            if (_root == null || _warmupRoot == null)
                return;

            var style = request.StyleOverride ?? RitsuToastThemeResolver.Resolve(request.Level);
            var entry = AcquireEntry();
            entry.Configure(request, style);
            entry.Clicked += OnEntryClicked;
            entry.HoverStateChanged += OnEntryHoverStateChanged;
            _warmupRoot.AddChild(entry);
            var duration = request.DurationSeconds ?? _settings.DurationSeconds;
            var visibleToast = new VisibleToast(entry, request, style, Math.Max(0d, duration));
            _prewarming.Add(visibleToast);
            QueuePrewarmCommit();
        }

        private void RequestClose(VisibleToast item, bool immediate)
        {
            if (item.IsClosing)
                return;

            item.IsClosing = true;
            item.Entry.Clicked -= OnEntryClicked;
            item.Entry.HoverStateChanged -= OnEntryHoverStateChanged;
            _prewarming.Remove(item);
            _pendingEnter.Remove(item);
            if (!_closing.Contains(item))
                _closing.Add(item);
            if (item.IsHovering)
            {
                item.IsHovering = false;
                _hoveringCount = Math.Max(0, _hoveringCount - 1);
            }

            if (immediate)
            {
                item.Exiting = false;
                item.ExitCompleted = true;
                FinalizeClose(item);
                return;
            }

            var axis = ResolveAnchorAxis();
            var preset = item.Request.AnimationOverride ?? _settings.AnimationPreset;
            item.Exiting = true;
            item.ExitCompleted = false;
            item.ExitElapsed = 0f;
            item.ExitDuration = Math.Max(0.01f, item.Style.ExitDuration);
            item.ExitPreset = preset;
            item.ExitAxis = axis;
            item.ExitSlideDistance = item.Style.ExitSlideDistance;
            item.ExitFromPosition = item.Entry.Position;
            item.ExitFromScale = item.Entry.Scale;
        }

        private void FinalizeClose(VisibleToast item)
        {
            _closing.Remove(item);
            if (!IsInstanceValid(item.Entry))
                return;
            item.Entry.ResetForPool();
            item.Entry.GetParent()?.RemoveChild(item.Entry);
            _entryPool.Push(item.Entry);
            _visible.Remove(item);
            TryDequeue(true);
            Reflow(false);
        }

        private void TryDequeue(bool commitImmediately = false)
        {
            var enqueuedAny = false;
            while (_settings.Enabled &&
                   _pending.Count > 0 &&
                   GetOccupiedSlotsCount() < Math.Max(1, _settings.QueuePolicy.MaxVisible))
            {
                ShowNow(_pending.Dequeue());
                enqueuedAny = true;
            }

            if (!commitImmediately || !enqueuedAny)
                return;
            CommitPrewarmedEntries(true);
        }

        private void OnEntryClicked(RitsuToastEntry entry)
        {
            var item = _visible.FirstOrDefault(x => x.Entry == entry);
            if (item == null || item.IsClosing)
                return;

            RequestClose(item, false);
            var callback = item.Request.OnClick;

            try
            {
                callback?.Invoke();
                entry.GetViewport()?.SetInputAsHandled();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Toast] Click callback failed: {ex.Message}");
            }
        }

        private void OnEntryHoverStateChanged(RitsuToastEntry entry, bool hovering)
        {
            var item = _visible.FirstOrDefault(x => x.Entry == entry);
            if (item == null || item.IsClosing || item.IsHovering == hovering)
                return;
            item.IsHovering = hovering;
            _hoveringCount += hovering ? 1 : -1;
            if (_hoveringCount < 0)
                _hoveringCount = 0;
        }

        private void Reflow(bool immediate)
        {
            if (_root == null || _visible.Count == 0)
                return;

            var viewportSize = ResolveViewportSize();
            var gap = Math.Max(0f, _settings.QueuePolicy.Gap);
            var placement = _settings.Placement;
            var stackModel = ResolveStackModel(placement, viewportSize);
            var measured = new List<Vector2>(_visible.Count);
            var margin = 0f;
            var imageOnRight = stackModel.Horizontal == ToastHorizontalAnchor.Left;
            foreach (var item in _visible)
            {
                item.Entry.SetImageOnRight(imageOnRight);
                var size = item is { HasEntered: false, HasMeasuredSize: true }
                    ? item.MeasuredSize
                    : MeasureItem(item, viewportSize.X);
                item.MeasuredSize = size;
                item.HasMeasuredSize = true;
                measured.Add(size);
                margin = Math.Max(margin, item.Style.ScreenMargin);
            }

            var slotYs = BuildSlotYPositions(measured, gap, stackModel.HeadY, stackModel.Direction);
            var shiftY = ResolveVerticalBlockShift(slotYs, measured, viewportSize.Y, margin, stackModel.Direction);

            for (var i = 0; i < _visible.Count; i++)
            {
                var item = _visible[i];
                var size = measured[i];
                var x = ResolveAnchoredX(stackModel.Horizontal, size.X, viewportSize.X, placement.Offset.X);
                x = ClampX(x, size.X, viewportSize.X, item.Style.ScreenMargin);
                var y = slotYs[i] + shiftY;
                var target = new Vector2(x, y);
                item.TargetPosition = target;
                if (item.HasEntered)
                    BeginMoveAnimation(item, target, immediate);
                else
                    item.Entry.SetPositionImmediate(target);
            }
        }

        private void QueueDeferredReflow()
        {
            if (_hasDeferredReflowQueued || !IsInsideTree())
                return;

            _hasDeferredReflowQueued = true;
            CallDeferred(nameof(DeferredReflow));
        }

        private void DeferredReflow()
        {
            _hasDeferredReflowQueued = false;
            Reflow(false);
        }

        private void QueuePendingEnterDispatch()
        {
            if (_hasPendingEnterDispatchQueued || !IsInsideTree())
                return;
            _hasPendingEnterDispatchQueued = true;
            CallDeferred(nameof(DispatchPendingEnterAnimations));
        }

        private void QueuePrewarmCommit()
        {
            if (_hasPrewarmCommitQueued || !IsInsideTree())
                return;
            _hasPrewarmCommitQueued = true;
            CallDeferred(nameof(CommitPrewarmedEntries));
        }

        private void CommitPrewarmedEntries()
        {
            // ReSharper disable once IntroduceOptionalParameters.Local
            CommitPrewarmedEntries(false);
        }

        private void CommitPrewarmedEntries(bool dispatchImmediately)
        {
            _hasPrewarmCommitQueued = false;
            if (_root == null || _prewarming.Count == 0)
                return;

            var prewarmed = _prewarming.ToList();
            _prewarming.Clear();
            var viewportSize = ResolveViewportSize();
            var stackModel = ResolveStackModel(_settings.Placement, viewportSize);
            var imageOnRight = stackModel.Horizontal == ToastHorizontalAnchor.Left;
            foreach (var item in prewarmed.Where(item => !item.IsClosing && IsInstanceValid(item.Entry)))
            {
                item.Entry.SetImageOnRight(imageOnRight);
                item.MeasuredSize = MeasureItem(item, viewportSize.X);
                item.HasMeasuredSize = true;
                item.Entry.GetParent()?.RemoveChild(item.Entry);
                _root.AddChild(item.Entry);
                _visible.Insert(0, item);
                _pendingEnter.Add(item);
            }

            Reflow(false);
            foreach (var item in _pendingEnter.Where(item => !item.IsClosing && IsInstanceValid(item.Entry)))
                item.Entry.SetPositionImmediate(item.TargetPosition);

            if (dispatchImmediately)
                DispatchPendingEnterAnimations();
            else
                QueuePendingEnterDispatch();
            QueueDeferredReflow();
            TryDequeue();
        }

        private void DispatchPendingEnterAnimations()
        {
            _hasPendingEnterDispatchQueued = false;
            if (_pendingEnter.Count == 0)
                return;

            var axis = ResolveAnchorAxis();
            var pending = _pendingEnter.ToList();
            _pendingEnter.Clear();
            foreach (var item in pending.Where(item => !item.IsClosing && IsInstanceValid(item.Entry)))
            {
                item.Entry.SetPositionImmediate(item.TargetPosition);
                item.Entry.SetPivotCenter(item.MeasuredSize);
                BeginEnterAnimation(item, axis);
            }
        }

        private RitsuToastEntry AcquireEntry()
        {
            return _entryPool.Count > 0 ? _entryPool.Pop() : new();
        }

        private int GetOccupiedSlotsCount()
        {
            return _visible.Count + _prewarming.Count;
        }

        private Vector2 MeasureItem(VisibleToast item, float viewportWidth)
        {
            var desiredWidth = item.Style.Width;
            var desiredMinHeight = item.Style.MinHeight;
            if (item.Request.Image != null)
            {
                var extraImageSlotWidth = item.Style.ImageSize + item.Style.RowSpacing;
                desiredWidth += extraImageSlotWidth;
                var imageDrivenHeight = item.Style.ImageSize + item.Style.PaddingVertical * 2f;
                desiredMinHeight = Math.Max(desiredMinHeight, imageDrivenHeight);
            }

            var maxWidth = Math.Max(120f, viewportWidth - item.Style.ScreenMargin * 2f);
            var width = Math.Min(desiredWidth, maxWidth);
            var size = item.Entry.Measure(width, desiredMinHeight);
            return size is { X: > 1f, Y: > 1f } ? size : new(width, desiredMinHeight);
        }

        private void RecyclePrewarming(VisibleToast item)
        {
            item.Entry.Clicked -= OnEntryClicked;
            item.Entry.HoverStateChanged -= OnEntryHoverStateChanged;
            if (!IsInstanceValid(item.Entry))
                return;
            item.Entry.ResetForPool();
            item.Entry.GetParent()?.RemoveChild(item.Entry);
            _entryPool.Push(item.Entry);
        }

        private static List<float> BuildSlotYPositions(IReadOnlyList<Vector2> sizes, float gap, float headY,
            int direction)
        {
            var positions = new List<float>(sizes.Count);
            var cursor = 0f;
            for (var i = 0; i < sizes.Count; i++)
            {
                var h = sizes[i].Y;
                var y = direction >= 0
                    ? headY + cursor
                    : headY - cursor - h;
                positions.Add(y);
                cursor += h + gap;
            }

            return positions;
        }

        private static float ResolveVerticalBlockShift(IReadOnlyList<float> slotYs, IReadOnlyList<Vector2> sizes,
            float viewportHeight, float margin, int direction)
        {
            if (slotYs.Count == 0)
                return 0f;

            var blockTop = float.MaxValue;
            var blockBottom = float.MinValue;
            for (var i = 0; i < slotYs.Count; i++)
            {
                var top = slotYs[i];
                var bottom = top + sizes[i].Y;
                if (top < blockTop)
                    blockTop = top;
                if (bottom > blockBottom)
                    blockBottom = bottom;
            }

            var maxY = viewportHeight - margin;
            var available = maxY - margin;
            var blockHeight = blockBottom - blockTop;

            if (blockHeight > available)
                return direction >= 0 ? margin - blockTop : maxY - blockBottom;

            var shift = 0f;
            if (blockTop < margin)
                shift = margin - blockTop;
            if (blockBottom + shift > maxY)
                shift = maxY - blockBottom;
            return shift;
        }

        private static float ClampX(float x, float width, float viewportWidth, float margin)
        {
            var maxX = Math.Max(margin, viewportWidth - width - margin);
            return Mathf.Clamp(x, margin, maxX);
        }

        private static float ResolveAnchoredX(ToastHorizontalAnchor horizontal, float width, float viewportWidth,
            float offsetX)
        {
            return horizontal switch
            {
                ToastHorizontalAnchor.Left => 0f + offsetX,
                ToastHorizontalAnchor.Center => (viewportWidth - width) * 0.5f + offsetX,
                _ => viewportWidth - width + offsetX,
            };
        }

        private static ToastStackModel ResolveStackModel(RitsuToastPlacement placement, Vector2 viewportSize)
        {
            var offset = placement.Offset;
            return placement.Anchor switch
            {
                RitsuToastAnchor.TopLeft => new(0f + offset.Y, 1, ToastHorizontalAnchor.Left),
                RitsuToastAnchor.TopCenter => new(0f + offset.Y, 1, ToastHorizontalAnchor.Center),
                RitsuToastAnchor.TopRight => new(0f + offset.Y, 1, ToastHorizontalAnchor.Right),
                RitsuToastAnchor.MiddleLeft => new(viewportSize.Y * 0.5f + offset.Y, 1, ToastHorizontalAnchor.Left),
                RitsuToastAnchor.MiddleCenter =>
                    new(viewportSize.Y * 0.5f + offset.Y, 1, ToastHorizontalAnchor.Center),
                RitsuToastAnchor.MiddleRight =>
                    new(viewportSize.Y * 0.5f + offset.Y, 1, ToastHorizontalAnchor.Right),
                RitsuToastAnchor.BottomLeft => new(viewportSize.Y + offset.Y, -1, ToastHorizontalAnchor.Left),
                RitsuToastAnchor.BottomCenter => new(viewportSize.Y + offset.Y, -1, ToastHorizontalAnchor.Center),
                _ => new(viewportSize.Y + offset.Y, -1, ToastHorizontalAnchor.Right),
            };
        }

        private Vector2 ResolveViewportSize()
        {
            var viewportSize = GetViewport()?.GetVisibleRect().Size ?? Vector2.Zero;
            if (viewportSize is { X: > 1f, Y: > 1f })
                return viewportSize;

            var windowSize = GetWindow()?.Size ?? Vector2.Zero;
            if (windowSize is { X: > 1f, Y: > 1f })
                return windowSize;

            var displaySize = DisplayServer.WindowGetSize();
            if (displaySize is { X: > 1, Y: > 1 })
                return new(displaySize.X, displaySize.Y);

            return new(1920f, 1080f);
        }

        private Vector2 ResolveAnchorAxis()
        {
            var anchor = _settings.Placement.Anchor;
            return anchor switch
            {
                RitsuToastAnchor.TopLeft or RitsuToastAnchor.MiddleLeft or RitsuToastAnchor.BottomLeft => Vector2.Left,
                RitsuToastAnchor.TopRight or RitsuToastAnchor.MiddleRight or RitsuToastAnchor.BottomRight =>
                    Vector2.Right,
                _ => Vector2.Up,
            };
        }

        private void BeginEnterAnimation(VisibleToast item, Vector2 axis)
        {
            var preset = item.Request.AnimationOverride ?? _settings.AnimationPreset;
            item.HasEntered = true;
            item.Entering = true;
            item.EnterElapsed = 0f;
            item.EnterDuration = Math.Max(0.01f, item.Style.EnterDuration);
            item.EnterPreset = preset;
            item.EnterAxis = axis;
            item.EnterSlideDistance = item.Style.EnterSlideDistance;
            item.EnterScale = item.Style.EnterScale;
            item.EnterToPosition = item.TargetPosition;

            item.Entry.Modulate = new(item.Entry.Modulate.R, item.Entry.Modulate.G, item.Entry.Modulate.B, 0f);
            item.Entry.Scale = Vector2.One;
            item.Entry.Position = item.TargetPosition;

            switch (preset)
            {
                case RitsuToastAnimationPreset.FadeScale:
                    item.Entry.Scale = new(item.EnterScale, item.EnterScale);
                    item.EnterFromPosition = item.EnterToPosition;
                    break;
                case RitsuToastAnimationPreset.FadeSlide:
                    item.EnterFromPosition = item.EnterToPosition + axis * item.EnterSlideDistance;
                    item.Entry.Position = item.EnterFromPosition;
                    break;
                default:
                    item.EnterFromPosition = item.EnterToPosition;
                    break;
            }
        }

        private void BeginMoveAnimation(VisibleToast item, Vector2 target, bool immediate)
        {
            if (immediate)
            {
                item.Moving = false;
                item.Entry.SetPositionImmediate(target);
                return;
            }

            if (item.Entering)
            {
                var delta = target - item.EnterToPosition;
                item.EnterToPosition = target;
                item.EnterFromPosition += delta;
                return;
            }

            var duration = Math.Max(0.01f, item.Style.MoveDuration);
            item.Moving = duration > 0.01f;
            item.MoveElapsed = 0f;
            item.MoveDuration = duration;
            item.MoveFromPosition = item.Entry.Position;
            item.MoveToPosition = target;
        }

        private void StepAnimations(double delta)
        {
            var dt = (float)Math.Max(0d, delta);

            foreach (var item in _visible.Where(item => !item.IsClosing && IsInstanceValid(item.Entry)))
                if (item.Entering)
                    StepEnter(item, dt);
                else if (item.Moving)
                    StepMove(item, dt);

            for (var i = _closing.Count - 1; i >= 0; i--)
            {
                var item = _closing[i];
                if (!IsInstanceValid(item.Entry))
                {
                    _closing.RemoveAt(i);
                    continue;
                }

                if (!item.Exiting)
                {
                    FinalizeClose(item);
                    continue;
                }

                StepExit(item, dt);
                if (item.ExitCompleted)
                    FinalizeClose(item);
            }
        }

        private static void StepEnter(VisibleToast item, float dt)
        {
            item.EnterElapsed += dt;
            var t = Mathf.Clamp(item.EnterElapsed / item.EnterDuration, 0f, 1f);
            var p = EaseOutCubic(t);

            var c = item.Entry.Modulate;
            item.Entry.Modulate = new(c.R, c.G, c.B, p);

            switch (item.EnterPreset)
            {
                case RitsuToastAnimationPreset.FadeScale:
                {
                    var s = Mathf.Lerp(item.EnterScale, 1f, EaseOutBack(t));
                    item.Entry.Scale = new(s, s);
                    item.Entry.Position = item.EnterToPosition;
                    break;
                }
                case RitsuToastAnimationPreset.FadeSlide:
                {
                    item.Entry.Position = item.EnterFromPosition.Lerp(item.EnterToPosition, p);
                    item.Entry.Scale = Vector2.One;
                    break;
                }
                default:
                    item.Entry.Position = item.EnterToPosition;
                    item.Entry.Scale = Vector2.One;
                    break;
            }

            if (!(t >= 1f)) return;
            item.Entering = false;
            item.Entry.Modulate = new(c.R, c.G, c.B);
            item.Entry.Scale = Vector2.One;
            item.Entry.Position = item.EnterToPosition;
        }

        private static void StepMove(VisibleToast item, float dt)
        {
            item.MoveElapsed += dt;
            var t = Mathf.Clamp(item.MoveElapsed / item.MoveDuration, 0f, 1f);
            var p = EaseOutCubic(t);
            item.Entry.Position = item.MoveFromPosition.Lerp(item.MoveToPosition, p);
            if (!(t >= 1f)) return;
            item.Moving = false;
            item.Entry.Position = item.MoveToPosition;
        }

        private static void StepExit(VisibleToast item, float dt)
        {
            item.ExitElapsed += dt;
            var t = Mathf.Clamp(item.ExitElapsed / item.ExitDuration, 0f, 1f);
            var p = EaseInCubic(t);

            var c = item.Entry.Modulate;
            item.Entry.Modulate = new(c.R, c.G, c.B, 1f - t);

            switch (item.ExitPreset)
            {
                case RitsuToastAnimationPreset.FadeScale:
                {
                    var s = Mathf.Lerp(1f, 0.94f, p);
                    item.Entry.Scale = new(s, s);
                    break;
                }
                case RitsuToastAnimationPreset.FadeSlide:
                {
                    item.Entry.Position = item.ExitFromPosition - item.ExitAxis * item.ExitSlideDistance * p;
                    break;
                }
            }

            if (t >= 1f)
                item.ExitCompleted = true;
        }

        private static float EaseOutCubic(float t)
        {
            var u = 1f - t;
            return 1f - u * u * u;
        }

        private static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            var u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        private sealed class VisibleToast(
            RitsuToastEntry entry,
            RitsuToastRequest request,
            RitsuToastVisualStyle style,
            double remainingSeconds)
        {
            public RitsuToastEntry Entry { get; } = entry;
            public RitsuToastRequest Request { get; } = request;
            public double RemainingSeconds { get; set; } = remainingSeconds;
            public Vector2 TargetPosition { get; set; }
            public Vector2 MeasuredSize { get; set; }
            public bool HasMeasuredSize { get; set; }
            public bool HasEntered { get; set; }
            public bool IsHovering { get; set; }
            public bool IsClosing { get; set; }
            public RitsuToastVisualStyle Style { get; set; } = style;

            public bool Entering { get; set; }
            public float EnterElapsed { get; set; }
            public float EnterDuration { get; set; }
            public RitsuToastAnimationPreset EnterPreset { get; set; }
            public Vector2 EnterAxis { get; set; }
            public float EnterSlideDistance { get; set; }
            public float EnterScale { get; set; }
            public Vector2 EnterFromPosition { get; set; }
            public Vector2 EnterToPosition { get; set; }

            public bool Moving { get; set; }
            public float MoveElapsed { get; set; }
            public float MoveDuration { get; set; }
            public Vector2 MoveFromPosition { get; set; }
            public Vector2 MoveToPosition { get; set; }

            public bool Exiting { get; set; }
            public bool ExitCompleted { get; set; }
            public float ExitElapsed { get; set; }
            public float ExitDuration { get; set; }
            public RitsuToastAnimationPreset ExitPreset { get; set; }
            public Vector2 ExitAxis { get; set; }
            public float ExitSlideDistance { get; set; }
            public Vector2 ExitFromPosition { get; set; }
            public Vector2 ExitFromScale { get; set; }
        }

        private enum ToastHorizontalAnchor
        {
            Left,
            Center,
            Right,
        }

        private readonly record struct ToastStackModel(float HeadY, int Direction, ToastHorizontalAnchor Horizontal);
    }
}
