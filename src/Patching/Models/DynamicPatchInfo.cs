using System.Reflection;
using HarmonyLib;
using STS2RitsuLib.Patching.Core;

namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes a patch target resolved at runtime and the Harmony patch methods to apply.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述在运行时解析的补丁目标，以及要应用的 Harmony 补丁方法。
    ///     </para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">Stable patch ID used for logging and unpatching.</para>
    ///     <para xml:lang="zh-CN">用于日志记录和移除补丁的稳定补丁 ID。</para>
    /// </param>
    /// <param name="originalMethod">
    ///     <para xml:lang="en">Vanilla method to patch.</para>
    ///     <para xml:lang="zh-CN">要添加补丁的原版方法。</para>
    /// </param>
    /// <param name="prefix">
    ///     <para xml:lang="en">Optional Harmony prefix.</para>
    ///     <para xml:lang="zh-CN">可选的 Harmony 前置补丁。</para>
    /// </param>
    /// <param name="postfix">
    ///     <para xml:lang="en">Optional Harmony postfix.</para>
    ///     <para xml:lang="zh-CN">可选的 Harmony 后置补丁。</para>
    /// </param>
    /// <param name="transpiler">
    ///     <para xml:lang="en">Optional Harmony transpiler.</para>
    ///     <para xml:lang="zh-CN">可选的 Harmony 指令转换器。</para>
    /// </param>
    /// <param name="finalizer">
    ///     <para xml:lang="en">Optional Harmony finalizer.</para>
    ///     <para xml:lang="zh-CN">可选的 Harmony 终结器。</para>
    /// </param>
    /// <param name="isCritical">
    ///     <para xml:lang="en">Whether failure to apply the patch is critical.</para>
    ///     <para xml:lang="zh-CN">补丁应用失败是否属于严重错误。</para>
    /// </param>
    /// <param name="description">
    ///     <para xml:lang="en">Human-readable description; defaults to the target type and method.</para>
    ///     <para xml:lang="zh-CN">便于阅读的描述；默认使用目标类型和方法。</para>
    /// </param>
    public sealed class DynamicPatchInfo(
        string id,
        MethodBase originalMethod,
        HarmonyMethod? prefix = null,
        HarmonyMethod? postfix = null,
        HarmonyMethod? transpiler = null,
        HarmonyMethod? finalizer = null,
        bool isCritical = true,
        string? description = null)
    {
        private Func<IDisposable>? _lifetimeLeaseFactory;

        /// <summary>
        ///     <para xml:lang="en">Gets the patch ID, which is unique within the owning patcher.</para>
        ///     <para xml:lang="zh-CN">获取补丁 ID；该 ID 在所属补丁器内唯一。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the target method.</para>
        ///     <para xml:lang="zh-CN">获取补丁的目标方法。</para>
        /// </summary>
        public MethodBase OriginalMethod { get; } = originalMethod;

        /// <summary>
        ///     <para xml:lang="en">Gets the Harmony prefix, if any.</para>
        ///     <para xml:lang="zh-CN">获取 Harmony 前置补丁（如果有）。</para>
        /// </summary>
        public HarmonyMethod? Prefix { get; } = prefix;

        /// <summary>
        ///     <para xml:lang="en">Gets the Harmony postfix, if any.</para>
        ///     <para xml:lang="zh-CN">获取 Harmony 后置补丁（如果有）。</para>
        /// </summary>
        public HarmonyMethod? Postfix { get; } = postfix;

        /// <summary>
        ///     <para xml:lang="en">Gets the Harmony transpiler, if any.</para>
        ///     <para xml:lang="zh-CN">获取 Harmony 指令转换器（如果有）。</para>
        /// </summary>
        public HarmonyMethod? Transpiler { get; } = transpiler;

        /// <summary>
        ///     <para xml:lang="en">Gets the Harmony finalizer, if any.</para>
        ///     <para xml:lang="zh-CN">获取 Harmony 终结器（如果有）。</para>
        /// </summary>
        public HarmonyMethod? Finalizer { get; } = finalizer;

        /// <summary>
        ///     <para xml:lang="en">Gets whether failure to apply this patch is critical.</para>
        ///     <para xml:lang="zh-CN">获取补丁应用失败是否属于严重错误。</para>
        /// </summary>
        public bool IsCritical { get; } = isCritical;

        /// <summary>
        ///     <para xml:lang="en">Gets a log-friendly description of the patch.</para>
        ///     <para xml:lang="zh-CN">获取适合写入日志的补丁描述。</para>
        /// </summary>
        public string Description { get; } = string.IsNullOrWhiteSpace(description)
            ? $"Patch {originalMethod.DeclaringType?.Name}.{originalMethod.Name}"
            : description;

        /// <summary>
        ///     <para xml:lang="en">Gets whether at least one Harmony patch method is specified.</para>
        ///     <para xml:lang="zh-CN">获取是否至少指定了一个 Harmony 补丁方法。</para>
        /// </summary>
        public bool HasPatchMethods => Prefix != null || Postfix != null || Transpiler != null || Finalizer != null;

        internal IDisposable? AcquireLifetimeLease()
        {
            return _lifetimeLeaseFactory?.Invoke();
        }

        internal void SetLifetimeLeaseFactory(Func<IDisposable> lifetimeLeaseFactory)
        {
            ArgumentNullException.ThrowIfNull(lifetimeLeaseFactory);
            if (_lifetimeLeaseFactory != null)
                throw new InvalidOperationException("A dynamic patch lifetime is already attached.");

            _lifetimeLeaseFactory = lifetimeLeaseFactory;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Id}: {OriginalMethod.DeclaringType?.Name}.{OriginalMethod.Name}";
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves <paramref name="target" /> as <see cref="ModPatcher" /> resolves a
        ///         <see cref="ModPatchInfo" />, then creates a dynamic patch.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按照 <see cref="ModPatcher" /> 解析 <see cref="ModPatchInfo" /> 的方式解析
        ///         <paramref name="target" />，然后创建动态补丁。
        ///     </para>
        /// </summary>
        public static DynamicPatchInfo FromModPatchTarget(
            string id,
            ModPatchTarget target,
            HarmonyMethod? prefix = null,
            HarmonyMethod? postfix = null,
            HarmonyMethod? transpiler = null,
            HarmonyMethod? finalizer = null,
            bool isCritical = true,
            string? description = null)
        {
            ArgumentNullException.ThrowIfNull(target);

            var originalMethod = PatchTargetMethodResolver.ResolveRequired(target);
            return new(
                id,
                originalMethod,
                prefix,
                postfix,
                transpiler,
                finalizer,
                isCritical,
                description);
        }
    }
}
