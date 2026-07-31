using System.Reflection;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Patching.Rules
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a rule that selects methods from assemblies and creates <see cref="ModPatchInfo" /> instances for them.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义从程序集中选择方法并为其创建 <see cref="ModPatchInfo" /> 实例的规则。
    ///     </para>
    /// </summary>
    public class ModPatchRule
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the prefix used for generated patch IDs.</para>
        ///     <para xml:lang="zh-CN">获取用于生成补丁 ID 的前缀。</para>
        /// </summary>
        public string Id { get; init; } = "";

        /// <summary>
        ///     <para xml:lang="en">Gets the predicate used to select declaring types.</para>
        ///     <para xml:lang="zh-CN">获取用于选择声明类型的谓词。</para>
        /// </summary>
        public Func<Type, bool> TypeSelector { get; init; } = _ => false;

        /// <summary>
        ///     <para xml:lang="en">Gets the predicate used to select methods on matched types.</para>
        ///     <para xml:lang="zh-CN">获取用于从匹配类型中选择方法的谓词。</para>
        /// </summary>
        public Func<MethodInfo, bool> MethodSelector { get; init; } = _ => false;

        /// <summary>
        ///     <para xml:lang="en">Gets the static patch type applied to each selected method.</para>
        ///     <para xml:lang="zh-CN">获取要应用到每个选中方法的静态补丁类型。</para>
        /// </summary>
        public Type? PatchType { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether generated patches are critical.</para>
        ///     <para xml:lang="zh-CN">获取生成的补丁是否属于严重补丁。</para>
        /// </summary>
        public bool IsCritical { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets the description prefix used for generated patches.</para>
        ///     <para xml:lang="zh-CN">获取生成补丁所使用的描述前缀。</para>
        /// </summary>
        public string Description { get; init; } = "";

        /// <summary>
        ///     <para xml:lang="en">Scans <paramref name="assembly" /> and creates one patch for each selected method.</para>
        ///     <para xml:lang="zh-CN">扫描 <paramref name="assembly" />，并为每个选中的方法创建一个补丁。</para>
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
        ///     <para xml:lang="en">Generates and combines patches from <paramref name="assemblies" />.</para>
        ///     <para xml:lang="zh-CN">从 <paramref name="assemblies" /> 生成并合并补丁。</para>
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
    ///     <para xml:lang="en">Provides a fluent builder for <see cref="ModPatchRule" />.</para>
    ///     <para xml:lang="zh-CN">提供 <see cref="ModPatchRule" /> 的流式构建器。</para>
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
        ///     <para xml:lang="en">Starts a rule with the specified ID prefix.</para>
        ///     <para xml:lang="zh-CN">使用指定的 ID 前缀开始构建规则。</para>
        /// </summary>
        public static PatchRuleBuilder Create(string id)
        {
            return new() { _id = id };
        }

        /// <summary>
        ///     <para xml:lang="en">Sets the type-selection predicate.</para>
        ///     <para xml:lang="zh-CN">设置类型选择谓词。</para>
        /// </summary>
        public PatchRuleBuilder ForTypes(Func<Type, bool> selector)
        {
            _typeSelector = selector;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Sets the method-selection predicate.</para>
        ///     <para xml:lang="zh-CN">设置方法选择谓词。</para>
        /// </summary>
        public PatchRuleBuilder ForMethods(Func<MethodInfo, bool> selector)
        {
            _methodSelector = selector;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Sets the patch type applied to each selected method.</para>
        ///     <para xml:lang="zh-CN">设置要应用到每个选中方法的补丁类型。</para>
        /// </summary>
        public PatchRuleBuilder WithPatch(Type patchType)
        {
            _patchType = patchType;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Sets whether generated patches are critical.</para>
        ///     <para xml:lang="zh-CN">设置生成的补丁是否属于严重补丁。</para>
        /// </summary>
        public PatchRuleBuilder Critical(bool isCritical = true)
        {
            _isCritical = isCritical;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Sets the description prefix used for generated patches.</para>
        ///     <para xml:lang="zh-CN">设置生成补丁所使用的描述前缀。</para>
        /// </summary>
        public PatchRuleBuilder WithDescription(string description)
        {
            _description = description;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Builds the configured rule.</para>
        ///     <para xml:lang="zh-CN">构建已配置的规则。</para>
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
