using Godot;
using STS2RitsuLib.Scaffolding.Characters.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Characters
{
    /// <summary>
    ///     <para xml:lang="en">Groups character scene paths used in combat and world rooms.</para>
    ///     <para xml:lang="zh-CN">组合角色在战斗与世界房间中使用的场景路径。</para>
    /// </summary>
    /// <param name="VisualsPath">
    ///     <para xml:lang="en">Combat creature-visuals scene path.</para>
    ///     <para xml:lang="zh-CN">战斗生物视觉节点的场景路径。</para>
    /// </param>
    /// <param name="EnergyCounterPath">
    ///     <para xml:lang="en">Combat energy counter scene path.</para>
    ///     <para xml:lang="zh-CN">战斗能量计数器的场景路径。</para>
    /// </param>
    /// <param name="MerchantAnimPath">
    ///     <para xml:lang="en">Merchant-room character scene path.</para>
    ///     <para xml:lang="zh-CN">商店房间角色场景的路径。</para>
    /// </param>
    /// <param name="RestSiteAnimPath">
    ///     <para xml:lang="en">Rest-site character scene path.</para>
    ///     <para xml:lang="zh-CN">休息处角色场景的路径。</para>
    /// </param>
    public sealed record CharacterSceneAssetSet(
        string? VisualsPath = null,
        string? EnergyCounterPath = null,
        string? MerchantAnimPath = null,
        string? RestSiteAnimPath = null);

    /// <summary>
    ///     <para xml:lang="en">Groups character UI texture, scene, and material paths.</para>
    ///     <para xml:lang="zh-CN">组合角色界面使用的贴图、场景与材质路径。</para>
    /// </summary>
    /// <param name="IconTexturePath">
    ///     <para xml:lang="en">Top-panel icon texture path.</para>
    ///     <para xml:lang="zh-CN">顶部面板图标贴图的路径。</para>
    /// </param>
    /// <param name="IconOutlineTexturePath">
    ///     <para xml:lang="en">Top-panel icon outline texture path.</para>
    ///     <para xml:lang="zh-CN">顶部面板图标描边贴图的路径。</para>
    /// </param>
    /// <param name="IconPath">
    ///     <para xml:lang="en">Optional character icon scene path.</para>
    ///     <para xml:lang="zh-CN">可选的角色图标场景路径。</para>
    /// </param>
    /// <param name="CharacterSelectBgPath">
    ///     <para xml:lang="en">Character-select background scene path.</para>
    ///     <para xml:lang="zh-CN">角色选择背景的场景路径。</para>
    /// </param>
    /// <param name="CharacterSelectIconPath">
    ///     <para xml:lang="en">Unlocked character-select portrait path.</para>
    ///     <para xml:lang="zh-CN">已解锁角色选择肖像的路径。</para>
    /// </param>
    /// <param name="CharacterSelectLockedIconPath">
    ///     <para xml:lang="en">Locked character-select portrait path.</para>
    ///     <para xml:lang="zh-CN">未解锁角色选择肖像的路径。</para>
    /// </param>
    /// <param name="CharacterSelectTransitionPath">
    ///     <para xml:lang="en">Character-select transition material path.</para>
    ///     <para xml:lang="zh-CN">角色选择转场材质的路径。</para>
    /// </param>
    /// <param name="MapMarkerPath">
    ///     <para xml:lang="en">Run-map marker texture path.</para>
    ///     <para xml:lang="zh-CN">游戏地图标记贴图的路径。</para>
    /// </param>
    public sealed record CharacterUiAssetSet(
        string? IconTexturePath = null,
        string? IconOutlineTexturePath = null,
        string? IconPath = null,
        string? CharacterSelectBgPath = null,
        string? CharacterSelectIconPath = null,
        string? CharacterSelectLockedIconPath = null,
        string? CharacterSelectTransitionPath = null,
        string? MapMarkerPath = null);

    /// <summary>
    ///     <para xml:lang="en">Groups the card-trail scene path and optional style overrides.</para>
    ///     <para xml:lang="zh-CN">组合卡牌轨迹场景路径与可选样式覆盖。</para>
    /// </summary>
    /// <param name="TrailPath">
    ///     <para xml:lang="en">Card-trail VFX scene path.</para>
    ///     <para xml:lang="zh-CN">卡牌轨迹特效的场景路径。</para>
    /// </param>
    /// <param name="TrailStyle">
    ///     <para xml:lang="en">Optional card-trail style overrides.</para>
    ///     <para xml:lang="zh-CN">可选的卡牌轨迹样式覆盖。</para>
    /// </param>
    public sealed record CharacterVfxAssetSet(
        string? TrailPath = null,
        CharacterTrailStyle? TrailStyle = null);

    /// <summary>
    ///     <para xml:lang="en">Defines optional card-trail ribbon, spark, and sprite overrides.</para>
    ///     <para xml:lang="zh-CN">定义可选的卡牌轨迹带、火花与精灵覆盖。</para>
    /// </summary>
    /// <param name="OuterTrailModulate">
    ///     <para xml:lang="en">Outer trail ribbon modulation.</para>
    ///     <para xml:lang="zh-CN">外侧轨迹带的调制颜色。</para>
    /// </param>
    /// <param name="OuterTrailWidth">
    ///     <para xml:lang="en">Outer trail ribbon width scale.</para>
    ///     <para xml:lang="zh-CN">外侧轨迹带的宽度缩放。</para>
    /// </param>
    /// <param name="InnerTrailModulate">
    ///     <para xml:lang="en">Inner trail ribbon modulation.</para>
    ///     <para xml:lang="zh-CN">内侧轨迹带的调制颜色。</para>
    /// </param>
    /// <param name="InnerTrailWidth">
    ///     <para xml:lang="en">Inner trail ribbon width scale.</para>
    ///     <para xml:lang="zh-CN">内侧轨迹带的宽度缩放。</para>
    /// </param>
    /// <param name="BigSparksColor">
    ///     <para xml:lang="en">Large-spark color.</para>
    ///     <para xml:lang="zh-CN">大火花的颜色。</para>
    /// </param>
    /// <param name="LittleSparksColor">
    ///     <para xml:lang="en">Small-spark color.</para>
    ///     <para xml:lang="zh-CN">小火花的颜色。</para>
    /// </param>
    /// <param name="PrimarySpriteModulate">
    ///     <para xml:lang="en">Primary trail sprite modulation.</para>
    ///     <para xml:lang="zh-CN">主轨迹精灵的调制颜色。</para>
    /// </param>
    /// <param name="PrimarySpriteScale">
    ///     <para xml:lang="en">Primary trail sprite scale.</para>
    ///     <para xml:lang="zh-CN">主轨迹精灵的缩放。</para>
    /// </param>
    /// <param name="SecondarySpriteModulate">
    ///     <para xml:lang="en">Secondary trail sprite modulation.</para>
    ///     <para xml:lang="zh-CN">副轨迹精灵的调制颜色。</para>
    /// </param>
    /// <param name="SecondarySpriteScale">
    ///     <para xml:lang="en">Secondary trail sprite scale.</para>
    ///     <para xml:lang="zh-CN">副轨迹精灵的缩放。</para>
    /// </param>
    public sealed record CharacterTrailStyle(
        Color? OuterTrailModulate = null,
        float? OuterTrailWidth = null,
        Color? InnerTrailModulate = null,
        float? InnerTrailWidth = null,
        Color? BigSparksColor = null,
        Color? LittleSparksColor = null,
        Color? PrimarySpriteModulate = null,
        Vector2? PrimarySpriteScale = null,
        Color? SecondarySpriteModulate = null,
        Vector2? SecondarySpriteScale = null);

    /// <summary>
    ///     <para xml:lang="en">Groups Spine resources used by a character in combat.</para>
    ///     <para xml:lang="zh-CN">组合角色在战斗中使用的 Spine 资源。</para>
    /// </summary>
    /// <param name="CombatSkeletonDataPath">
    ///     <para xml:lang="en">Combat Spine skeleton-data resource path.</para>
    ///     <para xml:lang="zh-CN">战斗 Spine 骨骼数据的资源路径。</para>
    /// </param>
    public sealed record CharacterSpineAssetSet(
        string? CombatSkeletonDataPath = null);

    /// <summary>
    ///     <para xml:lang="en">Groups FMOD Studio event paths for character feedback sounds.</para>
    ///     <para xml:lang="zh-CN">组合角色反馈音效使用的 FMOD Studio 事件路径。</para>
    /// </summary>
    /// <param name="CharacterSelectSfx">
    ///     <para xml:lang="en">Character-select confirmation event path.</para>
    ///     <para xml:lang="zh-CN">角色选择确认音效的事件路径。</para>
    /// </param>
    /// <param name="CharacterTransitionSfx">
    ///     <para xml:lang="en">Character transition event path.</para>
    ///     <para xml:lang="zh-CN">角色转场音效的事件路径。</para>
    /// </param>
    /// <param name="AttackSfx">
    ///     <para xml:lang="en">Default attack event path.</para>
    ///     <para xml:lang="zh-CN">默认攻击音效的事件路径。</para>
    /// </param>
    /// <param name="CastSfx">
    ///     <para xml:lang="en">Default card-cast event path.</para>
    ///     <para xml:lang="zh-CN">默认卡牌施放音效的事件路径。</para>
    /// </param>
    /// <param name="DeathSfx">
    ///     <para xml:lang="en">Player-death event path.</para>
    ///     <para xml:lang="zh-CN">玩家死亡音效的事件路径。</para>
    /// </param>
    public sealed record CharacterAudioAssetSet(
        string? CharacterSelectSfx = null,
        string? CharacterTransitionSfx = null,
        string? AttackSfx = null,
        string? CastSfx = null,
        string? DeathSfx = null);

    /// <summary>
    ///     <para xml:lang="en">Groups multiplayer pointing and rock-paper-scissors hand textures.</para>
    ///     <para xml:lang="zh-CN">组合多人游戏使用的指向与石头剪刀布手势贴图。</para>
    /// </summary>
    /// <param name="ArmPointingTexturePath">
    ///     <para xml:lang="en">Pointing-hand texture path.</para>
    ///     <para xml:lang="zh-CN">指向手势贴图的路径。</para>
    /// </param>
    /// <param name="ArmRockTexturePath">
    ///     <para xml:lang="en">Rock-hand texture path.</para>
    ///     <para xml:lang="zh-CN">石头手势贴图的路径。</para>
    /// </param>
    /// <param name="ArmPaperTexturePath">
    ///     <para xml:lang="en">Paper-hand texture path.</para>
    ///     <para xml:lang="zh-CN">布手势贴图的路径。</para>
    /// </param>
    /// <param name="ArmScissorsTexturePath">
    ///     <para xml:lang="en">Scissors-hand texture path.</para>
    ///     <para xml:lang="zh-CN">剪刀手势贴图的路径。</para>
    /// </param>
    public sealed record CharacterMultiplayerAssetSet(
        string? ArmPointingTexturePath = null,
        string? ArmRockTexturePath = null,
        string? ArmPaperTexturePath = null,
        string? ArmScissorsTexturePath = null);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines character-specific visuals for a base-game relic.
    ///     </para>
    ///     <para xml:lang="zh-CN">定义某件原版遗物的角色专属视觉资源。</para>
    /// </summary>
    /// <param name="RelicModelIdEntry">
    ///     <para xml:lang="en">Relic <c>ModelId.Entry</c>, matched without regard to case.</para>
    ///     <para xml:lang="zh-CN">遗物的 <c>ModelId.Entry</c>；匹配时不区分大小写。</para>
    /// </param>
    /// <param name="Assets">
    ///     <para xml:lang="en">Replacement relic asset profile.</para>
    ///     <para xml:lang="zh-CN">替换用遗物资源配置。</para>
    /// </param>
    public sealed record CharacterVanillaRelicVisualOverride(string RelicModelIdEntry, RelicAssetProfile Assets);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines character-specific visuals for a base-game potion.
    ///     </para>
    ///     <para xml:lang="zh-CN">定义某瓶原版药水的角色专属视觉资源。</para>
    /// </summary>
    /// <param name="PotionModelIdEntry">
    ///     <para xml:lang="en">Potion <c>ModelId.Entry</c>, matched without regard to case.</para>
    ///     <para xml:lang="zh-CN">药水的 <c>ModelId.Entry</c>；匹配时不区分大小写。</para>
    /// </param>
    /// <param name="Assets">
    ///     <para xml:lang="en">Replacement potion asset profile.</para>
    ///     <para xml:lang="zh-CN">替换用药水资源配置。</para>
    /// </param>
    public sealed record CharacterVanillaPotionVisualOverride(string PotionModelIdEntry, PotionAssetProfile Assets);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines character-specific visuals for a base-game card.
    ///     </para>
    ///     <para xml:lang="zh-CN">定义某张原版卡牌的角色专属视觉资源。</para>
    /// </summary>
    /// <param name="CardModelIdEntry">
    ///     <para xml:lang="en">Card <c>ModelId.Entry</c>, matched without regard to case.</para>
    ///     <para xml:lang="zh-CN">卡牌的 <c>ModelId.Entry</c>；匹配时不区分大小写。</para>
    /// </param>
    /// <param name="Assets">
    ///     <para xml:lang="en">Replacement card asset profile.</para>
    ///     <para xml:lang="zh-CN">替换用卡牌资源配置。</para>
    /// </param>
    public sealed record CharacterVanillaCardVisualOverride(string CardModelIdEntry, CardAssetProfile Assets);

    /// <summary>
    ///     <para xml:lang="en">Provides well-known base-game relic entry names used by character-specific art.</para>
    ///     <para xml:lang="zh-CN">提供角色专属美术常用的原版遗物条目名。</para>
    /// </summary>
    public static class CharacterOwnedVanillaRelicModelId
    {
        /// <summary>
        ///     <para xml:lang="en">Entry name of the base-game <c>YummyCookie</c> relic.</para>
        ///     <para xml:lang="zh-CN">原版 <c>YummyCookie</c> 遗物的条目名。</para>
        /// </summary>
        public const string YummyCookie = "YUMMY_COOKIE";
    }

    /// <summary>
    ///     <para xml:lang="en">Groups optional assets and visual overrides for a mod character.</para>
    ///     <para xml:lang="zh-CN">组合模组角色的可选资源与视觉覆盖。</para>
    /// </summary>
    /// <param name="Scenes">
    ///     <para xml:lang="en">Combat and world-room scenes.</para>
    ///     <para xml:lang="zh-CN">战斗与世界房间场景。</para>
    /// </param>
    /// <param name="Ui">
    ///     <para xml:lang="en">HUD, character-select, and map UI assets.</para>
    ///     <para xml:lang="zh-CN">HUD、角色选择与地图界面资源。</para>
    /// </param>
    /// <param name="Vfx">
    ///     <para xml:lang="en">Card-trail assets and style.</para>
    ///     <para xml:lang="zh-CN">卡牌轨迹资源与样式。</para>
    /// </param>
    /// <param name="Spine">
    ///     <para xml:lang="en">Combat Spine resources.</para>
    ///     <para xml:lang="zh-CN">战斗 Spine 资源。</para>
    /// </param>
    /// <param name="Audio">
    ///     <para xml:lang="en">Character FMOD Studio event paths.</para>
    ///     <para xml:lang="zh-CN">角色 FMOD Studio 事件路径。</para>
    /// </param>
    /// <param name="Multiplayer">
    ///     <para xml:lang="en">Multiplayer hand textures.</para>
    ///     <para xml:lang="zh-CN">多人游戏手势贴图。</para>
    /// </param>
    /// <param name="VisualCues">
    ///     <para xml:lang="en">Named visual cues used by combat, game-over, and other character displays.</para>
    ///     <para xml:lang="zh-CN">战斗、游戏结束等角色显示场景使用的具名视觉提示。</para>
    /// </param>
    /// <param name="WorldProceduralVisuals">
    ///     <para xml:lang="en">Procedural merchant and rest-site visuals.</para>
    ///     <para xml:lang="zh-CN">程序化生成的商店与休息处视觉资源。</para>
    /// </param>
    /// <param name="VanillaRelicVisualOverrides">
    ///     <para xml:lang="en">Character-specific visual overrides for base-game relics.</para>
    ///     <para xml:lang="zh-CN">原版遗物的角色专属视觉覆盖。</para>
    /// </param>
    /// <param name="VanillaPotionVisualOverrides">
    ///     <para xml:lang="en">Character-specific visual overrides for base-game potions.</para>
    ///     <para xml:lang="zh-CN">原版药水的角色专属视觉覆盖。</para>
    /// </param>
    /// <param name="VanillaCardVisualOverrides">
    ///     <para xml:lang="en">Character-specific visual overrides for base-game cards.</para>
    ///     <para xml:lang="zh-CN">原版卡牌的角色专属视觉覆盖。</para>
    /// </param>
    public sealed record CharacterAssetProfile(
        CharacterSceneAssetSet? Scenes = null,
        CharacterUiAssetSet? Ui = null,
        CharacterVfxAssetSet? Vfx = null,
        CharacterSpineAssetSet? Spine = null,
        CharacterAudioAssetSet? Audio = null,
        CharacterMultiplayerAssetSet? Multiplayer = null,
        VisualCueSet? VisualCues = null,
        CharacterWorldProceduralVisualSet? WorldProceduralVisuals = null,
        CharacterVanillaRelicVisualOverride[]? VanillaRelicVisualOverrides = null,
        CharacterVanillaPotionVisualOverride[]? VanillaPotionVisualOverrides = null,
        CharacterVanillaCardVisualOverride[]? VanillaCardVisualOverrides = null)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Preserves the legacy eight-parameter constructor for binary compatibility.
        ///     </para>
        ///     <para xml:lang="zh-CN">保留旧版八参数构造函数以维持二进制兼容性。</para>
        /// </summary>
        public CharacterAssetProfile(
            CharacterSceneAssetSet? scenes,
            CharacterUiAssetSet? ui,
            CharacterVfxAssetSet? vfx,
            CharacterSpineAssetSet? spine,
            CharacterAudioAssetSet? audio,
            CharacterMultiplayerAssetSet? multiplayer,
            VisualCueSet? visualCues,
            CharacterWorldProceduralVisualSet? worldProceduralVisuals)
            : this(
                scenes,
                ui,
                vfx,
                spine,
                audio,
                multiplayer,
                visualCues,
                worldProceduralVisuals,
                null)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Profile whose optional components are all <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN">所有可选组件均为 <see langword="null" /> 的配置。</para>
        /// </summary>
        public static CharacterAssetProfile Empty { get; } = new();
    }
}
