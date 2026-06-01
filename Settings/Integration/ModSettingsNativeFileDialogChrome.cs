using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal static partial class ModSettingsNativeFileDialogChrome
    {
        private const int FileDialogLayer = 132;

        internal static void Popup(FileDialog dialog)
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree?.Root == null)
            {
                dialog.QueueFree();
                return;
            }

            var viewport = tree.Root.GetViewport();
            var previousFocus = viewport?.GuiGetFocusOwner();
            var previousMouseMode = Input.MouseMode;

            var layer = new CanvasLayer
            {
                Name = "RitsuModSettingsNativeFileDialogModal",
                Layer = FileDialogLayer,
            };
            tree.Root.AddChild(layer);

            var shield = new Control
            {
                Name = "FileDialogShieldRoot",
                MouseFilter = Control.MouseFilterEnum.Stop,
            };
            layer.AddChild(shield);

            var dim = new ColorRect
            {
                Name = "FileDialogDim",
                Color = RitsuShellTheme.Current.Color.ModalBackdrop,
                MouseFilter = Control.MouseFilterEnum.Stop,
            };
            dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            shield.AddChild(dim);

            layer.AddChild(dialog);
            ConfigureDialog(dialog);
            Callable.From(FitShieldToViewport).CallDeferred();
            viewport!.SizeChanged += FitShieldToViewport;

            dialog.Canceled += CloseChrome;
            dialog.CloseRequested += CloseChrome;
            dialog.TreeExiting += RestoreMouseAndFocus;

            Input.MouseMode = Input.MouseModeEnum.Visible;
            dialog.PopupCenteredRatio(0.68f);
            return;

            void FitShieldToViewport()
            {
                if (!GodotObject.IsInstanceValid(shield) || viewport == null)
                    return;

                var rect = viewport.GetVisibleRect();
                shield.Position = rect.Position;
                shield.Size = rect.Size;
            }

            void CloseChrome()
            {
                if (GodotObject.IsInstanceValid(dialog))
                    dialog.QueueFree();
            }

            void RestoreMouseAndFocus()
            {
                if (GodotObject.IsInstanceValid(viewport))
                    viewport.SizeChanged -= FitShieldToViewport;
                Input.MouseMode = previousMouseMode;
                if (GodotObject.IsInstanceValid(layer))
                    layer.QueueFree();

                var target = previousFocus;
                if (target == null || !GodotObject.IsInstanceValid(target) || !target.IsVisibleInTree())
                    return;

                Callable.From(() =>
                {
                    if (GodotObject.IsInstanceValid(target) && target.IsVisibleInTree())
                        target.GrabFocus();
                }).CallDeferred();
            }
        }

        private static void ConfigureDialog(FileDialog dialog)
        {
            dialog.Name = "RitsuNativeFileDialog";
            dialog.Exclusive = true;
            dialog.Unresizable = false;
            dialog.Transparent = false;
            var minSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.fileDialog.layout.minSize",
                new(760f, 520f));
            dialog.MinSize = new(Mathf.CeilToInt(minSize.X), Mathf.CeilToInt(minSize.Y));
            dialog.Size = dialog.MinSize;
        }
    }
}
