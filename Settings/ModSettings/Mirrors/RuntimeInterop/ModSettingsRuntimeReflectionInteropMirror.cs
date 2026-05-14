namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Public entry points for registering runtime-reflection mod settings providers (types that expose
    ///     <c>CreateRitsuLibSettingsSchema</c> and value resolvers without a compile-time dependency from RitsuLib on the
    ///     provider assembly).
    ///     用于注册运行时反射 mod 设置提供器的公共入口点（这些类型公开
    ///     <c>CreateRitsuLibSettingsSchema</c> 和值解析器，而 RitsuLib 对提供器程序集没有编译期依赖）。
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
