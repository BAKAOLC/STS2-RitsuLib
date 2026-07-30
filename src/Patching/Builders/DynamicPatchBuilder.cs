using System.Reflection;
using HarmonyLib;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Patching.Builders
{
    /// <summary>
    ///     <para xml:lang="en">Provides a fluent builder for Harmony patches whose targets are resolved at runtime.</para>
    ///     <para xml:lang="zh-CN">提供流式构建器，用于创建在运行时解析目标的 Harmony 补丁。</para>
    /// </summary>
    /// <param name="idPrefix">
    ///     <para xml:lang="en">Prefix used for generated patch IDs when an overload receives no <c>patchId</c>.</para>
    ///     <para xml:lang="zh-CN">重载未收到 <c>patchId</c> 时，用于生成补丁 ID 的前缀。</para>
    /// </param>
    public sealed class DynamicPatchBuilder(string idPrefix)
    {
        private readonly List<DynamicPatchInfo> _patches = [];
        private int _counter;

        /// <summary>
        ///     <para xml:lang="en">Gets the prefix used for generated patch IDs.</para>
        ///     <para xml:lang="zh-CN">获取用于生成补丁 ID 的前缀。</para>
        /// </summary>
        public string IdPrefix { get; } = idPrefix;

        /// <summary>
        ///     <para xml:lang="en">Gets the patches accumulated by this builder. They are not applied automatically.</para>
        ///     <para xml:lang="zh-CN">获取此构建器已收集的补丁。这些补丁不会自动应用。</para>
        /// </summary>
        public IReadOnlyList<DynamicPatchInfo> Patches => _patches;

        /// <summary>
        ///     <para xml:lang="en">Adds a patch for <paramref name="originalMethod" />.</para>
        ///     <para xml:lang="zh-CN">添加以 <paramref name="originalMethod" /> 为目标的补丁。</para>
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
        ///     <para xml:lang="en">Resolves <paramref name="target" /> and adds a patch for the resulting method.</para>
        ///     <para xml:lang="zh-CN">解析 <paramref name="target" />，并为解析得到的方法添加补丁。</para>
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
        ///     <para xml:lang="en">Resolves a property getter on <paramref name="targetType" /> and adds a patch for it.</para>
        ///     <para xml:lang="zh-CN">解析 <paramref name="targetType" /> 上的属性 getter，并为其添加补丁。</para>
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

            var getter = FindDeclaredPropertyGetter(targetType, propertyName)
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
        ///     <para xml:lang="en">Resolves a normal method on <paramref name="targetType" /> and adds a patch for it.</para>
        ///     <para xml:lang="zh-CN">解析 <paramref name="targetType" /> 上的普通方法，并为其添加补丁。</para>
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
        ///     <para xml:lang="en">
        ///         Resolves a method on <paramref name="targetType" /> using <paramref name="parameterTypes" /> and
        ///         <paramref name="harmonyMethodType" />, then adds a patch for it.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="parameterTypes" /> 和 <paramref name="harmonyMethodType" /> 解析
        ///         <paramref name="targetType" /> 上的方法，并为其添加补丁。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Attempts to resolve and add a method patch. Returns <see langword="false" /> without modifying the builder
        ///         when the target type is <see langword="null" /> or the method cannot be resolved.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试解析并添加方法补丁。目标类型为 <see langword="null" /> 或无法解析方法时返回
        ///         <see langword="false" />，且不修改构建器。
        ///     </para>
        /// </summary>
        public bool TryAddMethod(
            Type? targetType,
            string methodName,
            Type[]? parameterTypes = null,
            HarmonyMethod? prefix = null,
            HarmonyMethod? postfix = null,
            HarmonyMethod? transpiler = null,
            HarmonyMethod? finalizer = null,
            bool isCritical = false,
            string? description = null,
            string? patchId = null,
            MethodType harmonyMethodType = MethodType.Normal)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            if (targetType == null)
                return false;

            var method = PatchTargetMethodResolver.Resolve(targetType, methodName, parameterTypes, harmonyMethodType);
            if (method == null)
                return false;

            Add(
                method,
                prefix,
                postfix,
                transpiler,
                finalizer,
                isCritical,
                description ?? $"Patch method {targetType.Name}.{methodName}",
                patchId);
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves <paramref name="targetTypeName" /> with <see cref="AccessTools.TypeByName" /> and attempts to
        ///         add a method patch. Returns <see langword="false" /> when the type or method cannot be resolved.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <see cref="AccessTools.TypeByName" /> 解析 <paramref name="targetTypeName" /> 并尝试添加方法补丁。
        ///         无法解析类型或方法时返回 <see langword="false" />。
        ///     </para>
        /// </summary>
        public bool TryAddMethodByName(
            string targetTypeName,
            string methodName,
            Type[]? parameterTypes = null,
            HarmonyMethod? prefix = null,
            HarmonyMethod? postfix = null,
            HarmonyMethod? transpiler = null,
            HarmonyMethod? finalizer = null,
            bool isCritical = false,
            string? description = null,
            string? patchId = null,
            MethodType harmonyMethodType = MethodType.Normal)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(targetTypeName);

            return TryAddMethod(
                AccessTools.TypeByName(targetTypeName),
                methodName,
                parameterTypes,
                prefix,
                postfix,
                transpiler,
                finalizer,
                isCritical,
                description,
                patchId,
                harmonyMethodType);
        }

        /// <summary>
        ///     <para xml:lang="en">Wraps a static patch method on <paramref name="patchType" /> in a <see cref="HarmonyMethod" />.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="patchType" /> 上的静态补丁方法包装为 <see cref="HarmonyMethod" />。</para>
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

        private static MethodInfo? FindDeclaredPropertyGetter(Type targetType, string propertyName)
        {
            for (var walk = targetType; walk != null; walk = walk.BaseType)
            {
                var property = walk.GetProperty(
                    propertyName,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);
                if (property?.GetMethod is { IsAbstract: false } getter)
                    return getter;
            }

            return null;
        }
    }
}
