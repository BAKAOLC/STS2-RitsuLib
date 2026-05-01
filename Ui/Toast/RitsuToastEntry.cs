using Godot;

namespace STS2RitsuLib.Ui.Toast
{
    internal sealed partial class RitsuToastEntry : PanelContainer
    {
        private Label? _bodyLabel;
        private TextureRect? _image;
        private bool _isExiting;
        private StyleBoxFlat? _panelHover;
        private StyleBoxFlat? _panelNormal;
        private RitsuToastRequest _request = new(string.Empty);
        private HBoxContainer? _row;
        private RitsuToastVisualStyle _style = null!;
        private VBoxContainer? _textColumn;
        private Label? _titleLabel;

        public RitsuToastEntry()
        {
            Name = "RitsuToastEntry";
            MouseFilter = MouseFilterEnum.Stop;
            FocusMode = FocusModeEnum.None;
            ProcessMode = ProcessModeEnum.Always;
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            SizeFlagsVertical = SizeFlags.ShrinkBegin;
            BuildTree();
        }

        public bool IsEntering { get; private set; }

        public event Action<RitsuToastEntry>? Clicked;
        public event Action<RitsuToastEntry, bool>? HoverStateChanged;

        public void Configure(RitsuToastRequest request, RitsuToastVisualStyle style)
        {
            _request = request;
            ApplyContent();
            ApplyStyle(style);
            _isExiting = false;
            Modulate = new(Modulate.R, Modulate.G, Modulate.B, 0f);
            Scale = Vector2.One;
        }

        public void SetPivotCenter(Vector2 measuredSize)
        {
            PivotOffset = measuredSize * 0.5f;
        }

        public Vector2 Measure(float width, float minHeight)
        {
            CustomMinimumSize = new(Math.Max(120f, width), minHeight);
            ResetSize();
            var min = GetCombinedMinimumSize();
            return new(Math.Max(CustomMinimumSize.X, min.X), Math.Max(CustomMinimumSize.Y, min.Y));
        }

        public void ApplyStyle(RitsuToastVisualStyle style)
        {
            _style = style;
            _panelNormal = BuildPanel(style, style.Border);
            _panelHover = BuildPanel(style, style.AccentColor);
            ApplyHoverVisual(false);
            _titleLabel?.AddThemeColorOverride("font_color", style.TitleColor);
            _titleLabel?.AddThemeFontSizeOverride("font_size", style.TitleFontSize);
            _bodyLabel?.AddThemeColorOverride("font_color", style.BodyColor);
            _bodyLabel?.AddThemeFontSizeOverride("font_size", style.BodyFontSize);
            if (_textColumn != null)
            {
                _textColumn.AddThemeConstantOverride("separation",
                    (int)Math.Round(style.TextSpacing, MidpointRounding.AwayFromZero));
                _textColumn.CustomMinimumSize = new(style.Width, 0f);
                _textColumn.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            }

            _row?.AddThemeConstantOverride("separation",
                (int)Math.Round(style.RowSpacing, MidpointRounding.AwayFromZero));
            if (_image != null)
                _image.CustomMinimumSize = new(style.ImageSize, style.ImageSize);
        }

        public void SetImageOnRight(bool imageOnRight)
        {
            if (_row == null || _image == null || _textColumn == null)
                return;

            var imageIndex = _image.GetIndex();
            var textIndex = _textColumn.GetIndex();
            if (imageOnRight)
            {
                if (imageIndex > textIndex)
                    return;
                _row.MoveChild(_image, _row.GetChildCount() - 1);
                return;
            }

            if (imageIndex < textIndex)
                return;
            _row.MoveChild(_image, 0);
        }

        public void SetPositionImmediate(Vector2 target)
        {
            Position = target;
        }

