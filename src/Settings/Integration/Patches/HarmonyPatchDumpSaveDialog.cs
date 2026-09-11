using Godot;
using STS2RitsuLib.Ui.Files;

namespace STS2RitsuLib.Settings
{
    internal static class HarmonyPatchDumpSaveDialog
    {
        internal static void Show(
            IModSettingsValueBinding<string> outputPathBinding,
            IModSettingsUiActionHost uiHost)
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree?.Root == null)
            {
                RitsuLibFramework.Logger.Warn(
                    "[HarmonyDump] Cannot open file dialog: SceneTree root is not available.");
                return;
            }

            var current = outputPathBinding.Read();
            RitsuFileDialog.Show(tree.Root, new()
            {
                Title = ModSettingsLocalization.Get("ritsulib.harmonyDump.browseTitle", "Save Harmony patch dump"),
                Mode = FileDialog.FileModeEnum.SaveFile,
                StateKey = "ritsulib.harmonyDump",
                InitialDirectory = string.IsNullOrWhiteSpace(current) ? null : Path.GetDirectoryName(current),
                InitialFile = string.IsNullOrWhiteSpace(current)
                    ? "ritsulib_harmony_patch_dump.log"
                    : Path.GetFileName(current),
                Filters = ["*.log;Log", "*.txt;Text"],
            }, paths =>
            {
                outputPathBinding.Write(paths[0]);
                outputPathBinding.Save();
                uiHost.RequestRefresh();
            });
        }
    }
}
