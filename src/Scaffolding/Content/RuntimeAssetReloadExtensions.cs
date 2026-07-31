using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">Provides model extension methods for requesting runtime visual reloads.</para>
    ///     <para xml:lang="zh-CN">提供用于请求运行时视觉重载的模型扩展方法。</para>
    /// </summary>
    public static class RuntimeAssetReloadExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">Requests card-node reloads matching this card by reference or ID.</para>
        ///     <para xml:lang="zh-CN">请求重新加载引用或 ID 与此卡牌匹配的卡牌节点。</para>
        /// </summary>
        public static void RequestVisualReload(this CardModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            RuntimeAssetRefreshCoordinator.RequestCardsWhere(candidate =>
                ReferenceEquals(candidate, model) || candidate.Id == model.Id);
        }

        /// <summary>
        ///     <para xml:lang="en">Requests relic-node reloads matching this relic by reference or ID.</para>
        ///     <para xml:lang="zh-CN">请求重新加载引用或 ID 与此遗物匹配的遗物节点。</para>
        /// </summary>
        public static void RequestVisualReload(this RelicModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            RuntimeAssetRefreshCoordinator.RequestRelicsWhere(candidate =>
                ReferenceEquals(candidate, model) || candidate.Id == model.Id);
        }

        /// <summary>
        ///     <para xml:lang="en">Requests potion-node reloads matching this potion by reference or ID.</para>
        ///     <para xml:lang="zh-CN">请求重新加载引用或 ID 与此药水匹配的药水节点。</para>
        /// </summary>
        public static void RequestVisualReload(this PotionModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            RuntimeAssetRefreshCoordinator.RequestPotionsWhere(candidate =>
                ReferenceEquals(candidate, model) || candidate.Id == model.Id);
        }

        /// <summary>
        ///     <para xml:lang="en">Requests power-node reloads matching this power by reference or ID.</para>
        ///     <para xml:lang="zh-CN">请求重新加载引用或 ID 与此能力匹配的能力节点。</para>
        /// </summary>
        public static void RequestVisualReload(this PowerModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            RuntimeAssetRefreshCoordinator.RequestPowersWhere(candidate =>
                ReferenceEquals(candidate, model) || candidate.Id == model.Id);
        }

        /// <summary>
        ///     <para xml:lang="en">Requests visual updates for orb nodes matching this orb by reference or ID.</para>
        ///     <para xml:lang="zh-CN">请求更新引用或 ID 与此充能球匹配的充能球节点视觉效果。</para>
        /// </summary>
        public static void RequestVisualReload(this OrbModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            RuntimeAssetRefreshCoordinator.RequestOrbsWhere(candidate =>
                ReferenceEquals(candidate, model) || candidate.Id == model.Id);
        }
    }
}
