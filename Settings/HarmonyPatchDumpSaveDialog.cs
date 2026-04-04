using Godot;
using STS2RitsuLib.Data.Models;

namespace STS2RitsuLib.Settings
{
    internal static class HarmonyPatchDumpSaveDialog
    {
        internal static void Show(
            ModSettingsValueBinding<RitsuLibSettings, string> outputPathBinding,
            IModSettingsUiActionHost uiHost)
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree?.Root == null)
            {
                RitsuLibFramework.Logger.Warn(
                    "[HarmonyDump] Cannot open file dialog: SceneTree root is not available.");
                return;
            }

            var dialog = new FileDialog
            {
                Title = ModSettingsLocalization.Get("ritsulib.harmonyDump.browseTitle", "Save Harmony patch dump"),
                FileMode = FileDialog.FileModeEnum.SaveFile,
                Access = FileDialog.AccessEnum.Filesystem,
                CurrentFile = "ritsulib_harmony_patch_dump.log",
            };
            dialog.AddFilter("*.log", "Log");
            dialog.AddFilter("*.txt", "Text");

            dialog.FileSelected += path =>
            {
                outputPathBinding.Write(path);
                outputPathBinding.Save();
                uiHost.RequestRefresh();
                dialog.QueueFree();
            };
            dialog.Canceled += dialog.QueueFree;

            tree.Root.AddChild(dialog);
            dialog.PopupCenteredRatio(0.55f);
        }
    }
}
