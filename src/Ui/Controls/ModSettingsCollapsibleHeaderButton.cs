using Godot;
using STS2RitsuLib.Ui.Layout;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         A themed expandable-section header with a title, optional subtitle, and selection indicator.
    ///         Use on the Godot main thread.
    ///     </para>
    ///     <para xml:lang="zh-CN">带标题、可选副标题和选择标记的主题折叠栏标题控件。请在 Godot 主线程使用。</para>
    /// </summary>
    public sealed partial class ModSettingsCollapsibleHeaderButton : ModSettingsGamepadCompatibleButton
    {
        private bool _applyingSelectedState;
        private Label? _arrowLabel;
        private bool _contentEnabled = true;
        private Vector2 _dynamicMinimumSize;
        private BoxEdges _headerPadding;
        private int _headerSeparation;
        private bool _selected;
        private string? _subtitle;
        private Label? _subtitleLabel;
        private int _textSeparation;
        private string _title = string.Empty;
        private Label? _titleLabel;
        private bool _visualContentEnabled;
        private bool _visualSelected;
        private bool _visualStateApplied;

        /// <summary>
        ///     <para xml:lang="en">Creates and configures the control on the Godot main thread.</para>
        ///     <para xml:lang="zh-CN">在 Godot 主线程创建并配置此控件。</para>
        /// </summary>
        /// <param name="title">
        ///     <para xml:lang="en">Non-null title text; an empty title is allowed.</para>
        ///     <para xml:lang="zh-CN">非 null 的标题文本；允许为空字符串。</para>
        /// </param>
        /// <param name="subtitle">
        ///     <para xml:lang="en">Optional subtitle; blank text hides it.</para>
        ///     <para xml:lang="zh-CN">可选副标题；空白文本会将其隐藏。</para>
        /// </param>
        /// <param name="action">
        ///     <para xml:lang="en">Callback on a header press; exceptions propagate.</para>
        ///     <para xml:lang="zh-CN">按下标题时的回调；异常会继续传播。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">A required reference argument is null.</para>
        ///     <para xml:lang="zh-CN">必需的引用参数为 null。</para>
        /// </exception>
        public ModSettingsCollapsibleHeaderButton(string title, string? subtitle, Action action)
        {
            ArgumentNullException.ThrowIfNull(title);
            ArgumentNullException.ThrowIfNull(action);
            _title = title;
            _subtitle = subtitle;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            ClipContents = false;
            Text = string.Empty;
            CustomMinimumSize = ResolveHeaderMinSize(subtitle);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;

            AddThemeStyleboxOverride("normal", CreateHeaderStyle(false, false, true));
            AddThemeStyleboxOverride("hover", CreateHeaderStyle(false, true, true));
            AddThemeStyleboxOverride("pressed", CreateHeaderStyle(true, true, true));
            AddThemeStyleboxOverride("focus", CreateHeaderFocusStyle(false, true));

            var headerPadding = RitsuShellThemeLayoutResolver.ResolveEdges(
                "components.collapsible.layout.header.padding", 14);
            _headerPadding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.header.padding.left",
                    headerPadding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.header.padding.top", 10),
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.header.padding.right",
                    headerPadding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.header.padding.bottom", 10));
            _headerSeparation = RitsuShellThemeLayoutResolver.ResolveInt(
                "components.collapsible.layout.header.separation", 12);
            _textSeparation = RitsuShellThemeLayoutResolver.ResolveInt(
                "components.collapsible.layout.header.textSeparation", 2);

            var arrowLabel = new Label
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.collapsible.layout.header.arrow.minSize",
                    new(28f, 28f)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            arrowLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            arrowLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.HeaderArrow);
            arrowLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.RichSecondary);
            AddChild(arrowLabel);
            _arrowLabel = arrowLabel;

            var titleLabel = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                ClipText = false,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            titleLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            titleLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.HeaderTitle);
            titleLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            AddChild(titleLabel);
            _titleLabel = titleLabel;

            var subtitleLabel = new Label
            {
                Text = subtitle ?? string.Empty,
                Visible = !string.IsNullOrWhiteSpace(subtitle),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ClipText = false,
            };
            subtitleLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            subtitleLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.HeaderSubtitle);
            subtitleLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            AddChild(subtitleLabel);
            _subtitleLabel = subtitleLabel;

            Pressed += action;
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes the control for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化此控件。</para>
        /// </summary>
        public ModSettingsCollapsibleHeaderButton()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Updates the title and optional subtitle and requests layout.</para>
        ///     <para xml:lang="zh-CN">更新标题及可选副标题，并请求重新布局。</para>
        /// </summary>
        /// <param name="title">
        ///     <para xml:lang="en">Non-null title text; an empty title is allowed.</para>
        ///     <para xml:lang="zh-CN">非 null 的标题文本；允许为空字符串。</para>
        /// </param>
        /// <param name="subtitle">
        ///     <para xml:lang="en">Optional subtitle; blank text hides it.</para>
        ///     <para xml:lang="zh-CN">可选副标题；空白文本会将其隐藏。</para>
        /// </param>
        public void SetTexts(string title, string? subtitle)
        {
            ArgumentNullException.ThrowIfNull(title);
            _title = title;
            _subtitle = subtitle;
            if (_titleLabel != null)
                _titleLabel.Text = title;
            if (_subtitleLabel != null)
            {
                _subtitleLabel.Text = subtitle ?? string.Empty;
                _subtitleLabel.Visible = !string.IsNullOrWhiteSpace(subtitle);
            }

            CustomMinimumSize = ResolveHeaderMinSize(subtitle);
            RequestLayout();
        }

        /// <inheritdoc />
        public override Vector2 _GetMinimumSize()
        {
            return new(
                Math.Max(CustomMinimumSize.X, _dynamicMinimumSize.X),
                Math.Max(CustomMinimumSize.Y, _dynamicMinimumSize.Y));
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            if (_titleLabel != null)
                _titleLabel.Text = _title;
            if (_subtitleLabel != null)
                _subtitleLabel.Text = _subtitle ?? string.Empty;
            ApplySelectedState();
            RequestLayout();
        }

        /// <inheritdoc />
        public override void _Notification(int what)
        {
            base._Notification(what);
            switch (what)
            {
                case (int)NotificationResized:
                    LayoutLabels();
                    break;
                case (int)NotificationThemeChanged:
                    RefreshLayoutTokens();
                    if (_applyingSelectedState)
                    {
                        RequestLayout();
                        break;
                    }

                    _visualStateApplied = false;
                    ApplySelectedState();
                    RequestLayout();
                    break;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Updates the selection indicator without invoking the pressed callback.</para>
        ///     <para xml:lang="zh-CN">更新选择标记，不调用按下回调。</para>
        /// </summary>
        /// <param name="selected">
        ///     <para xml:lang="en">Whether to show the selected indicator.</para>
        ///     <para xml:lang="zh-CN">是否显示选择标记。</para>
        /// </param>
        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplySelectedState();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Updates the visual treatment of the section header. It does not disable, free, or alter child
        ///         controls.
        ///     </para>
        ///     <para xml:lang="zh-CN">更新折叠栏标题的视觉状态，不禁用、释放或修改子控件。</para>
        /// </summary>
        /// <param name="enabled">
        ///     <para xml:lang="en">Whether to use the enabled visual state.</para>
        ///     <para xml:lang="zh-CN">是否使用启用时的视觉状态。</para>
        /// </param>
        public void SetContentEnabled(bool enabled)
        {
            _contentEnabled = enabled;
            ApplySelectedState();
        }

        private void ApplySelectedState()
        {
            if (_visualStateApplied && _visualSelected == _selected && _visualContentEnabled == _contentEnabled)
                return;

            _visualSelected = _selected;
            _visualContentEnabled = _contentEnabled;
            _visualStateApplied = true;

            _applyingSelectedState = true;
            try
            {
                AddThemeStyleboxOverride("normal", CreateHeaderStyle(_selected, false, _contentEnabled));
                AddThemeStyleboxOverride("hover", CreateHeaderStyle(_selected, true, _contentEnabled));
                AddThemeStyleboxOverride("pressed", CreateHeaderStyle(true, true, _contentEnabled));
                AddThemeStyleboxOverride("focus", CreateHeaderFocusStyle(_selected, _contentEnabled));
            }
            finally
            {
                _applyingSelectedState = false;
            }

            if (_arrowLabel != null)
                _arrowLabel.Text = _selected ? "▼" : "▶";

            var opacity = _contentEnabled ? 1f : ResolveDisabledOpacityFactor();
            ApplyLabelOpacity(_arrowLabel, opacity);
            ApplyLabelOpacity(_titleLabel, opacity);
            ApplyLabelOpacity(_subtitleLabel, opacity);
            QueueRedraw();
        }

        private void RequestLayout()
        {
            UpdateMinimumSize();
            LayoutLabels();
        }

        private void RequestAncestorLayout()
        {
            if (GetParent() is Control parent)
            {
                parent.UpdateMinimumSize();
                if (parent is Container container && container.IsInsideTree())
                    container.QueueSort();
            }

            RitsuVerticalStack.RequestAncestorLayouts(this);
        }

        private void RefreshLayoutTokens()
        {
            var headerPadding = RitsuShellThemeLayoutResolver.ResolveEdges(
                "components.collapsible.layout.header.padding", 14);
            _headerPadding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.header.padding.left",
                    headerPadding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.header.padding.top", 10),
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.header.padding.right",
                    headerPadding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.header.padding.bottom", 10));
            _headerSeparation = RitsuShellThemeLayoutResolver.ResolveInt(
                "components.collapsible.layout.header.separation", 12);
            _textSeparation = RitsuShellThemeLayoutResolver.ResolveInt(
                "components.collapsible.layout.header.textSeparation", 2);
        }

        private void LayoutLabels()
        {
            if (!IsInsideTree())
                return;

            var contentX = (float)_headerPadding.Left;
            var contentY = (float)_headerPadding.Top;
            var contentWidth = Math.Max(0f, Size.X - _headerPadding.Left - _headerPadding.Right);
            var contentHeight = Math.Max(0f, Size.Y - _headerPadding.Top - _headerPadding.Bottom);
            var arrowMin = _arrowLabel?.GetCombinedMinimumSize() ?? Vector2.Zero;
            if (_arrowLabel is { Visible: true })
            {
                _arrowLabel.Position = new(contentX, contentY + Math.Max(0f, (contentHeight - arrowMin.Y) * 0.5f));
                _arrowLabel.Size = arrowMin;
            }

            var textX = contentX + arrowMin.X + _headerSeparation;
            var textWidth = Math.Max(0f, contentX + contentWidth - textX);
            var titleMin = GetWrappedLabelMinSize(_titleLabel, textWidth);
            var subtitleVisible = _subtitleLabel is { Visible: true };
            var subtitleMin = subtitleVisible ? GetWrappedLabelMinSize(_subtitleLabel, textWidth) : Vector2.Zero;
            var textHeight = titleMin.Y;
            if (subtitleVisible)
                textHeight += _textSeparation + subtitleMin.Y;

            var nextDynamicMin = new Vector2(
                0f,
                _headerPadding.Top + _headerPadding.Bottom + Math.Max(arrowMin.Y, textHeight));
            if (_dynamicMinimumSize.DistanceSquaredTo(nextDynamicMin) > 0.01f)
            {
                _dynamicMinimumSize = nextDynamicMin;
                UpdateMinimumSize();
                RequestAncestorLayout();
            }

            var y = contentY + Math.Max(0f, (contentHeight - textHeight) * 0.5f);
            if (_titleLabel is { Visible: true })
            {
                _titleLabel.Position = new(textX, y);
                _titleLabel.Size = new(textWidth, titleMin.Y);
                y += titleMin.Y;
            }

            if (!subtitleVisible)
                return;
            y += _textSeparation;
            _subtitleLabel!.Position = new(textX, y);
            _subtitleLabel.Size = new(textWidth, subtitleMin.Y);
        }

        private static Vector2 GetWrappedLabelMinSize(Label? label, float width)
        {
            if (label is not { Visible: true })
                return Vector2.Zero;

            label.Size = new(Math.Max(1f, width), label.Size.Y);
            label.UpdateMinimumSize();
            return label.GetCombinedMinimumSize();
        }

        private static StyleBoxFlat CreateHeaderStyle(bool selected, bool hovered, bool contentEnabled)
        {
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.collapsible.layout.borderWidth", 2);
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.collapsible.layout.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);

            return new()
            {
                BgColor = !contentEnabled
                    ? RitsuShellTheme.Current.Component.Collapsible.Disabled.Bg
                    : selected
                        ? RitsuShellTheme.Current.Component.Collapsible.Selected.Bg
                        : hovered
                            ? RitsuShellTheme.Current.Component.Collapsible.Hover.Bg
                            : RitsuShellTheme.Current.Component.Collapsible.Default.Bg,
                BorderColor = !contentEnabled
                    ? RitsuShellTheme.Current.Component.Collapsible.Disabled.Border
                    : selected
                        ? RitsuShellTheme.Current.Component.Collapsible.Selected.Border
                        : RitsuShellTheme.Current.Component.Collapsible.Default.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
            };
        }

        private static StyleBoxFlat CreateHeaderFocusStyle(bool selected, bool contentEnabled)
        {
            var key = $"settings.collapsible.header.{selected}.{contentEnabled}.focus";
            return RitsuShellStyleCache.GetOrBuild(key, () => BuildHeaderFocusStyle(selected, contentEnabled));
        }

        private static StyleBoxFlat BuildHeaderFocusStyle(bool selected, bool contentEnabled)
        {
            var style = (StyleBoxFlat)CreateHeaderStyle(selected, true, contentEnabled).Duplicate();
            if (!contentEnabled)
                return style;

            var border = RitsuShellThemeLayoutResolver.ResolveEdges(
                "components.collapsible.layout.borderWidthFocus", 3);
            style.BorderColor = RitsuShellTheme.Current.Text.HoverHighlight;
            style.BorderWidthLeft = border.Left;
            style.BorderWidthTop = border.Top;
            style.BorderWidthRight = border.Right;
            style.BorderWidthBottom = border.Bottom;
            style.ShadowColor = new(style.BorderColor.R, style.BorderColor.G, style.BorderColor.B, 0.42f);
            style.ShadowSize = RitsuShellThemeLayoutResolver.ResolveInt(
                "components.collapsible.layout.focusShadowSize", 7);
            return style;
        }

        private static void ApplyLabelOpacity(Label? label, float opacity)
        {
            if (label == null)
                return;
            var m = label.Modulate;
            label.Modulate = new(m.R, m.G, m.B, opacity);
        }

        private static float ResolveDisabledOpacityFactor()
        {
            if (RitsuShellTheme.Current.TryGetNumber("semantic.state.disabled.opacity", out var rawValue) &&
                (float)rawValue is > 0.05f and <= 1.0f and var value)
                return value;

            return 0.78f;
        }

        private static Vector2 ResolveHeaderMinSize(string? subtitle)
        {
            var hasSubtitle = !string.IsNullOrWhiteSpace(subtitle);
            var fallback = new Vector2(0f, hasSubtitle ? 84f : 56f);
            var path = hasSubtitle
                ? "components.collapsible.layout.header.minSizeWithSubtitle"
                : "components.collapsible.layout.header.minSize";
            return RitsuShellThemeLayoutResolver.ResolveMinSize(path, fallback);
        }
    }
}
