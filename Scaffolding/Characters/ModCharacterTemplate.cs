using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Characters
{
    public interface IModCharacterAssetOverrides
    {
        CharacterAssetProfile AssetProfile { get; }
        string? CustomVisualsPath { get; }
        string? CustomEnergyCounterPath { get; }
        string? CustomMerchantAnimPath { get; }
        string? CustomRestSiteAnimPath { get; }
        string? CustomIconTexturePath { get; }
        string? CustomIconOutlineTexturePath { get; }
        string? CustomIconPath { get; }
        string? CustomCharacterSelectBgPath { get; }
        string? CustomCharacterSelectIconPath { get; }
        string? CustomCharacterSelectLockedIconPath { get; }
        string? CustomCharacterSelectTransitionPath { get; }
        string? CustomMapMarkerPath { get; }
        string? CustomTrailPath { get; }
        CharacterTrailStyle? CustomTrailStyle { get; }
        string? CustomCombatSpineSkeletonDataPath { get; }
        string? CustomCharacterSelectSfx { get; }
        string? CustomCharacterTransitionSfx { get; }
        string? CustomAttackSfx { get; }
        string? CustomCastSfx { get; }
        string? CustomDeathSfx { get; }
        string? CustomArmPointingTexturePath { get; }
        string? CustomArmRockTexturePath { get; }
        string? CustomArmPaperTexturePath { get; }
        string? CustomArmScissorsTexturePath { get; }
    }

    public abstract class ModCharacterTemplate<TCardPool, TRelicPool, TPotionPool> : CharacterModel
        , IModCharacterAssetOverrides
        where TCardPool : CardPoolModel
        where TRelicPool : RelicPoolModel
        where TPotionPool : PotionPoolModel
    {
        public override string CharacterSelectSfx =>
            CustomCharacterSelectSfx ?? base.CharacterSelectSfx;

        public override string CharacterTransitionSfx =>
            CustomCharacterTransitionSfx ?? base.CharacterTransitionSfx;

        protected override string CharacterSelectIconPath =>
            CustomCharacterSelectIconPath ?? base.CharacterSelectIconPath;

        protected override string CharacterSelectLockedIconPath =>
            CustomCharacterSelectLockedIconPath ?? base.CharacterSelectLockedIconPath;

        protected override string MapMarkerPath =>
            CustomMapMarkerPath ?? base.MapMarkerPath;

        public sealed override CardPoolModel CardPool =>
            ModelDb.GetById<CardPoolModel>(ModelDb.GetId<TCardPool>());

        public sealed override RelicPoolModel RelicPool =>
            ModelDb.GetById<RelicPoolModel>(ModelDb.GetId<TRelicPool>());

        public sealed override PotionPoolModel PotionPool =>
            ModelDb.GetById<PotionPoolModel>(ModelDb.GetId<TPotionPool>());

        public sealed override IEnumerable<CardModel> StartingDeck => ResolveModels<CardModel>(StartingDeckTypes);

        public sealed override IReadOnlyList<RelicModel> StartingRelics =>
            ResolveModels<RelicModel>(StartingRelicTypes).ToArray();

        public sealed override IReadOnlyList<PotionModel> StartingPotions =>
            ResolveModels<PotionModel>(StartingPotionTypes).ToArray();

        protected sealed override CharacterModel? UnlocksAfterRunAs => UnlocksAfterRunAsType == null
            ? null
            : ModelDb.GetById<CharacterModel>(ModelDb.GetId(UnlocksAfterRunAsType));

        protected abstract IEnumerable<Type> StartingDeckTypes { get; }
        protected abstract IEnumerable<Type> StartingRelicTypes { get; }
        protected virtual IEnumerable<Type> StartingPotionTypes => [];
        protected virtual Type? UnlocksAfterRunAsType => null;

        // ReSharper disable once ReturnTypeCanBeNotNullable
        public virtual string? PlaceholderCharacterId => CharacterAssetProfiles.DefaultPlaceholderCharacterId;

        protected CharacterAssetProfile ResolvedAssetProfile =>
            CharacterAssetProfiles.Resolve(AssetProfile, PlaceholderCharacterId);

        public virtual CharacterAssetProfile AssetProfile => CharacterAssetProfile.Empty;

        public virtual string? CustomVisualsPath => ResolvedAssetProfile.Scenes?.VisualsPath;
        public virtual string? CustomEnergyCounterPath => ResolvedAssetProfile.Scenes?.EnergyCounterPath;
        public virtual string? CustomMerchantAnimPath => ResolvedAssetProfile.Scenes?.MerchantAnimPath;
        public virtual string? CustomRestSiteAnimPath => ResolvedAssetProfile.Scenes?.RestSiteAnimPath;
        public virtual string? CustomIconTexturePath => ResolvedAssetProfile.Ui?.IconTexturePath;
        public virtual string? CustomIconOutlineTexturePath => ResolvedAssetProfile.Ui?.IconOutlineTexturePath;
        public virtual string? CustomIconPath => ResolvedAssetProfile.Ui?.IconPath;
        public virtual string? CustomCharacterSelectBgPath => ResolvedAssetProfile.Ui?.CharacterSelectBgPath;
        public virtual string? CustomCharacterSelectIconPath => ResolvedAssetProfile.Ui?.CharacterSelectIconPath;

        public virtual string? CustomCharacterSelectLockedIconPath =>
            ResolvedAssetProfile.Ui?.CharacterSelectLockedIconPath;

        public virtual string? CustomCharacterSelectTransitionPath =>
            ResolvedAssetProfile.Ui?.CharacterSelectTransitionPath;

        public virtual string? CustomMapMarkerPath => ResolvedAssetProfile.Ui?.MapMarkerPath;
        public virtual string? CustomTrailPath => ResolvedAssetProfile.Vfx?.TrailPath;
        public virtual CharacterTrailStyle? CustomTrailStyle => ResolvedAssetProfile.Vfx?.TrailStyle;
        public virtual string? CustomCombatSpineSkeletonDataPath => ResolvedAssetProfile.Spine?.CombatSkeletonDataPath;
        public virtual string? CustomCharacterSelectSfx => ResolvedAssetProfile.Audio?.CharacterSelectSfx;
        public virtual string? CustomCharacterTransitionSfx => ResolvedAssetProfile.Audio?.CharacterTransitionSfx;
        public virtual string? CustomAttackSfx => ResolvedAssetProfile.Audio?.AttackSfx;
        public virtual string? CustomCastSfx => ResolvedAssetProfile.Audio?.CastSfx;
        public virtual string? CustomDeathSfx => ResolvedAssetProfile.Audio?.DeathSfx;
        public virtual string? CustomArmPointingTexturePath => ResolvedAssetProfile.Multiplayer?.ArmPointingTexturePath;
        public virtual string? CustomArmRockTexturePath => ResolvedAssetProfile.Multiplayer?.ArmRockTexturePath;
        public virtual string? CustomArmPaperTexturePath => ResolvedAssetProfile.Multiplayer?.ArmPaperTexturePath;
        public virtual string? CustomArmScissorsTexturePath => ResolvedAssetProfile.Multiplayer?.ArmScissorsTexturePath;

        protected static IEnumerable<TModel> ResolveModels<TModel>(IEnumerable<Type> types)
            where TModel : AbstractModel
        {
            return types
                .Select(type => ModelDb.GetById<TModel>(ModelDb.GetId(type)))
                .ToArray();
        }
    }
}
