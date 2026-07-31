using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Saves
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Detects missing <see cref="CharacterModel" /> instances when resuming a run and presents the base game's
    ///         invalid-save dialog without deleting the run save.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         恢复跑局时检测缺失的 <see cref="CharacterModel" />，并显示原版游戏的无效存档对话框，而不删除局内存档。
    ///     </para>
    /// </summary>
    internal static class RunResumeMissingCharacterSupport
    {
        internal static bool AnyPlayerMissingRegisteredCharacter(SerializableRun run)
        {
            return run.Players.Select(p => p.CharacterId)
                .Any(cid => cid == null || ModelDb.GetByIdOrNull<CharacterModel>(cid) == null);
        }

        internal static void TryShowInvalidRunSaveModal()
        {
            try
            {
                var modal = NErrorPopup.Create(
                    new("main_menu_ui", "INVALID_SAVE_POPUP.title"),
                    new("main_menu_ui", "INVALID_SAVE_POPUP.description_run"),
                    new("main_menu_ui", "INVALID_SAVE_POPUP.dismiss"),
                    true);
                if (modal == null || NModalContainer.Instance == null)
                    return;
                NModalContainer.Instance.Add(modal);
                NModalContainer.Instance.ShowBackstop();
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Saves] Failed to show invalid-run modal: {ex.Message}");
            }
        }
    }
}
