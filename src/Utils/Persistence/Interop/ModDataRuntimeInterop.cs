using STS2RitsuLib.Utils.Persistence.Interop;

namespace STS2RitsuLib
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides public entry points for registering runtime mod-data interoperability providers.
    ///         Providers expose <c>CreateRitsuLibModDataSchema</c> and value synchronizers without introducing a compile-time
    ///         dependency from RitsuLib to the provider assembly.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供注册运行时模组数据互操作提供程序的公共入口。提供程序公开 <c>CreateRitsuLibModDataSchema</c> 和值同步器，RitsuLib
    ///         无需在编译期依赖其程序集。
    ///     </para>
    /// </summary>
    public static class ModDataRuntimeInterop
    {
        /// <summary>
        ///     <para xml:lang="en">Adds a provider type name to runtime discovery without immediately registering its schema.</para>
        ///     <para xml:lang="zh-CN">将提供程序类型名加入运行时发现列表，但不立即注册其架构。</para>
        /// </summary>
        /// <param name="providerTypeFullName">
        ///     <para xml:lang="en">Fully qualified provider type name.</para>
        ///     <para xml:lang="zh-CN">提供程序类型的完全限定名。</para>
        /// </param>
        /// <param name="assemblyName">
        ///     <para xml:lang="en">Optional assembly name used to narrow discovery.</para>
        ///     <para xml:lang="zh-CN">用于缩小发现范围的可选程序集名称。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the type name was accepted; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">类型名被接受时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool RegisterProviderType(string providerTypeFullName, string? assemblyName = null)
        {
            return RuntimeModDataInteropSource.RegisterProviderType(providerTypeFullName, assemblyName);
        }

        /// <summary>
        ///     <para xml:lang="en">Adds a provider type to runtime discovery without immediately registering its schema.</para>
        ///     <para xml:lang="zh-CN">将提供程序类型加入运行时发现列表，但不立即注册其架构。</para>
        /// </summary>
        /// <param name="providerType">
        ///     <para xml:lang="en">Provider type to discover.</para>
        ///     <para xml:lang="zh-CN">要发现的提供程序类型。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the type could be added; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">类型可加入发现列表时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool RegisterProviderType(Type providerType)
        {
            return RuntimeModDataInteropSource.RegisterProviderType(providerType);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds <typeparamref name="TProvider" /> to runtime discovery without immediately registering its
        ///         schema.
        ///     </para>
        ///     <para xml:lang="zh-CN">将 <typeparamref name="TProvider" /> 加入运行时发现列表，但不立即注册其架构。</para>
        /// </summary>
        /// <typeparam name="TProvider">
        ///     <para xml:lang="en">Provider type to discover.</para>
        ///     <para xml:lang="zh-CN">要发现的提供程序类型。</para>
        /// </typeparam>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the type could be added; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">类型可加入发现列表时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool RegisterProviderType<TProvider>()
        {
            return RuntimeModDataInteropSource.RegisterProviderType<TProvider>();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds <typeparamref name="TProvider" /> to runtime discovery and then attempts to register every
        ///         discovered provider schema.
        ///     </para>
        ///     <para xml:lang="zh-CN">将 <typeparamref name="TProvider" /> 加入运行时发现列表，然后尝试注册所有已发现提供程序的架构。</para>
        /// </summary>
        /// <typeparam name="TProvider">
        ///     <para xml:lang="en">Provider type to discover.</para>
        ///     <para xml:lang="zh-CN">要发现的提供程序类型。</para>
        /// </typeparam>
        /// <returns>
        ///     <para xml:lang="en">Number of provider schemas newly registered by the discovery pass.</para>
        ///     <para xml:lang="zh-CN">本次发现过程中新增注册的提供程序架构数量。</para>
        /// </returns>
        public static int RegisterProviderTypeAndTryRegister<TProvider>()
        {
            return RuntimeModDataInteropSource.RegisterProviderTypeAndTryRegister<TProvider>();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a provider type name to runtime discovery and then attempts to register every discovered
        ///         provider schema.
        ///     </para>
        ///     <para xml:lang="zh-CN">将提供程序类型名加入运行时发现列表，然后尝试注册所有已发现提供程序的架构。</para>
        /// </summary>
        /// <param name="providerTypeFullName">
        ///     <para xml:lang="en">Fully qualified provider type name.</para>
        ///     <para xml:lang="zh-CN">提供程序类型的完全限定名。</para>
        /// </param>
        /// <param name="assemblyName">
        ///     <para xml:lang="en">Optional assembly name used to narrow discovery.</para>
        ///     <para xml:lang="zh-CN">用于缩小发现范围的可选程序集名称。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">Number of provider schemas newly registered by the discovery pass.</para>
        ///     <para xml:lang="zh-CN">本次发现过程中新增注册的提供程序架构数量。</para>
        /// </returns>
        public static int RegisterProviderTypeAndTryRegister(string providerTypeFullName, string? assemblyName = null)
        {
            return RuntimeModDataInteropSource.RegisterProviderTypeAndTryRegister(providerTypeFullName, assemblyName);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a provider type to runtime discovery and then attempts to register every discovered
        ///         provider schema.
        ///     </para>
        ///     <para xml:lang="zh-CN">将提供程序类型加入运行时发现列表，然后尝试注册所有已发现提供程序的架构。</para>
        /// </summary>
        /// <param name="providerType">
        ///     <para xml:lang="en">Provider type to discover.</para>
        ///     <para xml:lang="zh-CN">要发现的提供程序类型。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">Number of provider schemas newly registered by the discovery pass.</para>
        ///     <para xml:lang="zh-CN">本次发现过程中新增注册的提供程序架构数量。</para>
        /// </returns>
        public static int RegisterProviderTypeAndTryRegister(Type providerType)
        {
            return RuntimeModDataInteropSource.RegisterProviderTypeAndTryRegister(providerType);
        }

        /// <summary>
        ///     <para xml:lang="en">Discovers providers and registers every valid schema that has not already been processed.</para>
        ///     <para xml:lang="zh-CN">发现提供程序，并注册所有尚未处理的有效架构。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">Number of provider schemas newly registered by this pass.</para>
        ///     <para xml:lang="zh-CN">本次处理新增注册的提供程序架构数量。</para>
        /// </returns>
        public static int TryRegisterAll()
        {
            return RuntimeModDataInteropSource.TryRegisterAll();
        }

        /// <summary>
        ///     <para xml:lang="en">Copies current values from all registered providers into their RitsuLib mod-data slots.</para>
        ///     <para xml:lang="zh-CN">将所有已注册提供程序的当前值复制到对应的 RitsuLib 模组数据槽。</para>
        /// </summary>
        public static void SyncAllFromProviders()
        {
            RuntimeModDataInteropSource.SyncAllFromProviders();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Pushes values loaded by RitsuLib into all registered providers and invokes their post-load
        ///         hooks.
        ///     </para>
        ///     <para xml:lang="zh-CN">将 RitsuLib 加载的值推送到所有已注册提供程序，并调用其加载后钩子。</para>
        /// </summary>
        public static void PushLoadedDataToAllProviders()
        {
            RuntimeModDataInteropSource.PushLoadedDataToAllProviders();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Subscribes once to synchronize provider snapshots into
        ///         <see cref="STS2RitsuLib.Data.ModDataStore" /> when the active profile changes.
        ///     </para>
        ///     <para xml:lang="zh-CN">仅订阅一次，在活动档案变化时将提供程序快照同步到 <see cref="STS2RitsuLib.Data.ModDataStore" />。</para>
        /// </summary>
        public static void EnsureProfileSwitchSyncHook()
        {
            RuntimeModDataInteropSource.EnsureProfileChangedHook();
        }
    }
}
