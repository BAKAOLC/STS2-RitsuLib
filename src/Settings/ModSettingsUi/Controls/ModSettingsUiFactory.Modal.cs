using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">Creates the reusable layout, page chrome, and controls used by RitsuLib settings pages.</para>
    ///     <para xml:lang="zh-CN">创建 RitsuLib 设置页面使用的可复用布局、页面框架与控件。</para>
    /// </summary>
    public static partial class ModSettingsUiFactory
    {
        private const int ModalCanvasLayer = 120;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Shows a full-viewport, input-blocking confirmation dialog with themed chrome and restores the
        ///         previous focus when it closes.
        ///     </para>
        ///     <para xml:lang="zh-CN">显示一个覆盖整个视口、阻止下层输入的主题确认对话框，并在关闭时恢复此前的焦点。</para>
        /// </summary>
        /// <param name="attachParent">
        ///     <para xml:lang="en">The node that receives the modal canvas layer.</para>
        ///     <para xml:lang="zh-CN">用于挂载模态画布层的节点。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The nonempty dialog title.</para>
        ///     <para xml:lang="zh-CN">非空的对话框标题。</para>
        /// </param>
        /// <param name="body">
        ///     <para xml:lang="en">The dialog body; an empty string leaves a blank body row.</para>
        ///     <para xml:lang="zh-CN">对话框正文；空字符串会保留一个空白正文行。</para>
        /// </param>
        /// <param name="cancelText">
        ///     <para xml:lang="en">
        ///         The cancel-button label, required when <paramref name="showCancel" /> is
        ///         <see langword="true" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">取消按钮标签；<paramref name="showCancel" /> 为 <see langword="true" /> 时必须提供非空文本。</para>
        /// </param>
        /// <param name="confirmText">
        ///     <para xml:lang="en">The nonempty confirm-button label.</para>
        ///     <para xml:lang="zh-CN">非空的确认按钮标签。</para>
        /// </param>
        /// <param name="confirmIsDanger">
        ///     <para xml:lang="en">Whether the confirm button uses the danger tone instead of the accent tone.</para>
        ///     <para xml:lang="zh-CN">确认按钮是否使用危险色调而非强调色调。</para>
        /// </param>
        /// <param name="onConfirm">
        ///     <para xml:lang="en">The action invoked before the dialog closes after confirmation.</para>
        ///     <para xml:lang="zh-CN">确认后、对话框关闭前调用的操作。</para>
        /// </param>
        /// <param name="showCancel">
        ///     <para xml:lang="en">Whether to include a cancel button.</para>
        ///     <para xml:lang="zh-CN">是否显示取消按钮。</para>
        /// </param>
        /// <param name="onCancel">
        ///     <para xml:lang="en">The optional action invoked when the cancel button is used.</para>
        ///     <para xml:lang="zh-CN">使用取消按钮时调用的可选操作。</para>
        /// </param>
        /// <param name="onDismiss">
        ///     <para xml:lang="en">The optional action invoked when the modal shield dismisses the dialog.</para>
        ///     <para xml:lang="zh-CN">模态遮罩关闭对话框时调用的可选操作。</para>
        /// </param>
        /// <param name="escapeTriggersCancel">
        ///     <para xml:lang="en">Whether Escape activates cancel, or confirm when no cancel button is shown.</para>
        ///     <para xml:lang="zh-CN">Escape 键是否触发取消；未显示取消按钮时则触发确认。</para>
        /// </param>
        /// <param name="cancelIsDanger">
        ///     <para xml:lang="en">Whether the cancel button uses the danger tone.</para>
        ///     <para xml:lang="zh-CN">取消按钮是否使用危险色调。</para>
        /// </param>
        public static void ShowStyledConfirm(
            Node attachParent,
            string title,
            string body,
            string cancelText,
            string confirmText,
            bool confirmIsDanger,
            Action onConfirm,
            bool showCancel = true,
            Action? onCancel = null,
            Action? onDismiss = null,
            bool escapeTriggersCancel = true,
            bool cancelIsDanger = false)
        {
            ArgumentNullException.ThrowIfNull(attachParent);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentNullException.ThrowIfNull(body);
            if (showCancel)
                ArgumentException.ThrowIfNullOrWhiteSpace(cancelText);
            ArgumentException.ThrowIfNullOrWhiteSpace(confirmText);
            ArgumentNullException.ThrowIfNull(onConfirm);

            var viewport = attachParent.GetViewport();
            if (viewport == null)
                return;

            var previousFocus = viewport.GuiGetFocusOwner();
            var canvasLayer = new CanvasLayer
            {
                Layer = ModalCanvasLayer,
                Name = "RitsuModSettingsStyledModal",
            };
            attachParent.AddChild(canvasLayer);

            ModSettingsModalShield rootShield = null!;

            rootShield = new(() => CloseDialog(false, true))
            {
                Name = "ModalShieldRoot",
            };
            canvasLayer.AddChild(rootShield);

            viewport.SizeChanged += OnViewportSized;
            Callable.From(OnViewportSized).CallDeferred();

            var dim = new ColorRect
            {
                Name = "ModalDim",
                Color = RitsuShellTheme.Current.Color.ModalBackdrop,
                MouseFilter = Control.MouseFilterEnum.Stop,
            };
            dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            rootShield.AddChild(dim);

            var center = new CenterContainer
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            rootShield.AddChild(center);

            var rootPanel = new PanelContainer
            {
                MouseFilter = Control.MouseFilterEnum.Stop,
            };
            rootPanel.AddThemeStyleboxOverride("panel", CreateSurfaceStyle());
            center.AddChild(rootPanel);

            var margin = new MarginContainer
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            var panelMargins = RitsuShellThemeLayoutResolver.ResolveEdges("components.modal.layout.panel.margin", 22);
            panelMargins = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.modal.layout.panel.margin.left",
                    panelMargins.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.modal.layout.panel.margin.top", 20),
                RitsuShellThemeLayoutResolver.ResolveInt("components.modal.layout.panel.margin.right",
                    panelMargins.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.modal.layout.panel.margin.bottom", 20));
            margin.AddThemeConstantOverride("margin_left", panelMargins.Left);
            margin.AddThemeConstantOverride("margin_top", panelMargins.Top);
            margin.AddThemeConstantOverride("margin_right", panelMargins.Right);
            margin.AddThemeConstantOverride("margin_bottom", panelMargins.Bottom);
            rootPanel.AddChild(margin);

            var vbox = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.modal.layout.panel.contentMinSize",
                    new(560f, 0f)),
            };
            vbox.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.modal.layout.panel.separation", 14));
            margin.AddChild(vbox);

            var titleLabel = CreateHeaderLabel(title, 22, HorizontalAlignment.Left, null,
                RitsuShellTheme.Current.Text.RichTitle);
            titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            vbox.AddChild(titleLabel);

            var bodyLabel = CreateHeaderLabel(
                string.IsNullOrWhiteSpace(body) ? "\u200b" : body.Trim(),
                17,
                HorizontalAlignment.Left,
                null,
                RitsuShellTheme.Current.Text.RichBody);
            bodyLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            bodyLabel.FitContent = true;
            vbox.AddChild(bodyLabel);

            var btnRow = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                Alignment = BoxContainer.AlignmentMode.End,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            btnRow.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.modal.layout.buttonRow.separation", 12));
            vbox.AddChild(btnRow);

            var actionButtonMinSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.modal.layout.buttonRow.actionMinSize",
                new(184f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));

            var confirmBtn = new ModSettingsTextButton(
                confirmText,
                confirmIsDanger ? ModSettingsButtonTone.Danger : ModSettingsButtonTone.Accent,
                () =>
                {
                    try
                    {
                        onConfirm();
                    }
                    finally
                    {
                        CloseDialog(false, false);
                    }
                })
            {
                CustomMinimumSize = actionButtonMinSize,
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
            };

            ModSettingsTextButton? cancelBtn = null;
            if (showCancel)
            {
                cancelBtn = new(
                    cancelText,
                    cancelIsDanger ? ModSettingsButtonTone.Danger : ModSettingsButtonTone.Normal,
                    () => CloseDialog(true, false))
                {
                    CustomMinimumSize = actionButtonMinSize,
                    SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
                };
                btnRow.AddChild(cancelBtn);
            }

            btnRow.AddChild(confirmBtn);

            var confirmPath = confirmBtn.GetPath();
            if (cancelBtn != null)
            {
                var cancelPath = cancelBtn.GetPath();
                cancelBtn.FocusNeighborLeft = cancelPath;
                cancelBtn.FocusNeighborTop = cancelPath;
                cancelBtn.FocusNeighborBottom = cancelPath;
                cancelBtn.FocusNeighborRight = confirmPath;
                confirmBtn.FocusNeighborLeft = cancelPath;
            }
            else
            {
                confirmBtn.FocusNeighborLeft = confirmPath;
            }

            confirmBtn.FocusNeighborRight = confirmPath;
            confirmBtn.FocusNeighborTop = confirmPath;
            confirmBtn.FocusNeighborBottom = confirmPath;

            var escShortcut = new Shortcut();
            escShortcut.Events = [new InputEventKey { Keycode = Key.Escape, Pressed = true }];
            switch (escapeTriggersCancel)
            {
                case true when cancelBtn != null:
                    cancelBtn.Shortcut = escShortcut;
                    cancelBtn.ShortcutInTooltip = false;
                    break;
                case true:
                    confirmBtn.Shortcut = escShortcut;
                    confirmBtn.ShortcutInTooltip = false;
                    break;
            }

            Callable.From(() =>
            {
                if (!GodotObject.IsInstanceValid(rootPanel))
                    return;
                Callable.From(ApplyPanelSizePass2).CallDeferred();
            }).CallDeferred();

            return;

            void CloseDialog(bool cancelled, bool dismissed)
            {
                if (GodotObject.IsInstanceValid(viewport))
                    viewport.SizeChanged -= OnViewportSized;
                if (GodotObject.IsInstanceValid(canvasLayer))
                    canvasLayer.QueueFree();
                try
                {
                    if (cancelled)
                        onCancel?.Invoke();
                    else if (dismissed)
                        onDismiss?.Invoke();
                }
                finally
                {
                    RestorePreviousFocus();
                }
            }

            void RestorePreviousFocus()
            {
                var target = previousFocus;
                if (target == null || !GodotObject.IsInstanceValid(target) || !target.IsVisibleInTree())
                    return;

                Callable.From(() =>
                {
                    if (GodotObject.IsInstanceValid(target) && target.IsVisibleInTree())
                        target.GrabFocus();
                }).CallDeferred();
            }

            void OnViewportSized()
            {
                // ReSharper disable AccessToModifiedClosure
                if (!GodotObject.IsInstanceValid(rootShield))
                    return;
                var sz = viewport.GetVisibleRect().Size;
                rootShield.Position = Vector2.Zero;
                rootShield.Size = sz;
                // ReSharper restore AccessToModifiedClosure
            }

            void ApplyPanelSizePass2()
            {
                if (!GodotObject.IsInstanceValid(rootPanel))
                    return;

                var min = rootPanel.GetCombinedMinimumSize();
                var minW = RitsuShellThemeLayoutResolver.ResolveFloat("components.modal.layout.panel.minWidth", 560f);
                var minH = RitsuShellThemeLayoutResolver.ResolveFloat("components.modal.layout.panel.minHeight", 120f);
                var w = Mathf.CeilToInt(Mathf.Max(min.X, minW));
                var h = Mathf.CeilToInt(Mathf.Max(min.Y, minH));
                rootPanel.CustomMinimumSize = new(w, h);
                Callable.From(ApplyPanelSizeFinal).CallDeferred();
            }

            void ApplyPanelSizeFinal()
            {
                if (!GodotObject.IsInstanceValid(rootPanel))
                    return;

                var min = rootPanel.GetCombinedMinimumSize();
                var minW = RitsuShellThemeLayoutResolver.ResolveFloat("components.modal.layout.panel.minWidth", 560f);
                var minH = RitsuShellThemeLayoutResolver.ResolveFloat("components.modal.layout.panel.minHeight", 120f);
                var w = Mathf.CeilToInt(Mathf.Max(min.X, minW));
                var h = Mathf.CeilToInt(Mathf.Max(min.Y, minH));
                rootPanel.CustomMinimumSize = new(w, h);
                Callable.From(() =>
                {
                    if (cancelBtn != null && GodotObject.IsInstanceValid(cancelBtn) && cancelBtn.IsVisibleInTree())
                    {
                        cancelBtn.GrabFocus();
                        return;
                    }

                    if (GodotObject.IsInstanceValid(confirmBtn) && confirmBtn.IsVisibleInTree())
                        confirmBtn.GrabFocus();
                }).CallDeferred();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Shows a themed notice dialog with one dismiss button.</para>
        ///     <para xml:lang="zh-CN">显示只有一个关闭按钮的主题提示对话框。</para>
        /// </summary>
        /// <param name="attachParent">
        ///     <para xml:lang="en">The node that receives the modal canvas layer.</para>
        ///     <para xml:lang="zh-CN">用于挂载模态画布层的节点。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The nonempty dialog title.</para>
        ///     <para xml:lang="zh-CN">非空的对话框标题。</para>
        /// </param>
        /// <param name="body">
        ///     <para xml:lang="en">The dialog body.</para>
        ///     <para xml:lang="zh-CN">对话框正文。</para>
        /// </param>
        /// <param name="dismissText">
        ///     <para xml:lang="en">The nonempty dismiss-button label.</para>
        ///     <para xml:lang="zh-CN">非空的关闭按钮标签。</para>
        /// </param>
        public static void ShowStyledNotice(
            Node attachParent,
            string title,
            string body,
            string dismissText)
        {
            ShowStyledConfirm(
                attachParent,
                title,
                body,
                dismissText,
                dismissText,
                false,
                static () => { },
                false);
        }

        private sealed partial class ModSettingsModalShield : Control
        {
            private readonly Action? _onDismiss;

            public ModSettingsModalShield(Action onDismiss)
            {
                _onDismiss = onDismiss;
                MouseFilter = MouseFilterEnum.Stop;
            }

            public ModSettingsModalShield()
            {
            }

            public override void _Ready()
            {
                SetProcessUnhandledInput(true);
            }

            public override void _UnhandledInput(InputEvent @event)
            {
                if (!@event.IsEcho() &&
                    (@event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack)))
                {
                    _onDismiss?.Invoke();
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
                if (@event.IsEcho())
                    return false;

                return @event.IsActionPressed("ui_up") ||
                       @event.IsActionPressed("ui_down") ||
                       @event.IsActionPressed("ui_left") ||
                       @event.IsActionPressed("ui_right") ||
                       @event.IsActionPressed("ui_accept") ||
                       @event.IsActionPressed("ui_cancel") ||
                       @event.IsActionPressed(MegaInput.left) ||
                       @event.IsActionPressed(MegaInput.right) ||
                       @event.IsActionPressed(MegaInput.select) ||
                       @event.IsActionPressed(Sts2InputCompat.ConfirmAction) ||
                       @event.IsActionPressed(MegaInput.cancel) ||
                       @event.IsActionPressed(MegaInput.pauseAndBack);
            }
        }
    }
}