        public void PlayEnter(RitsuToastAnimationPreset preset, Vector2 axisHint, Vector2 targetPosition,
            float duration,
            float slideDistance,
            float enterScale)
        {
            IsEntering = true;
            Modulate = new(Modulate.R, Modulate.G, Modulate.B, 0f);
            Scale = Vector2.One;
            Position = targetPosition;

            switch (preset)
            {
                case RitsuToastAnimationPreset.FadeScale:
                    Scale = new(enterScale, enterScale);
                    break;
                case RitsuToastAnimationPreset.FadeSlide:
                    Position = targetPosition + axisHint * slideDistance;
                    break;
            }
        }

        public void PlayExit(RitsuToastAnimationPreset preset, Vector2 axisHint, float duration, float slideDistance,
            Action onDone)
        {
            if (_isExiting)
                return;

            _isExiting = true;
            Modulate = new(Modulate.R, Modulate.G, Modulate.B, 0f);
            onDone();
        }

        public void ResetForPool()
        {
            IsEntering = false;
            _isExiting = false;
            Modulate = new(Modulate.R, Modulate.G, Modulate.B);
            Scale = Vector2.One;
            Position = Vector2.Zero;
            _request = new(string.Empty);
            _panelNormal = null;
            _panelHover = null;
            if (_titleLabel != null)
                _titleLabel.Text = string.Empty;
            if (_bodyLabel != null)
                _bodyLabel.Text = string.Empty;

            if (_image == null) return;
            _image.Texture = null;
            _image.Visible = false;
        }

        private void BuildTree()
        {
            var root = new VBoxContainer();
            AddChild(root);

            _row = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            root.AddChild(_row);

            _image = new()
            {
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                Visible = false,
            };
            _row.AddChild(_image);

            _textColumn = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            _row.AddChild(_textColumn);

            _titleLabel = new()
            {
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            _textColumn.AddChild(_titleLabel);

            _bodyLabel = new()
            {
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            _textColumn.AddChild(_bodyLabel);

            MouseEntered += () =>
            {
                ApplyHoverVisual(true);
                HoverStateChanged?.Invoke(this, true);
            };
            MouseExited += () =>
            {
                ApplyHoverVisual(false);
                HoverStateChanged?.Invoke(this, false);
            };
            GuiInput += OnGuiInput;
        }

        private void OnGuiInput(InputEvent inputEvent)
        {
            if (inputEvent is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                return;
            Clicked?.Invoke(this);
            GetViewport()?.SetInputAsHandled();
        }

        private void ApplyContent()
        {
            if (_titleLabel != null)
            {
                _titleLabel.Text = _request.Title ?? string.Empty;
                _titleLabel.Visible = !string.IsNullOrWhiteSpace(_request.Title);
            }

            if (_bodyLabel != null)
                _bodyLabel.Text = _request.Body;

            if (_image == null) return;
            _image.Texture = _request.Image;
            _image.Visible = _request.Image != null;
        }

        private static StyleBoxFlat BuildPanel(RitsuToastVisualStyle style, Color borderColor)
        {
            return new()
            {
                BgColor = style.Background,
                BorderColor = borderColor,
                BorderWidthLeft = style.BorderWidth,
                BorderWidthTop = style.BorderWidth,
                BorderWidthRight = style.BorderWidth,
                BorderWidthBottom = style.BorderWidth,
                CornerRadiusTopLeft = style.CornerRadius,
                CornerRadiusTopRight = style.CornerRadius,
                CornerRadiusBottomRight = style.CornerRadius,
                CornerRadiusBottomLeft = style.CornerRadius,
                ShadowColor = style.ShadowColor,
                ShadowSize = (int)Math.Round(style.ShadowSize, MidpointRounding.AwayFromZero),
                ContentMarginLeft = style.PaddingHorizontal,
                ContentMarginTop = style.PaddingVertical,
                ContentMarginRight = style.PaddingHorizontal,
                ContentMarginBottom = style.PaddingVertical,
            };
        }

        private void ApplyHoverVisual(bool hovering)
        {
            var panel = hovering ? _panelHover : _panelNormal;
            if (panel == null)
                return;
            AddThemeStyleboxOverride("panel", panel);
        }
    }
}
