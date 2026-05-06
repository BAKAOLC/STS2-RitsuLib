using System.Reflection;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Patching.Rules
{
    /// <summary>
    ///     Declarative rule: select types and methods in an assembly and emit <see cref="ModPatchInfo" /> rows for one patch
    ///     type.
    /// </summary>
    public class ModPatchRule
    {
        /// <summary>
        ///     Rule id prefix used when generating patch ids.
        /// </summary>
        public string Id { get; init; } = "";

        /// <summary>
        ///     Predicate that filters candidate declaring types.
        /// </summary>
        public Func<Type, bool> TypeSelector { get; init; } = _ => false;

        /// <summary>
        ///     Predicate that filters methods on matched types.
        /// </summary>
        public Func<MethodInfo, bool> MethodSelector { get; init; } = _ => false;

        /// <summary>
        ///     Static patch type whose Harmony methods are applied to each match.
        /// </summary>
        public Type? PatchType { get; init; }

        /// <summary>
        ///     Whether generated patches are critical.
        /// </summary>
        public bool IsCritical { get; init; } = true;

        /// <summary>
        ///     Base description appended to each generated patch.
        /// </summary>
        public string Description { get; init; } = "";

        /// <summary>
        ///     Scans <paramref name="assembly" /> and returns one <see cref="ModPatchInfo" /> per selected method.
        /// </summary>
        public ModPatchInfo[] GeneratePatches(Assembly assembly)
        {
            if (PatchType == null)
                throw new InvalidOperationException("PatchType must be set before generating patches");

            var types = assembly.GetTypes()
                .Where(TypeSelector)
                .OrderBy(static t => t.FullName ?? t.Name, StringComparer.Ordinal);

            return (from type in types
                let methods = type
                    .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                BindingFlags.NonPublic)
                    .Where(MethodSelector)
                    .OrderBy(static m => m.Name, StringComparer.Ordinal)
                    .ThenBy(static m => m.ToString(), StringComparer.Ordinal)
                from method in methods
                select new ModPatchInfo($"{Id}_{type.Name}_{method.Name}", type, method.Name, PatchType, IsCritical,
                    $"{Description} -> {type.Name}.{method.Name}")).ToArray();
        }

        /// <summary>
        ///     Merges <see cref="GeneratePatches(Assembly)" /> across multiple assemblies.
        /// </summary>
        public ModPatchInfo[] GeneratePatches(params ReadOnlySpan<Assembly> assemblies)
        {
            var result = new List<ModPatchInfo>();
            foreach (var assembly in assemblies)
                result.AddRange(GeneratePatches(assembly));
            return [..result];
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Rule: {Id} - {Description}";
        }
    }

    /// <summary>
    ///     Fluent builder for <see cref="ModPatchRule" />.
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
        /// </summary>
        public static PatchRuleBuilder Create(string id)
        {
            return new() { _id = id };
        }

        /// <summary>
        ///     Sets the type filter.
        /// </summary>
        public PatchRuleBuilder ForTypes(Func<Type, bool> selector)
        {
            _typeSelector = selector;
            return this;
        }

        /// <summary>
        ///     Sets the method filter.
        /// </summary>
        public PatchRuleBuilder ForMethods(Func<MethodInfo, bool> selector)
        {
            _methodSelector = selector;
            return this;
        }

        /// <summary>
        ///     Sets the patch type applied to each match.
        /// </summary>
        public PatchRuleBuilder WithPatch(Type patchType)
        {
            _patchType = patchType;
            return this;
        }

        /// <summary>
        ///     Sets whether generated patches are critical (default true).
        /// </summary>
        public PatchRuleBuilder Critical(bool isCritical = true)
        {
            _isCritical = isCritical;
            return this;
        }

        /// <summary>
        ///     Sets the rule description prefix.
        /// </summary>
        public PatchRuleBuilder WithDescription(string description)
        {
            _description = description;
            return this;
        }

        /// <summary>
        ///     Materializes the rule.
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
