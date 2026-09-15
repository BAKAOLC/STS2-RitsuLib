using Godot;
using STS2RitsuLib.Ui.Files;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Opens a reusable <see cref="RitsuFileDialog" /> in <c>OpenDir</c> mode, then writes and saves the selected
    ///         folder path through a settings binding.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         以 <c>OpenDir</c> 模式打开可复用的 <see cref="RitsuFileDialog" />，然后通过设置绑定写入并保存
    ///         所选文件夹路径。
    ///     </para>
    /// </summary>
    internal static class ModSettingsOpenFolderDialog
    {
        internal static void Show(
            IModSettingsValueBinding<string> outputDirBinding,
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

            RitsuFileDialog.Show(tree.Root, new()
            {
                Title = ModSettingsLocalization.Get(titleLocalizationKey, titleFallback),
                Mode = FileDialog.FileModeEnum.OpenDir,
                StateKey = titleLocalizationKey,
                InitialDirectory = outputDirBinding.Read(),
            }, paths =>
            {
                outputDirBinding.Write(paths[0]);
                outputDirBinding.Save();
                uiHost.RequestRefresh();
            });
        }
    }
}
