namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     Describes a static patch type targeting one vanilla method by reflection.
    /// </summary>
    /// <param name="id">Stable patch identifier.</param>
    /// <param name="targetType">Declaring type of the method to patch.</param>
    /// <param name="methodName">Name of the method to patch.</param>
    /// <param name="patchType">
    ///     Type containing optional Harmony <c>Prefix</c>/<c>Postfix</c>/<c>Transpiler</c>/
    ///     <c>Finalizer</c>.
    /// </param>
    /// <param name="isCritical">Whether failure should block the patcher.</param>
    /// <param name="description">Optional description; defaults to <c>Patch Type.Method</c>.</param>
    /// <param name="parameterTypes">Method parameter types for overload resolution; null selects by name only.</param>
    /// <param name="ignoreIfTargetMissing">When true, missing targets produce an ignored result instead of failure.</param>
    public class ModPatchInfo(
        string id,
        Type targetType,
        string methodName,
        Type patchType,
        bool isCritical = true,
        string description = "",
        Type[]? parameterTypes = null,
        bool ignoreIfTargetMissing = false)
    {
        /// <summary>
        ///     Unique patch id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Type that declares the original method.
        /// </summary>
        public Type TargetType { get; } = targetType;

        /// <summary>
        ///     Original method name.
        /// </summary>
        public string MethodName { get; } = methodName;

        /// <summary>
        ///     Patch class applied via Harmony.
        /// </summary>
        public Type PatchType { get; } = patchType;

        /// <summary>
        ///     Whether this patch is critical.
        /// </summary>
        public bool IsCritical { get; } = isCritical;

        /// <summary>
        ///     Parameter signature for overload resolution, when needed.
        /// </summary>
        public Type[]? ParameterTypes { get; } = parameterTypes;

        /// <summary>
        ///     When true, a missing vanilla method yields an ignored success result.
        /// </summary>
        public bool IgnoreIfTargetMissing { get; } = ignoreIfTargetMissing;

        /// <summary>
        ///     Human-readable description of the patch.
        /// </summary>
        public string Description { get; } =
            string.IsNullOrEmpty(description) ? $"Patch {targetType.Name}.{methodName}" : description;

        /// <inheritdoc />
        public override string ToString()
        {
            if (ParameterTypes == null) return $"{Id}: {TargetType.Name}.{MethodName} <- {PatchType.Name}";

            var paramNames = ParameterTypes.Length == 0
                ? "no parameters"
                : string.Join(", ", ParameterTypes.Select(p => p.Name));
            return $"{Id}: {TargetType.Name}.{MethodName}({paramNames}) <- {PatchType.Name}";
        }
    }
}
