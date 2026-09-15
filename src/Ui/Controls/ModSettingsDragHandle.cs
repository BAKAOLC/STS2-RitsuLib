using System.Globalization;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         A themed drag handle with a one-based row label. Drag data belongs to the caller's
    ///         drag-and-drop protocol. Use on the Godot main thread.
    ///     </para>
    ///     <para xml:lang="zh-CN">显示从一开始的行号的主题拖拽手柄。拖拽数据由调用方的拖放协议定义。请在 Godot 主线程使用。</para>
    /// </summary>
    public sealed partial class ModSettingsDragHandle : Button
    {
        private readonly Func<Dictionary>? _dragDataProvider;
        private readonly Func<int>? _rowIndexZeroBased;
        private NControllerManager? _hookedControllerManagerDrag;
        private Label? _indexNumberLabel;

        /// <summary>
        ///     <para xml:lang="en">Creates and configures the control on the Godot main thread.</para>
        ///     <para xml:lang="zh-CN">在 Godot 主线程创建并配置此控件。</para>
        /// </summary>
        /// <param name="rowIndexZeroBased">
        ///     <para xml:lang="en">Returns the current nonnegative zero-based row index; callback exceptions propagate.</para>
        ///     <para xml:lang="zh-CN">返回当前从零开始的非负行号；回调异常会继续传播。</para>
        /// </param>
        /// <param name="dragDataProvider">
        ///     <para xml:lang="en">
        ///         Returns drag data understood by the drop target. Called when dragging starts; exceptions
        ///         propagate.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回放置目标可理解的拖拽数据。在拖拽开始时调用；异常会继续传播。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">A required reference argument is null.</para>
        ///     <para xml:lang="zh-CN">必需的引用参数为 null。</para>
        /// </exception>
        public ModSettingsDragHandle(Func<int> rowIndexZeroBased, Func<Dictionary> dragDataProvider)
        {
            ArgumentNullException.ThrowIfNull(rowIndexZeroBased);
            ArgumentNullException.ThrowIfNull(dragDataProvider);
            _rowIndexZeroBased = rowIndexZeroBased;
            _dragDataProvider = dragDataProvider;

            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.dragHandle.layout.minSize",
                new(52f, 0f));
            SizeFlagsVertical = SizeFlags.ExpandFill;
            AddThemeStyleboxOverride("normal", CreateRailStyle(false));
            AddThemeStyleboxOverride("hover", CreateRailStyle(true));
            AddThemeStyleboxOverride("pressed", CreateRailStyle(true));
            AddThemeStyleboxOverride("focus", CreateRailFocusStyle());
            MouseDefaultCursorShape = CursorShape.Drag;

            var content = new VBoxContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            content.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.contentSeparation", 3));
            AddChild(content);

            var number = new Label
            {
                Text = FormatDragIndexLabel(_rowIndexZeroBased()),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            number.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            number.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.ValueLabel);
            number.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.Number);
            content.AddChild(number);
            _indexNumberLabel = number;

            var grip = new Label
            {
                Text = "::::",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            grip.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            grip.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Grip);
            grip.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.Grip);
            content.AddChild(grip);

            var hint = new Label
            {
                Text = RitsuModuleLocalization.Get("list.dragShort", "Drag"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            hint.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            hint.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.HintSmall);
            hint.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.Hint);
            content.AddChild(hint);
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes the control for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化此控件。</para>
        /// </summary>
        public ModSettingsDragHandle()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Refreshes the displayed row number by invoking the index callback. Callback exceptions
        ///         propagate.
        ///     </para>
        ///     <para xml:lang="zh-CN">调用行号回调刷新显示的行号；回调异常会继续传播。</para>
        /// </summary>
        public void RefreshIndexDisplay()
        {
            if (_indexNumberLabel != null && _rowIndexZeroBased != null)
                _indexNumberLabel.Text = FormatDragIndexLabel(_rowIndexZeroBased());
        }

        private static string FormatDragIndexLabel(int zeroBasedRowIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(zeroBasedRowIndex);
            return ((long)zeroBasedRowIndex + 1).ToString(CultureInfo.CurrentCulture);
        }

        /// <inheritdoc />
        public override void _EnterTree()
        {
            base._EnterTree();
            _hookedControllerManagerDrag = NControllerManager.Instance;
            if (_hookedControllerManagerDrag != null)
            {
                _hookedControllerManagerDrag.ControllerDetected += ApplyDragHandleMousePolicy;
                _hookedControllerManagerDrag.MouseDetected += ApplyDragHandleMousePolicy;
            }

            ApplyDragHandleMousePolicy();
        }

        /// <inheritdoc />
        public override void _ExitTree()
        {
            if (_hookedControllerManagerDrag != null)
            {
                _hookedControllerManagerDrag.ControllerDetected -= ApplyDragHandleMousePolicy;
                _hookedControllerManagerDrag.MouseDetected -= ApplyDragHandleMousePolicy;
                _hookedControllerManagerDrag = null;
            }

            base._ExitTree();
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            ApplyDragHandleMousePolicy();
            base._Ready();
        }

        private void ApplyDragHandleMousePolicy()
        {
            var blockMouse = Sts2InputCompat.IsUsingDirectionalNavigation;
            MouseFilter = blockMouse ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
            FocusMode = FocusModeEnum.All;
        }

        /// <inheritdoc />
        public override Variant _GetDragData(Vector2 atPosition)
        {
            if (Sts2InputCompat.IsUsingDirectionalNavigation)
                return default;

            if (_dragDataProvider == null)
                return default;

            var preview = new PanelContainer
            {
                CustomMinimumSize = new(48f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            preview.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListItemCardStyle(true));
            SetDragPreview(preview);
            return Variant.From(_dragDataProvider());
        }

        private static StyleBoxFlat CreateRailStyle(bool highlighted)
        {
            return RitsuShellStyleCache.GetOrBuild(
                highlighted ? "settings.dragHandle.highlighted" : "settings.dragHandle",
                () => BuildRailStyle(highlighted));
        }

        private static StyleBoxFlat CreateRailFocusStyle()
        {
            return RitsuShellStyleCache.GetOrBuild("settings.dragHandle.focus", BuildRailFocusStyle);
        }

        private static StyleBoxFlat BuildRailFocusStyle()
        {
            var style = (StyleBoxFlat)BuildRailStyle(true).Duplicate();
            style.BorderColor = RitsuShellTheme.Current.Text.HoverHighlight;
            style.BorderWidthLeft = Math.Max(style.BorderWidthLeft, 3);
            style.BorderWidthTop = Math.Max(style.BorderWidthTop, 3);
            style.BorderWidthRight = Math.Max(style.BorderWidthRight, 3);
            style.BorderWidthBottom = Math.Max(style.BorderWidthBottom, 3);
            style.ShadowColor = new(style.BorderColor.R, style.BorderColor.G, style.BorderColor.B, 0.45f);
            style.ShadowSize = RitsuShellThemeLayoutResolver.ResolveInt(
                "components.dragHandle.layout.focusShadowSize", 6);
            return style;
        }

        private static StyleBoxFlat BuildRailStyle(bool highlighted)
        {
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.dragHandle.layout.borderWidth", 0);
            border = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.borderWidth.left", 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.borderWidth.top", 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.borderWidth.right",
                    border.Right == 0 ? 1 : border.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.borderWidth.bottom", 0));
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.dragHandle.layout.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.dragHandle.layout.padding", 6);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.padding.bottom", 8));
            return new()
            {
                BgColor = highlighted
                    ? RitsuShellTheme.Current.Component.DragHandle.Selected.Bg
                    : RitsuShellTheme.Current.Component.DragHandle.Default.Bg,
                BorderColor = highlighted
                    ? RitsuShellTheme.Current.Component.DragHandle.Selected.Border
                    : RitsuShellTheme.Current.Component.DragHandle.Default.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }
    }
}
