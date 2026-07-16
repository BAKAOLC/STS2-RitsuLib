using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Stable LocString variable naming for capability-owned dynamic vars used on shared model text surfaces.
    ///     共享模型文本 surface 中能力自有动态变量使用的稳定 LocString 变量命名。
    /// </summary>
    public static class ModelCapabilityDynamicVarNames
    {
        /// <summary>
        ///     Root selector used for capability-scoped variables.
        ///     能力作用域变量使用的根 selector。
        /// </summary>
        public const string RootName = "Capabilities";

        /// <summary>
        ///     Returns a selector-safe scope name. <paramref name="requestedScope" /> takes precedence over the
        ///     capability id. Characters that are not letters, digits, or underscores are replaced with underscores.
        ///     返回 selector 安全的 scope 名称。<paramref name="requestedScope" /> 优先于 capability ID；
        ///     字母、数字和下划线以外的字符会替换为下划线。
        /// </summary>
        public static string GetScopeName(string capabilityId, string? requestedScope = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

            return NormalizeSegment(string.IsNullOrWhiteSpace(requestedScope) ? capabilityId : requestedScope);
        }

        /// <summary>
        ///     Returns a selector-safe dynamic-var name within a capability scope.
        ///     返回能力 scope 内 selector 安全的动态变量名称。
        /// </summary>
        public static string GetVariableName(string dynamicVarName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(dynamicVarName);
            return NormalizeSegment(dynamicVarName);
        }

        private static string NormalizeSegment(string value)
        {
            return new(value.Select(static character =>
                char.IsAsciiLetterOrDigit(character) || character == '_' ? character : '_').ToArray());
        }
    }

    /// <summary>
    ///     Known model asset path query scopes used by framework adapters.
    ///     框架 adapter 使用的已知模型资源路径查询作用域。
    /// </summary>
    public enum ModelAssetPathScope
    {
        /// <summary>
        ///     General model assets.
        ///     通用模型资源。
        /// </summary>
        General,

        /// <summary>
        ///     Assets needed while a run is active.
        ///     跑局中需要的资源。
        /// </summary>
        Run,

        /// <summary>
        ///     Assets needed by combat-facing views.
        ///     战斗侧视图需要的资源。
        /// </summary>
        Combat,

        /// <summary>
        ///     Assets needed by map or route views.
        ///     地图或路线视图需要的资源。
        /// </summary>
        Map,

        /// <summary>
        ///     Assets needed by character selection views.
        ///     选角视图需要的资源。
        /// </summary>
        CharacterSelect,
    }

    /// <summary>
    ///     Context passed to model asset path capabilities.
    ///     传给模型资源路径能力的上下文。
    /// </summary>
    public readonly record struct ModelAssetPathContext(
        AbstractModel Model,
        ModelAssetPathScope Scope,
        object? RuntimeContext = null);

    /// <summary>
    ///     Optional model capability that contributes a capability-owned dynamic-var set to supported model text
    ///     surfaces. The returned set remains separate from the owning model's own dynamic vars. Capability variables
    ///     are available through <c>{Capabilities.Scope.Variable}</c>. Unscoped short names remain compatibility aliases
    ///     and must not be used when more than one contributor can provide the same name.
    ///     可选能力：向支持的模型文本 surface 贡献能力自有动态变量集合。该集合与 owner 模型自身的动态变量保持分离；
    ///     能力变量可通过 <c>{Capabilities.Scope.Variable}</c> 使用。无 scope 的短名称仅作为兼容别名保留；
    ///     多个 contributor 可能提供同名变量时，不应使用短名称。
    /// </summary>
    public interface IModelDynamicVarContributor
    {
        /// <summary>
        ///     Optional stable selector scope used by localized text. For example, scope <c>Burning</c> and variable
        ///     <c>Damage</c> are addressed as <c>{Capabilities.Burning.Damage}</c>. Distinct instances that must be
        ///     addressed separately should return distinct stable scopes.
        ///     本地化文本使用的可选稳定 selector scope。例如 scope 为 <c>Burning</c>、变量为 <c>Damage</c> 时，
        ///     使用 <c>{Capabilities.Burning.Damage}</c>。需要分别寻址的不同实例应返回不同且稳定的 scope。
        /// </summary>
        string? LocStringVariableScope => null;

        /// <summary>
        ///     Returns the capability-owned dynamic-var set for <paramref name="model" />.
        ///     返回 <paramref name="model" /> 对应的能力自有动态变量集合。
        /// </summary>
        DynamicVarSet GetDynamicVars(AbstractModel model);
    }

    /// <summary>
    ///     Optional model capability that contributes hover tips for any model.
    ///     可选能力：为任意模型贡献悬停提示。
    /// </summary>
    public interface IModelHoverTipContributor
    {
        /// <summary>
        ///     Returns additional hover tips for <paramref name="model" />.
        ///     返回 <paramref name="model" /> 的额外悬停提示。
        /// </summary>
        IEnumerable<IHoverTip> GetHoverTips(AbstractModel model);
    }

    /// <summary>
    ///     Optional typed model capability that contributes hover tips for <typeparamref name="TModel" />.
    ///     可选类型化能力：为 <typeparamref name="TModel" /> 贡献悬停提示。
    /// </summary>
    public interface IModelHoverTipContributor<in TModel> where TModel : AbstractModel
    {
        /// <summary>
        ///     Returns additional hover tips for <paramref name="model" />.
        ///     返回 <paramref name="model" /> 的额外悬停提示。
        /// </summary>
        IEnumerable<IHoverTip> GetHoverTips(TModel model);
    }

    /// <summary>
    ///     Optional model capability that contributes asset paths for any model.
    ///     可选能力：为任意模型贡献资源路径。
    /// </summary>
    public interface IModelAssetPathContributor
    {
        /// <summary>
        ///     Returns additional asset paths.
        ///     返回额外资源路径。
        /// </summary>
        IEnumerable<string> GetAssetPaths(ModelAssetPathContext context);
    }

    /// <summary>
    ///     Optional typed model capability that contributes asset paths for <typeparamref name="TModel" />.
    ///     可选类型化能力：为 <typeparamref name="TModel" /> 贡献资源路径。
    /// </summary>
    public interface IModelAssetPathContributor<in TModel> where TModel : AbstractModel
    {
        /// <summary>
        ///     Returns additional asset paths.
        ///     返回额外资源路径。
        /// </summary>
        IEnumerable<string> GetAssetPaths(TModel model, ModelAssetPathContext context);
    }

    internal static partial class ModelCapabilityHost
    {
        private const string ModelHoverTipsSurface = "model display/hover-tips";
        private const string ModelAssetPathsSurface = "model asset/paths";
        private const string ModelDynamicVarsSurface = "model dynamic-var/add-to-loc-string";

        internal static IEnumerable<IHoverTip> GetHoverTips<TModel>(TModel model)
            where TModel : AbstractModel
        {
            foreach (var capability in GetCapabilitySnapshot(model))
                switch (capability)
                {
                    case IModelHoverTipContributor general:
                    {
                        IEnumerable<IHoverTip> tips = [];
                        TryRun(capability, model, ModelHoverTipsSurface, () => tips = general.GetHoverTips(model));
                        foreach (var tip in tips)
                            yield return tip;
                        break;
                    }
                    case IModelHoverTipContributor<TModel> typed:
                    {
                        IEnumerable<IHoverTip> tips = [];
                        TryRun(capability, model, ModelHoverTipsSurface, () => tips = typed.GetHoverTips(model));
                        foreach (var tip in tips)
                            yield return tip;
                        break;
                    }
                }
        }

        internal static IEnumerable<string> GetAssetPaths<TModel>(TModel model, ModelAssetPathContext context)
            where TModel : AbstractModel
        {
            foreach (var capability in GetCapabilitySnapshot(model))
                switch (capability)
                {
                    case IModelAssetPathContributor general:
                    {
                        IEnumerable<string> paths = [];
                        TryRun(capability, model, ModelAssetPathsSurface, () => paths = general.GetAssetPaths(context));
                        foreach (var path in paths)
                            yield return path;
                        break;
                    }
                    case IModelAssetPathContributor<TModel> typed:
                    {
                        IEnumerable<string> paths = [];
                        TryRun(capability, model, ModelAssetPathsSurface,
                            () => paths = typed.GetAssetPaths(model, context));
                        foreach (var path in paths)
                            yield return path;
                        break;
                    }
                }
        }

        internal static IEnumerable<TCapability> GetCapabilities<TCapability>(AbstractModel model)
            where TCapability : class
        {
            foreach (var capability in GetCapabilitySnapshot(model))
                if (capability is TCapability typed)
                    yield return typed;
        }

        internal static void AddDynamicVarsTo(AbstractModel model, LocString locString)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(locString);

            foreach (var capability in GetCapabilitySnapshot(model))
            {
                if (capability is not IModelDynamicVarContributor dynamicVarCapability)
                    continue;

                DynamicVarSet? dynamicVars = null;
                TryRun(capability, model, ModelDynamicVarsSurface, () =>
                    dynamicVars = dynamicVarCapability.GetDynamicVars(model));
                if (dynamicVars != null)
                    AddDynamicVarsTo(
                        model,
                        capability,
                        dynamicVarCapability.LocStringVariableScope,
                        dynamicVars,
                        locString);
            }
        }

        internal static void AddDynamicVarsTo(
            AbstractModel model,
            IModelCapability capability,
            string? requestedScope,
            DynamicVarSet dynamicVars,
            LocString locString)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(capability);
            ArgumentNullException.ThrowIfNull(dynamicVars);
            ArgumentNullException.ThrowIfNull(locString);

            var energyPrefix = GetEnergyPrefix(model, locString);
            var scopedVariables = GetOrCreateScopedVariables(locString, capability.CapabilityId, requestedScope);
            foreach (var dynamicVar in dynamicVars.Values)
            {
                if (dynamicVar is EnergyVar energyVar && energyPrefix != null)
                    energyVar.ColorPrefix = energyPrefix;

                var scopedName = ModelCapabilityDynamicVarNames.GetVariableName(dynamicVar.Name);
                scopedVariables?.TryAdd(scopedName, dynamicVar);

                var normalizedShortName = dynamicVar.Name.Replace(' ', '-');
                if (!locString.Variables.ContainsKey(normalizedShortName))
                    locString.Add(dynamicVar);
            }
        }

        private static Dictionary<string, object>? GetOrCreateScopedVariables(
            LocString locString,
            string capabilityId,
            string? requestedScope)
        {
            Dictionary<string, object> scopes;
            if (locString.Variables.TryGetValue(ModelCapabilityDynamicVarNames.RootName, out var rootValue))
            {
                if (rootValue is not Dictionary<string, object> existingScopes)
                    return null;

                scopes = existingScopes;
            }
            else
            {
                scopes = new(StringComparer.Ordinal);
                locString.AddObj(ModelCapabilityDynamicVarNames.RootName, scopes);
            }

            var scopeName = ModelCapabilityDynamicVarNames.GetScopeName(capabilityId, requestedScope);
            if (scopes.TryGetValue(scopeName, out var scopeValue)) return scopeValue as Dictionary<string, object>;

            Dictionary<string, object> scopedVariables = new(StringComparer.Ordinal);
            scopes.Add(scopeName, scopedVariables);
            return scopedVariables;
        }

        private static string? GetEnergyPrefix(AbstractModel model, LocString locString)
        {
            if (locString.Variables.TryGetValue("energyPrefix", out var value) && value is string prefix)
                return prefix;

            return model switch
            {
                CardModel or EnchantmentModel or PotionModel or PowerModel or RelicModel =>
                    EnergyIconHelper.GetPrefix(model),
                AfflictionModel { HasCard: true } affliction => EnergyIconHelper.GetPrefix(affliction.Card),
                OrbModel { IsMutable: true } orb => orb.Owner.Character.CardPool.Title,
                _ => null,
            };
        }

        internal static void TryRun(
            IModelCapability capability,
            AbstractModel model,
            string surface,
            Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ModelCapabilityDiagnostics.WarnFailure(surface, model, capability, ex);
            }
        }

        private static IReadOnlyList<IModelCapability> GetCapabilitySnapshot(AbstractModel model)
        {
            if (ModelCapabilities.TryGet(model, out var collection))
                return collection.Count == 0 ? [] : collection.GetAttachedSnapshot();
            if (!ModelCapabilityDefaults.HasDefaultCapabilitySource(model))
                return [];

            collection = ModelCapabilities.Get(model);

            return collection.Count == 0 ? [] : collection.GetAttachedSnapshot();
        }
    }
}
