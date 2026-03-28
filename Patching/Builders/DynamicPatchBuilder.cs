using System.Reflection;
using HarmonyLib;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Patching.Builders
{
    /// <summary>
    ///     Fluent builder for runtime-discovered Harmony patches.
    /// </summary>
    /// <param name="idPrefix">Prefix for auto-generated patch ids unless <c>patchId</c> is passed to <see cref="Add" />.</param>
    public sealed class DynamicPatchBuilder(string idPrefix)
    {
        private readonly List<DynamicPatchInfo> _patches = [];
        private int _counter;

        /// <summary>
        ///     Id prefix used when synthesizing patch identifiers.
        /// </summary>
        public string IdPrefix { get; } = idPrefix;

        /// <summary>
        ///     Patches accumulated so far (not applied until registered with a
        ///     <see cref="STS2RitsuLib.Patching.Core.ModPatcher" />).
        /// </summary>
        public IReadOnlyList<DynamicPatchInfo> Patches => _patches;

        /// <summary>
        ///     Appends a <see cref="DynamicPatchInfo" /> for <paramref name="originalMethod" />.
        /// </summary>
        public DynamicPatchBuilder Add(
            MethodBase originalMethod,
            HarmonyMethod? prefix = null,
            HarmonyMethod? postfix = null,
            HarmonyMethod? transpiler = null,
            HarmonyMethod? finalizer = null,
            bool isCritical = true,
            string? description = null,
            string? patchId = null)
        {
            ArgumentNullException.ThrowIfNull(originalMethod);

            var resolvedPatchId = patchId ??
                                  $"{IdPrefix}_{++_counter:D3}_{originalMethod.DeclaringType?.Name}_{originalMethod.Name}";
            _patches.Add(new(
                resolvedPatchId,
                originalMethod,
                prefix,
                postfix,
                transpiler,
                finalizer,
                isCritical,
                description));

            return this;
        }

        /// <summary>
        ///     Resolves a property getter on <paramref name="targetType" /> and calls <see cref="Add" />.
        /// </summary>
        public DynamicPatchBuilder AddPropertyGetter(
            Type targetType,
            string propertyName,
            HarmonyMethod? prefix = null,
            HarmonyMethod? postfix = null,
            HarmonyMethod? transpiler = null,
            HarmonyMethod? finalizer = null,
            bool isCritical = true,
            string? description = null,
            string? patchId = null)
        {
            ArgumentNullException.ThrowIfNull(targetType);
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

            var property = targetType.GetProperty(
                               propertyName,
                               BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                               BindingFlags.NonPublic)
                           ?? throw new MissingMemberException(targetType.FullName, propertyName);

            var getter = property.GetMethod
                         ?? throw new MissingMethodException(targetType.FullName, $"get_{propertyName}");

            return Add(
                getter,
                prefix,
                postfix,
                transpiler,
                finalizer,
                isCritical,
                description ?? $"Patch property getter {targetType.Name}.{propertyName}",
                patchId);
        }

        /// <summary>
        ///     Resolves a method on <paramref name="targetType" /> (optionally by <paramref name="parameterTypes" />) and
        ///     calls <see cref="Add" />.
        /// </summary>
        public DynamicPatchBuilder AddMethod(
            Type targetType,
            string methodName,
            Type[]? parameterTypes = null,
            HarmonyMethod? prefix = null,
            HarmonyMethod? postfix = null,
            HarmonyMethod? transpiler = null,
            HarmonyMethod? finalizer = null,
            bool isCritical = true,
            string? description = null,
            string? patchId = null)
        {
            ArgumentNullException.ThrowIfNull(targetType);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            MethodInfo? method;
            if (parameterTypes != null)
                method = targetType.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    parameterTypes,
                    null);
            else
                method = targetType.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
                throw new MissingMethodException(targetType.FullName, methodName);

            return Add(
                method,
                prefix,
                postfix,
                transpiler,
                finalizer,
                isCritical,
                description ?? $"Patch method {targetType.Name}.{methodName}",
                patchId);
        }

        /// <summary>
        ///     Wraps a static patch method on <paramref name="patchType" /> as a <see cref="HarmonyMethod" />.
        /// </summary>
        public static HarmonyMethod FromMethod(Type patchType, string methodName)
        {
            ArgumentNullException.ThrowIfNull(patchType);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            var method = patchType.GetMethod(
                             methodName,
                             BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                         ?? throw new MissingMethodException(patchType.FullName, methodName);

            return new(method);
        }
    }
}
