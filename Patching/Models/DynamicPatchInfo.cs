using System.Reflection;
using HarmonyLib;
using STS2RitsuLib.Patching.Core;

namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     Describes a runtime-discovered patch target and the Harmony methods to apply to it.
    /// </summary>
    /// <param name="id">Stable patch identifier for logging and unpatch.</param>
    /// <param name="originalMethod">Vanilla method to patch.</param>
    /// <param name="prefix">Optional Harmony prefix.</param>
    /// <param name="postfix">Optional Harmony postfix.</param>
    /// <param name="transpiler">Optional transpiler.</param>
    /// <param name="finalizer">Optional finalizer.</param>
    /// <param name="isCritical">When false, failures may be treated as optional by the patcher.</param>
    /// <param name="description">Human-readable description; defaults to type.method.</param>
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
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target method being patched.
        /// </summary>
        public MethodBase OriginalMethod { get; } = originalMethod;

        /// <summary>
        ///     Harmony prefix delegate, if any.
        /// </summary>
        public HarmonyMethod? Prefix { get; } = prefix;

        /// <summary>
        ///     Harmony postfix delegate, if any.
        /// </summary>
        public HarmonyMethod? Postfix { get; } = postfix;

        /// <summary>
        ///     Harmony transpiler, if any.
        /// </summary>
        public HarmonyMethod? Transpiler { get; } = transpiler;

        /// <summary>
        ///     Harmony finalizer, if any.
        /// </summary>
        public HarmonyMethod? Finalizer { get; } = finalizer;

        /// <summary>
        ///     Whether this patch is considered critical for mod correctness.
        /// </summary>
        public bool IsCritical { get; } = isCritical;

        /// <summary>
        ///     Log-friendly description of the patch purpose.
        /// </summary>
        public string Description { get; } = string.IsNullOrWhiteSpace(description)
            ? $"Patch {originalMethod.DeclaringType?.Name}.{originalMethod.Name}"
            : description;

        /// <summary>
        ///     True when at least one Harmony hook is non-null.
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
