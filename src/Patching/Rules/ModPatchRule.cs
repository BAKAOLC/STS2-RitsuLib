using System.Reflection;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Patching.Rules
{
    /// <summary>
    ///     Declarative rule: select types and methods in an assembly and emit <see cref="ModPatchInfo" /> rows for one patch
    ///     type.
    ///     声明式规则：在程序集中选择类型和方法，并为一个 patch 类型生成 <see cref="ModPatchInfo" /> 行。
    /// </summary>
    public class ModPatchRule
    {
        /// <summary>
        ///     Rule id prefix used when generating patch ids.
        ///     生成 patch id 时使用的规则 id 前缀。
        /// </summary>
        public string Id { get; init; } = "";

        /// <summary>
        ///     Predicate that filters candidate declaring types.
        ///     过滤候选声明类型的谓词。
        /// </summary>
        public Func<Type, bool> TypeSelector { get; init; } = _ => false;

        /// <summary>
        ///     Predicate that filters methods on matched types.
        ///     过滤匹配类型上的方法的谓词。
        /// </summary>
        public Func<MethodInfo, bool> MethodSelector { get; init; } = _ => false;

        /// <summary>
        ///     Static patch type whose Harmony methods are applied to each match.
        ///     其 Harmony 方法会应用到每个匹配项的静态 patch 类型。
        /// </summary>
        public Type? PatchType { get; init; }

        /// <summary>
        ///     Whether generated patches are critical.
        ///     生成的 patch 是否为关键 patch。
        /// </summary>
        public bool IsCritical { get; init; } = true;

        /// <summary>
        ///     Base description appended to each generated patch.
        ///     追加到每个生成 patch 的基础描述。
        /// </summary>
        public string Description { get; init; } = "";

        /// <summary>
        ///     Scans <paramref name="assembly" /> and returns one <see cref="ModPatchInfo" /> per selected method.
        ///     扫描 <paramref name="assembly" />，并为每个选中的方法返回一个 <see cref="ModPatchInfo" />。
        /// </summary>
        public ModPatchInfo[] GeneratePatches(Assembly assembly)
        {
            if (PatchType == null)
                throw new InvalidOperationException("PatchType must be set before generating patches");

            var types = assembly.GetTypes()
                .Where(TypeSelector)
                .OrderBy(static t => t.FullName ?? t.Name, StringComparer.Ordinal);

            return
            [
                .. from type in types
                let methods = type
                    .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                BindingFlags.NonPublic)
                    .Where(MethodSelector)
                    .OrderBy(static m => m.Name, StringComparer.Ordinal)
                    .ThenBy(static m => m.ToString(), StringComparer.Ordinal)
                from method in methods
                let parameterTypes = method.GetParameters().Select(static p => p.ParameterType).ToArray()
                select new ModPatchInfo(
                    $"{Id}_{type.Name}_{method.Name}_{FormatPatchIdSignature(parameterTypes)}",
                    type,
                    method.Name,
                    PatchType,
                    IsCritical,
                    $"{Description} -> {type.Name}.{FormatDescriptionSignature(method)}",
                    parameterTypes),
            ];
        }

        /// <summary>
        ///     Merges <see cref="GeneratePatches(Assembly)" /> across multiple assemblies.
        ///     跨多个程序集合并 <see cref="GeneratePatches(Assembly)" />。
        /// </summary>
        public ModPatchInfo[] GeneratePatches(params ReadOnlySpan<Assembly> assemblies)
        {
            var result = new List<ModPatchInfo>();
            foreach (var assembly in assemblies)
                result.AddRange(GeneratePatches(assembly));
            return [.. result];
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Rule: {Id} - {Description}";
        }

        private static string FormatDescriptionSignature(MethodInfo method)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
                return $"{method.Name}()";

            var signature = string.Join(", ", parameters.Select(static p => p.ParameterType.Name));
            return $"{method.Name}({signature})";
        }

        private static string FormatPatchIdSignature(IReadOnlyList<Type> parameterTypes)
        {
            if (parameterTypes.Count == 0)
                return "NoArgs";

            return string.Join("_", parameterTypes.Select(static type =>
                new string(GetStableTypeName(type).Select(static ch =>
                    char.IsLetterOrDigit(ch) ? ch : '_').ToArray()).Trim('_')));
        }

        private static string GetStableTypeName(Type type)
        {
            if (!type.IsGenericType)
                return type.FullName ?? type.Name;

            var genericTypeName = type.GetGenericTypeDefinition().FullName ?? type.Name;
            var genericArguments = string.Join("_", type.GetGenericArguments().Select(GetStableTypeName));
            return $"{genericTypeName}_{genericArguments}";
        }
    }

    /// <summary>
    ///     Fluent builder for <see cref="ModPatchRule" />.
    ///     <see cref="ModPatchRule" /> 的流式构建器。
    /// </summary>
    public class PatchRuleBuilder
    {
        private string _description = "";
        private string _id = "";
        private bool _isCritical = true;
        private Func<MethodInfo, bool> _methodSelector = _ => false;
        private Type? _patchType;
        private Func<Type, bool> _typeSelector = _ => false;

        /// <summary>
        ///     Starts a rule with the given id prefix.
        ///     使用给定 id 前缀开始一条规则。
        /// </summary>
        public static PatchRuleBuilder Create(string id)
        {
            return new() { _id = id };
        }

        /// <summary>
        ///     Sets the type filter.
        ///     设置类型过滤器。
        /// </summary>
        public PatchRuleBuilder ForTypes(Func<Type, bool> selector)
        {
            _typeSelector = selector;
            return this;
        }

        /// <summary>
        ///     Sets the method filter.
        ///     设置方法过滤器。
        /// </summary>
        public PatchRuleBuilder ForMethods(Func<MethodInfo, bool> selector)
        {
            _methodSelector = selector;
            return this;
        }

        /// <summary>
        ///     Sets the patch type applied to each match.
        ///     设置应用到每个匹配项的 patch 类型。
        /// </summary>
        public PatchRuleBuilder WithPatch(Type patchType)
        {
            _patchType = patchType;
            return this;
        }

        /// <summary>
        ///     Sets whether generated patches are critical (default true).
        ///     设置生成的 patch 是否为关键 patch（默认 true）。
        /// </summary>
        public PatchRuleBuilder Critical(bool isCritical = true)
        {
            _isCritical = isCritical;
            return this;
        }

        /// <summary>
        ///     Sets the rule description prefix.
        ///     设置规则描述前缀。
        /// </summary>
        public PatchRuleBuilder WithDescription(string description)
        {
            _description = description;
            return this;
        }

        /// <summary>
        ///     Materializes the rule.
        ///     实体化规则。
        /// </summary>
        public ModPatchRule Build()
        {
            return new()
            {
                Id = _id,
                TypeSelector = _typeSelector,
                MethodSelector = _methodSelector,
                PatchType = _patchType,
                IsCritical = _isCritical,
                Description = _description,
            };
        }
    }
}
