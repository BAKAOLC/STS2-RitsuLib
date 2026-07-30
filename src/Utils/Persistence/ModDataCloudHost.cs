using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Platform;
using STS2RitsuLib.Platform.Steam;

namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     <para xml:lang="en">Uses the game's <see cref="CloudSaveStore" /> as the capability boundary for mod-data cloud synchronization, avoiding dependencies on launcher-specific cloud-storage types.</para>
    ///     <para xml:lang="zh-CN">以游戏的 <see cref="CloudSaveStore" /> 作为模组数据云同步的能力边界，从而避免依赖启动器专用的云存储类型。</para>
    /// </summary>
    internal static class ModDataCloudHost
    {
        internal static bool MayEnumerateNativeSteamRemoteStorage =>
            !RitsuLibMobileSteamRuntime.SuppressNativeSteamIntegration &&
            SteamInitializer.Initialized &&
            RitsuLibSteamworks.IsAvailable;

        internal static CloudSaveStore? TryGetCloudSaveStore()
        {
            try
            {
                return SaveStore(SaveManager.Instance) as CloudSaveStore;
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

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_saveStore")]
        private static extern ref readonly ISaveStore SaveStore(SaveManager manager);
    }
}
