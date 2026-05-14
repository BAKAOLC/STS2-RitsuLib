using System.Reflection;
using HarmonyLib;
using STS2RitsuLib.Patching.Core;

namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     Describes a runtime-discovered patch target and the Harmony methods to apply to it.
    ///     描述运行时发现的 patch 目标以及要应用到它的 Harmony 方法。
    /// </summary>
    /// <param name="id">
    ///     Stable patch identifier for logging and unpatch.
    ///     用于日志和 unpatch 的稳定 patch 标识符。
    /// </param>
    /// <param name="originalMethod">
    ///     Vanilla method to patch.
    ///     要 patch 的原版方法。
    /// </param>
    /// <param name="prefix">
    ///     Optional Harmony prefix.
    ///     可选 Harmony 前置补丁.
    /// </param>
    /// <param name="postfix">
    ///     Optional Harmony postfix.
    ///     可选 Harmony 后置补丁.
    /// </param>
    /// <param name="transpiler">
    ///     Optional transpiler.
    ///     可选 transpiler。
    /// </param>
    /// <param name="finalizer">
    ///     Optional finalizer.
    ///     可选 finalizer。
    /// </param>
    /// <param name="isCritical">
    ///     When false, failures may be treated as optional by the patcher.
    ///     为 false 时，patcher 可将失败视为可选。
    /// </param>
    /// <param name="description">
    ///     Human-readable description; defaults to type.method.
    ///     人类可读的描述；默认为 type.method。
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
        /// <summary>
        ///     Unique patch id within the owning patcher.
        ///     所属 patcher 内的唯一 patch id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target method being patched.
        ///     正在被 patch 的目标方法。
        /// </summary>
        public MethodBase OriginalMethod { get; } = originalMethod;

        /// <summary>
        ///     Harmony prefix delegate, if any.
        ///     Harmony prefix 委托（如果有）。
        /// </summary>
        public HarmonyMethod? Prefix { get; } = prefix;

        /// <summary>
        ///     Harmony postfix delegate, if any.
        ///     Harmony postfix 委托（如果有）。
        /// </summary>
        public HarmonyMethod? Postfix { get; } = postfix;

        /// <summary>
        ///     Harmony transpiler, if any.
        ///     Harmony transpiler（如果有）。
        /// </summary>
        public HarmonyMethod? Transpiler { get; } = transpiler;

        /// <summary>
        ///     Harmony finalizer, if any.
        ///     Harmony finalizer（如果有）。
        /// </summary>
        public HarmonyMethod? Finalizer { get; } = finalizer;

        /// <summary>
        ///     Whether this patch is considered critical for mod correctness.
        ///     此 patch 是否被视为对 mod 正确性关键。
        /// </summary>
        public bool IsCritical { get; } = isCritical;

        /// <summary>
        ///     Log-friendly description of the patch purpose.
        ///     适合日志记录的 patch 目的描述。
        /// </summary>
        public string Description { get; } = string.IsNullOrWhiteSpace(description)
            ? $"Patch {originalMethod.DeclaringType?.Name}.{originalMethod.Name}"
            : description;

        /// <summary>
        ///     True when at least one Harmony hook is non-null.
        ///     至少一个 Harmony hook 非 null 时为 True。
        /// </summary>
        public bool HasPatchMethods => Prefix != null || Postfix != null || Transpiler != null || Finalizer != null;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Id}: {OriginalMethod.DeclaringType?.Name}.{OriginalMethod.Name}";
        }

        /// <summary>
        ///     Builds a dynamic patch by resolving <paramref name="target" /> the same way as <see cref="ModPatcher" />
        ///     resolves <see cref="ModPatchInfo" />.
        ///     通过解析 <paramref name="target" /> 构建动态 patch，方式与 <see cref="ModPatcher" />
        ///     解析 <see cref="ModPatchInfo" /> 相同。
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
