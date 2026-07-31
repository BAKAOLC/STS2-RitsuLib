using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Stable LocString variable names for capability-owned dynamic variables used in shared model
    ///         text.
    ///     </para>
    ///     <para xml:lang="zh-CN">共享模型文本中能力自有动态变量的稳定 LocString 变量名。</para>
    /// </summary>
    public static class ModelCapabilityDynamicVarNames
    {
        /// <summary>
        ///     <para xml:lang="en">Root selector for capability-scoped variables.</para>
        ///     <para xml:lang="zh-CN">能力作用域变量使用的根选择器。</para>
        /// </summary>
        public const string RootName = "Capabilities";

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a selector-safe scope name. <paramref name="requestedScope" /> takes precedence over the
        ///         capability ID. Characters other than letters, digits, and underscores are replaced with underscores.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回可安全用于选择器的作用域名称。<paramref name="requestedScope" /> 优先于能力 ID；
        ///         字母、数字和下划线以外的字符会替换为下划线。
        ///     </para>
        /// </summary>
        public static string GetScopeName(string capabilityId, string? requestedScope = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

            return NormalizeSegment(string.IsNullOrWhiteSpace(requestedScope) ? capabilityId : requestedScope);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a selector-safe dynamic-variable name within a capability scope.</para>
        ///     <para xml:lang="zh-CN">返回能力作用域内可安全用于选择器的动态变量名称。</para>
        /// </summary>
        public static string GetVariableName(string dynamicVarName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(dynamicVarName);
            return NormalizeSegment(dynamicVarName);
        }

        private static string NormalizeSegment(string value)
        {
            return new([
                .. value.Select(static character =>
                    char.IsAsciiLetterOrDigit(character) || character == '_' ? character : '_'),
            ]);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Known model asset-path query scopes used by framework adapters.</para>
    ///     <para xml:lang="zh-CN">框架适配器使用的已知模型资源路径查询作用域。</para>
    /// </summary>
    public enum ModelAssetPathScope
    {
        /// <summary>
        ///     <para xml:lang="en">General model assets.</para>
        ///     <para xml:lang="zh-CN">通用模型资源。</para>
        /// </summary>
        General,

        /// <summary>
        ///     <para xml:lang="en">Assets needed while a run is active.</para>
        ///     <para xml:lang="zh-CN">一局游戏进行期间需要的资源。</para>
        /// </summary>
        Run,

        /// <summary>
        ///     <para xml:lang="en">Assets needed by combat-facing views.</para>
        ///     <para xml:lang="zh-CN">战斗界面需要的资源。</para>
        /// </summary>
        Combat,

        /// <summary>
        ///     <para xml:lang="en">Assets needed by map or route views.</para>
        ///     <para xml:lang="zh-CN">地图或路线视图需要的资源。</para>
        /// </summary>
        Map,

        /// <summary>
        ///     <para xml:lang="en">Assets needed by character selection views.</para>
        ///     <para xml:lang="zh-CN">选角视图需要的资源。</para>
        /// </summary>
        CharacterSelect,
    }

    /// <summary>
    ///     <para xml:lang="en">Context passed to model asset path capabilities.</para>
    ///     <para xml:lang="zh-CN">传给模型资源路径能力的上下文。</para>
    /// </summary>
    public readonly record struct ModelAssetPathContext(
        AbstractModel Model,
        ModelAssetPathScope Scope,
        object? RuntimeContext = null);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Optional model capability that contributes a capability-owned dynamic-variable set to supported
    ///         model text surfaces. The set remains separate from the model's own dynamic variables. Access capability
    ///         variables through <c>{Capabilities.Scope.Variable}</c>; unscoped short names are compatibility aliases and must
    ///         not be used when multiple contributors can provide the same name.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可选的模型能力，可向受支持的模型文本呈现位置提供能力自有的动态变量集合。该集合与模型自身的动态变量保持分离。通过
    ///         <c>{Capabilities.Scope.Variable}</c> 访问能力变量；无作用域短名称仅是兼容别名，多个贡献者可能提供同名变量时不得使用。
    ///     </para>
    /// </summary>
    public interface IModelDynamicVarContributor
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional stable selector scope used by localized text. For example, scope <c>Burning</c> and variable
        ///         <c>Damage</c> are addressed as <c>{Capabilities.Burning.Damage}</c>. Distinct instances that must be
        ///         addressed separately should return distinct stable scopes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         本地化文本使用的可选稳定选择器作用域。例如作用域为 <c>Burning</c>、变量为 <c>Damage</c> 时，
        ///         使用 <c>{Capabilities.Burning.Damage}</c>。需要分别寻址的不同实例应返回不同且稳定的作用域。
        ///     </para>
        /// </summary>
        string? LocStringVariableScope => null;

        /// <summary>
        ///     <para xml:lang="en">Returns the capability-owned dynamic-var set for <paramref name="model" />.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="model" /> 对应的能力自有动态变量集合。</para>
        /// </summary>
        DynamicVarSet GetDynamicVars(AbstractModel model);
    }

    /// <summary>
    ///     <para xml:lang="en">Optional model capability that contributes hover tips for any model.</para>
    ///     <para xml:lang="zh-CN">可选能力：为任意模型贡献悬停提示。</para>
    /// </summary>
    public interface IModelHoverTipContributor
    {
        /// <summary>
        ///     <para xml:lang="en">Returns additional hover tips for <paramref name="model" />.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="model" /> 的额外悬停提示。</para>
        /// </summary>
        IEnumerable<IHoverTip> GetHoverTips(AbstractModel model);
    }

    /// <summary>
    ///     <para xml:lang="en">Optional typed model capability that contributes hover tips for <typeparamref name="TModel" />.</para>
    ///     <para xml:lang="zh-CN">可选类型化能力：为 <typeparamref name="TModel" /> 贡献悬停提示。</para>
    /// </summary>
    public interface IModelHoverTipContributor<in TModel> where TModel : AbstractModel
    {
        /// <summary>
        ///     <para xml:lang="en">Returns additional hover tips for <paramref name="model" />.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="model" /> 的额外悬停提示。</para>
        /// </summary>
        IEnumerable<IHoverTip> GetHoverTips(TModel model);
    }

    /// <summary>
    ///     <para xml:lang="en">Optional model capability that contributes asset paths for any model.</para>
    ///     <para xml:lang="zh-CN">可选能力：为任意模型贡献资源路径。</para>
    /// </summary>
    public interface IModelAssetPathContributor
    {
        /// <summary>
        ///     <para xml:lang="en">Returns additional asset paths.</para>
        ///     <para xml:lang="zh-CN">返回额外资源路径。</para>
        /// </summary>
        IEnumerable<string> GetAssetPaths(ModelAssetPathContext context);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Optional typed model capability that contributes asset paths for <typeparamref name="TModel" />
    ///         .
    ///     </para>
    ///     <para xml:lang="zh-CN">可选类型化能力：为 <typeparamref name="TModel" /> 贡献资源路径。</para>
    /// </summary>
    public interface IModelAssetPathContributor<in TModel> where TModel : AbstractModel
    {
        /// <summary>
        ///     <para xml:lang="en">Returns additional asset paths.</para>
        ///     <para xml:lang="zh-CN">返回额外资源路径。</para>
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
                        IReadOnlyList<IHoverTip> tips = [];
                        // ReSharper disable once AccessToModifiedClosure
                        TryRun(capability, model, ModelHoverTipsSurface,
                            () => tips = general.GetHoverTips(model)?.ToArray() ?? []);
                        foreach (var tip in tips)
                            yield return tip;
                        break;
                    }
                    case IModelHoverTipContributor<TModel> typed:
                    {
                        IReadOnlyList<IHoverTip> tips = [];
                        // ReSharper disable once AccessToModifiedClosure
                        TryRun(capability, model, ModelHoverTipsSurface,
                            () => tips = typed.GetHoverTips(model)?.ToArray() ?? []);
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
                        IReadOnlyList<string> paths = [];
                        // ReSharper disable once AccessToModifiedClosure
                        TryRun(capability, model, ModelAssetPathsSurface,
                            () => paths = general.GetAssetPaths(context)?.ToArray() ?? []);
                        foreach (var path in paths)
                            yield return path;
                        break;
                    }
                    case IModelAssetPathContributor<TModel> typed:
                    {
                        IReadOnlyList<string> paths = [];
                        // ReSharper disable once AccessToModifiedClosure
                        TryRun(capability, model, ModelAssetPathsSurface,
                            () => paths = typed.GetAssetPaths(model, context)?.ToArray() ?? []);
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
                TryRun(capability, model, ModelDynamicVarsSurface,
                    () => dynamicVars = dynamicVarCapability.GetDynamicVars(model));
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
