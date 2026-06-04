using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
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
    ///     Optional model capability that contributes dynamic vars for any model text surface.
    ///     可选能力：为任意模型文本 surface 贡献动态变量。
    /// </summary>
    public interface IModelDynamicVarContributor
    {
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
                dynamicVars?.AddTo(locString);
            }
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
                return collection.Count == 0 ? [] : collection.All.ToArray();
            if (!ModelCapabilityDefaults.HasDefaultCapabilitySource(model))
                return [];

            collection = ModelCapabilities.Get(model);

            return collection.Count == 0 ? [] : collection.All.ToArray();
        }
    }
}
