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
    ///     Declares whether a character participates in vanilla epoch and timeline progression.
    ///     声明角色是否参与原版纪元和时间线进度。
    /// </summary>
    public interface IModCharacterEpochTimelineRequirement
    {
        /// <summary>
        ///     When false, runtime compatibility patches skip vanilla character epoch/timeline grant paths that assume
        ///     built-in <c>*_EPOCH</c> ids exist.
        ///     <c>*_EPOCH</c> id。
        ///     为 false 时，运行时兼容性 patch 会跳过假定内置 <c>*_EPOCH</c> id 存在的
        ///     原版角色纪元/时间线授予路径。
        ///     <c>*_EPOCH</c> id。
        /// </summary>
        bool RequiresEpochAndTimeline { get; }
    }

    /// <summary>
    ///     Exposes the character whose run path unlocks this mod character.
    ///     暴露用于解锁该 mod 角色的前置角色。
    /// </summary>
    public interface IModCharacterUnlockPrerequisite
    {
        /// <summary>
        ///     CLR type of the prerequisite character, or <c>null</c> when none is declared.
        ///     前置角色的 CLR 类型；未声明时为 <c>null</c>。
        /// </summary>
        Type? UnlocksAfterRunAsType { get; }
    }

    /// <summary>
    ///     Controls mod-character visibility in vanilla character-select, random selection, and the card library
    ///     compendium pool-filter row.
    ///     控制 mod 角色在原版角色选择、随机选择和卡牌库
    ///     compendium 牌池过滤行中的可见性。
    /// </summary>
    public interface IModCharacterVanillaSelectionPolicy
    {
        /// <summary>
        ///     When true, hides the character from vanilla character-select UI lists.
        ///     为 true 时，从原版角色选择 UI 列表中隐藏该角色。
        /// </summary>
        bool HideFromVanillaCharacterSelect { get; }

        /// <summary>
        ///     When false, excludes the character from vanilla random character selection.
        ///     为 false 时，将该角色排除出原版随机角色选择。
        /// </summary>
        bool AllowInVanillaRandomCharacterSelect { get; }

        /// <summary>
        ///     When true, <see cref="STS2RitsuLib.Scaffolding.Characters.Patches.CardLibraryCompendiumPatch" /> does not add a
        ///     card-pool filter toggle for
        ///     this character (aligned with BaseLib <c>CustomCharacterModel.HideInCompendium</c>).
        ///     为 true 时，<see cref="STS2RitsuLib.Scaffolding.Characters.Patches.CardLibraryCompendiumPatch" /> 不会为
        ///     此角色添加卡牌牌池过滤开关（与 BaseLib <c>CustomCharacterModel.HideInCompendium</c> 对齐）。
        /// </summary>
        bool HideInCardLibraryCompendium { get; }
    }

    /// <summary>
    ///     Declarative starting-deck entry that expands one card CLR type into <see cref="Count" /> copies.
    ///     声明式初始牌组条目，将一个卡牌 CLR 类型展开为 <see cref="Count" /> 份。
    /// </summary>
    /// <param name="CardType">
    ///     Registered <see cref="CardModel" /> CLR type.
    ///     已注册的 <see cref="CardModel" /> CLR 类型。
    /// </param>
    /// <param name="Count">
    ///     Number of copies to add to the starting deck.
    ///     要添加到初始牌组中的复制数量。
    /// </param>
    public readonly record struct StartingDeckEntry(Type CardType, int Count = 1)
    {
        /// <summary>
        ///     Typed helper for concise collection expressions.
        ///     用于简洁集合表达式的类型化 helper。
        /// </summary>
        public static StartingDeckEntry Of<TCard>(int count = 1) where TCard : CardModel
        {
            return new(typeof(TCard), count);
        }
    }

    /// <summary>
    ///     Optional asset paths and profile data for mod characters. Patches read these values to override
    ///     vanilla <see cref="CharacterModel" /> asset resolution (visuals, UI, audio, multiplayer arms, combat Spine).
    ///     mod 角色的可选资产路径和 profile 数据。patch 会读取这些值以覆盖
    ///     原版 <see cref="CharacterModel" /> 资产解析（视觉、UI、音频、多人手臂、战斗 Spine）。
    /// </summary>
    public interface IModCharacterAssetOverrides
    {
        /// <summary>
        ///     Structured bundle of paths and styles; individual <c>Custom*</c> properties typically resolve from this
        ///     profile unless overridden in a subclass.
        ///     路径和样式的结构化包；单个 <c>Custom*</c> 属性通常会从此
        ///     profile 解析，除非在子类中覆盖。
        /// </summary>
        CharacterAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Resource path for the character combat / scene visuals (replaces vanilla <c>VisualsPath</c> when set).
        ///     角色战斗/场景视觉的资源路径（设置时替换原版 <c>VisualsPath</c>）。
        /// </summary>
        string? CustomVisualsPath { get; }

        /// <summary>
        ///     Resource path for the energy counter UI used with this character.
        ///     此角色使用的能量计数器 UI 资源路径。
        /// </summary>
        string? CustomEnergyCounterPath { get; }

        /// <summary>
        ///     Resource path for merchant-room character animation assets.
        ///     商人房间角色动画资产的资源路径。
        /// </summary>
        string? CustomMerchantAnimPath { get; }

        /// <summary>
        ///     Resource path for rest-site character animation assets.
        ///     营火角色动画资产的资源路径。
        /// </summary>
        string? CustomRestSiteAnimPath { get; }

        /// <summary>
        ///     Path to the main icon texture (atlas entry or image) for UI that uses <c>IconTexturePath</c>.
        ///     UI 使用 <c>IconTexturePath</c> 时的主图标纹理路径（atlas 条目或图像）。
        /// </summary>
        string? CustomIconTexturePath { get; }

        /// <summary>
        ///     Path to the icon outline texture used for UI framing.
        ///     UI 边框使用的图标轮廓贴图路径。
        /// </summary>
        string? CustomIconOutlineTexturePath { get; }

        /// <summary>
        ///     Path resolved as the compact icon asset (<c>IconPath</c>).
        ///     解析为紧凑图标资产的路径（<c>IconPath</c>）。
        /// </summary>
        string? CustomIconPath { get; }

        /// <summary>
        ///     Scene or resource path for the character-select background art.
        ///     角色选择背景图的场景或资源路径。
        /// </summary>
        string? CustomCharacterSelectBgPath { get; }

        /// <summary>
        ///     Path for the selectable character portrait/icon on the character-select screen.
        ///     角色选择界面上可选角色肖像 / 图标的路径。
        /// </summary>
        string? CustomCharacterSelectIconPath { get; }

        /// <summary>
        ///     Path for the locked-state icon on the character-select screen.
        ///     角色选择界面上锁定状态图标的路径。
        /// </summary>
        string? CustomCharacterSelectLockedIconPath { get; }

        /// <summary>
        ///     Path for transition art/video when confirming character selection.
        ///     确认角色选择时转场美术 / 视频的路径。
        /// </summary>
        string? CustomCharacterSelectTransitionPath { get; }

        /// <summary>
        ///     Path for the world-map marker icon representing this character.
        ///     表示此角色的世界地图标记图标路径。
        /// </summary>
        string? CustomMapMarkerPath { get; }

        /// <summary>
        ///     Path to the trail VFX scene or resource used when playing cards.
        ///     打出卡牌时使用的 trail VFX 场景或资源路径。
        /// </summary>
        string? CustomTrailPath { get; }

        /// <summary>
        ///     Optional modulate/width/color overrides when reusing a vanilla trail scene (see trail style patch).
        ///     复用原版 trail 场景时的可选 modulate/宽度/颜色覆盖（见 trail style patch）。
        /// </summary>
        CharacterTrailStyle? CustomTrailStyle { get; }

        /// <summary>
        ///     Path to Spine skeleton data (<c>.tres</c> / resource) for combat, when reusing vanilla visuals scenes.
        ///     复用原版视觉场景时，用于战斗的 Spine skeleton 数据（<c>.tres</c> / 资源）路径。
        /// </summary>
        string? CustomCombatSpineSkeletonDataPath { get; }

        /// <summary>
        ///     FMOD event id or path for the sound played when this character is chosen on the select screen.
        ///     在选择界面选中此角色时播放声音的 FMOD 事件 id 或路径。
        /// </summary>
        string? CustomCharacterSelectSfx { get; }

        /// <summary>
        ///     FMOD event id or path for the transition sound when locking in this character.
        ///     锁定此角色时转场声音的 FMOD 事件 id 或路径。
        /// </summary>
        string? CustomCharacterTransitionSfx { get; }

        /// <summary>
        ///     FMOD event id or path for the basic attack sound in combat.
        ///     战斗中基础攻击声音的 FMOD 事件 id 或路径。
        /// </summary>
        string? CustomAttackSfx { get; }

        /// <summary>
        ///     FMOD event id or path for casting / card-play style combat audio.
        ///     施放 / 打牌风格战斗音频的 FMOD 事件 id 或路径。
        /// </summary>
        string? CustomCastSfx { get; }

        /// <summary>
        ///     FMOD event id or path for this character’s death sound.
        ///     此角色死亡声音的 FMOD 事件 id 或路径。
        /// </summary>
        string? CustomDeathSfx { get; }

        /// <summary>
        ///     Texture path for the “pointing” arm pose in multiplayer UI.
        ///     多人 UI 中“指向”手臂姿势的贴图路径。
        /// </summary>
        string? CustomArmPointingTexturePath { get; }

        /// <summary>
        ///     Texture path for the rock hand in multiplayer RPS-style UI.
        ///     多人石头剪刀布风格 UI 中石头手势的贴图路径。
        /// </summary>
        string? CustomArmRockTexturePath { get; }

        /// <summary>
        ///     Texture path for the paper hand in multiplayer RPS-style UI.
        ///     多人石头剪刀布风格 UI 中布手势的贴图路径。
        /// </summary>
        string? CustomArmPaperTexturePath { get; }

        /// <summary>
        ///     Texture path for the scissors hand in multiplayer RPS-style UI.
        ///     多人石头剪刀布风格 UI 中剪刀手势的贴图路径。
        /// </summary>
        string? CustomArmScissorsTexturePath { get; }

        /// <summary>
        ///     Optional per-cue static textures and frame sequences for non-Spine combat / game-over visuals; define with
        ///     <c>ModVisualCues</c> (runtime: <c>ModCreatureVisualPlayback</c>).
        ///     非 Spine 战斗/游戏结束视觉的可选逐 cue 静态纹理和帧序列；使用
        ///     <c>ModVisualCues</c> 定义（运行时：<c>ModCreatureVisualPlayback</c>）。
        /// </summary>
        VisualCueSet? VisualCues { get; }

        /// <summary>
        ///     Optional merchant / rest-site procedural shells (no custom merchant or rest-site character <c>tscn</c>);
        ///     see <see cref="ModCharacterWorldSceneVisuals" />.
        ///     <see cref="ModCharacterWorldSceneVisuals" />。
        ///     可选商人/营火程序化 shell（无自定义商人或营火角色 <c>tscn</c>）；
        ///     见 <see cref="ModCharacterWorldSceneVisuals" />。
        ///     <see cref="ModCharacterWorldSceneVisuals" />。
        /// </summary>
        CharacterWorldProceduralVisualSet? WorldProceduralVisuals { get; }

        /// <summary>
        ///     Optional vanilla character id used with <see cref="CharacterAssetProfiles.Resolve" /> when expanding
        ///     partial <see cref="AssetProfile" /> data (defaults to <c>null</c>: no placeholder merge).
        ///     展开部分 <see cref="AssetProfile" /> 数据时，供 <see cref="CharacterAssetProfiles.Resolve" /> 使用的可选原版角色 id
        ///     （默认为 <c>null</c>：不进行占位符合并）。
        /// </summary>
        string? CharacterAssetPlaceholderCharacterId => null;

        /// <summary>
        ///     When <paramref name="relic" /> is owned by a player using this character, returns icon path overrides
        ///     registered for that relic’s <c>ModelId.Entry</c>; otherwise <c>null</c>. Patches resolve this before
        ///     mod-relic <c>IModRelicAssetOverrides</c> so per-owner character art wins over relic-wide defaults.
        ///     当 <paramref name="relic" /> 由使用此角色的玩家拥有时，返回为该遗物 <c>ModelId.Entry</c> 注册的图标路径覆盖；
        ///     否则返回 <c>null</c>。patch 会先于 mod 遗物 <c>IModRelicAssetOverrides</c> 解析此项，因此逐拥有者角色美术优先于遗物全局默认。
        /// </summary>
        RelicAssetProfile? TryGetVanillaRelicVisualOverrideForOwnedRelic(RelicModel relic);

        /// <summary>
        ///     When <paramref name="potion" /> is encountered or held by a player using this character, returns
        ///     image/outline overrides registered for that potion’s <c>ModelId.Entry</c>; otherwise <c>null</c>.
        ///     Patches resolve this before mod-potion <c>IModPotionAssetOverrides</c>.
        ///     当 <paramref name="potion" /> 被使用此角色的玩家遇到或持有时，返回
        ///     为该药水 <c>ModelId.Entry</c> 注册的图像/描边覆盖；否则返回 <c>null</c>。
        ///     patch 会先于 mod 药水 <c>IModPotionAssetOverrides</c> 解析此项。
        /// </summary>
        PotionAssetProfile? TryGetVanillaPotionVisualOverrideForContext(PotionModel potion);

        /// <summary>
        ///     When <paramref name="card" /> is encountered or held by a player using this character, returns
        ///     portrait/frame/banner/overlay overrides registered for that card’s <c>ModelId.Entry</c>;
        ///     otherwise <c>null</c>. Patches resolve this before mod-card <c>IModCardAssetOverrides</c>.
        ///     当 <paramref name="card" /> 被使用此角色的玩家遇到或持有时，返回
        ///     为该卡牌 <c>ModelId.Entry</c> 注册的肖像/框/banner/覆盖层覆盖；
        ///     否则返回 <c>null</c>。patch 会先于 mod 卡牌 <c>IModCardAssetOverrides</c> 解析此项。
        /// </summary>
        CardAssetProfile? TryGetVanillaCardVisualOverrideForContext(CardModel card);
    }

    /// <summary>
    ///     Base <see cref="CharacterModel" /> for mods: typed card/relic/potion pools, starting loadout,
    ///     <see cref="IModCharacterAssetOverrides" />, and optional <see cref="TryCreateCreatureVisuals" />.
    ///     mod 的基础 <see cref="CharacterModel" />：类型化卡牌/遗物/药水池、起始配置、
    ///     <see cref="IModCharacterAssetOverrides" />，以及可选 <see cref="TryCreateCreatureVisuals" />。
    /// </summary>
    /// <typeparam name="TCardPool">
    ///     Concrete <see cref="CardPoolModel" /> type registered for this character.
    ///     为此角色注册的具体 <see cref="CardPoolModel" /> 类型。
    /// </typeparam>
    /// <typeparam name="TRelicPool">
    ///     Concrete <see cref="RelicPoolModel" /> type registered for this character.
    ///     为此角色注册的具体 <see cref="RelicPoolModel" /> 类型。
    /// </typeparam>
    /// <typeparam name="TPotionPool">
    ///     Concrete <see cref="PotionPoolModel" /> type registered for this character.
    ///     为此角色注册的具体 <see cref="PotionPoolModel" /> 类型。
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
        ///     Resolves this character’s card pool from <typeparamref name="TCardPool" /> via <see cref="ModelDb" />.
        ///     通过 <see cref="ModelDb" /> 从 <typeparamref name="TCardPool" /> 解析此角色的卡牌池。
        /// </summary>
        public sealed override CardPoolModel CardPool =>
            ModelDb.GetById<CardPoolModel>(ModelDb.GetId<TCardPool>());

        /// <summary>
        ///     Resolves this character’s relic pool from <typeparamref name="TRelicPool" /> via <see cref="ModelDb" />.
        ///     通过 <see cref="ModelDb" /> 从 <typeparamref name="TRelicPool" /> 解析此角色的遗物池。
        /// </summary>
        public sealed override RelicPoolModel RelicPool =>
            ModelDb.GetById<RelicPoolModel>(ModelDb.GetId<TRelicPool>());

        /// <summary>
        ///     Resolves this character’s potion pool from <typeparamref name="TPotionPool" /> via <see cref="ModelDb" />.
        ///     通过 <see cref="ModelDb" /> 从 <typeparamref name="TPotionPool" /> 解析此角色的药水池。
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
        ///     Local starting deck cards owned by this character class. Additive character-starter registrations are appended
        ///     after this collection by <see cref="StartingDeck" />.
        ///     此角色类拥有的本地初始牌组卡牌。<see cref="StartingDeck" /> 会在此集合之后追加增量角色初始内容注册。
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
        ///     Local starting relics owned by this character class. Additive character-starter registrations are appended
        ///     after this collection by <see cref="StartingRelics" />.
        ///     此角色类拥有的本地初始遗物。<see cref="StartingRelics" /> 会在此集合之后追加增量角色初始内容注册。
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
        ///     Local starting potions owned by this character class. Additive character-starter registrations are appended
        ///     after this collection by <see cref="StartingPotions" />.
        ///     此角色类拥有的本地初始药水。<see cref="StartingPotions" /> 会在此集合之后追加增量角色初始内容注册。
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
        ///     Legacy local starter-deck hook. Prefer additive character-starter registration so starter content can be
        ///     appended outside the character class and remain insensitive to registration order.
        ///     旧版本地初始牌组钩子。优先使用增量角色初始内容注册，使初始内容可以
        ///     在角色类外追加，并且不受注册顺序影响。
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
        ///     CLR types of cards that form the starting deck; each type must be registered as a <see cref="CardModel" />.
        ///     Prefer additive character-starter registration in new mods.
        ///     组成初始牌组的卡牌 CLR 类型；每个类型都必须注册为 <see cref="CardModel" />。
        ///     新 mod 中优先使用增量角色初始内容注册。
        /// </summary>
        [Obsolete(
            "Prefer additive character-starter registration. This legacy hook requires repeating the same type for duplicate starter cards. "
            + "Override only for legacy mods; suppress CS0618 if required.")]
        protected virtual IEnumerable<Type> StartingDeckTypes => [];

        /// <summary>
        ///     Legacy local starting-relic hook. Prefer additive character-starter registration in new mods.
        ///     旧版本地初始遗物钩子。新 mod 中优先使用增量角色初始内容注册。
        /// </summary>
        [Obsolete(
            "Prefer additive character-starter registration through CharacterRegistrationEntry.AddStartingRelic(...) or "
            + "ModContentRegistry.RegisterCharacterStarterRelic(...). Override only for legacy mods; suppress CS0618 if required.")]
        protected virtual IEnumerable<Type> StartingRelicTypes => [];

        /// <summary>
        ///     Legacy local starting-potion hook. Prefer additive character-starter registration in new mods.
        ///     旧版本地初始药水钩子。新 mod 中优先使用增量角色初始内容注册。
        /// </summary>
        [Obsolete(
            "Prefer additive character-starter registration through CharacterRegistrationEntry.AddStartingPotion(...) or "
            + "ModContentRegistry.RegisterCharacterStarterPotion(...). Override only for legacy mods; suppress CS0618 if required.")]
        protected virtual IEnumerable<Type> StartingPotionTypes => [];

        /// <summary>
        ///     Optional prerequisite character type for vanilla <see cref="CharacterModel.GetUnlockText" /> (the
        ///     <c>{Prerequisite}</c> placeholder). Root
        ///     <see cref="Timeline.Scaffolding.CharacterUnlockEpochTemplate{TCharacter}" /> slots whose character declares
        ///     this property are obtained with Neow's initial expansion for
        ///     <see cref="MegaCrit.Sts2.Core.Models.Characters.Ironclad" /> prerequisites, or after a completed run as any
        ///     other prerequisite character.
        ///     用于原版 <see cref="CharacterModel.GetUnlockText" /> 的可选前置角色类型（
        ///     <c>{Prerequisite}</c> 占位符）。角色声明此属性时，其根
        ///     <see cref="Timeline.Scaffolding.CharacterUnlockEpochTemplate{TCharacter}" /> 槽位会在前置为
        ///     <see cref="MegaCrit.Sts2.Core.Models.Characters.Ironclad" /> 时随 Neow 初始扩展获得；其他前置角色则在完成该角色 run
        ///     后获得。
        /// </summary>
        protected virtual Type? UnlocksAfterRunAsType => null;

        /// <summary>
        ///     Placeholder vanilla character id used when merging partial <see cref="CharacterAssetProfile" /> data
        ///     (see <see cref="CharacterAssetProfiles.Resolve" />).
        ///     合并部分 <see cref="CharacterAssetProfile" /> 数据时使用的占位原版角色 id
        ///     （见 <see cref="CharacterAssetProfiles.Resolve" />）。
        /// </summary>
        // ReSharper disable once ReturnTypeCanBeNotNullable
        public virtual string? PlaceholderCharacterId => CharacterAssetProfiles.DefaultPlaceholderCharacterId;

        /// <summary>
        ///     Effective asset profile after resolving against <see cref="PlaceholderCharacterId" />.
        ///     针对 <see cref="PlaceholderCharacterId" /> 解析后的有效资产 profile。
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
        ///     Non-null combat visuals; otherwise <see cref="IModCharacterAssetOverrides.CustomVisualsPath" /> / vanilla
        ///     paths apply.
        ///     非 null 战斗视觉；否则应用 <see cref="IModCharacterAssetOverrides.CustomVisualsPath" /> / 原版
        ///     路径。
        /// </summary>
        protected virtual NCreatureVisuals? TryCreateCreatureVisuals()
        {
            return null;
        }

        /// <summary>
        ///     Optional override producing a fully wired Spine <see cref="CreatureAnimator" /> (state graph for idle /
        ///     hit / attack / cast / die / relaxed). Return <see langword="null" /> to defer to vanilla
        ///     <see cref="CharacterModel.GenerateAnimator" />. Prefer <see cref="ModAnimStateMachines.Standard" /> to
        ///     match baselib semantics.
        ///     可选覆盖，用于生成完整接线的 Spine <see cref="CreatureAnimator" />（idle /
        ///     hit / attack / cast / die / relaxed 的状态图）。返回 <see langword="null" /> 以交给原版
        ///     <see cref="CharacterModel.GenerateAnimator" />。优先使用 <see cref="ModAnimStateMachines.Standard" /> 以
        ///     匹配 baselib 语义。
        /// </summary>
        /// <param name="controller">
        ///     Spine controller attached to the character's combat visuals.
        ///     附加到角色战斗视觉的 Spine 控制器。
        /// </param>
        protected virtual CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller)
        {
            return null;
        }

        /// <summary>
        ///     Optional override producing a <see cref="ModAnimStateMachine" /> for the character's combat visuals
        ///     (any <see cref="IAnimationBackend" />, including Spine via
        ///     <see cref="ModAnimStateMachineBuilder.BuildSpine" />). Return <see langword="null" /> to defer to vanilla
        ///     Spine <see cref="CreatureAnimator" /> triggers or, when there is no Spine animator, to single-shot
        ///     playback via <c>ModCreatureVisualPlayback</c>.
        ///     可选覆盖，用于为角色战斗视觉生成 <see cref="ModAnimStateMachine" />
        ///     （任意 <see cref="IAnimationBackend" />，包括通过
        ///     <see cref="ModAnimStateMachineBuilder.BuildSpine" /> 使用 Spine）。返回 <see langword="null" /> 以交给原版
        ///     Spine <see cref="CreatureAnimator" /> 触发器；没有 Spine animator 时，则交给通过 <c>ModCreatureVisualPlayback</c> 的单次
        ///     播放。
        /// </summary>
        /// <param name="visualsRoot">
        ///     Combat visuals root node.
        ///     战斗视觉根节点。
        /// </param>
        /// <param name="character">
        ///     Character model (always <see langword="this" />, exposed for convenience).
        ///     角色模型（始终为 <see langword="this" />，仅为方便而暴露）。
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
            return SetupCustomCombatAnimationStateMachine(visualsRoot, character);
        }

        /// <summary>
        ///     Optional override producing a merchant / rest-site <see cref="ModAnimStateMachine" /> for the character.
        ///     Return <see langword="null" /> to defer to single-shot playback via <c>ModCreatureVisualPlayback</c>.
        ///     可选覆盖，用于为角色生成商人/营火 <see cref="ModAnimStateMachine" />。
        ///     返回 <see langword="null" /> 以交给通过 <c>ModCreatureVisualPlayback</c> 的单次播放。
        /// </summary>
        /// <param name="merchantRoot">
        ///     Merchant character root node.
        ///     商人角色根节点。
        /// </param>
        /// <param name="character">
        ///     Character model (always <see langword="this" />, exposed for convenience).
        ///     角色模型（始终为 <see langword="this" />，仅为方便而暴露）。
        /// </param>
        protected virtual ModAnimStateMachine? SetupCustomMerchantAnimationStateMachine(Node merchantRoot,
            CharacterModel character)
        {
            return null;
        }

        /// <summary>
        ///     Optional override producing a rest-site <see cref="ModAnimStateMachine" /> for the character. The default
        ///     delegates to <see cref="SetupCustomMerchantAnimationStateMachine" /> so one world-scene override can drive
        ///     both merchant and rest-site visuals.
        ///     可选覆盖，用于为角色生成休息点 <see cref="ModAnimStateMachine" />。默认转发到
        ///     <see cref="SetupCustomMerchantAnimationStateMachine" />，因此一个世界场景 override 可同时驱动商人和休息点视觉。
        /// </summary>
        /// <param name="restSiteRoot">
        ///     Rest-site character root node.
        ///     休息点角色根节点。
        /// </param>
        /// <param name="character">
        ///     Character model (always <see langword="this" />, exposed for convenience).
        ///     角色模型（始终为 <see langword="this" />，仅为方便而暴露）。
        /// </param>
        protected virtual ModAnimStateMachine? SetupCustomRestSiteAnimationStateMachine(Node restSiteRoot,
            CharacterModel character)
        {
            return SetupCustomMerchantAnimationStateMachine(restSiteRoot, character);
        }

        /// <summary>
        ///     Maps model CLR types to live <typeparamref name="TModel" /> instances from <see cref="ModelDb" />.
        ///     将模型 CLR 类型映射到来自 <see cref="ModelDb" /> 的实时 <typeparamref name="TModel" /> 实例。
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
