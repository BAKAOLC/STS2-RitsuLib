using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Utils.Persistence.Context;

namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Tracks the active game profile ID and resolves mod-data paths under Godot <c>user://</c>
    ///         storage.
    ///     </para>
    ///     <para xml:lang="zh-CN">跟踪活动游戏档案 ID，并解析 Godot <c>user://</c> 存储中的模组数据路径。</para>
    /// </summary>
    public class ProfileManager
    {
        private static ProfileManager? _instance;
        private bool _isInitialized;

        private ProfileManager()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the shared profile manager.</para>
        ///     <para xml:lang="zh-CN">获取共享的档案管理器。</para>
        /// </summary>
        public static ProfileManager Instance => _instance ??= new();

        /// <summary>
        ///     <para xml:lang="en">Last known game profile ID, or <c>-1</c> before initialization.</para>
        ///     <para xml:lang="zh-CN">最后获知的游戏档案 ID；初始化前为 <c>-1</c>。</para>
        /// </summary>
        public int CurrentProfileId { get; private set; } = -1;

        /// <summary>
        ///     <para xml:lang="en">Raised with <c>(oldProfileId, newProfileId)</c> after the active profile changes.</para>
        ///     <para xml:lang="zh-CN">活动档案变化后以 <c>(oldProfileId, newProfileId)</c> 触发。</para>
        /// </summary>
        public event Action<int, int>? ProfileChanged;

        /// <summary>
        ///     <para xml:lang="en">Raised when mod data for a profile is deleted via game APIs.</para>
        ///     <para xml:lang="zh-CN">通过游戏 API 删除某个档案的模组数据时触发。</para>
        /// </summary>
        public event Action<int>? ProfileDeleted;

        /// <summary>
        ///     <para xml:lang="en">Subscribes to game profile changes and initializes <see cref="CurrentProfileId" />.</para>
        ///     <para xml:lang="zh-CN">订阅游戏档案变化，并初始化 <see cref="CurrentProfileId" />。</para>
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
        ///     <para xml:lang="en">Updates <see cref="CurrentProfileId" /> and notifies subscribers when the value changes.</para>
        ///     <para xml:lang="zh-CN">更新 <see cref="CurrentProfileId" />，并在值变化时通知订阅者。</para>
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
        ///     <para xml:lang="en">Re-reads the profile ID from the game and calls <see cref="OnProfileChanged" /> if it changed.</para>
        ///     <para xml:lang="zh-CN">从游戏重新读取档案 ID，并在值发生变化时调用 <see cref="OnProfileChanged" />。</para>
        /// </summary>
        public void RefreshCurrentProfile()
        {
            var newProfileId = GetCurrentProfileIdFromGame();
            if (newProfileId != CurrentProfileId)
                OnProfileChanged(newProfileId);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the account-level, profile-independent mod-data root for <paramref name="modId" />.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="modId" /> 的账户级模组数据根目录；该目录不属于特定档案。</para>
        /// </summary>
        public static string GetAccountBasePath(string modId = Const.ModId)
        {
            var platformDir = GetPlatformDirectory();
            var userId = GetUserId();
            return $"user://{platformDir}/{userId}/mod_data/{modId}";
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the profile subdirectory path for <see cref="CurrentProfileId" />.</para>
        ///     <para xml:lang="zh-CN">返回 <see cref="CurrentProfileId" /> 的档案子目录路径。</para>
        /// </summary>
        public string GetProfileDirectory()
        {
            return GetProfileDirectory(CurrentProfileId);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the game's relative profile directory name for <paramref name="profileId" />.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="profileId" /> 的游戏相对档案目录名。</para>
        /// </summary>
        public static string GetProfileDirectory(int profileId)
        {
            return UserDataPathProvider.GetProfileDir(profileId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves the Godot user-data base path for <paramref name="scope" /> using
        ///         <see cref="CurrentProfileId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用 <see cref="CurrentProfileId" /> 解析 <paramref name="scope" /> 的 Godot 用户数据基础路径。</para>
        /// </summary>
        public string GetBasePath(SaveScope scope)
        {
            return GetBasePath(scope, CurrentProfileId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves the Godot user-data base path for <paramref name="scope" /> using explicit profile and
        ///         mod IDs.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用显式指定的档案 ID 和模组 ID，解析 <paramref name="scope" /> 的 Godot 用户数据基础路径。</para>
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
        ///     <para xml:lang="en">
        ///         Resolves the Godot user-data base path for <paramref name="scope" /> and
        ///         <paramref name="modId" /> using the supplied <paramref name="context" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用提供的 <paramref name="context" />，解析 <paramref name="scope" /> 和 <paramref name="modId" />
        ///         对应的 Godot 用户数据基础路径。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Returns the Godot user-data path for <paramref name="fileName" /> using the active profile and
        ///         RitsuLib mod ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用活动档案和 RitsuLib 模组 ID，返回 <paramref name="fileName" /> 的 Godot 用户数据路径。</para>
        /// </summary>
        public string GetFilePath(string fileName, SaveScope scope)
        {
            return GetFilePath(fileName, scope, CurrentProfileId, Const.ModId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the Godot user-data path for <paramref name="fileName" /> using
        ///         <paramref name="profileId" /> and the RitsuLib mod ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="profileId" /> 和 RitsuLib 模组 ID，返回 <paramref name="fileName" /> 的 Godot
        ///         用户数据路径。
        ///     </para>
        /// </summary>
        public static string GetFilePath(string fileName, SaveScope scope, int profileId)
        {
            return GetFilePath(fileName, scope, profileId, Const.ModId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the Godot user-data path for <paramref name="fileName" /> using the active profile and
        ///         <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用活动档案和 <paramref name="modId" />，返回 <paramref name="fileName" /> 的 Godot 用户数据路径。</para>
        /// </summary>
        public string GetFilePath(string fileName, SaveScope scope, string modId)
        {
            return GetFilePath(fileName, scope, CurrentProfileId, modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the Godot user-data path for <paramref name="fileName" /> using explicit profile and
        ///         mod IDs.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用显式指定的档案 ID 和模组 ID，返回 <paramref name="fileName" /> 的 Godot 用户数据路径。</para>
        /// </summary>
        public static string GetFilePath(string fileName, SaveScope scope, int profileId, string modId)
        {
            return $"{GetBasePath(scope, profileId, modId)}/{fileName}";
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the Godot user-data path for <paramref name="fileName" /> and <paramref name="modId" />
        ///         using the supplied <paramref name="context" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用提供的 <paramref name="context" />，返回 <paramref name="fileName" /> 和
        ///         <paramref name="modId" /> 对应的 Godot 用户数据路径。
        ///     </para>
        /// </summary>
        public static string GetFilePath(string fileName, SaveScope scope, StorageContext context,
            string modId = Const.ModId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            ArgumentNullException.ThrowIfNull(context);

            return StoragePathResolver.ResolveFilePathUser(modId, fileName, scope, context);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Deletes all profile-scoped mod-data files for <paramref name="profileId" /> and
        ///         <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">删除 <paramref name="profileId" /> 和 <paramref name="modId" /> 对应的所有档案作用域模组数据文件。</para>
        /// </summary>
        public static void DeleteProfileData(int profileId, string modId = Const.ModId)
        {
            var profilePath = GetBasePath(SaveScope.Profile, profileId, modId);
            RitsuLibFramework.Logger.Info($"[Persistence] Deleting mod data for profile {profileId} at: {profilePath}");

            try
            {
                var result = FileOperations.DeleteDirectoryRecursive(profilePath);
                if (!result.Success)
                {
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[Persistence] Failed to delete mod data for profile {profileId}: {result.ErrorMessage}");
                    return;
                }

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
