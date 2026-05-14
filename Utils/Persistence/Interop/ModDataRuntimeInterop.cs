using STS2RitsuLib.Utils.Persistence.Interop;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Public entry points for registering runtime ModData interop providers (types that expose
    ///     <c>CreateRitsuLibModDataSchema</c> and value synchronizers without a compile-time dependency from RitsuLib on the
    ///     provider assembly).
    ///     用于注册运行时 ModData interop 提供方的公共入口点（这些类型公开
    ///     <c>CreateRitsuLibModDataSchema</c> 和值同步器，且 RitsuLib 对
    ///     提供方程序集没有编译期依赖）。
    /// </summary>
    public static class ModDataRuntimeInterop
    {
        /// <inheritdoc cref="RuntimeModDataInteropSource.RegisterProviderType(string, string?)" />
        public static bool RegisterProviderType(string providerTypeFullName, string? assemblyName = null)
        {
            return RuntimeModDataInteropSource.RegisterProviderType(providerTypeFullName, assemblyName);
        }

        /// <inheritdoc cref="RuntimeModDataInteropSource.RegisterProviderType(Type)" />
        public static bool RegisterProviderType(Type providerType)
        {
            return RuntimeModDataInteropSource.RegisterProviderType(providerType);
        }

        /// <inheritdoc cref="RuntimeModDataInteropSource.RegisterProviderType{TProvider}" />
        public static bool RegisterProviderType<TProvider>()
        {
            return RuntimeModDataInteropSource.RegisterProviderType<TProvider>();
        }

        /// <inheritdoc cref="RuntimeModDataInteropSource.RegisterProviderTypeAndTryRegister{TProvider}" />
        public static int RegisterProviderTypeAndTryRegister<TProvider>()
        {
            return RuntimeModDataInteropSource.RegisterProviderTypeAndTryRegister<TProvider>();
        }

        /// <inheritdoc cref="RuntimeModDataInteropSource.RegisterProviderTypeAndTryRegister(string, string?)" />
        public static int RegisterProviderTypeAndTryRegister(string providerTypeFullName, string? assemblyName = null)
        {
            return RuntimeModDataInteropSource.RegisterProviderTypeAndTryRegister(providerTypeFullName, assemblyName);
        }

        /// <inheritdoc cref="RuntimeModDataInteropSource.RegisterProviderTypeAndTryRegister(Type)" />
        public static int RegisterProviderTypeAndTryRegister(Type providerType)
        {
            return RuntimeModDataInteropSource.RegisterProviderTypeAndTryRegister(providerType);
        }

        /// <inheritdoc cref="RuntimeModDataInteropSource.TryRegisterAll" />
        public static int TryRegisterAll()
        {
            return RuntimeModDataInteropSource.TryRegisterAll();
        }

        /// <inheritdoc cref="RuntimeModDataInteropSource.SyncAllFromProviders" />
        public static void SyncAllFromProviders()
        {
            RuntimeModDataInteropSource.SyncAllFromProviders();
        }

        /// <inheritdoc cref="RuntimeModDataInteropSource.PushLoadedDataToAllProviders" />
        public static void PushLoadedDataToAllProviders()
        {
            RuntimeModDataInteropSource.PushLoadedDataToAllProviders();
        }

        /// <summary>
        ///     Ensures provider snapshots are written into <see cref="STS2RitsuLib.Data.ModDataStore" /> before the game
        ///     persists profile mod data on profile switches (subscribe once).
        ///     确保在游戏因档案切换持久化档案 mod 数据前，将提供方快照写入
        ///     <see cref="STS2RitsuLib.Data.ModDataStore" />（只订阅一次）。
        /// </summary>
        public static void EnsureProfileSwitchSyncHook()
        {
            RuntimeModDataInteropSource.EnsureProfileChangedHook();
        }
    }
}
