using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal static partial class ModSettingsUiFactory
    {
        private const int ModalCanvasLayer = 120;

        /// <summary>
        ///     Full-viewport dim + centered panel, same chrome as mod settings. Blocks input under the layer.
        /// </summary>
        internal static void ShowStyledConfirm(
            Node attachParent,
            string title,
            string body,
            string cancelText,
            string confirmText,
            bool confirmIsDanger,
            Action onConfirm,
            bool showCancel = true)
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

            var canvasLayer = new CanvasLayer
            {
                Layer = ModalCanvasLayer,
                Name = "RitsuModSettingsStyledModal",
            };
            attachParent.AddChild(canvasLayer);

            ModSettingsModalShield rootShield = null!;

            rootShield = new(CloseDialog)
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
                    new(400f, 0f)),
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

            var confirmBtn = new ModSettingsTextButton(
                confirmText,
                confirmIsDanger ? ModSettingsButtonTone.Danger : ModSettingsButtonTone.Accent,
                () =>
                {
                    onConfirm();
                    CloseDialog();
                })
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.modal.layout.buttonRow.confirmMinSize",
                    new(168f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
            };

            ModSettingsTextButton? cancelBtn = null;
            if (showCancel)
            {
                cancelBtn = new(cancelText, ModSettingsButtonTone.Normal, CloseDialog)
                {
                    CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                        "components.modal.layout.buttonRow.cancelMinSize",
                        new(132f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
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
            if (cancelBtn != null)
            {
                cancelBtn.Shortcut = escShortcut;
                cancelBtn.ShortcutInTooltip = false;
            }
            else
            {
                confirmBtn.Shortcut = escShortcut;
                confirmBtn.ShortcutInTooltip = false;
            }

            Callable.From(() =>
            {
                if (!GodotObject.IsInstanceValid(rootPanel))
                    return;
                Callable.From(ApplyPanelSizePass2).CallDeferred();
            }).CallDeferred();

            return;

            void CloseDialog()
            {
                if (GodotObject.IsInstanceValid(viewport))
                    viewport.SizeChanged -= OnViewportSized;
                if (GodotObject.IsInstanceValid(canvasLayer))
                    canvasLayer.QueueFree();
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
                var minW = RitsuShellThemeLayoutResolver.ResolveFloat("components.modal.layout.panel.minWidth", 400f);
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
                var minW = RitsuShellThemeLayoutResolver.ResolveFloat("components.modal.layout.panel.minWidth", 400f);
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

        internal static void ShowStyledNotice(
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

                base._UnhandledInput(@event);
            }
        }
    }
}
