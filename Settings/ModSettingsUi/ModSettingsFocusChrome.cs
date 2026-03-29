using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Vanilla-style <see cref="NSelectionReticle" />; only visible in controller mode so mouse focus does not show
    ///     the reticle.
    /// </summary>
    internal static class ModSettingsFocusChrome
    {
        private const string ReticleMetaKey = "ritsu_mod_settings_reticle";

        internal static void AttachControllerSelectionReticle(Control host)
        {
            if (host.HasMeta(ReticleMetaKey))
                return;
            host.SetMeta(ReticleMetaKey, true);
            host.ClipContents = false;

            var reticle = ModSettingsUiResources.SelectionReticleScene.Instantiate<NSelectionReticle>();
            reticle.Name = "SelectionReticle";
            reticle.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            reticle.MouseFilter = Control.MouseFilterEnum.Ignore;
            host.AddChild(reticle);
            Callable.From(() =>
            {
                if (reticle.IsInsideTree() && host.IsInsideTree())
                    host.MoveChild(reticle, host.GetChildCount() - 1);
            }).CallDeferred();

            host.FocusEntered += () =>
            {
                if (NControllerManager.Instance?.IsUsingController == true)
                    reticle.OnSelect();
            };
            host.FocusExited += () => reticle.OnDeselect();
        }
    }
}
