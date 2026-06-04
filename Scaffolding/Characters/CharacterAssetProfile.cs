using Godot;
using STS2RitsuLib.Scaffolding.Characters.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Characters
{
    /// <summary>
    ///     Scene paths for combat visuals, energy counter, merchant, and rest site animations.
    ///     战斗视觉、能量计数器、商人和休息点动画的场景路径。
    /// </summary>
    /// <param name="VisualsPath">
    ///     Creature visuals scene.
    ///     生物视觉场景。
    /// </param>
    /// <param name="EnergyCounterPath">
    ///     Energy counter scene.
    ///     能量计数器场景。
    /// </param>
    /// <param name="MerchantAnimPath">
    ///     Merchant character scene.
    ///     商人角色场景。
    /// </param>
    /// <param name="RestSiteAnimPath">
    ///     Rest site character scene.
    ///     休息点角色场景。
    /// </param>
    public sealed record CharacterSceneAssetSet(
        string? VisualsPath = null,
        string? EnergyCounterPath = null,
        string? MerchantAnimPath = null,
        string? RestSiteAnimPath = null);

    /// <summary>
    ///     UI textures and scenes: HUD icon, character select, map marker, transitions.
    ///     UI 贴图和场景：HUD 图标、角色选择、地图标记和转场。
    /// </summary>
    /// <param name="IconTexturePath">
    ///     Top-panel icon texture.
    ///     顶部面板图标贴图。
    /// </param>
    /// <param name="IconOutlineTexturePath">
    ///     Outlined variant for HUD.
    ///     HUD 使用的描边变体。
    /// </param>
    /// <param name="IconPath">
    ///     Optional icon scene.
    ///     可选图标场景。
    /// </param>
    /// <param name="CharacterSelectBgPath">
    ///     Character select background scene.
    ///     角色选择背景场景。
    /// </param>
    /// <param name="CharacterSelectIconPath">
    ///     Portrait when unlocked.
    ///     解锁时的肖像。
    /// </param>
    /// <param name="CharacterSelectLockedIconPath">
    ///     Portrait when locked.
    ///     未解锁时的肖像。
    /// </param>
    /// <param name="CharacterSelectTransitionPath">
    ///     Transition material resource.
    ///     转场材质资源。
    /// </param>
    /// <param name="MapMarkerPath">
    ///     Run map marker texture.
    ///     Run 地图标记贴图。
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
    ///     Card trail scene and optional style overrides.
    ///     卡牌轨迹场景和可选样式覆盖。
    /// </summary>
    /// <param name="TrailPath">
    ///     Trail VFX scene path.
    ///     轨迹 VFX 场景路径。
    /// </param>
    /// <param name="TrailStyle">
    ///     Trail color/width tuning.
    ///     轨迹颜色 / 宽度调节。
    /// </param>
    public sealed record CharacterVfxAssetSet(
        string? TrailPath = null,
        CharacterTrailStyle? TrailStyle = null);

    /// <summary>
    ///     Tunable trail / sparkle parameters applied by trail override patches.
    ///     由轨迹覆盖补丁应用的可调轨迹 / 火花参数。
    /// </summary>
    /// <param name="OuterTrailModulate">
    ///     Outer ribbon tint.
    ///     外侧缎带染色。
    /// </param>
    /// <param name="OuterTrailWidth">
    ///     Outer ribbon width scale.
    ///     外侧缎带宽度缩放。
    /// </param>
    /// <param name="InnerTrailModulate">
    ///     Inner ribbon tint.
    ///     内侧缎带染色。
    /// </param>
    /// <param name="InnerTrailWidth">
    ///     Inner ribbon width scale.
    ///     内侧缎带宽度缩放。
    /// </param>
    /// <param name="BigSparksColor">
    ///     Large spark color.
    ///     大火花颜色。
    /// </param>
    /// <param name="LittleSparksColor">
    ///     Small spark color.
    ///     小火花颜色。
    /// </param>
    /// <param name="PrimarySpriteModulate">
    ///     Primary trail sprite tint.
    ///     主 trail 精灵色调。
    /// </param>
    /// <param name="PrimarySpriteScale">
    ///     Primary trail sprite scale.
    ///     主 trail 精灵缩放。
    /// </param>
    /// <param name="SecondarySpriteModulate">
    ///     Secondary trail sprite tint.
    ///     副 trail 精灵色调。
    /// </param>
    /// <param name="SecondarySpriteScale">
    ///     Secondary trail sprite scale.
    ///     副 trail 精灵缩放。
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
    ///     Spine skeleton data used in combat.
    ///     战斗中使用的 Spine skeleton 数据。
    /// </summary>
    /// <param name="CombatSkeletonDataPath">
    ///     Spine skeleton resource path.
    ///     Spine skeleton ResourcePath。
    /// </param>
    public sealed record CharacterSpineAssetSet(
        string? CombatSkeletonDataPath = null);

    /// <summary>
    ///     FMOD-style event paths for character feedback audio.
    ///     角色反馈音频的 FMOD 风格事件路径。
    /// </summary>
    /// <param name="CharacterSelectSfx">
    ///     Select / confirm on character screen.
    ///     角色界面选择 / 确认音效。
    /// </param>
    /// <param name="CharacterTransitionSfx">
    ///     Screen transition sting.
    ///     界面转场短音效。
    /// </param>
    /// <param name="AttackSfx">
    ///     Basic attack cue.
    ///     基础攻击 cue。
    /// </param>
    /// <param name="CastSfx">
    ///     Card cast cue.
    ///     卡牌施放 cue。
    /// </param>
    /// <param name="DeathSfx">
    ///     Player death cue.
    ///     玩家死亡 cue。
    /// </param>
    public sealed record CharacterAudioAssetSet(
        string? CharacterSelectSfx = null,
        string? CharacterTransitionSfx = null,
        string? AttackSfx = null,
        string? CastSfx = null,
        string? DeathSfx = null);

    /// <summary>
    ///     RPS hand textures for multiplayer UI.
    ///     多人 UI 的石头剪刀布手势贴图。
    /// </summary>
    /// <param name="ArmPointingTexturePath">
    ///     Pointing hand.
    ///     指向手势。
    /// </param>
    /// <param name="ArmRockTexturePath">
    ///     Rock hand.
    ///     石头手势。
    /// </param>
    /// <param name="ArmPaperTexturePath">
    ///     Paper hand.
    ///     布手势。
    /// </param>
    /// <param name="ArmScissorsTexturePath">
    ///     Scissors hand.
    ///     剪刀手势。
    /// </param>
    public sealed record CharacterMultiplayerAssetSet(
        string? ArmPointingTexturePath = null,
        string? ArmRockTexturePath = null,
        string? ArmPaperTexturePath = null,
        string? ArmScissorsTexturePath = null);

    /// <summary>
    ///     One entry in <see cref="CharacterAssetProfile.VanillaRelicVisualOverrides" />: when this mod character owns a
    ///     relic whose <c>ModelId.Entry</c> equals <paramref name="RelicModelIdEntry" /> (ordinal ignore-case), use
    ///     <paramref name="Assets" /> for icon paths.
    ///     <see cref="CharacterAssetProfile.VanillaRelicVisualOverrides" /> 中的一个条目：当此 mod 角色拥有
    ///     <c>ModelId.Entry</c> 等于 <paramref name="RelicModelIdEntry" />（ordinal ignore-case）的遗物时，使用
    ///     <paramref name="Assets" /> 作为图标路径。
    /// </summary>
    /// <param name="RelicModelIdEntry">
    ///     Stable relic id (same string as <c>RelicModel.Id.Entry</c>).
    ///     稳定遗物 id（与 <c>RelicModel.Id.Entry</c> 相同的字符串）。
    /// </param>
    /// <param name="Assets">
    ///     Packed icon, outline, and large art paths (same shape as mod relic
    ///     <see cref="RelicAssetProfile" />).
    ///     打包后的图标、描边和大图路径（与 mod 遗物 <see cref="RelicAssetProfile" /> 形态相同）。
    /// </param>
    public sealed record CharacterVanillaRelicVisualOverride(string RelicModelIdEntry, RelicAssetProfile Assets);

    /// <summary>
    ///     One entry in <see cref="CharacterAssetProfile.VanillaPotionVisualOverrides" />: when this mod character
    ///     encounters or holds a potion whose <c>ModelId.Entry</c> equals <paramref name="PotionModelIdEntry" />
    ///     (ordinal ignore-case), use <paramref name="Assets" /> for image/outline paths.
    ///     <see cref="CharacterAssetProfile.VanillaPotionVisualOverrides" /> 中的一个条目：当此 mod 角色
    ///     遇到或持有 <c>ModelId.Entry</c> 等于 <paramref name="PotionModelIdEntry" />
    ///     （ordinal ignore-case）的药水时，使用 <paramref name="Assets" /> 作为图像/描边路径。
    /// </summary>
    /// <param name="PotionModelIdEntry">
    ///     Stable potion id (same string as <c>PotionModel.Id.Entry</c>).
    ///     稳定药水 id（与 <c>PotionModel.Id.Entry</c> 相同的字符串）。
    /// </param>
    /// <param name="Assets">
    ///     Bottle and outline paths (same shape as mod potion <see cref="PotionAssetProfile" />).
    ///     瓶身和描边路径（与 mod 药水 <see cref="PotionAssetProfile" /> 形态相同）。
    /// </param>
    public sealed record CharacterVanillaPotionVisualOverride(string PotionModelIdEntry, PotionAssetProfile Assets);

    /// <summary>
    ///     One entry in <see cref="CharacterAssetProfile.VanillaCardVisualOverrides" />: when this mod character
    ///     encounters or holds a card whose <c>ModelId.Entry</c> equals <paramref name="CardModelIdEntry" />
    ///     (ordinal ignore-case), use <paramref name="Assets" /> for portrait/frame/banner/overlay paths and materials.
    ///     <see cref="CharacterAssetProfile.VanillaCardVisualOverrides" /> 中的一个条目：当此 mod 角色
    ///     遇到或持有 <c>ModelId.Entry</c> 等于 <paramref name="CardModelIdEntry" />
    ///     （ordinal ignore-case）的卡牌时，使用 <paramref name="Assets" /> 作为肖像/边框/banner/覆盖层路径和材质。
    /// </summary>
    /// <param name="CardModelIdEntry">
    ///     Stable card id (same string as <c>CardModel.Id.Entry</c>).
    ///     稳定卡牌 id（与 <c>CardModel.Id.Entry</c> 相同的字符串）。
    /// </param>
    /// <param name="Assets">
    ///     Card portrait and frame/border/material/overlay/banner path and material bundle (same shape as
    ///     mod card <see cref="CardAssetProfile" />).
    ///     卡牌肖像和框/边框/材质/覆盖层/banner 路径和材质包（与
    ///     mod 卡牌 <see cref="CardAssetProfile" /> 形态相同）。
    /// </param>
    public sealed record CharacterVanillaCardVisualOverride(string CardModelIdEntry, CardAssetProfile Assets);

    /// <summary>
    ///     Well-known <see cref="CharacterVanillaRelicVisualOverride.RelicModelIdEntry" /> values for base-game relics
    ///     that commonly need per-character art.
    ///     基础游戏遗物中常需要逐角色美术的知名 <see cref="CharacterVanillaRelicVisualOverride.RelicModelIdEntry" /> 值。
    /// </summary>
    public static class CharacterOwnedVanillaRelicModelId
    {
        /// <summary>
        ///     Canonical entry id for the vanilla <c>YummyCookie</c> relic (uppercase); live
        ///     <c>RelicModel.Id.Entry</c> is still matched ordinal-ignore-case.
        ///     原版 <c>YummyCookie</c> 遗物的规范条目 id（大写）；实时
        ///     <c>RelicModel.Id.Entry</c> 仍按 ordinal-ignore-case 匹配。
        /// </summary>
        public const string YummyCookie = "YUMMY_COOKIE";
    }

    /// <summary>
    ///     Bundles optional path sets for scaffolding a mod character alongside vanilla layout conventions.
    ///     按原版布局约定打包用于搭建 mod 角色的可选路径集合。
    /// </summary>
    /// <param name="Scenes">
    ///     Combat / world scenes.
    ///     战斗 / 世界场景。
    /// </param>
    /// <param name="Ui">
    ///     HUD and character select assets.
    ///     HUD 和角色选择资源。
    /// </param>
    /// <param name="Vfx">
    ///     Trails and similar.
    ///     轨迹和类似 VFX。
    /// </param>
    /// <param name="Spine">
    ///     Spine data.
    ///     Spine 数据。
    /// </param>
    /// <param name="Audio">
    ///     FMOD event ids or paths.
    ///     FMOD 事件 id 或路径。
    /// </param>
    /// <param name="Multiplayer">
    ///     Multiplayer hand art.
    ///     多人手势美术。
    /// </param>
    /// <param name="VisualCues">
    ///     Per-cue textures / frame sequences (combat, game-over, and other consumers).
    ///     逐 cue 贴图 / 帧序列（战斗、游戏结束和其它消费者）。
    /// </param>
    /// <param name="WorldProceduralVisuals">
    ///     Merchant / rest-site shells without custom character <c>tscn</c> scenes.
    ///     没有自定义角色 <c>tscn</c> 场景的商人/营火 shell。
    /// </param>
    /// <param name="VanillaRelicVisualOverrides">
    ///     Per–relic-id icon overrides when this character is the relic owner (see
    ///     <see cref="CharacterVanillaRelicVisualOverride" />).
    ///     当此角色是遗物拥有者时，按遗物 id 应用的图标覆盖（见
    ///     <see cref="CharacterVanillaRelicVisualOverride" />）。
    /// </param>
    /// <param name="VanillaPotionVisualOverrides">
    ///     Per–potion-id image/outline overrides when this character encounters or holds that potion (see
    ///     <see cref="CharacterVanillaPotionVisualOverride" />).
    ///     当此角色遇到或持有该药水时，按药水 id 应用的图片 / 轮廓覆盖（见
    ///     <see cref="CharacterVanillaPotionVisualOverride" />）。
    /// </param>
    /// <param name="VanillaCardVisualOverrides">
    ///     Per–card-id portrait/frame/banner/overlay overrides when this character encounters or holds that card (see
    ///     <see cref="CharacterVanillaCardVisualOverride" />).
    ///     当此角色遇到或持有该卡牌时，按卡牌 id 应用的肖像 / 边框 / 横幅 / 覆盖层覆盖（见
    ///     <see cref="CharacterVanillaCardVisualOverride" />）。
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
        ///     Binary compatibility constructor for assemblies compiled before
        ///     <see cref="VanillaRelicVisualOverrides" /> was added (legacy eight-parameter <c>.ctor</c>).
        ///     为在添加 <see cref="VanillaRelicVisualOverrides" /> 之前编译的程序集提供二进制兼容构造函数（旧版八参数 <c>.ctor</c>）。
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
        ///     Profile with all components null (merge / fill helpers treat null as “missing”).
        ///     所有组件均为 null 的 profile（merge/fill helper 将 null 视为“缺失”）。
        /// </summary>
        public static CharacterAssetProfile Empty { get; } = new();
    }
}
