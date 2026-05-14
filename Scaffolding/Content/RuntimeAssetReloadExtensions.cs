using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Public model-oriented helpers for runtime visual reload requests.
    ///     面向公共模型的运行时视觉重载请求辅助方法。
    /// </summary>
    public static class RuntimeAssetReloadExtensions
    {
        /// <summary>
        ///     Requests card-node reloads for this card instance (reference or id match).
        ///     为此卡牌实例请求卡牌节点重载（按引用或 id 匹配）。
        /// </summary>
        public static void RequestVisualReload(this CardModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            RuntimeAssetRefreshCoordinator.RequestCardsWhere(candidate =>
                ReferenceEquals(candidate, model) || candidate.Id == model.Id);
        }

        /// <summary>
        ///     Requests relic-node reloads for this relic instance (reference or id match).
        ///     为此遗物实例请求遗物节点重载（按引用或 id 匹配）。
        /// </summary>
        public static void RequestVisualReload(this RelicModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            RuntimeAssetRefreshCoordinator.RequestRelicsWhere(candidate =>
                ReferenceEquals(candidate, model) || candidate.Id == model.Id);
        }

        /// <summary>
        ///     Requests potion-node reloads for this potion instance (reference or id match).
        ///     为此药水实例请求药水节点重载（按引用或 id 匹配）。
        /// </summary>
        public static void RequestVisualReload(this PotionModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            RuntimeAssetRefreshCoordinator.RequestPotionsWhere(candidate =>
                ReferenceEquals(candidate, model) || candidate.Id == model.Id);
        }

        /// <summary>
        ///     Requests power-node reloads for this power instance (reference or id match).
        ///     为此能力实例请求能力节点重载（按引用或 id 匹配）。
        /// </summary>
        public static void RequestVisualReload(this PowerModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            RuntimeAssetRefreshCoordinator.RequestPowersWhere(candidate =>
                ReferenceEquals(candidate, model) || candidate.Id == model.Id);
        }

        /// <summary>
        ///     Requests orb-node visual updates for this orb instance (reference or id match).
        ///     为此充能球实例请求充能球节点视觉更新（按引用或 id 匹配）。
        /// </summary>
        public static void RequestVisualReload(this OrbModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            RuntimeAssetRefreshCoordinator.RequestOrbsWhere(candidate =>
                ReferenceEquals(candidate, model) || candidate.Id == model.Id);
        }
    }
}
