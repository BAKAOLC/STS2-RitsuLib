using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Utils.Persistence.Context;

namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     Tracks the active game profile id and resolves mod data paths under Godot <c>user://</c> storage.
    ///     跟踪活动游戏档案 id，并解析 Godot <c>user://</c> 存储下的 mod 数据路径。
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
        ///     共享档案管理器的单例访问器。
        /// </summary>
        public static ProfileManager Instance => _instance ??= new();

        /// <summary>
        ///     Last known game profile id, or <c>-1</c> before initialization.
        ///     最后已知的游戏档案 id；初始化前为 <c>-1</c>。
        /// </summary>
        public int CurrentProfileId { get; private set; } = -1;

        /// <summary>
        ///     Raised with <c>(oldProfileId, newProfileId)</c> after the active profile changes.
        ///     活动档案变化后以 <c>(oldProfileId, newProfileId)</c> 触发。
        /// </summary>
        public event Action<int, int>? ProfileChanged;

        /// <summary>
        ///     Raised when mod data for a profile is deleted via game APIs.
        ///     通过游戏 API 删除某个档案的 Mod 数据时触发。
        /// </summary>
        public event Action<int>? ProfileDeleted;

        /// <summary>
        ///     Subscribes to game profile changes and seeds <see cref="CurrentProfileId" />.
        ///     订阅游戏档案变化，并初始化 <see cref="CurrentProfileId" />。
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
        ///     更新 <see cref="CurrentProfileId" />，并在值变化时通知订阅者。
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
        ///     从游戏重新读取档案 id，并在需要时应用 <see cref="OnProfileChanged" />。
        /// </summary>
        public void RefreshCurrentProfile()
        {
            var newProfileId = GetCurrentProfileIdFromGame();
            if (newProfileId != CurrentProfileId)
                OnProfileChanged(newProfileId);
        }

        /// <summary>
        ///     Returns the account-level mod data root for <paramref name="modId" /> (not profile-specific).
        ///     返回 <paramref name="modId" /> 的账户级 mod 数据根目录（非档案专属）。
        /// </summary>
        public static string GetAccountBasePath(string modId = Const.ModId)
        {
            var platformDir = GetPlatformDirectory();
            var userId = GetUserId();
            return $"user://{platformDir}/{userId}/mod_data/{modId}";
        }

        /// <summary>
        ///     Returns the profile subdirectory path for <see cref="CurrentProfileId" />.
        ///     返回 <see cref="CurrentProfileId" /> 的档案子目录路径。
        /// </summary>
        public string GetProfileDirectory()
        {
            return GetProfileDirectory(CurrentProfileId);
        }

        /// <summary>
        ///     Returns the game's relative profile directory name for <paramref name="profileId" />.
        ///     返回 <paramref name="profileId" /> 的游戏相对档案目录名。
        /// </summary>
        public static string GetProfileDirectory(int profileId)
        {
            return UserDataPathProvider.GetProfileDir(profileId);
        }

        /// <summary>
        ///     Resolves the base storage path for <paramref name="scope" /> using <see cref="CurrentProfileId" />.
        ///     使用 <see cref="CurrentProfileId" /> 解析 <paramref name="scope" /> 的基础存储路径。
        /// </summary>
        public string GetBasePath(SaveScope scope)
        {
            return GetBasePath(scope, CurrentProfileId);
        }

        /// <summary>
        ///     Resolves the base storage path for <paramref name="scope" /> and explicit profile/mod ids.
        ///     使用显式档案 / mod id 解析 <paramref name="scope" /> 的基础存储路径。
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
        ///     Resolves the base storage path for <paramref name="scope" /> using a supplied
        ///     <paramref name="context" />.
        ///     使用提供的 <paramref name="context" /> 解析 <paramref name="scope" /> 的基础存储路径。
        /// </summary>
        public static string GetBasePath(SaveScope scope, StorageContext context, string modId = Const.ModId)
        {
            ArgumentNullException.ThrowIfNull(context);

            return scope switch
            {
                SaveScope.Global => GetAccountBasePath(modId),
                SaveScope.Profile => GetBasePath(SaveScope.Profile,
                    context.TryGet(StorageContextKeys.ProfileId, out var pid)
                        ? pid
                        : Instance.CurrentProfileId,
                    modId),
                _ => StoragePathResolver.ResolveBasePathUser(modId, scope, context),
            };
        }

        /// <summary>
        ///     Returns the full path for <paramref name="fileName" /> under the active profile and framework mod id.
        ///     返回活动档案和框架 mod id 下 <paramref name="fileName" /> 的完整路径。
        /// </summary>
        public string GetFilePath(string fileName, SaveScope scope)
        {
            return GetFilePath(fileName, scope, CurrentProfileId, Const.ModId);
        }

        /// <summary>
        ///     Returns the full path for <paramref name="fileName" /> using explicit <paramref name="profileId" />
        ///     and the framework mod id.
        ///     使用显式 <paramref name="profileId" />
        ///     和框架 mod id 返回 <paramref name="fileName" /> 的完整路径。
        /// </summary>
        public static string GetFilePath(string fileName, SaveScope scope, int profileId)
        {
            return GetFilePath(fileName, scope, profileId, Const.ModId);
        }

        /// <summary>
        ///     Returns the full path for <paramref name="fileName" /> under the active profile and
        ///     <paramref name="modId" />.
        ///     返回活动档案和
        ///     <paramref name="modId" /> 下 <paramref name="fileName" /> 的完整路径。
        /// </summary>
        public string GetFilePath(string fileName, SaveScope scope, string modId)
        {
            return GetFilePath(fileName, scope, CurrentProfileId, modId);
        }

        /// <summary>
        ///     Returns the full path for <paramref name="fileName" /> using explicit profile and mod ids.
        ///     使用显式档案和 mod id 返回 <paramref name="fileName" /> 的完整路径。
        /// </summary>
        public static string GetFilePath(string fileName, SaveScope scope, int profileId, string modId)
        {
            return $"{GetBasePath(scope, profileId, modId)}/{fileName}";
        }

        /// <summary>
        ///     Returns the full path for <paramref name="fileName" /> using a supplied
        ///     <paramref name="context" />.
        ///     使用提供的 <paramref name="context" /> 返回 <paramref name="fileName" /> 的完整路径。
        /// </summary>
        public static string GetFilePath(string fileName, SaveScope scope, StorageContext context,
            string modId = Const.ModId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            ArgumentNullException.ThrowIfNull(context);

            return StoragePathResolver.ResolveFilePathUser(modId, fileName, scope, context);
        }

        /// <summary>
        ///     Deletes all profile-scoped mod files for <paramref name="profileId" />.
        ///     删除 <paramref name="profileId" /> 的所有档案作用域 mod 文件。
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
                RitsuLibFramework.Logger.ErrorNoTrace(
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
