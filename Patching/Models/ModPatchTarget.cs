namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     Vanilla method identity used with <see cref="IPatchMethod.GetTargets" /> to build <see cref="ModPatchInfo" />.
    /// </summary>
    /// <param name="TargetType">Declaring type.</param>
    /// <param name="MethodName">Method name.</param>
    /// <param name="ParameterTypes">Overload parameter types, or null for name-only lookup.</param>
    /// <param name="IgnoreIfMissing">Maps to <see cref="ModPatchInfo.IgnoreIfTargetMissing" />.</param>
    public record ModPatchTarget(Type TargetType, string MethodName, Type[]? ParameterTypes, bool IgnoreIfMissing)
    {
        /// <summary>
        ///     Target with optional overload signature; not ignored if missing.
        /// </summary>
        /// <param name="targetType">Declaring type.</param>
        /// <param name="methodName">Method name.</param>
        /// <param name="parameterTypes">Overload parameter types.</param>
        public ModPatchTarget(Type targetType, string methodName, Type[]? parameterTypes)
            // ReSharper disable once IntroduceOptionalParameters.Global
            : this(targetType, methodName, parameterTypes, false)
        {
        }

        /// <summary>
        ///     Target without overload disambiguation; optional ignore-if-missing flag.
        /// </summary>
        /// <param name="targetType">Declaring type.</param>
        /// <param name="methodName">Method name.</param>
        /// <param name="ignoreIfMissing">When true, missing method is non-fatal for optional patches.</param>
        public ModPatchTarget(Type targetType, string methodName, bool ignoreIfMissing)
            : this(targetType, methodName, null, ignoreIfMissing)
        {
        }

        /// <summary>
        ///     Simple target: any overload with that name, fail if missing.
        /// </summary>
        /// <param name="targetType">Declaring type.</param>
        /// <param name="methodName">Method name.</param>
        public ModPatchTarget(Type targetType, string methodName)
            // ReSharper disable IntroduceOptionalParameters.Global
            : this(targetType, methodName, null, false)
        // ReSharper restore IntroduceOptionalParameters.Global
        {
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (ParameterTypes == null) return $"{TargetType.Name}.{MethodName}";

            var paramNames = ParameterTypes.Length == 0
                ? "no parameters"
                : string.Join(", ", ParameterTypes.Select(p => p.Name));
            return $"{TargetType.Name}.{MethodName}({paramNames})";
        }
    }
}
