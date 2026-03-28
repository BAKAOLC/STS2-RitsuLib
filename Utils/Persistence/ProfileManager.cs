using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     Tracks the active game profile id and resolves mod data paths under Godot <c>user://</c> storage.
    /// </summary>
    public class ProfileManager
    {
        private static ProfileManager? _instance;
        private bool _isInitialized;

        private ProfileManager()
        {
        }

        /// <summary>
        ///     Singleton accessor for the shared profile manager.
        /// </summary>
        public static ProfileManager Instance => _instance ??= new();

        /// <summary>
        ///     Last known game profile id, or <c>-1</c> before initialization.
        /// </summary>
        public int CurrentProfileId { get; private set; } = -1;

        /// <summary>
        ///     Raised with <c>(oldProfileId, newProfileId)</c> after the active profile changes.
        /// </summary>
        public event Action<int, int>? ProfileChanged;

        /// <summary>
        ///     Raised when mod data for a profile is deleted via game APIs.
        /// </summary>
        public event Action<int>? ProfileDeleted;

        /// <summary>
        ///     Subscribes to game profile changes and seeds <see cref="CurrentProfileId" />.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            CurrentProfileId = GetCurrentProfileIdFromGame();
            SaveManager.Instance.ProfileIdChanged += OnGameProfileChanged;

            _isInitialized = true;
            RitsuLibFramework.Logger.Info(
                $"[Persistence] ProfileManager initialized with profile ID: {CurrentProfileId}");
        }

        private void OnGameProfileChanged(int newProfileId)
        {
            OnProfileChanged(newProfileId);
        }

        /// <summary>
        ///     Updates <see cref="CurrentProfileId" /> and notifies subscribers when the value changes.
        /// </summary>
        public void OnProfileChanged(int newProfileId)
        {
            if (newProfileId == CurrentProfileId) return;

            var oldProfileId = CurrentProfileId;
            CurrentProfileId = newProfileId;

            if (oldProfileId >= 0)
                RitsuLibFramework.Logger.Info($"[Persistence] Profile changed from {oldProfileId} to {newProfileId}");
            ProfileChanged?.Invoke(oldProfileId, newProfileId);
        }

        /// <summary>
        ///     Re-reads the profile id from the game and applies <see cref="OnProfileChanged" /> if needed.
        /// </summary>
        public void RefreshCurrentProfile()
        {
            var newProfileId = GetCurrentProfileIdFromGame();
            if (newProfileId != CurrentProfileId)
                OnProfileChanged(newProfileId);
        }

        /// <summary>
        ///     Returns the account-level mod data root for <paramref name="modId" /> (not profile-specific).
        /// </summary>
        public static string GetAccountBasePath(string modId = Const.ModId)
        {
            var platformDir = GetPlatformDirectory();
            var userId = GetUserId();
            return $"user://{platformDir}/{userId}/mod_data/{modId}";
        }

        /// <summary>
        ///     Returns the profile subdirectory path for <see cref="CurrentProfileId" />.
        /// </summary>
        public string GetProfileDirectory()
        {
            return GetProfileDirectory(CurrentProfileId);
        }

        /// <summary>
        ///     Returns the game's relative profile directory name for <paramref name="profileId" />.
        /// </summary>
        public static string GetProfileDirectory(int profileId)
        {
            return UserDataPathProvider.GetProfileDir(profileId);
        }

        /// <summary>
        ///     Resolves the base storage path for <paramref name="scope" /> using <see cref="CurrentProfileId" />.
        /// </summary>
        public string GetBasePath(SaveScope scope)
        {
            return GetBasePath(scope, CurrentProfileId);
        }

        /// <summary>
        ///     Resolves the base storage path for <paramref name="scope" /> and explicit profile/mod ids.
        /// </summary>
        public static string GetBasePath(SaveScope scope, int profileId, string modId = Const.ModId)
        {
            var accountBase = GetAccountBasePath(modId);
            return scope switch
            {
                SaveScope.Global => accountBase,
                SaveScope.Profile => $"{accountBase}/{GetProfileDirectory(profileId)}",
                _ => accountBase,
            };
        }

        /// <summary>
        ///     Returns the full path for <paramref name="fileName" /> under the active profile and framework mod id.
        /// </summary>
        public string GetFilePath(string fileName, SaveScope scope)
        {
            return GetFilePath(fileName, scope, CurrentProfileId, Const.ModId);
        }

        /// <summary>
        ///     Returns the full path for <paramref name="fileName" /> using explicit <paramref name="profileId" />
        ///     and the framework mod id.
        /// </summary>
        public static string GetFilePath(string fileName, SaveScope scope, int profileId)
        {
            return GetFilePath(fileName, scope, profileId, Const.ModId);
        }

        /// <summary>
        ///     Returns the full path for <paramref name="fileName" /> under the active profile and
        ///     <paramref name="modId" />.
        /// </summary>
        public string GetFilePath(string fileName, SaveScope scope, string modId)
        {
            return GetFilePath(fileName, scope, CurrentProfileId, modId);
        }

        /// <summary>
        ///     Returns the full path for <paramref name="fileName" /> using explicit profile and mod ids.
        /// </summary>
        public static string GetFilePath(string fileName, SaveScope scope, int profileId, string modId)
        {
            return $"{GetBasePath(scope, profileId, modId)}/{fileName}";
        }

        /// <summary>
        ///     Deletes all profile-scoped mod files for <paramref name="profileId" />.
        /// </summary>
        public static void DeleteProfileData(int profileId, string modId = Const.ModId)
        {
            var profilePath = GetBasePath(SaveScope.Profile, profileId, modId);
            RitsuLibFramework.Logger.Info($"[Persistence] Deleting mod data for profile {profileId} at: {profilePath}");

            try
            {
                FileOperations.DeleteDirectoryRecursive(profilePath);
                RitsuLibFramework.Logger.Info($"[Persistence] Successfully deleted mod data for profile {profileId}");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error(
                    $"[Persistence] Failed to delete mod data for profile {profileId}: {ex.Message}");
            }
        }

        internal void OnProfileDeleted(int profileId)
        {
            ProfileDeleted?.Invoke(profileId);
        }

        private static int GetCurrentProfileIdFromGame()
        {
            try
            {
                return SaveManager.Instance.CurrentProfileId;
            }
            catch
            {
                return 1;
            }
        }

        private static string GetPlatformDirectory()
        {
            try
            {
                var platform = PlatformUtil.PrimaryPlatform;
                return UserDataPathProvider.GetPlatformDirectoryName(platform);
            }
            catch
            {
                return "default";
            }
        }

        private static string GetUserId()
        {
            try
            {
                var platform = PlatformUtil.PrimaryPlatform;
                return PlatformUtil.GetLocalPlayerId(platform).ToString();
            }
            catch
            {
                return "0";
            }
        }
    }
}
