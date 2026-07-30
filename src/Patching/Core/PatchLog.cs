using System.Collections.Concurrent;
using MegaCrit.Sts2.Core.Logging;

namespace STS2RitsuLib.Patching.Core
{
    /// <summary>
    ///     <para xml:lang="en">Provides a logger registry for patch types.</para>
    ///     <para xml:lang="zh-CN">提供补丁类型的日志器注册表。</para>
    /// </summary>
    public static class PatchLog
    {
        private static readonly ConcurrentDictionary<Type, Logger> Registry = new();

        /// <summary>
        ///     <para xml:lang="en">Associates <paramref name="logger" /> with <paramref name="patchType" />.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="logger" /> 与 <paramref name="patchType" /> 关联。</para>
        /// </summary>
        public static void Bind(Type patchType, Logger logger)
        {
            ArgumentNullException.ThrowIfNull(patchType);
            ArgumentNullException.ThrowIfNull(logger);

            Registry[patchType] = logger;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the logger bound to <paramref name="patchType" />, or <see cref="RitsuLibFramework.Logger" />
        ///         when none is bound.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取绑定到 <paramref name="patchType" /> 的日志器；未绑定时返回
        ///         <see cref="RitsuLibFramework.Logger" />。
        ///     </para>
        /// </summary>
        public static Logger For(Type patchType)
        {
            ArgumentNullException.ThrowIfNull(patchType);
            return Registry.TryGetValue(patchType, out var logger)
                ? logger
                : RitsuLibFramework.Logger;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the logger for <typeparamref name="TPatch" />.</para>
        ///     <para xml:lang="zh-CN">获取 <typeparamref name="TPatch" /> 的日志器。</para>
        /// </summary>
        public static Logger For<TPatch>()
        {
            return For(typeof(TPatch));
        }
    }
}
