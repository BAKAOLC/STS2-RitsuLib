using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Public model-oriented helpers for runtime visual reload requests.
    /// </summary>
    public static class RuntimeAssetReloadExtensions
    {
        /// <summary>
        ///     Requests card-node reloads for this card instance (reference or id match).
        /// </summary>
        public static void RequestVisualReload(this CardModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            RuntimeAssetRefreshCoordinator.RequestCardsWhere(candidate =>
                ReferenceEquals(candidate, model) || candidate.Id == model.Id);
        }

        /// <summary>
        ///     Requests relic-node reloads for this relic instance (reference or id match).
        /// </summary>
        public static void RequestVisualReload(this RelicModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            RuntimeAssetRefreshCoordinator.RequestRelicsWhere(candidate =>
                ReferenceEquals(candidate, model) || candidate.Id == model.Id);
        }

        /// <summary>
        ///     Requests potion-node reloads for this potion instance (reference or id match).
        /// </summary>
        public static void RequestVisualReload(this PotionModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            RuntimeAssetRefreshCoordinator.RequestPotionsWhere(candidate =>
                ReferenceEquals(candidate, model) || candidate.Id == model.Id);
        }

        /// <summary>
        ///     Requests power-node reloads for this power instance (reference or id match).
        /// </summary>
        public static void RequestVisualReload(this PowerModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            RuntimeAssetRefreshCoordinator.RequestPowersWhere(candidate =>
                ReferenceEquals(candidate, model) || candidate.Id == model.Id);
        }

        /// <summary>
        ///     Requests orb-node visual updates for this orb instance (reference or id match).
        /// </summary>
        public static void RequestVisualReload(this OrbModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            RuntimeAssetRefreshCoordinator.RequestOrbsWhere(candidate =>
                ReferenceEquals(candidate, model) || candidate.Id == model.Id);
        }
    }
}
