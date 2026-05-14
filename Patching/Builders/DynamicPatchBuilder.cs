using System.Reflection;
using HarmonyLib;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Patching.Builders
{
    /// <summary>
    ///     Fluent builder for runtime-discovered Harmony patches.
    ///     用于运行时发现 Harmony patch 的流式构建器。
    /// </summary>
    /// <param name="idPrefix">
    ///     Prefix for auto-generated patch ids unless <c>patchId</c> is passed to an overload.
    ///     自动生成 patch id 的前缀，除非向重载传入 <c>patchId</c>。
    /// </param>
    public sealed class DynamicPatchBuilder(string idPrefix)
    {
        private readonly List<DynamicPatchInfo> _patches = [];
        private int _counter;

        /// <summary>
        ///     Id prefix used when synthesizing patch identifiers.
        ///     合成 patch 标识符时使用的 id 前缀。
        /// </summary>
        public string IdPrefix { get; } = idPrefix;

        /// <summary>
        ///     Patches accumulated so far (not applied until registered with a
        ///     <see cref="STS2RitsuLib.Patching.Core.ModPatcher" />).
        ///     目前累计的 patch（在注册到
        ///     <see cref="STS2RitsuLib.Patching.Core.ModPatcher" /> 之前不会应用）。
        /// </summary>
        public IReadOnlyList<DynamicPatchInfo> Patches => _patches;

        /// <summary>
        ///     Appends a <see cref="DynamicPatchInfo" /> for <paramref name="originalMethod" />.
        ///     追加一个 <see cref="DynamicPatchInfo" />，用于 <paramref name="originalMethod" />。
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
        ///     Resolves <paramref name="target" /> via <see cref="PatchTargetMethodResolver" /> and appends the result to
        ///     <see cref="Patches" />.
        ///     解析 <paramref name="target" />，通过 <see cref="PatchTargetMethodResolver" />，并将结果追加到
        ///     <see cref="Patches" />。
        /// </summary>
        public DynamicPatchBuilder Add(
            ModPatchTarget target,
            HarmonyMethod? prefix = null,
            HarmonyMethod? postfix = null,
            HarmonyMethod? transpiler = null,
            HarmonyMethod? finalizer = null,
            bool isCritical = true,
            string? description = null,
            string? patchId = null)
        {
            ArgumentNullException.ThrowIfNull(target);

            var originalMethod = PatchTargetMethodResolver.ResolveRequired(target);
            var resolvedPatchId = patchId ??
                                  $"{IdPrefix}_{++_counter:D3}_{target.TargetType.Name}_{target.MethodName}";
            _patches.Add(new(
                resolvedPatchId,
                originalMethod,
                prefix,
                postfix,
                transpiler,
                finalizer,
                isCritical,
                description ?? $"Patch {target}"));

            return this;
        }

        /// <summary>
        ///     Resolves a property getter on <paramref name="targetType" /> and appends it to <see cref="Patches" />.
        ///     解析 <paramref name="targetType" /> 上的属性 getter，并将其追加到 <see cref="Patches" />。
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
        ///     Legacy overload: resolves with <see cref="MethodType.Normal" /> (same behavior as before
        ///     <see cref="HarmonyLib.MethodType" /> was exposed on this API).
        ///     旧版重载：使用 <see cref="MethodType.Normal" /> 解析（与此 API 暴露
        ///     <see cref="HarmonyLib.MethodType" /> 之前的行为相同）。
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
            return AddMethod(
                targetType,
                methodName,
                parameterTypes,
                prefix,
                postfix,
                transpiler,
                finalizer,
                isCritical,
                description,
                patchId,
                MethodType.Normal);
        }

        /// <summary>
        ///     Resolves a method on <paramref name="targetType" /> (optionally by <paramref name="parameterTypes" />)
        ///     using <paramref name="harmonyMethodType" /> (same semantics as <see cref="ModPatchTarget.HarmonyMethodType" />),
        ///     then appends it to <see cref="Patches" />.
        ///     解析 <paramref name="targetType" /> 上的方法（可选按 <paramref name="parameterTypes" />），
        ///     使用 <paramref name="harmonyMethodType" />（语义与 <see cref="ModPatchTarget.HarmonyMethodType" /> 相同），
        ///     然后将其追加到 <see cref="Patches" />。
        /// </summary>
        public DynamicPatchBuilder AddMethod(
            Type targetType,
            string methodName,
            Type[]? parameterTypes,
            HarmonyMethod? prefix,
            HarmonyMethod? postfix,
            HarmonyMethod? transpiler,
            HarmonyMethod? finalizer,
            bool isCritical,
            string? description,
            string? patchId,
            MethodType harmonyMethodType)
        {
            ArgumentNullException.ThrowIfNull(targetType);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            var method = PatchTargetMethodResolver.Resolve(targetType, methodName, parameterTypes, harmonyMethodType);
            if (method == null)
                throw new MissingMethodException(targetType.FullName, $"{methodName} ({harmonyMethodType})");

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
        ///     将 <paramref name="patchType" /> 上的静态 patch 方法包装为 <see cref="HarmonyMethod" />。
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
