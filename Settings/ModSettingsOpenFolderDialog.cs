using Godot;
using STS2RitsuLib.Data.Models;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Reusable <see cref="FileDialog" /> in <c>OpenDir</c> mode for persisting a folder path to
    ///     <see cref="RitsuLibSettings" />.
    /// </summary>
    internal static class ModSettingsOpenFolderDialog
    {
        internal static void Show(
            ModSettingsValueBinding<RitsuLibSettings, string> outputDirBinding,
            IModSettingsUiActionHost uiHost,
            string logPrefix,
            string titleLocalizationKey,
            string titleFallback)
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree?.Root == null)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[{logPrefix}] Cannot open folder dialog: SceneTree root is not available.");
                return;
            }

            var dialog = new FileDialog
            {
                Title = ModSettingsLocalization.Get(titleLocalizationKey, titleFallback),
                FileMode = FileDialog.FileModeEnum.OpenDir,
                Access = FileDialog.AccessEnum.Filesystem,
            };

            dialog.DirSelected += path =>
            {
                outputDirBinding.Write(path);
                outputDirBinding.Save();
                uiHost.RequestRefresh();
                dialog.QueueFree();
            };
            dialog.Canceled += dialog.QueueFree;

            tree.Root.AddChild(dialog);
            dialog.PopupCenteredRatio(0.55f);
        }
    }
}
