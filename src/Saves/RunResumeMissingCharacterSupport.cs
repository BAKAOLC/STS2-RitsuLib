using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Saves
{
    /// <summary>
    ///     Shared checks when resuming a run: missing <see cref="CharacterModel" /> means a mod was unloaded.
    ///     We never delete run saves so the player can restore mods and continue later.
    ///     恢复跑局时的共享检查：缺失 <see cref="CharacterModel" /> 表示某个 mod 已卸载。
    ///     我们从不删除跑局存档，因此玩家可以稍后恢复 mod 并继续。
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
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Saves] Failed to show invalid-run modal: {ex.Message}");
            }
        }
    }
}
