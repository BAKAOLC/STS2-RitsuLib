using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Content;
using STS2RitsuLib.Scaffolding.Characters.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;
using STS2RitsuLib.Scaffolding.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace STS2RitsuLib.Scaffolding.Characters
{
    /// <summary>
    ///     <para xml:lang="en">Declares whether a character uses base-game epoch and timeline progression.</para>
    ///     <para xml:lang="zh-CN">声明角色是否使用原版历史节点与时间线进度。</para>
    /// </summary>
    public interface IModCharacterEpochTimelineRequirement
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Whether compatibility patches retain base-game progression paths that require built-in
        ///         <c>*_EPOCH</c> IDs.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         兼容补丁是否保留依赖内置 <c>*_EPOCH</c> ID 的原版进度流程。
        ///     </para>
        /// </summary>
        bool RequiresEpochAndTimeline { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes the prerequisite character used to unlock a mod character.</para>
    ///     <para xml:lang="zh-CN">公开用于解锁模组角色的前置角色。</para>
    /// </summary>
    public interface IModCharacterUnlockPrerequisite
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Prerequisite character type, or <see langword="null" /> when none is declared.
        ///     </para>
        ///     <para xml:lang="zh-CN">前置角色类型；未声明时为 <see langword="null" />。</para>
        /// </summary>
        Type? UnlocksAfterRunAsType { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Controls a mod character's participation in base-game character selection and the Card Library.
    ///     </para>
    ///     <para xml:lang="zh-CN">控制模组角色是否参与原版角色选择与卡牌总览。</para>
    /// </summary>
    public interface IModCharacterVanillaSelectionPolicy
    {
        /// <summary>
        ///     <para xml:lang="en">Whether to hide the character from base-game character-select lists.</para>
        ///     <para xml:lang="zh-CN">是否从原版角色选择列表中隐藏该角色。</para>
        /// </summary>
        bool HideFromVanillaCharacterSelect { get; }

        /// <summary>
        ///     <para xml:lang="en">Whether base-game random character selection may choose this character.</para>
        ///     <para xml:lang="zh-CN">原版随机角色选择是否可以选中该角色。</para>
        /// </summary>
        bool AllowInVanillaRandomCharacterSelect { get; }

        /// <summary>
        ///     <para xml:lang="en">Whether to omit this character's card-pool filter from the Card Library.</para>
        ///     <para xml:lang="zh-CN">是否在卡牌总览中省略该角色的卡牌池筛选项。</para>
        /// </summary>
        bool HideInCardLibraryCompendium { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">Declares copies of one card type in a character's starting deck.</para>
    ///     <para xml:lang="zh-CN">声明角色初始牌组中某种卡牌的份数。</para>
    /// </summary>
    /// <param name="CardType">
    ///     <para xml:lang="en">Registered <see cref="CardModel" /> type.</para>
    ///     <para xml:lang="zh-CN">已注册的 <see cref="CardModel" /> 类型。</para>
    /// </param>
    /// <param name="Count">
    ///     <para xml:lang="en">Number of copies; non-positive values add none.</para>
    ///     <para xml:lang="zh-CN">要添加的份数；非正数不会添加卡牌。</para>
    /// </param>
    public readonly record struct StartingDeckEntry(Type CardType, int Count = 1)
    {
        /// <summary>
        ///     <para xml:lang="en">Creates an entry for <typeparamref name="TCard" />.</para>
        ///     <para xml:lang="zh-CN">创建 <typeparamref name="TCard" /> 的条目。</para>
        /// </summary>
        public static StartingDeckEntry Of<TCard>(int count = 1) where TCard : CardModel
        {
            return new(typeof(TCard), count);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Provides optional asset overrides for a mod character.</para>
    ///     <para xml:lang="zh-CN">提供模组角色的可选资源覆盖。</para>
    /// </summary>
    public interface IModCharacterAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Structured character asset profile.</para>
        ///     <para xml:lang="zh-CN">结构化角色资源配置。</para>
        /// </summary>
        CharacterAssetProfile AssetProfile { get; }

        /// <summary>
        ///     <para xml:lang="en">Combat creature-visuals scene path override.</para>
        ///     <para xml:lang="zh-CN">战斗生物视觉场景的路径覆盖。</para>
        /// </summary>
        string? CustomVisualsPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Combat energy counter scene path override.</para>
        ///     <para xml:lang="zh-CN">战斗能量计数器场景的路径覆盖。</para>
        /// </summary>
        string? CustomEnergyCounterPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Merchant-room character scene path override.</para>
        ///     <para xml:lang="zh-CN">商店房间角色场景的路径覆盖。</para>
        /// </summary>
        string? CustomMerchantAnimPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Rest-site character scene path override.</para>
        ///     <para xml:lang="zh-CN">休息处角色场景的路径覆盖。</para>
        /// </summary>
        string? CustomRestSiteAnimPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Top-panel icon texture path override.</para>
        ///     <para xml:lang="zh-CN">顶部面板图标贴图的路径覆盖。</para>
        /// </summary>
        string? CustomIconTexturePath { get; }

        /// <summary>
        ///     <para xml:lang="en">Top-panel icon outline texture path override.</para>
        ///     <para xml:lang="zh-CN">顶部面板图标描边贴图的路径覆盖。</para>
        /// </summary>
        string? CustomIconOutlineTexturePath { get; }

        /// <summary>
        ///     <para xml:lang="en">Character icon scene path override.</para>
        ///     <para xml:lang="zh-CN">角色图标场景的路径覆盖。</para>
        /// </summary>
        string? CustomIconPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Character-select background scene path override.</para>
        ///     <para xml:lang="zh-CN">角色选择背景场景的路径覆盖。</para>
        /// </summary>
        string? CustomCharacterSelectBgPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Unlocked character-select portrait path override.</para>
        ///     <para xml:lang="zh-CN">已解锁角色选择肖像的路径覆盖。</para>
        /// </summary>
        string? CustomCharacterSelectIconPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Locked character-select portrait path override.</para>
        ///     <para xml:lang="zh-CN">未解锁角色选择肖像的路径覆盖。</para>
        /// </summary>
        string? CustomCharacterSelectLockedIconPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Character-select transition material path override.</para>
        ///     <para xml:lang="zh-CN">角色选择转场材质的路径覆盖。</para>
        /// </summary>
        string? CustomCharacterSelectTransitionPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Run-map marker texture path override.</para>
        ///     <para xml:lang="zh-CN">游戏地图标记贴图的路径覆盖。</para>
        /// </summary>
        string? CustomMapMarkerPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Card-trail VFX scene path override.</para>
        ///     <para xml:lang="zh-CN">卡牌轨迹特效场景的路径覆盖。</para>
        /// </summary>
        string? CustomTrailPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Optional card-trail style overrides.</para>
        ///     <para xml:lang="zh-CN">可选的卡牌轨迹样式覆盖。</para>
        /// </summary>
        CharacterTrailStyle? CustomTrailStyle { get; }

        /// <summary>
        ///     <para xml:lang="en">Combat Spine skeleton-data resource path override.</para>
        ///     <para xml:lang="zh-CN">战斗 Spine 骨骼数据的资源路径覆盖。</para>
        /// </summary>
        string? CustomCombatSpineSkeletonDataPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Character-select confirmation event path override.</para>
        ///     <para xml:lang="zh-CN">角色选择确认音效的事件路径覆盖。</para>
        /// </summary>
        string? CustomCharacterSelectSfx { get; }

        /// <summary>
        ///     <para xml:lang="en">Character transition event path override.</para>
        ///     <para xml:lang="zh-CN">角色转场音效的事件路径覆盖。</para>
        /// </summary>
        string? CustomCharacterTransitionSfx { get; }

        /// <summary>
        ///     <para xml:lang="en">Default attack event path override.</para>
        ///     <para xml:lang="zh-CN">默认攻击音效的事件路径覆盖。</para>
        /// </summary>
        string? CustomAttackSfx { get; }

        /// <summary>
        ///     <para xml:lang="en">Default card-cast event path override.</para>
        ///     <para xml:lang="zh-CN">默认卡牌施放音效的事件路径覆盖。</para>
        /// </summary>
        string? CustomCastSfx { get; }

        /// <summary>
        ///     <para xml:lang="en">Player-death event path override.</para>
        ///     <para xml:lang="zh-CN">玩家死亡音效的事件路径覆盖。</para>
        /// </summary>
        string? CustomDeathSfx { get; }

        /// <summary>
        ///     <para xml:lang="en">Multiplayer pointing-hand texture path override.</para>
        ///     <para xml:lang="zh-CN">多人游戏指向手势贴图的路径覆盖。</para>
        /// </summary>
        string? CustomArmPointingTexturePath { get; }

        /// <summary>
        ///     <para xml:lang="en">Multiplayer rock-hand texture path override.</para>
        ///     <para xml:lang="zh-CN">多人游戏石头手势贴图的路径覆盖。</para>
        /// </summary>
        string? CustomArmRockTexturePath { get; }

        /// <summary>
        ///     <para xml:lang="en">Multiplayer paper-hand texture path override.</para>
        ///     <para xml:lang="zh-CN">多人游戏布手势贴图的路径覆盖。</para>
        /// </summary>
        string? CustomArmPaperTexturePath { get; }

        /// <summary>
        ///     <para xml:lang="en">Multiplayer scissors-hand texture path override.</para>
        ///     <para xml:lang="zh-CN">多人游戏剪刀手势贴图的路径覆盖。</para>
        /// </summary>
        string? CustomArmScissorsTexturePath { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional named textures and frame sequences for non-Spine character visuals.
        ///     </para>
        ///     <para xml:lang="zh-CN">非 Spine 角色视觉使用的可选具名贴图与帧序列。</para>
        /// </summary>
        VisualCueSet? VisualCues { get; }

        /// <summary>
        ///     <para xml:lang="en">Optional procedural merchant and rest-site visuals.</para>
        ///     <para xml:lang="zh-CN">可选的程序化商店与休息处视觉资源。</para>
        /// </summary>
        CharacterWorldProceduralVisualSet? WorldProceduralVisuals { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional base-game character ID used to fill missing fields in <see cref="AssetProfile" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         用于填充 <see cref="AssetProfile" /> 缺失字段的可选原版角色 ID。
        ///     </para>
        /// </summary>
        string? CharacterAssetPlaceholderCharacterId => null;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns this character's visual override for <paramref name="relic" />, or
        ///         <see langword="null" /> when none applies.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回此角色适用于 <paramref name="relic" /> 的视觉覆盖；没有匹配项时返回
        ///         <see langword="null" />。
        ///     </para>
        /// </summary>
        RelicAssetProfile? TryGetVanillaRelicVisualOverrideForOwnedRelic(RelicModel relic);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns this character's visual override for <paramref name="potion" />, or
        ///         <see langword="null" /> when none applies.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回此角色适用于 <paramref name="potion" /> 的视觉覆盖；没有匹配项时返回
        ///         <see langword="null" />。
        ///     </para>
        /// </summary>
        PotionAssetProfile? TryGetVanillaPotionVisualOverrideForContext(PotionModel potion);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns this character's visual override for <paramref name="card" />, or
        ///         <see langword="null" /> when none applies.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回此角色适用于 <paramref name="card" /> 的视觉覆盖；没有匹配项时返回
        ///         <see langword="null" />。
        ///     </para>
        /// </summary>
        CardAssetProfile? TryGetVanillaCardVisualOverrideForContext(CardModel card);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Base class for mod characters with typed content pools, extensible starting content, and asset
    ///         overrides.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         模组角色基类，提供泛型内容池、可扩展初始内容与资源覆盖。
    ///     </para>
    /// </summary>
    /// <typeparam name="TCardPool">
    ///     <para xml:lang="en">Registered card pool type.</para>
    ///     <para xml:lang="zh-CN">已注册的卡牌池类型。</para>
    /// </typeparam>
    /// <typeparam name="TRelicPool">
    ///     <para xml:lang="en">Registered relic pool type.</para>
    ///     <para xml:lang="zh-CN">已注册的遗物池类型。</para>
    /// </typeparam>
    /// <typeparam name="TPotionPool">
    ///     <para xml:lang="en">Registered potion pool type.</para>
    ///     <para xml:lang="zh-CN">已注册的药水池类型。</para>
    /// </typeparam>
#pragma warning disable CS0618
    // Template keeps the obsolete IModCharacter* visuals / animator factory interfaces wired so existing derived
    // classes and external consumers that type-check against the old interface names continue to work.
    public abstract class ModCharacterTemplate<TCardPool, TRelicPool, TPotionPool> : CharacterModel
        , IModCharacterAssetOverrides, IModCreatureVisualsFactory, IModCharacterCreatureVisualsFactory,
        IModCreatureAnimatorFactory, IModCharacterCreatureAnimatorFactory,
        IModCreatureCombatAnimationStateMachineFactory, IModNonSpineAnimationStateMachineFactory,
        IModCharacterMerchantAnimationStateMachineFactory, IModCharacterRestSiteAnimationStateMachineFactory,
        IModCharacterEpochTimelineRequirement, IModCharacterVanillaSelectionPolicy,
        IModCharacterCardLibraryCompendiumPlacement, IModCharacterUnlockPrerequisite
#pragma warning restore CS0618
        where TCardPool : CardPoolModel
        where TRelicPool : RelicPoolModel
        where TPotionPool : PotionPoolModel
    {
        /// <inheritdoc />
        public override string CharacterSelectSfx =>
            CustomCharacterSelectSfx ?? base.CharacterSelectSfx;

        /// <inheritdoc />
        public override string CharacterTransitionSfx =>
            CustomCharacterTransitionSfx ?? base.CharacterTransitionSfx;

        /// <inheritdoc />
        protected override string CharacterSelectIconPath =>
            CustomCharacterSelectIconPath ?? base.CharacterSelectIconPath;

        /// <inheritdoc />
        protected override string CharacterSelectLockedIconPath =>
            CustomCharacterSelectLockedIconPath ?? base.CharacterSelectLockedIconPath;

        /// <inheritdoc />
        protected override string MapMarkerPath =>
            CustomMapMarkerPath ?? base.MapMarkerPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the registered <typeparamref name="TCardPool" /> instance.</para>
        ///     <para xml:lang="zh-CN">获取已注册的 <typeparamref name="TCardPool" /> 实例。</para>
        /// </summary>
        public sealed override CardPoolModel CardPool =>
            ModelDb.GetById<CardPoolModel>(ModelDb.GetId<TCardPool>());

        /// <summary>
        ///     <para xml:lang="en">Gets the registered <typeparamref name="TRelicPool" /> instance.</para>
        ///     <para xml:lang="zh-CN">获取已注册的 <typeparamref name="TRelicPool" /> 实例。</para>
        /// </summary>
        public sealed override RelicPoolModel RelicPool =>
            ModelDb.GetById<RelicPoolModel>(ModelDb.GetId<TRelicPool>());

        /// <summary>
        ///     <para xml:lang="en">Gets the registered <typeparamref name="TPotionPool" /> instance.</para>
        ///     <para xml:lang="zh-CN">获取已注册的 <typeparamref name="TPotionPool" /> 实例。</para>
        /// </summary>
        public sealed override PotionPoolModel PotionPool =>
            ModelDb.GetById<PotionPoolModel>(ModelDb.GetId<TPotionPool>());

        /// <inheritdoc />
        public sealed override IEnumerable<CardModel> StartingDeck => ResolveStartingDeck();

        /// <inheritdoc />
        public sealed override IReadOnlyList<RelicModel> StartingRelics => ResolveStartingRelics();

        /// <inheritdoc />
        public sealed override IReadOnlyList<PotionModel> StartingPotions => ResolveStartingPotions();

        /// <inheritdoc />
        protected sealed override CharacterModel? UnlocksAfterRunAs => UnlocksAfterRunAsType == null
            ? null
            : ModelDb.GetById<CharacterModel>(ModelDb.GetId(UnlocksAfterRunAsType));

        /// <summary>
        ///     <para xml:lang="en">
        ///         Starting deck supplied by the character class. Registered additions are appended afterward.
        ///     </para>
        ///     <para xml:lang="zh-CN">角色类提供的初始牌组；之后会追加通过注册表添加的卡牌。</para>
        /// </summary>
        protected virtual IEnumerable<CardModel> LocalStartingDeck
        {
            get
            {
#pragma warning disable CS0618 // Intentional compatibility bridge from legacy type-based hooks
                return
                [
                    .. StartingDeckEntries
                        .SelectMany(static entry => Enumerable.Repeat(entry.CardType, Math.Max(entry.Count, 0)))
                        .Select(static type => ModelDb.GetById<CardModel>(ModelDb.GetId(type))),
                ];
#pragma warning restore CS0618
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Starting relics supplied by the character class. Registered additions are appended afterward.
        ///     </para>
        ///     <para xml:lang="zh-CN">角色类提供的初始遗物；之后会追加通过注册表添加的遗物。</para>
        /// </summary>
        protected virtual IEnumerable<RelicModel> LocalStartingRelics
        {
            get
            {
#pragma warning disable CS0618 // Intentional compatibility bridge from legacy type-based hooks
                return ResolveModels<RelicModel>(StartingRelicTypes);
#pragma warning restore CS0618
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Starting potions supplied by the character class. Registered additions are appended afterward.
        ///     </para>
        ///     <para xml:lang="zh-CN">角色类提供的初始药水；之后会追加通过注册表添加的药水。</para>
        /// </summary>
        protected virtual IEnumerable<PotionModel> LocalStartingPotions
        {
            get
            {
#pragma warning disable CS0618 // Intentional compatibility bridge from legacy type-based hooks
                return ResolveModels<PotionModel>(StartingPotionTypes);
#pragma warning restore CS0618
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Legacy type-and-count starting-deck declaration.</para>
        ///     <para xml:lang="zh-CN">旧版按类型与份数声明初始牌组的入口。</para>
        /// </summary>
        [Obsolete(
            "Prefer additive character-starter registration through CharacterRegistrationEntry.AddStartingCard(...) or "
            + "ModContentRegistry.RegisterCharacterStarterCard(...). Override only for legacy mods; suppress CS0618 if required.")]
        protected virtual IEnumerable<StartingDeckEntry> StartingDeckEntries
        {
            get
            {
#pragma warning disable CS0618 // Intentional compatibility bridge from legacy StartingDeckTypes
                return StartingDeckTypes.Select(static type => new StartingDeckEntry(type));
#pragma warning restore CS0618
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Legacy card-type list for the starting deck.</para>
        ///     <para xml:lang="zh-CN">旧版初始牌组卡牌类型列表。</para>
        /// </summary>
        [Obsolete(
            "Prefer additive character-starter registration. This legacy hook requires repeating the same type for duplicate starter cards. "
            + "Override only for legacy mods; suppress CS0618 if required.")]
        protected virtual IEnumerable<Type> StartingDeckTypes => [];

        /// <summary>
        ///     <para xml:lang="en">Legacy relic-type list for starting relics.</para>
        ///     <para xml:lang="zh-CN">旧版初始遗物类型列表。</para>
        /// </summary>
        [Obsolete(
            "Prefer additive character-starter registration through CharacterRegistrationEntry.AddStartingRelic(...) or "
            + "ModContentRegistry.RegisterCharacterStarterRelic(...). Override only for legacy mods; suppress CS0618 if required.")]
        protected virtual IEnumerable<Type> StartingRelicTypes => [];

        /// <summary>
        ///     <para xml:lang="en">Legacy potion-type list for starting potions.</para>
        ///     <para xml:lang="zh-CN">旧版初始药水类型列表。</para>
        /// </summary>
        [Obsolete(
            "Prefer additive character-starter registration through CharacterRegistrationEntry.AddStartingPotion(...) or "
            + "ModContentRegistry.RegisterCharacterStarterPotion(...). Override only for legacy mods; suppress CS0618 if required.")]
        protected virtual IEnumerable<Type> StartingPotionTypes => [];

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional prerequisite character type used by base-game unlock text and RitsuLib character-unlock
        ///         epochs. Ironclad prerequisites unlock with Neow's initial expansion; other prerequisites unlock
        ///         after completing a run as that character.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         原版解锁文本与 RitsuLib 角色解锁历史节点使用的可选前置角色类型。前置角色为铁甲战士时，
        ///         随涅奥的初始扩展解锁；其他角色则在使用该角色完成一局游戏后解锁。
        ///     </para>
        /// </summary>
        protected virtual Type? UnlocksAfterRunAsType => null;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Base-game character ID used to fill missing fields in <see cref="AssetProfile" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         用于填充 <see cref="AssetProfile" /> 缺失字段的原版角色 ID。
        ///     </para>
        /// </summary>
        // ReSharper disable once ReturnTypeCanBeNotNullable
        public virtual string? PlaceholderCharacterId => CharacterAssetProfiles.DefaultPlaceholderCharacterId;

        /// <summary>
        ///     <para xml:lang="en">Asset profile after placeholder fields are filled.</para>
        ///     <para xml:lang="zh-CN">使用占位角色填充缺失字段后的资源配置。</para>
        /// </summary>
        protected CharacterAssetProfile ResolvedAssetProfile =>
            CharacterAssetProfiles.Resolve(AssetProfile, PlaceholderCharacterId);

        /// <inheritdoc />
        public virtual CharacterAssetProfile AssetProfile => CharacterAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomVisualsPath => ResolvedAssetProfile.Scenes?.VisualsPath;

        /// <inheritdoc />
        public virtual string? CustomEnergyCounterPath => ResolvedAssetProfile.Scenes?.EnergyCounterPath;

        /// <inheritdoc />
        public virtual string? CustomMerchantAnimPath => ResolvedAssetProfile.Scenes?.MerchantAnimPath;

        /// <inheritdoc />
        public virtual string? CustomRestSiteAnimPath => ResolvedAssetProfile.Scenes?.RestSiteAnimPath;

        /// <inheritdoc />
        public virtual string? CustomIconTexturePath => ResolvedAssetProfile.Ui?.IconTexturePath;

        /// <inheritdoc />
        public virtual string? CustomIconOutlineTexturePath => ResolvedAssetProfile.Ui?.IconOutlineTexturePath;

        /// <inheritdoc />
        public virtual string? CustomIconPath => ResolvedAssetProfile.Ui?.IconPath;

        /// <inheritdoc />
        public virtual string? CustomCharacterSelectBgPath => ResolvedAssetProfile.Ui?.CharacterSelectBgPath;

        /// <inheritdoc />
        public virtual string? CustomCharacterSelectIconPath => ResolvedAssetProfile.Ui?.CharacterSelectIconPath;

        /// <inheritdoc />
        public virtual string? CustomCharacterSelectLockedIconPath =>
            ResolvedAssetProfile.Ui?.CharacterSelectLockedIconPath;

        /// <inheritdoc />
        public virtual string? CustomCharacterSelectTransitionPath =>
            ResolvedAssetProfile.Ui?.CharacterSelectTransitionPath;

        /// <inheritdoc />
        public virtual string? CustomMapMarkerPath => ResolvedAssetProfile.Ui?.MapMarkerPath;

        /// <inheritdoc />
        public virtual string? CustomTrailPath => ResolvedAssetProfile.Vfx?.TrailPath;

        /// <inheritdoc />
        public virtual CharacterTrailStyle? CustomTrailStyle => ResolvedAssetProfile.Vfx?.TrailStyle;

        /// <inheritdoc />
        public virtual string? CustomCombatSpineSkeletonDataPath => ResolvedAssetProfile.Spine?.CombatSkeletonDataPath;

        /// <inheritdoc />
        public virtual string? CustomCharacterSelectSfx => ResolvedAssetProfile.Audio?.CharacterSelectSfx;

        /// <inheritdoc />
        public virtual string? CustomCharacterTransitionSfx => ResolvedAssetProfile.Audio?.CharacterTransitionSfx;

        /// <inheritdoc />
        public virtual string? CustomAttackSfx => ResolvedAssetProfile.Audio?.AttackSfx;

        /// <inheritdoc />
        public virtual string? CustomCastSfx => ResolvedAssetProfile.Audio?.CastSfx;

        /// <inheritdoc />
        public virtual string? CustomDeathSfx => ResolvedAssetProfile.Audio?.DeathSfx;

        /// <inheritdoc />
        public virtual string? CustomArmPointingTexturePath => ResolvedAssetProfile.Multiplayer?.ArmPointingTexturePath;

        /// <inheritdoc />
        public virtual string? CustomArmRockTexturePath => ResolvedAssetProfile.Multiplayer?.ArmRockTexturePath;

        /// <inheritdoc />
        public virtual string? CustomArmPaperTexturePath => ResolvedAssetProfile.Multiplayer?.ArmPaperTexturePath;

        /// <inheritdoc />
        public virtual string? CustomArmScissorsTexturePath => ResolvedAssetProfile.Multiplayer?.ArmScissorsTexturePath;

        /// <inheritdoc />
        public virtual RelicAssetProfile? TryGetVanillaRelicVisualOverrideForOwnedRelic(RelicModel relic)
        {
            return ModCharacterOwnedVisualOverrideHelper.ResolveOwnedRelicVisualOverride(this, relic);
        }

        /// <inheritdoc />
        public virtual PotionAssetProfile? TryGetVanillaPotionVisualOverrideForContext(PotionModel potion)
        {
            return ModCharacterOwnedVisualOverrideHelper.ResolveOwnedPotionVisualOverride(this, potion);
        }

        /// <inheritdoc />
        public virtual CardAssetProfile? TryGetVanillaCardVisualOverrideForContext(CardModel card)
        {
            return ModCharacterOwnedVisualOverrideHelper.ResolveOwnedCardVisualOverride(this, card);
        }

        string? IModCharacterAssetOverrides.CharacterAssetPlaceholderCharacterId => PlaceholderCharacterId;

        /// <inheritdoc />
        public virtual VisualCueSet? VisualCues => ResolvedAssetProfile.VisualCues;

        /// <inheritdoc />
        public virtual CharacterWorldProceduralVisualSet? WorldProceduralVisuals =>
            ResolvedAssetProfile.WorldProceduralVisuals;

        /// <inheritdoc cref="IModCharacterCardLibraryCompendiumPlacement.CardLibraryCompendiumPlacementRules" />
        public virtual IReadOnlyList<CardLibraryCompendiumPlacementRule>? CardLibraryCompendiumPlacementRules => null;

#pragma warning disable CS0618
        CreatureAnimator? IModCharacterCreatureAnimatorFactory.TryCreateCreatureAnimator(MegaSprite controller)
        {
            return SetupCustomCreatureAnimator(controller);
        }
#pragma warning restore CS0618

#pragma warning disable CS0618
        NCreatureVisuals? IModCharacterCreatureVisualsFactory.TryCreateCreatureVisuals()
        {
            return TryCreateCreatureVisuals();
        }
#pragma warning restore CS0618

        /// <inheritdoc />
        public virtual bool RequiresEpochAndTimeline => true;

        ModAnimStateMachine? IModCharacterMerchantAnimationStateMachineFactory.
            TryCreateMerchantAnimationStateMachine(Node merchantRoot, CharacterModel character)
        {
            return SetupCustomMerchantAnimationStateMachine(merchantRoot, character);
        }

        ModAnimStateMachine? IModCharacterRestSiteAnimationStateMachineFactory.
            TryCreateRestSiteAnimationStateMachine(Node restSiteRoot, CharacterModel character)
        {
            return SetupCustomRestSiteAnimationStateMachine(restSiteRoot, character);
        }

        Type? IModCharacterUnlockPrerequisite.UnlocksAfterRunAsType => UnlocksAfterRunAsType;

        /// <inheritdoc />
        public virtual bool HideFromVanillaCharacterSelect => false;

        /// <inheritdoc />
        public virtual bool AllowInVanillaRandomCharacterSelect => !HideFromVanillaCharacterSelect;

        /// <inheritdoc />
        public virtual bool HideInCardLibraryCompendium => false;

        CreatureAnimator? IModCreatureAnimatorFactory.TryCreateCreatureAnimator(MegaSprite controller)
        {
            return SetupCustomCreatureAnimator(controller);
        }

        ModAnimStateMachine? IModCreatureCombatAnimationStateMachineFactory.TryCreateCombatAnimationStateMachine(
            Node visualsRoot)
        {
            return ResolveCombatAnimationStateMachine(visualsRoot);
        }

        NCreatureVisuals? IModCreatureVisualsFactory.TryCreateCreatureVisuals()
        {
            return TryCreateCreatureVisuals();
        }

        ModAnimStateMachine? IModNonSpineAnimationStateMachineFactory.TryCreateNonSpineAnimationStateMachine(
            Node visualsRoot)
        {
            return ResolveCombatAnimationStateMachine(visualsRoot);
        }

        private ModAnimStateMachine? ResolveCombatAnimationStateMachine(Node visualsRoot)
        {
            var fromNew = SetupCustomCombatAnimationStateMachine(visualsRoot, this);
#pragma warning disable CS0618
            return fromNew ?? SetupCustomNonSpineAnimationStateMachine(visualsRoot, this);
#pragma warning restore CS0618
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates combat visuals directly, or returns <see langword="null" /> to use the configured scene path.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         直接创建战斗视觉节点；返回 <see langword="null" /> 时使用已配置的场景路径。
        ///     </para>
        /// </summary>
        protected virtual NCreatureVisuals? TryCreateCreatureVisuals()
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a Spine <see cref="CreatureAnimator" />, or returns <see langword="null" /> to use
        ///         <see cref="CharacterModel.GenerateAnimator" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建 Spine <see cref="CreatureAnimator" />；返回 <see langword="null" /> 时使用
        ///         <see cref="CharacterModel.GenerateAnimator" />。
        ///     </para>
        /// </summary>
        /// <param name="controller">
        ///     <para xml:lang="en">Spine controller attached to the combat visuals.</para>
        ///     <para xml:lang="zh-CN">附加到战斗视觉节点的 Spine 控制器。</para>
        /// </param>
        protected virtual CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller)
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a combat animation state machine, or returns <see langword="null" /> to use the standard
        ///         Spine animator or visual-cue playback.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建战斗动画状态机；返回 <see langword="null" /> 时使用标准 Spine 动画器或视觉提示播放。
        ///     </para>
        /// </summary>
        /// <param name="visualsRoot">
        ///     <para xml:lang="en">Combat visuals root node.</para>
        ///     <para xml:lang="zh-CN">战斗视觉根节点。</para>
        /// </param>
        /// <param name="character">
        ///     <para xml:lang="en">Current character model.</para>
        ///     <para xml:lang="zh-CN">当前角色模型。</para>
        /// </param>
        protected virtual ModAnimStateMachine? SetupCustomCombatAnimationStateMachine(Node visualsRoot,
            CharacterModel character)
        {
            return null;
        }

        /// <inheritdoc cref="SetupCustomCombatAnimationStateMachine" />
        [Obsolete("Override SetupCustomCombatAnimationStateMachine instead.")]
        protected virtual ModAnimStateMachine? SetupCustomNonSpineAnimationStateMachine(Node visualsRoot,
            CharacterModel character)
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates the merchant-room animation state machine, or returns <see langword="null" /> to use
        ///         visual-cue playback.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建商店房间动画状态机；返回 <see langword="null" /> 时使用视觉提示播放。
        ///     </para>
        /// </summary>
        /// <param name="merchantRoot">
        ///     <para xml:lang="en">Merchant character root node.</para>
        ///     <para xml:lang="zh-CN">商店角色根节点。</para>
        /// </param>
        /// <param name="character">
        ///     <para xml:lang="en">Current character model.</para>
        ///     <para xml:lang="zh-CN">当前角色模型。</para>
        /// </param>
        protected virtual ModAnimStateMachine? SetupCustomMerchantAnimationStateMachine(Node merchantRoot,
            CharacterModel character)
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates the rest-site animation state machine. By default, delegates to
        ///         <see cref="SetupCustomMerchantAnimationStateMachine" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建休息处动画状态机；默认委托给 <see cref="SetupCustomMerchantAnimationStateMachine" />。
        ///     </para>
        /// </summary>
        /// <param name="restSiteRoot">
        ///     <para xml:lang="en">Rest-site character root node.</para>
        ///     <para xml:lang="zh-CN">休息处角色根节点。</para>
        /// </param>
        /// <param name="character">
        ///     <para xml:lang="en">Current character model.</para>
        ///     <para xml:lang="zh-CN">当前角色模型。</para>
        /// </param>
        protected virtual ModAnimStateMachine? SetupCustomRestSiteAnimationStateMachine(Node restSiteRoot,
            CharacterModel character)
        {
            return SetupCustomMerchantAnimationStateMachine(restSiteRoot, character);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves model types to registered <typeparamref name="TModel" /> instances.</para>
        ///     <para xml:lang="zh-CN">将模型类型解析为已注册的 <typeparamref name="TModel" /> 实例。</para>
        /// </summary>
        protected static IEnumerable<TModel> ResolveModels<TModel>(IEnumerable<Type> types)
            where TModel : AbstractModel
        {
            return
            [
                .. types
                    .Select(type => ModelDb.GetById<TModel>(ModelDb.GetId(type))),
            ];
        }

        private IEnumerable<CardModel> ResolveStartingDeck()
        {
            var registeredTypes = ModContentRegistry.GetRegisteredCharacterStarterCards(GetType());

            return
            [
                .. LocalStartingDeck,
                .. ResolveModels<CardModel>(registeredTypes),
            ];
        }

        private IReadOnlyList<RelicModel> ResolveStartingRelics()
        {
            var registeredTypes = ModContentRegistry.GetRegisteredCharacterStarterRelics(GetType());

            return
            [
                .. LocalStartingRelics,
                .. ResolveModels<RelicModel>(registeredTypes),
            ];
        }

        private IReadOnlyList<PotionModel> ResolveStartingPotions()
        {
            var registeredTypes = ModContentRegistry.GetRegisteredCharacterStarterPotions(GetType());

            return
            [
                .. LocalStartingPotions,
                .. ResolveModels<PotionModel>(registeredTypes),
            ];
        }
    }
}
