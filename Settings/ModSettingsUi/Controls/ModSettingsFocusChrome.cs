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

        private static void OnSharedReticleHostFocusEntered(Control host)
        {
            if (NControllerManager.Instance?.IsUsingController != true || !host.IsInsideTree())
                return;

            var reticle = EnsureSharedReticle();
            if (reticle.GetParent() != host)
            {
                reticle.GetParent()?.RemoveChild(reticle);
                host.AddChild(reticle);
            }

            reticle.Name = "SelectionReticle";
            reticle.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            reticle.MouseFilter = Control.MouseFilterEnum.Ignore;
            host.MoveChild(reticle, host.GetChildCount() - 1);
            _sharedReticleOwner = host;
            reticle.OnSelect();
        }

        private static void OnSharedReticleHostFocusExited(Control host)
        {
            if (!ReferenceEquals(_sharedReticleOwner, host) || !GodotObject.IsInstanceValid(_sharedReticle))
                return;

            _sharedReticle.OnDeselect();
            _sharedReticleOwner = null;
        }

        private static void OnSharedReticleHostTreeExiting(Control host)
        {
            if (ReferenceEquals(_sharedReticleOwner, host))
                _sharedReticleOwner = null;
        }

        private static NSelectionReticle EnsureSharedReticle()
        {
            if (GodotObject.IsInstanceValid(_sharedReticle))
                return _sharedReticle!;

            _sharedReticle = ModSettingsUiResources.SelectionReticleScene.Instantiate<NSelectionReticle>();
            _sharedReticle.Name = "SelectionReticle";
            _sharedReticle.MouseFilter = Control.MouseFilterEnum.Ignore;
            _sharedReticleOwner = null;
            return _sharedReticle;
        }
    }
}
