using Godot;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    internal static partial class ModSettingsUiFactory
    {
        internal static void ShowModCloudSyncScopePicker(
            Node attachParent,
            string title,
            string body,
            string cancelText,
            string globalLabel,
            string profileLabel,
            string allLabel,
            Action<ModCloudSyncScope?> onChosen)
        {
            ArgumentNullException.ThrowIfNull(attachParent);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentNullException.ThrowIfNull(body);
            ArgumentException.ThrowIfNullOrWhiteSpace(cancelText);
            ArgumentException.ThrowIfNullOrWhiteSpace(globalLabel);
            ArgumentException.ThrowIfNullOrWhiteSpace(profileLabel);
            ArgumentException.ThrowIfNullOrWhiteSpace(allLabel);
            ArgumentNullException.ThrowIfNull(onChosen);

            var viewport = attachParent.GetViewport();
            if (viewport == null)
                return;

            var chosen = false;
            Action? viewportSizedHandler = null;

            CanvasLayer? canvasLayer = new()
            {
                Layer = ModalCanvasLayer,
                Name = "RitsuModSettingsModCloudScopeModal",
            };
            attachParent.AddChild(canvasLayer);

            var rootShield = new ModSettingsModalShield(() => Finish(null))
            {
                Name = "ModalShieldRoot",
            };
            canvasLayer.AddChild(rootShield);

            viewportSizedHandler = FitModalShieldToViewport;
            viewport.SizeChanged += viewportSizedHandler;
            Callable.From(FitModalShieldToViewport).CallDeferred();

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
                CustomMinimumSize = new(440f, 0f),
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
            };
            vbox.AddThemeConstantOverride("separation", 12);
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

            AddScopeButton(globalLabel, ModCloudSyncScope.GlobalOnly);
            AddScopeButton(profileLabel, ModCloudSyncScope.ProfileOnly);
            AddScopeButton(allLabel, ModCloudSyncScope.All);

            var btnRow = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                Alignment = BoxContainer.AlignmentMode.End,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            btnRow.AddThemeConstantOverride("separation", 12);
            vbox.AddChild(btnRow);

            var cancelBtn = new ModSettingsTextButton(cancelText, ModSettingsButtonTone.Normal, () => Finish(null))
            {
                CustomMinimumSize = new(132f, ModSettingsUiMetrics.EntryValueMinHeight),
            };
            btnRow.AddChild(cancelBtn);

            var escShortcut = new Shortcut();
            escShortcut.Events = [new InputEventKey { Keycode = Key.Escape, Pressed = true }];
            cancelBtn.Shortcut = escShortcut;
            cancelBtn.ShortcutInTooltip = false;

            Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(cancelBtn) && cancelBtn.IsVisibleInTree())
                    cancelBtn.GrabFocus();
            }).CallDeferred();
            return;

            void AddScopeButton(string label, ModCloudSyncScope scope)
            {
                var btn = new ModSettingsTextButton(label, ModSettingsButtonTone.Normal, () => Finish(scope))
                {
                    CustomMinimumSize = new(0f, ModSettingsUiMetrics.EntryValueMinHeight),
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                };
                vbox.AddChild(btn);
            }

            void CloseDialog()
            {
                if (GodotObject.IsInstanceValid(viewport) && viewportSizedHandler != null)
                    viewport.SizeChanged -= viewportSizedHandler;
                if (GodotObject.IsInstanceValid(canvasLayer))
                    canvasLayer.QueueFree();
            }

            void Finish(ModCloudSyncScope? scope)
            {
                if (chosen)
                    return;
                chosen = true;
                onChosen(scope);
                CloseDialog();
            }

            void FitModalShieldToViewport()
            {
                if (canvasLayer == null || canvasLayer.GetChildCount() == 0)
                    return;
                var shield = canvasLayer.GetChild(0) as Control;
                if (!GodotObject.IsInstanceValid(shield))
                    return;
                var sz = viewport.GetVisibleRect().Size;
                shield.Position = Vector2.Zero;
                shield.Size = sz;
            }
        }
    }
}
