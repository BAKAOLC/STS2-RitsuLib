using System.Collections.Concurrent;
using MegaCrit.Sts2.Core.Logging;

namespace STS2RitsuLib.Patching.Core
{
    /// <summary>
    ///     Logger registry for IPatchMethod types.
    ///     IPatchMethod 类型的 logger 注册表。
    /// </summary>
    public static class PatchLog
    {
        private static readonly ConcurrentDictionary<Type, Logger> Registry = new();

        /// <summary>
        ///     Associates <paramref name="logger" /> with <paramref name="patchType" /> for <see cref="For(Type)" />.
        ///     将 <paramref name="logger" /> 与 <paramref name="patchType" /> 关联，供 <see cref="For(Type)" /> 使用。
        /// </summary>
        public static void Bind(Type patchType, Logger logger)
        {
            ArgumentNullException.ThrowIfNull(patchType);
            ArgumentNullException.ThrowIfNull(logger);

            Registry[patchType] = logger;
        }

        /// <summary>
        ///     Returns the bound logger for <paramref name="patchType" />, or <see cref="RitsuLibFramework.Logger" />.
        ///     返回绑定到 <paramref name="patchType" /> 的 logger；否则返回 <see cref="RitsuLibFramework.Logger" />。
        /// </summary>
        public static Logger For(Type patchType)
        {
            ArgumentNullException.ThrowIfNull(patchType);
            return Registry.TryGetValue(patchType, out var logger)
                ? logger
                : RitsuLibFramework.Logger;
        }

        /// <summary>
        ///     <see cref="For(Type)" /> for <typeparamref name="TPatch" />.
        ///     <see cref="For(Type)" />，用于 <typeparamref name="TPatch" />。
        /// </summary>
        public static Logger For<TPatch>()
        {
            return For(typeof(TPatch));
        }
    }
}
