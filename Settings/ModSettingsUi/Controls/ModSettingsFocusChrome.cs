using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Vanilla-style <see cref="NSelectionReticle" />; only visible in controller mode so mouse focus does not show
    ///     the reticle.
    ///     原版风格的 <see cref="NSelectionReticle" />；仅在控制器模式下可见，避免鼠标焦点显示该光标框。
    /// </summary>
    internal static class ModSettingsFocusChrome
    {
        private const string ReticleMetaKey = "ritsu_mod_settings_reticle";
        private const int ReticleZIndex = (int)RenderingServer.CanvasItemZMax;
        private static NSelectionReticle? _sharedReticle;
        private static Control? _sharedReticleOwner;

        internal static void ReleaseFocusIfInsideTree(this Control? control)
        {
            if (control?.IsInsideTree() == true)
                control.ReleaseFocus();
        }

        internal static void AttachControllerSelectionReticle(Control host)
        {
            if (host.HasMeta(ReticleMetaKey))
                return;
            host.SetMeta(ReticleMetaKey, true);
            host.ClipContents = false;

            host.FocusEntered += () => OnSharedReticleHostFocusEntered(host);
            host.FocusExited += () => OnSharedReticleHostFocusExited(host);
            host.TreeExiting += () => OnSharedReticleHostTreeExiting(host);
        }

        internal static void ShowControllerSelectionReticle(Control host)
        {
            if (NControllerManager.Instance?.IsUsingController != true || !host.IsInsideTree())
                return;

            var reticle = EnsureSharedReticle();
            var ownerChanged = !ReferenceEquals(_sharedReticleOwner, host);
            if (ownerChanged && GodotObject.IsInstanceValid(_sharedReticleOwner))
                _sharedReticle?.OnDeselect();

            if (reticle.GetParent() != host)
            {
                reticle.GetParent()?.RemoveChild(reticle);
                host.AddChild(reticle);
            }

            ConfigureReticleForHost(host, reticle);
            _sharedReticleOwner = host;
            if (ownerChanged || !reticle.IsSelected)
                reticle.OnSelect();
        }

        internal static void HideControllerSelectionReticle(Control? host = null)
        {
            if (!GodotObject.IsInstanceValid(_sharedReticle))
            {
                _sharedReticleOwner = null;
                return;
            }

            if (host != null && !ReferenceEquals(_sharedReticleOwner, host))
                return;

            _sharedReticle.OnDeselect();
            _sharedReticleOwner = null;
        }

        private static void OnSharedReticleHostFocusEntered(Control host)
        {
            ShowControllerSelectionReticle(host);
        }

        private static void OnSharedReticleHostFocusExited(Control host)
        {
            HideControllerSelectionReticle(host);
        }

        private static void OnSharedReticleHostTreeExiting(Control host)
        {
            if (ReferenceEquals(_sharedReticleOwner, host))
                HideControllerSelectionReticle(host);
        }

        private static void ConfigureReticleForHost(Control host, NSelectionReticle reticle)
        {
            host.ClipContents = false;
            reticle.Name = "SelectionReticle";
            reticle.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            reticle.MouseFilter = Control.MouseFilterEnum.Ignore;
            reticle.ShowBehindParent = false;
            reticle.ZAsRelative = false;
            reticle.ZIndex = ReticleZIndex;
            host.MoveChild(reticle, host.GetChildCount() - 1);
        }

        private static NSelectionReticle EnsureSharedReticle()
        {
            if (GodotObject.IsInstanceValid(_sharedReticle))
                return _sharedReticle;

            _sharedReticle = ModSettingsUiResources.SelectionReticleScene.Instantiate<NSelectionReticle>();
            _sharedReticle.Name = "SelectionReticle";
            _sharedReticle.MouseFilter = Control.MouseFilterEnum.Ignore;
            _sharedReticle.ShowBehindParent = false;
            _sharedReticle.ZAsRelative = false;
            _sharedReticle.ZIndex = ReticleZIndex;
            _sharedReticleOwner = null;
            return _sharedReticle;
        }
    }
}
