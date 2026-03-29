using Godot;

namespace STS2RitsuLib.Settings
{
    internal static partial class ModSettingsUiFactory
    {
        private const int ModalCanvasLayer = 120;
        private const float ModalDimAlpha = 0.62f;

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
            Action onConfirm)
        {
            ArgumentNullException.ThrowIfNull(attachParent);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentNullException.ThrowIfNull(body);
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

            var rootShield = new Control
            {
                Name = "ModalShieldRoot",
                MouseFilter = Control.MouseFilterEnum.Stop,
            };
            canvasLayer.AddChild(rootShield);

            void OnViewportSized()
            {
                if (!GodotObject.IsInstanceValid(rootShield))
                    return;
                var sz = viewport.GetVisibleRect().Size;
                rootShield.Position = Vector2.Zero;
                rootShield.Size = sz;
            }

            viewport.SizeChanged += OnViewportSized;
            Callable.From(OnViewportSized).CallDeferred();

            var dim = new ColorRect
            {
                Name = "ModalDim",
                Color = new(0f, 0f, 0f, ModalDimAlpha),
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
            margin.AddThemeConstantOverride("margin_left", 22);
            margin.AddThemeConstantOverride("margin_top", 20);
            margin.AddThemeConstantOverride("margin_right", 22);
            margin.AddThemeConstantOverride("margin_bottom", 20);
            rootPanel.AddChild(margin);

            var vbox = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                CustomMinimumSize = new(400f, 0f),
            };
            vbox.AddThemeConstantOverride("separation", 14);
            margin.AddChild(vbox);

            var titleLabel = CreateHeaderLabel(title, 22, HorizontalAlignment.Left, null,
                ModSettingsUiPalette.RichTextTitle);
            titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            vbox.AddChild(titleLabel);

            var bodyLabel = CreateHeaderLabel(
                string.IsNullOrWhiteSpace(body) ? "\u200b" : body.Trim(),
                17,
                HorizontalAlignment.Left,
                null,
                ModSettingsUiPalette.RichTextBody);
            bodyLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            bodyLabel.FitContent = true;
            vbox.AddChild(bodyLabel);

            var btnRow = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                Alignment = BoxContainer.AlignmentMode.End,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            btnRow.AddThemeConstantOverride("separation", 12);
            vbox.AddChild(btnRow);

            var cancelBtn = new ModSettingsTextButton(cancelText, ModSettingsButtonTone.Normal, CloseDialog)
            {
                CustomMinimumSize = new(132f, ModSettingsUiMetrics.EntryValueMinHeight),
            };
            var confirmBtn = new ModSettingsTextButton(
                confirmText,
                confirmIsDanger ? ModSettingsButtonTone.Danger : ModSettingsButtonTone.Accent,
                () =>
                {
                    onConfirm();
                    CloseDialog();
                })
            {
                CustomMinimumSize = new(168f, ModSettingsUiMetrics.EntryValueMinHeight),
            };

            btnRow.AddChild(cancelBtn);
            btnRow.AddChild(confirmBtn);

            var escShortcut = new Shortcut();
            escShortcut.Events = [new InputEventKey { Keycode = Key.Escape, Pressed = true }];
            cancelBtn.Shortcut = escShortcut;
            cancelBtn.ShortcutInTooltip = false;

            Callable.From(() =>
            {
                if (!GodotObject.IsInstanceValid(rootPanel))
                    return;
                Callable.From(ApplyPanelSizePass2).CallDeferred();
            }).CallDeferred();
            return;

            void ApplyPanelSizePass2()
            {
                if (!GodotObject.IsInstanceValid(rootPanel))
                    return;

                var min = rootPanel.GetCombinedMinimumSize();
                var w = Mathf.CeilToInt(Mathf.Max(min.X, 400f));
                var h = Mathf.CeilToInt(Mathf.Max(min.Y, 120f));
                rootPanel.CustomMinimumSize = new(w, h);
                Callable.From(ApplyPanelSizeFinal).CallDeferred();
            }

            void ApplyPanelSizeFinal()
            {
                if (!GodotObject.IsInstanceValid(rootPanel))
                    return;

                var min = rootPanel.GetCombinedMinimumSize();
                var w = Mathf.CeilToInt(Mathf.Max(min.X, 400f));
                var h = Mathf.CeilToInt(Mathf.Max(min.Y, 120f));
                rootPanel.CustomMinimumSize = new(w, h);
                viewport.GuiReleaseFocus();
            }

            void CloseDialog()
            {
                if (GodotObject.IsInstanceValid(viewport))
                    viewport.SizeChanged -= OnViewportSized;
                if (GodotObject.IsInstanceValid(canvasLayer))
                    canvasLayer.QueueFree();
            }
        }
    }
}
