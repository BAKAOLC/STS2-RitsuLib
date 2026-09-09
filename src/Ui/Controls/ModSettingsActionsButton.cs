using System.Diagnostics.CodeAnalysis;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         A reusable action-menu button. One popup is shared across buttons; opening another button
    ///         closes the previous popup. Use on the Godot main thread.
    ///     </para>
    ///     <para xml:lang="zh-CN">可复用的操作菜单按钮。按钮共用一个弹出层；打开另一个按钮会关闭此前的菜单。请在 Godot 主线程使用。</para>
    /// </summary>
    public sealed partial class ModSettingsActionsButton : ModSettingsGamepadCompatibleButton,
        IModSettingsTransientPopupOwner, IModSettingsDirectionalInputClaimant
    {
        private const float DropMinWidth = 260f;
        private const float RowHeight = 38f;
        private const int MaximumActions = 1024;

        private static readonly SharedActionsDropdown SharedDropdown = new();
        private readonly IReadOnlyList<ModSettingsMenuAction> _actions;
        private readonly Func<IReadOnlyList<ModSettingsMenuAction>>? _actionsProvider;

        private readonly Action? _afterAction;
        private bool _dropOpen;
        private bool _hovered;
        private IReadOnlyList<ModSettingsMenuAction>? _openActions;
        private Vector2I? _preferredPopupPosition;

        /// <summary>
        ///     <para xml:lang="en">Creates and configures the control on the Godot main thread.</para>
        ///     <para xml:lang="zh-CN">在 Godot 主线程创建并配置此控件。</para>
        /// </summary>
        /// <param name="actions">
        ///     <para xml:lang="en">Snapshot of up to 1024 menu actions. Malformed entries are omitted when opening.</para>
        ///     <para xml:lang="zh-CN">最多 1024 个菜单操作的快照。打开时忽略无效条目。</para>
        /// </param>
        /// <param name="afterAction">
        ///     <para xml:lang="en">
        ///         Optional callback after a selected action completes successfully, before the menu closes.
        ///         Exceptions propagate.
        ///     </para>
        ///     <para xml:lang="zh-CN">所选操作成功完成后、菜单关闭前执行的可选回调。异常会继续传播。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">A required reference argument is null.</para>
        ///     <para xml:lang="zh-CN">必需的引用参数为 null。</para>
        /// </exception>
        public ModSettingsActionsButton(IReadOnlyList<ModSettingsMenuAction> actions, Action? afterAction = null)
        {
            ArgumentNullException.ThrowIfNull(actions);
            if (actions.Count > MaximumActions)
                throw new ArgumentOutOfRangeException(nameof(actions), "At most 1024 menu actions are supported.");
            _actions = [.. actions.Take(MaximumActions + 1)];
            if (_actions.Count > MaximumActions)
                throw new ArgumentOutOfRangeException(nameof(actions), "At most 1024 menu actions are supported.");
            _afterAction = afterAction;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            Text = string.Empty;
            TooltipText = RitsuModuleLocalization.Get("button.actionsShort", "Actions");
            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.chromeMenu.layout.trigger.minSize",
                new(36f, 32f));
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            AddThemeStyleboxOverride("normal", RitsuShellChromeStyles.CreateChromeActionsMenuStyle(false));
            AddThemeStyleboxOverride("hover", RitsuShellChromeStyles.CreateChromeActionsMenuStyle(true));
            AddThemeStyleboxOverride("pressed", RitsuShellChromeStyles.CreateChromeActionsMenuStyle(true));
            AddThemeStyleboxOverride("focus", CreateActionsTriggerFocusStyle());
            Pressed += OnEllipsisPressed;
        }

        /// <summary>
        ///     <para xml:lang="en">Creates and configures the control on the Godot main thread.</para>
        ///     <para xml:lang="zh-CN">在 Godot 主线程创建并配置此控件。</para>
        /// </summary>
        /// <param name="actionsProvider">
        ///     <para xml:lang="en">
        ///         Provides up to 1024 actions whenever the menu opens. Recoverable provider failures are logged
        ///         and produce an empty menu.
        ///     </para>
        ///     <para xml:lang="zh-CN">每次打开菜单时提供最多 1024 个操作。可恢复的提供器失败会被记录，并产生空菜单。</para>
        /// </param>
        /// <param name="afterAction">
        ///     <para xml:lang="en">
        ///         Optional callback after a selected action completes successfully, before the menu closes.
        ///         Exceptions propagate.
        ///     </para>
        ///     <para xml:lang="zh-CN">所选操作成功完成后、菜单关闭前执行的可选回调。异常会继续传播。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">A required reference argument is null.</para>
        ///     <para xml:lang="zh-CN">必需的引用参数为 null。</para>
        /// </exception>
        public ModSettingsActionsButton(Func<IReadOnlyList<ModSettingsMenuAction>> actionsProvider,
            Action? afterAction = null)
            : this([], afterAction)
        {
            _actionsProvider = actionsProvider ?? throw new ArgumentNullException(nameof(actionsProvider));
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes the control for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化此控件。</para>
        /// </summary>
        public ModSettingsActionsButton()
        {
            _actions = [];
            Pressed += OnEllipsisPressed;
        }

        bool IModSettingsDirectionalInputClaimant.ClaimsDirectionalInput => _dropOpen;

        void IModSettingsTransientPopupOwner.ForceCloseTransientUi()
        {
            CloseDropdown();
        }

        /// <inheritdoc />
        public override void _Notification(int what)
        {
            base._Notification(what);
            switch (what)
            {
                case (int)NotificationMouseEnter:
                    _hovered = true;
                    QueueRedraw();
                    break;
                case (int)NotificationMouseExit:
                    _hovered = false;
                    QueueRedraw();
                    break;
                case (int)NotificationFocusEnter:
                case (int)NotificationFocusExit:
                case (int)NotificationThemeChanged:
                    QueueRedraw();
                    break;
            }
        }

        /// <inheritdoc />
        public override void _Draw()
        {
            base._Draw();
            var color = Disabled
                ? RitsuShellTheme.Current.Text.LabelSecondary
                : _dropOpen || _hovered || HasFocus()
                    ? RitsuShellTheme.Current.Text.HoverHighlight
                    : RitsuShellTheme.Current.Text.RichSecondary;
            var radius = RitsuShellThemeLayoutResolver.ResolveFloat("components.chromeMenu.layout.trigger.dotRadius",
                2.2f);
            var gap = RitsuShellThemeLayoutResolver.ResolveFloat("components.chromeMenu.layout.trigger.dotGap",
                5.5f);
            var center = Size * 0.5f;
            DrawCircle(new(center.X, center.Y - gap), radius, color);
            DrawCircle(center, radius, color);
            DrawCircle(new(center.X, center.Y + gap), radius, color);
        }

        /// <inheritdoc />
        public override void _ExitTree()
        {
            if (_dropOpen)
                CloseDropdown();
            base._ExitTree();
        }

        /// <inheritdoc />
        public override void _Input(InputEvent @event)
        {
            if (_dropOpen && !@event.IsEcho() &&
                (@event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack)))
            {
                CloseDropdown();
                GetViewport()?.SetInputAsHandled();
                return;
            }

            base._Input(@event);
        }

        /// <inheritdoc />
        public override void _UnhandledInput(InputEvent @event)
        {
            if (_dropOpen && !@event.IsEcho() &&
                (@event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack)))
            {
                CloseDropdown();
                GetViewport()?.SetInputAsHandled();
                return;
            }

            base._UnhandledInput(@event);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Opens the menu at a finite global position, or moves an existing popup. Disabled controls do
        ///         nothing; the button must be inside the scene tree.
        ///     </para>
        ///     <para xml:lang="zh-CN">在有限的全局坐标处打开菜单，或移动已打开的菜单。已禁用的控件不执行操作；按钮须位于场景树中。</para>
        /// </summary>
        /// <param name="globalPosition">
        ///     <para xml:lang="en">Finite global popup position, within the 32-bit integer coordinate range.</para>
        ///     <para xml:lang="zh-CN">有限的全局弹出位置，须位于 32 位整数坐标范围内。</para>
        /// </param>
        public void OpenAt(Vector2 globalPosition)
        {
            if (!float.IsFinite(globalPosition.X) || !float.IsFinite(globalPosition.Y) ||
                (double)globalPosition.X is < int.MinValue or > int.MaxValue ||
                (double)globalPosition.Y is < int.MinValue or > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(globalPosition));
            if (Disabled || ProcessMode == ProcessModeEnum.Disabled)
                return;
            if (!IsInsideTree())
                throw new InvalidOperationException(
                    "The action button must be inside the scene tree before opening its menu.");
            _preferredPopupPosition = new Vector2I(
                Mathf.RoundToInt(globalPosition.X),
                Mathf.RoundToInt(globalPosition.Y));
            if (_dropOpen)
            {
                SharedDropdown.LayoutInViewport(this);
                return;
            }

            OpenDropdown();
        }

        private void OnEllipsisPressed()
        {
            if (Disabled)
                return;

            if (_dropOpen)
                CloseDropdown();
            else
                OpenDropdown();
        }

        private void OpenDropdown()
        {
            _openActions = GetActions();
            if (_openActions.Count == 0)
            {
                _openActions = null;
                return;
            }

            try
            {
                SharedDropdown.Open(this);
            }
            finally
            {
                if (!_dropOpen)
                    _openActions = null;
            }
        }

        private void CloseDropdown()
        {
            if (!_dropOpen)
                return;

            SharedDropdown.Close(this, true);
        }

        private void OnSharedDropdownOpened()
        {
            _dropOpen = true;
            QueueRedraw();
            SetProcessInput(true);
            SetProcessUnhandledInput(true);
        }

        private void OnSharedDropdownClosed(bool restoreFocus)
        {
            _dropOpen = false;
            QueueRedraw();
            SetProcessInput(false);
            SetProcessUnhandledInput(false);
            _preferredPopupPosition = null;
            _openActions = null;

            if (restoreFocus && IsInstanceValid(this) && IsVisibleInTree())
                GrabFocus();
        }

        private IReadOnlyList<ModSettingsMenuAction> GetActions()
        {
            if (_actionsProvider == null)
                return FilterActions(_actions);

            try
            {
                return FilterActions(_actionsProvider());
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[ModSettingsActionsButton] actionsProvider failed: {ex}");
                return FilterActions(_actions);
            }
        }

        private static IReadOnlyList<ModSettingsMenuAction> FilterActions(
            IReadOnlyList<ModSettingsMenuAction>? actions)
        {
            if (actions?.Count > MaximumActions)
                throw new InvalidOperationException("At most 1024 menu actions are supported.");
            // Mod-provided action lists may violate their declared nullable contracts at runtime.
            // ReSharper disable RedundantAlwaysMatchSubpattern
            var filtered = actions?
                               .Take(MaximumActions + 1)
                               .Where(static action => action is
                               {
                                   Label: not null,
                                   IsEnabled: not null,
                                   Action: not null,
                               })
                               .ToArray()
                           ?? [];
            // ReSharper restore RedundantAlwaysMatchSubpattern
            if (filtered.Length > MaximumActions)
                throw new InvalidOperationException("At most 1024 menu actions are supported.");
            return filtered;
        }

        private static StyleBoxFlat CreateActionsRowStyle(bool highlighted)
        {
            return RitsuShellStyleCache.GetOrBuild(
                highlighted ? "settings.actionsRow.highlighted" : "settings.actionsRow",
                () => BuildActionsRowStyle(highlighted));
        }

        private static StyleBoxFlat CreateActionsTriggerFocusStyle()
        {
            return RitsuShellStyleCache.GetOrBuild("settings.actionsTrigger.focus",
                BuildActionsTriggerFocusStyle);
        }

        private static StyleBoxFlat BuildActionsTriggerFocusStyle()
        {
            var style = (StyleBoxFlat)RitsuShellChromeStyles.CreateChromeActionsMenuStyle(true).Duplicate();
            style.BorderColor = RitsuShellTheme.Current.Text.HoverHighlight;
            style.BorderWidthLeft = Math.Max(style.BorderWidthLeft, 3);
            style.BorderWidthTop = Math.Max(style.BorderWidthTop, 3);
            style.BorderWidthRight = Math.Max(style.BorderWidthRight, 3);
            style.BorderWidthBottom = Math.Max(style.BorderWidthBottom, 3);
            style.ShadowColor = new(style.BorderColor.R, style.BorderColor.G, style.BorderColor.B, 0.45f);
            style.ShadowSize = RitsuShellThemeLayoutResolver.ResolveInt(
                "components.chromeMenu.layout.trigger.focusShadowSize", 6);
            return style;
        }

        private static StyleBoxFlat BuildActionsRowStyle(bool highlighted)
        {
            var border =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.actionsRow.borderWidth", 1);
            var padding =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.actionsRow.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.left",
                    padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.top", 5),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.bottom", 5));
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.dropdown.layout.actionsRow.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            return new()
            {
                BgColor = highlighted
                    ? RitsuShellTheme.Current.Component.Dropdown.Hover.Bg
                    : RitsuShellTheme.Current.Component.Dropdown.Open.Bg,
                BorderColor = highlighted
                    ? RitsuShellTheme.Current.Component.Dropdown.Hover.Border
                    : RitsuShellTheme.Current.Component.Dropdown.Open.Border,
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

        private static StyleBoxFlat CreateActionsRowPressedStyle()
        {
            return RitsuShellStyleCache.GetOrBuild("settings.actionsRow.pressed", BuildActionsRowPressedStyle);
        }

        private static StyleBoxFlat BuildActionsRowPressedStyle()
        {
            var border =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.actionsRow.borderWidth", 1);
            var padding =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.actionsRow.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.left",
                    padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.top", 5),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.bottom", 5));
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.dropdown.layout.actionsRow.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            return new()
            {
                BgColor = RitsuShellTheme.Current.Component.Dropdown.Pressed.Bg,
                BorderColor = RitsuShellTheme.Current.Component.Dropdown.Pressed.Border,
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

        private static StyleBoxFlat CreateActionsRowFocusStyle()
        {
            return RitsuShellStyleCache.GetOrBuild("settings.actionsRow.focus", BuildActionsRowFocusStyle);
        }

        private static StyleBoxFlat BuildActionsRowFocusStyle()
        {
            var border =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.actionsRow.borderWidth", 1);
            var padding =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.actionsRow.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.left",
                    padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.top", 5),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.bottom", 5));
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.dropdown.layout.actionsRow.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            return new()
            {
                BgColor = RitsuShellTheme.Current.Component.Dropdown.Focus.Bg,
                BorderColor = RitsuShellTheme.Current.Component.Dropdown.Focus.Border,
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

        private static StyleBoxFlat CreateActionsRowDisabledStyle()
        {
            return RitsuShellStyleCache.GetOrBuild("settings.actionsRow.disabled", BuildActionsRowDisabledStyle);
        }

        private static StyleBoxFlat BuildActionsRowDisabledStyle()
        {
            var border =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.actionsRow.borderWidth", 1);
            var padding =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.actionsRow.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.left",
                    padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.top", 5),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.bottom", 5));
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.dropdown.layout.actionsRow.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            return new()
            {
                BgColor = RitsuShellTheme.Current.Component.Dropdown.Open.Bg,
                BorderColor = RitsuShellTheme.Current.Component.Dropdown.Open.Border,
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

        /// <summary>
        ///     <para xml:lang="en">Closes this button's popup if open. Repeated calls are harmless.</para>
        ///     <para xml:lang="zh-CN">如果此按钮的菜单已打开则关闭；重复调用无副作用。</para>
        /// </summary>
        public void ForceCloseDropdown()
        {
            CloseDropdown();
        }

        private void ActivateRow(int index)
        {
            var actions = _openActions ?? GetActions();
            if (index < 0 || index >= actions.Count)
                return;

            var def = actions[index];
            if (!IsActionEnabled(def))
                return;

            try
            {
                def.Action();
                _afterAction?.Invoke();
            }
            finally
            {
                CloseDropdown();
            }
        }

        private static bool IsActionEnabled(ModSettingsMenuAction action)
        {
            try
            {
                return action.IsEnabled();
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[ModSettingsActionsButton] IsEnabled failed: {ex}");
                return false;
            }
        }

        [SuppressMessage(
            "Design",
            "CA1001:Types that own disposable fields should be disposable",
            Justification =
                "Godot nodes are owned and released by the scene tree rather than this shared coordinator.")]
        private sealed class SharedActionsDropdown
        {
            private readonly Dictionary<int, ModSettingsMiniButton> _rowButtonCache = [];
            private readonly List<ModSettingsMiniButton> _rowButtons = [];
            private Control? _backdrop;
            private VBoxContainer? _dropList;
            private PanelContainer? _dropPanel;
            private ModSettingsActionsButton? _owner;

            internal void Open(ModSettingsActionsButton owner)
            {
                if (!IsInstanceValid(owner))
                    return;

                if (_owner != null && !IsInstanceValid(_owner))
                    _owner = null;
                else if (_owner != null && !ReferenceEquals(_owner, owner))
                    Close(_owner, false);

                EnsureShell(owner);
                if (_dropPanel == null || _dropList == null || _backdrop == null)
                    return;

                _dropPanel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListShellStyle());
                RebuildRows(owner);
                if (_rowButtons.Count == 0)
                    return;

                _owner = owner;
                owner.OnSharedDropdownOpened();
                LayoutInViewport(owner);
                _backdrop.Visible = true;
                _dropPanel.Visible = true;
                WireRowFocusNeighbors();
                Callable.From(GrabFirstEnabledRow).CallDeferred();
            }

            internal void Close(ModSettingsActionsButton owner, bool restoreFocus)
            {
                if (_owner == null || !ReferenceEquals(_owner, owner))
                    return;

                _owner = null;
                if (_backdrop != null && IsInstanceValid(_backdrop))
                    _backdrop.Visible = false;
                if (_dropPanel != null && IsInstanceValid(_dropPanel))
                    _dropPanel.Visible = false;

                if (IsInstanceValid(owner))
                    owner.OnSharedDropdownClosed(restoreFocus);
            }

            internal void LayoutInViewport(ModSettingsActionsButton owner)
            {
                if (_backdrop == null || _dropPanel == null || !IsInstanceValid(owner))
                    return;

                var vr = owner.GetViewport().GetVisibleRect();
                _backdrop.GlobalPosition = vr.Position;
                _backdrop.Size = vr.Size;

                _dropPanel.Size = Vector2.Zero;
                _dropPanel.ResetSize();
                var panelSize = _dropPanel.GetCombinedMinimumSize();
                Vector2 desiredTopLeft;
                if (owner._preferredPopupPosition.HasValue)
                {
                    desiredTopLeft = new(
                        owner._preferredPopupPosition.Value.X,
                        owner._preferredPopupPosition.Value.Y);
                }
                else
                {
                    var gr = owner.GetGlobalRect();
                    desiredTopLeft = new(gr.End.X - panelSize.X, gr.End.Y);
                }

                var maxX = Mathf.Max(vr.Position.X, vr.End.X - panelSize.X);
                var maxY = Mathf.Max(vr.Position.Y, vr.End.Y - panelSize.Y);
                desiredTopLeft = new(
                    Mathf.Clamp(desiredTopLeft.X, vr.Position.X, maxX),
                    Mathf.Clamp(desiredTopLeft.Y, vr.Position.Y, maxY));
                _dropPanel.GlobalPosition = desiredTopLeft;
            }

            private void EnsureShell(ModSettingsActionsButton owner)
            {
                if (_backdrop == null || !IsInstanceValid(_backdrop) ||
                    _dropPanel == null || !IsInstanceValid(_dropPanel) ||
                    _dropList == null || !IsInstanceValid(_dropList))
                {
                    BuildShell(owner);
                    return;
                }

                EnsureParent(_backdrop, owner);
                EnsureParent(_dropPanel, owner);
            }

            private void BuildShell(ModSettingsActionsButton owner)
            {
                _backdrop = new()
                {
                    Name = "SharedActionsMenuBackdrop",
                    Visible = false,
                    MouseFilter = MouseFilterEnum.Stop,
                    TopLevel = true,
                    ZIndex = 900,
                };
                _backdrop.SetAnchorsPreset(LayoutPreset.TopLeft);
                _backdrop.GuiInput += OnBackdropGuiInput;
                owner.AddChild(_backdrop);

                _dropPanel = new()
                {
                    Name = "SharedActionsMenuPanel",
                    Visible = false,
                    MouseFilter = MouseFilterEnum.Stop,
                    ClipContents = true,
                    TopLevel = true,
                    ZIndex = 901,
                    CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                        "components.dropdown.layout.actionsMenu.minSize",
                        new(DropMinWidth, 0f)),
                };
                _dropPanel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListShellStyle());
                owner.AddChild(_dropPanel);

                _dropList = new()
                {
                    Name = "SharedActionsMenuList",
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                    ClipContents = true,
                };
                _dropList.AddThemeConstantOverride("separation",
                    RitsuShellThemeLayoutResolver.ResolveInt(
                        "components.dropdown.layout.actionsMenu.listSeparation",
                        8));
                _dropPanel.AddChild(_dropList);
                _rowButtonCache.Clear();
                _rowButtons.Clear();
            }

            private static void EnsureParent(Node node, Node parent)
            {
                if (node.GetParent() == parent)
                    return;

                node.GetParent()?.RemoveChild(node);
                parent.AddChild(node);
            }

            private void OnBackdropGuiInput(InputEvent @event)
            {
                if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } &&
                    _owner != null)
                    Close(_owner, true);
            }

            private void RebuildRows(ModSettingsActionsButton owner)
            {
                if (_dropList == null)
                    return;

                var actions = owner._openActions ?? owner.GetActions();
                _rowButtons.Clear();
                var liveIndexes = Enumerable.Range(0, actions.Count).ToHashSet();
                foreach (var staleIndex in _rowButtonCache.Keys.Where(index => !liveIndexes.Contains(index)).ToArray())
                {
                    if (_rowButtonCache.TryGetValue(staleIndex, out var staleRow) &&
                        IsInstanceValid(staleRow))
                    {
                        if (staleRow.GetParent() == _dropList)
                            _dropList.RemoveChild(staleRow);
                        staleRow.QueueFree();
                    }

                    _rowButtonCache.Remove(staleIndex);
                }

                for (var i = 0; i < actions.Count; i++)
                {
                    var index = i;
                    var def = actions[i];
                    if (!_rowButtonCache.TryGetValue(index, out var row) || !IsInstanceValid(row))
                    {
                        row = new(def.Label, () => _owner?.ActivateRow(index))
                        {
                            SizeFlagsHorizontal = SizeFlags.ExpandFill,
                            Alignment = HorizontalAlignment.Left,
                        };
                        row.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
                        row.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.PopupRow);
                        _rowButtonCache[index] = row;
                    }

                    row.Text = def.Label;
                    var rowPadX = RitsuShellThemeLayoutResolver.ResolveFloat(
                        "components.dropdown.layout.actionsRow.hInset", 24f);
                    row.CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                        "components.dropdown.layout.actionsRow.minSize",
                        new(DropMinWidth - rowPadX, RowHeight));
                    row.Disabled = !IsActionEnabled(def);
                    row.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.DropdownRow);
                    row.AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
                    row.AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
                    row.AddThemeColorOverride("font_disabled_color", RitsuShellTheme.Current.Text.LabelSecondary);
                    row.AddThemeStyleboxOverride("normal", CreateActionsRowStyle(false));
                    row.AddThemeStyleboxOverride("hover", CreateActionsRowStyle(true));
                    row.AddThemeStyleboxOverride("pressed", CreateActionsRowPressedStyle());
                    row.AddThemeStyleboxOverride("focus", CreateActionsRowFocusStyle());
                    row.AddThemeStyleboxOverride("disabled", CreateActionsRowDisabledStyle());
                    if (row.GetParent() != _dropList)
                        _dropList.AddChild(row);
                    _dropList.MoveChild(row, i);
                    _rowButtons.Add(row);
                }

                _dropList.ResetSize();
                _dropPanel?.ResetSize();
            }

            private void WireRowFocusNeighbors()
            {
                for (var i = 0; i < _rowButtons.Count; i++)
                {
                    var row = _rowButtons[i];
                    var selfPath = row.GetPath();
                    row.FocusNeighborLeft = selfPath;
                    row.FocusNeighborRight = selfPath;
                    row.FocusNeighborTop = i > 0 ? _rowButtons[i - 1].GetPath() : null;
                    row.FocusNeighborBottom = i < _rowButtons.Count - 1 ? _rowButtons[i + 1].GetPath() : null;
                }
            }

            private void GrabFirstEnabledRow()
            {
                foreach (var row in _rowButtons.Where(row => !row.Disabled && row.IsVisibleInTree()))
                {
                    row.GrabFocus();
                    return;
                }
            }
        }
    }
}
