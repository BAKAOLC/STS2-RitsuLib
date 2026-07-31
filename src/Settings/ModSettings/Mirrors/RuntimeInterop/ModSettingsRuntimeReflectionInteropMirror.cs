namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Exposes registration entry points for runtime-interoperability providers that supply a
    ///         <c>CreateRitsuLibSettingsSchema</c> method and value resolvers without a compile-time dependency from
    ///         RitsuLib to the provider assembly.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供运行时互操作设置提供器的注册入口；提供器通过 <c>CreateRitsuLibSettingsSchema</c> 方法及值解析器提供设置，
    ///         RitsuLib 无需在编译期依赖提供器程序集。
    ///     </para>
    /// </summary>
    public static class ModSettingsRuntimeReflectionInteropMirror
    {
        /// <inheritdoc cref="RuntimeInteropMirrorSource.RegisterProviderType(string, string?)" />
        public static bool RegisterProviderType(string providerTypeFullName, string? assemblyName = null)
        {
            return RuntimeInteropMirrorSource.RegisterProviderType(providerTypeFullName, assemblyName);
        }

        /// <inheritdoc cref="RuntimeInteropMirrorSource.RegisterProviderType(Type)" />
        public static bool RegisterProviderType(Type providerType)
        {
            return RuntimeInteropMirrorSource.RegisterProviderType(providerType);
        }

        /// <inheritdoc cref="RuntimeInteropMirrorSource.RegisterProviderType{TProvider}" />
        public static bool RegisterProviderType<TProvider>()
        {
            return RuntimeInteropMirrorSource.RegisterProviderType<TProvider>();
        }

        /// <inheritdoc cref="RuntimeInteropMirrorSource.RegisterProviderTypeAndTryRegister(string, string?)" />
        public static int RegisterProviderTypeAndTryRegister(string providerTypeFullName, string? assemblyName = null)
        {
            return RuntimeInteropMirrorSource.RegisterProviderTypeAndTryRegister(providerTypeFullName, assemblyName);
        }

        /// <inheritdoc cref="RuntimeInteropMirrorSource.RegisterProviderTypeAndTryRegister(Type)" />
        public static int RegisterProviderTypeAndTryRegister(Type providerType)
        {
            return RuntimeInteropMirrorSource.RegisterProviderTypeAndTryRegister(providerType);
        }

        /// <inheritdoc cref="RuntimeInteropMirrorSource.RegisterProviderTypeAndTryRegister{TProvider}" />
        public static int RegisterProviderTypeAndTryRegister<TProvider>()
        {
            return RuntimeInteropMirrorSource.RegisterProviderTypeAndTryRegister<TProvider>();
        }

        /// <inheritdoc cref="RuntimeInteropMirrorSource.TryRegisterMirroredPages" />
        public static int TryRegisterMirroredPages()
        {
            return RuntimeInteropMirrorSource.TryRegisterMirroredPages();
        }
    }
}
