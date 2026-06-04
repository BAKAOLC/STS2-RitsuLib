using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Saves
{
    internal static class ProgressMirrorStore
    {
        private const string FileName = "progress_records_mirror.save";
        private const string LogContext = "ProgressMirror";
        private static bool _isRefreshingFromProgress;
        private static bool _isSavingMirror;

        internal static void MergeMirrorInto(SerializableProgress save)
        {
            ArgumentNullException.ThrowIfNull(save);

            var mirror = LoadMirror();
            if (mirror == null)
                return;

            if (!IsSameProgress(save, mirror))
            {
                RitsuLibFramework.Logger.Info(
                    $"[Saves] Progress mirror ignored because unique_id differs: save={save.UniqueId}, mirror={mirror.UniqueId}");
                return;
            }

            PreservedProgressRecords.MergeSerializableProgressRecords(save, mirror);
            RitsuLibFramework.Logger.Info("[Saves] Progress mirror merged into loaded progress");
        }

        internal static void RefreshFromProgress(ProgressState progress)
        {
            if (_isRefreshingFromProgress)
                return;

            try
            {
                _isRefreshingFromProgress = true;
                _ = progress.ToSerializable();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Saves] Failed to refresh progress mirror from progress: {ex.Message}");
            }
            finally
            {
                _isRefreshingFromProgress = false;
            }
        }

        internal static void SaveMirror(SerializableProgress save)
        {
            if (_isSavingMirror || string.IsNullOrWhiteSpace(save.UniqueId))
                return;

            try
            {
                _isSavingMirror = true;
                var json = JsonSerializationUtility.ToJson(save);
                var result = FileOperations.WriteText(GetMirrorPath(), json, LogContext);
                if (!result.Success)
                    RitsuLibFramework.Logger.Warn(
                        $"[Saves] Failed to write progress mirror: {result.ErrorMessage ?? "unknown error"}");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Saves] Failed to write progress mirror: {ex.Message}");
            }
            finally
            {
                _isSavingMirror = false;
            }
        }

        private static SerializableProgress? LoadMirror()
        {
            var result = FileOperations.ReadTextWithBackupFallback(GetMirrorPath(), LogContext);
            if (!result.Success || string.IsNullOrWhiteSpace(result.Content))
                return null;

            var readResult = JsonSerializationUtility.FromJson<SerializableProgress>(result.Content);
            if (readResult is { Success: true, SaveData: not null })
                return readResult.SaveData;

            RitsuLibFramework.Logger.Warn(
                $"[Saves] Failed to parse progress mirror: {readResult.ErrorMessage ?? readResult.Status.ToString()}");
            return null;
        }

        private static bool IsSameProgress(SerializableProgress save, SerializableProgress mirror)
        {
            return !string.IsNullOrWhiteSpace(save.UniqueId) &&
                   string.Equals(save.UniqueId, mirror.UniqueId, StringComparison.Ordinal);
        }

        private static string GetMirrorPath()
        {
            return ProfileManager.GetFilePath(FileName, SaveScope.Profile, GetCurrentProfileId(), Const.ModId);
        }

        private static int GetCurrentProfileId()
        {
            try
            {
                return SaveManager.Instance.CurrentProfileId;
            }
            catch
            {
                var profileId = ProfileManager.Instance.CurrentProfileId;
                return profileId > 0 ? profileId : 1;
            }
        }
    }
}
