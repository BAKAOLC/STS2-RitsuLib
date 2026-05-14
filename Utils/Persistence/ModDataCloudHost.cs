using System.Reflection;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Platform;
using STS2RitsuLib.Platform.Steam;

namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     Capability boundary for mod-data cloud sync. The primary contract is the game's own
    ///     <see cref="CloudSaveStore" />, so compatible launchers can provide cloud storage by injecting a normal
    ///     <see cref="ICloudSaveStore" /> into <see cref="SaveManager" /> without RitsuLib knowing launcher-specific
    ///     types.
    ///     模组数据云同步的能力边界。主合约是游戏自身的 <see cref="CloudSaveStore" />；兼容启动器只要把
    ///     标准 <see cref="ICloudSaveStore" /> 注入 <see cref="SaveManager" />，RitsuLib 就能复用，不需要识别启动器类型。
    /// </summary>
    internal static class ModDataCloudHost
    {
        private static FieldInfo? _saveStoreField;

        internal static bool MayEnumerateNativeSteamRemoteStorage =>
            !RitsuLibMobileSteamRuntime.SuppressNativeSteamIntegration &&
            SteamInitializer.Initialized &&
            RitsuLibSteamworks.IsAvailable;

        internal static CloudSaveStore? TryGetCloudSaveStore()
        {
            try
            {
                _saveStoreField ??= typeof(SaveManager).GetField("_saveStore",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var store = _saveStoreField?.GetValue(SaveManager.Instance);
                return store as CloudSaveStore;
            }
            catch
            {
                return null;
            }
        }

        internal static bool HasCloudSaveStore()
        {
            return TryGetCloudSaveStore() != null;
        }

        internal static bool CanUseModDataCloud()
        {
            return HasCloudSaveStore();
        }
    }
}
