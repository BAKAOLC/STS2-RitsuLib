using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Bundle of optional resource paths for mod card portraits, frames, energy icon, overlay scene, and banner.
    ///     Mod 卡牌肖像、边框、能量图标、覆盖场景和横幅的可选ResourcePath集合。
    /// </summary>
    /// <param name="PortraitPath">
    ///     Main card portrait image path.
    ///     主卡牌肖像图片路径。
    /// </param>
    /// <param name="BetaPortraitPath">
    ///     Alternate “beta” portrait path, if any.
    ///     可选的替代 “beta” 肖像路径。
    /// </param>
    /// <param name="FramePath">
    ///     Card frame texture path.
    ///     卡牌边框贴图路径。
    /// </param>
    /// <param name="PortraitBorderPath">
    ///     Portrait border / frame accent texture.
    ///     肖像边框 / 边框强调贴图。
    /// </param>
    /// <param name="EnergyIconPath">
    ///     Small energy icon texture for this card.
    ///     此卡牌的小型能量图标贴图。
    /// </param>
    /// <param name="FrameMaterialPath">
    ///     Material resource path for the card frame.
    ///     卡牌边框的材质ResourcePath。
    /// </param>
    /// <param name="OverlayScenePath">
    ///     Packed scene path for built-in card overlay UI.
    ///     内置卡牌覆盖 UI 的 PackedScene 路径。
    /// </param>
    /// <param name="BannerTexturePath">
    ///     Texture used on run-summary or banner UI.
    ///     运行总结或横幅 UI 使用的贴图。
    /// </param>
    /// <param name="BannerMaterialPath">
    ///     Material path for banner rendering.
    ///     横幅渲染使用的材质路径。
    /// </param>
    /// <param name="FrameMaterial">
    ///     Direct card frame material override.
    ///     直接覆盖卡牌边框材质。
    /// </param>
    /// <param name="BannerMaterial">
    ///     Direct banner material override.
    ///     直接覆盖横幅材质。
    /// </param>
    public sealed record CardAssetProfile(
        string? PortraitPath = null,
        string? BetaPortraitPath = null,
        string? FramePath = null,
        string? PortraitBorderPath = null,
        string? EnergyIconPath = null,
        string? FrameMaterialPath = null,
        string? OverlayScenePath = null,
        string? BannerTexturePath = null,
        string? BannerMaterialPath = null,
        Material? FrameMaterial = null,
        Material? BannerMaterial = null)
    {
        /// <summary>
        ///     Backward-compatible constructor preserving the original parameter list.
        ///     保留原始参数列表的向后兼容构造函数。
        /// </summary>
        public CardAssetProfile(
            string? PortraitPath,
            string? BetaPortraitPath,
            string? FramePath,
            string? PortraitBorderPath,
            string? EnergyIconPath,
            string? FrameMaterialPath,
            string? OverlayScenePath,
            string? BannerTexturePath,
            string? BannerMaterialPath)
            : this(
                PortraitPath,
                BetaPortraitPath,
                FramePath,
                PortraitBorderPath,
                EnergyIconPath,
                FrameMaterialPath,
                OverlayScenePath,
                BannerTexturePath,
                BannerMaterialPath,
                null)
        {
        }

        /// <summary>
        ///     Default empty profile (no custom paths).
        ///     默认空 profile（无自定义路径）。
        /// </summary>
        public static CardAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional relic icon paths (atlas entries and large shop/detail image).
    ///     可选遗物图标路径（atlas 条目和商店/详情大图）。
    /// </summary>
    /// <param name="IconPath">
    ///     Primary relic icon texture path.
    ///     主要遗物图标贴图路径。
    /// </param>
    /// <param name="IconOutlinePath">
    ///     Outline / silhouette icon path.
    ///     轮廓 / 剪影图标路径。
    /// </param>
    /// <param name="BigIconPath">
    ///     Large relic art path.
    ///     遗物大图路径。
    /// </param>
    public sealed record RelicAssetProfile(
        string? IconPath = null,
        string? IconOutlinePath = null,
        string? BigIconPath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        ///     默认空 profile（无自定义路径）。
        /// </summary>
        public static RelicAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional power icon paths (atlas and large illustration).
    ///     可选能力图标路径（atlas 和大图）。
    /// </summary>
    /// <param name="IconPath">
    ///     Power icon texture path.
    ///     能力图标贴图路径。
    /// </param>
    /// <param name="BigIconPath">
    ///     Large power art path.
    ///     能力大图路径。
    /// </param>
    public sealed record PowerAssetProfile(
        string? IconPath = null,
        string? BigIconPath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        ///     默认空 profile（无自定义路径）。
        /// </summary>
        public static PowerAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional orb HUD icon and combat visuals scene paths.
    ///     可选充能球 HUD 图标和战斗视觉场景路径。
    /// </summary>
    /// <param name="IconPath">
    ///     Orb icon texture path.
    ///     充能球图标贴图路径。
    /// </param>
    /// <param name="VisualsScenePath">
    ///     Scene path for orb combat presentation.
    ///     充能球战斗表现的场景路径。
    /// </param>
    public sealed record OrbAssetProfile(
        string? IconPath = null,
        string? VisualsScenePath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        ///     默认空 profile（无自定义路径）。
        /// </summary>
        public static OrbAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional potion bottle image and outline atlas paths.
    ///     可选药水瓶图片和轮廓 atlas 路径。
    /// </summary>
    /// <param name="ImagePath">
    ///     Main potion image texture path.
    ///     主要药水图片贴图路径。
    /// </param>
    /// <param name="OutlinePath">
    ///     Outline texture path.
    ///     轮廓贴图路径。
    /// </param>
    public sealed record PotionAssetProfile(
        string? ImagePath = null,
        string? OutlinePath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        ///     默认空 profile（无自定义路径）。
        /// </summary>
        public static PotionAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional affliction card overlay scene path.
    ///     可选苦痛卡牌覆盖场景路径。
    /// </summary>
    /// <param name="OverlayScenePath">
    ///     Packed scene path for the affliction overlay.
    ///     苦痛覆盖层的 PackedScene 路径。
    /// </param>
    public sealed record AfflictionAssetProfile(
        string? OverlayScenePath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        ///     默认空 profile（无自定义路径）。
        /// </summary>
        public static AfflictionAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional enchantment icon texture path.
    ///     可选附魔图标贴图路径。
    /// </summary>
    /// <param name="IconPath">
    ///     Enchantment icon image path.
    ///     附魔图标图片路径。
    /// </param>
    public sealed record EnchantmentAssetProfile(
        string? IconPath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        ///     默认空 profile（无自定义路径）。
        /// </summary>
        public static EnchantmentAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional run modifier icon texture path.
    ///     可选运行修饰符图标贴图路径。
    /// </summary>
    /// <param name="IconPath">
    ///     Modifier icon image path.
    ///     修饰符图标图片路径。
    /// </param>
    public sealed record ModifierAssetProfile(
        string? IconPath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        ///     默认空 profile（无自定义路径）。
        /// </summary>
        public static ModifierAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional act-level background, map layer, rest site, and treasure chest Spine resource paths.
    ///     可选章节级背景、地图图层、休息点和宝箱 Spine 资源路径。
    /// </summary>
    /// <param name="BackgroundScenePath">
    ///     Main act background scene.
    ///     act 主背景场景。
    /// </param>
    /// <param name="RestSiteBackgroundPath">
    ///     Rest site background scene.
    ///     休息点背景场景。
    /// </param>
    /// <param name="MapTopBgPath">
    ///     Top layer of the act map background image.
    ///     act 地图背景图片的顶层。
    /// </param>
    /// <param name="MapMidBgPath">
    ///     Middle layer of the act map background image.
    ///     act 地图背景图片的中层。
    /// </param>
    /// <param name="MapBotBgPath">
    ///     Bottom layer of the act map background image.
    ///     act 地图背景图片的底层。
    /// </param>
    /// <param name="ChestSpineResourcePath">
    ///     Treasure room chest Spine data resource path.
    ///     宝藏房宝箱 Spine 数据ResourcePath。
    /// </param>
    /// <param name="BackgroundLayersDirectoryPath">
    ///     Optional <c>res://</c> directory scanned like vanilla <c>scenes/backgrounds/&lt;act&gt;/layers</c> (files must
    ///     contain
    ///     <c>_bg_</c> or <c>_fg_</c> in the name).
    ///     可选 <c>res://</c> 目录，扫描方式与原版 <c>scenes/backgrounds/&lt;act&gt;/layers</c> 相同（文件名必须包含
    ///     <c>_bg_</c> 或 <c>_fg_</c>）。
    /// </param>
    public sealed record ActAssetProfile(
        string? BackgroundScenePath = null,
        string? RestSiteBackgroundPath = null,
        string? MapTopBgPath = null,
        string? MapMidBgPath = null,
        string? MapBotBgPath = null,
        string? ChestSpineResourcePath = null,
        string? BackgroundLayersDirectoryPath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        ///     默认空 profile（无自定义路径）。
        /// </summary>
        public static ActAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional creature visuals scene path for <see cref="MegaCrit.Sts2.Core.Models.MonsterModel" /> (<c>VisualsPath</c>
    ///     ).
    ///     <see cref="MegaCrit.Sts2.Core.Models.MonsterModel" /> 的可选生物视觉场景路径（<c>VisualsPath</c>）。
    /// </summary>
    /// <param name="VisualsScenePath">
    ///     Packed scene root under <c>creature_visuals/</c> convention.
    ///     遵循 <c>creature_visuals/</c> 约定的 PackedScene 根路径。
    /// </param>
    public sealed record MonsterAssetProfile(string? VisualsScenePath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        ///     默认空 profile（无自定义路径）。
        /// </summary>
        public static MonsterAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional encounter combat scene, background (main scene + parallax layers dir), boss map node spine, and extra
    ///     preload paths (vanilla <c>EncounterModel</c> pipeline).
    ///     可选遭遇战斗场景、背景（主场景 + 视差图层目录）、Boss 地图节点 Spine，以及额外预加载路径
    ///     （原版 <c>EncounterModel</c> 管线）。
    /// </summary>
    /// <param name="EncounterScenePath">
    ///     Packed scene for <c>EncounterModel.CreateScene</c> when
    ///     <see cref="EncounterModel.HasScene" /> is used.
    ///     使用 <see cref="EncounterModel.HasScene" /> 时供 <c>EncounterModel.CreateScene</c> 使用的 PackedScene。
    /// </param>
    /// <param name="BackgroundScenePath">
    ///     Main combat background scene when using encounter-specific backgrounds.
    ///     使用遭遇专属背景时的主战斗背景场景。
    /// </param>
    /// <param name="BackgroundLayersDirectoryPath">
    ///     <c>res://</c> layers directory (<c>_bg_</c> / <c>_fg_</c> file names).
    ///     <c>res://</c> 图层目录（文件名包含 <c>_bg_</c> / <c>_fg_</c>）。
    /// </param>
    /// <param name="BossNodeSpinePath">
    ///     Spine skeleton resource for boss/elite map node (see <c>EncounterModel.BossNodePath</c>
    ///     ).
    ///     Boss / 精英地图节点的 Spine skeleton 资源（见 <c>EncounterModel.BossNodePath</c>）。
    /// </param>
    /// <param name="ExtraAssetPaths">
    ///     Additional paths merged into <c>GetAssetPaths</c> preload.
    ///     合并进 <c>GetAssetPaths</c> 预加载的额外路径。
    /// </param>
    /// <param name="MapNodeAssetPaths">
    ///     When non-empty, replaces <c>MapNodeAssetPaths</c> enumeration for this encounter.
    ///     非空时，替换此遭遇的 <c>MapNodeAssetPaths</c> 枚举结果。
    /// </param>
    /// <param name="RunHistoryIconPath">
    ///     Full <c>res://images/…</c> path for run-history / top-bar main icon (see
    ///     <see cref="ImageHelper.GetImagePath" />).
    ///     运行历史 / 顶栏主图标的完整 <c>res://images/…</c> 路径（见 <see cref="ImageHelper.GetImagePath" />）。
    /// </param>
    /// <param name="RunHistoryIconOutlinePath">
    ///     Outline texture path, same conventions as
    ///     <paramref name="RunHistoryIconPath" />.
    ///     轮廓贴图路径，约定与 <paramref name="RunHistoryIconPath" /> 相同。
    /// </param>
    public sealed record EncounterAssetProfile(
        string? EncounterScenePath = null,
        string? BackgroundScenePath = null,
        string? BackgroundLayersDirectoryPath = null,
        string? BossNodeSpinePath = null,
        string[]? ExtraAssetPaths = null,
        string[]? MapNodeAssetPaths = null,
        string? RunHistoryIconPath = null,
        string? RunHistoryIconOutlinePath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        ///     默认空 profile（无自定义路径）。
        /// </summary>
        public static EncounterAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional event layout scene, portrait, background scene, and VFX scene paths (vanilla <c>EventModel</c> pipeline).
    ///     可选事件布局场景、肖像、背景场景和 VFX 场景路径（原版 <c>EventModel</c> 管线）。
    /// </summary>
    /// <param name="LayoutScenePath">
    ///     Packed scene for the event layout root (<c>CreateScene</c>).
    ///     事件布局根节点的 PackedScene（<c>CreateScene</c>）。
    /// </param>
    /// <param name="InitialPortraitPath">
    ///     Texture path for the initial portrait (<c>CreateInitialPortrait</c>).
    ///     初始肖像的贴图路径（<c>CreateInitialPortrait</c>）。
    /// </param>
    /// <param name="BackgroundScenePath">
    ///     Packed scene path for the background (<c>CreateBackgroundScene</c>).
    ///     背景的 PackedScene 路径（<c>CreateBackgroundScene</c>）。
    /// </param>
    /// <param name="VfxScenePath">
    ///     Packed scene path for optional event VFX (<c>CreateVfx</c>).
    ///     可选事件 VFX 的 PackedScene 路径（<c>CreateVfx</c>）。
    /// </param>
    public sealed record EventAssetProfile(
        string? LayoutScenePath = null,
        string? InitialPortraitPath = null,
        string? BackgroundScenePath = null,
        string? VfxScenePath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        ///     默认空 profile（无自定义路径）。
        /// </summary>
        public static EventAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional ancient map node and run-history icon paths (vanilla <c>AncientEventModel</c> presentation), plus
    ///     optional procedural stage layers (background / foreground cues) for the ancient event backdrop.
    ///     可选远古事件地图节点和运行历史图标路径（原版 <c>AncientEventModel</c> 表现），以及用于远古事件背景的
    ///     可选程序化舞台图层（背景 / 前景 cue）。
    /// </summary>
    /// <param name="MapIconPath">
    ///     Compressed texture for map node icon.
    ///     地图节点图标的压缩贴图。
    /// </param>
    /// <param name="MapIconOutlinePath">
    ///     Compressed texture for map node outline.
    ///     地图节点轮廓的压缩贴图。
    /// </param>
    /// <param name="RunHistoryIconPath">
    ///     Run history main icon texture.
    ///     运行历史主图标贴图。
    /// </param>
    /// <param name="RunHistoryIconOutlinePath">
    ///     Run history outline texture.
    ///     运行历史轮廓贴图。
    /// </param>
    /// <param name="StageProcedural">
    ///     When set, replaces the packed background scene in <c>NAncientEventLayout</c> with in-memory layered sprites and
    ///     cue playback (see <see cref="AncientEventStageProceduralVisualSet" />).
    ///     设置后，用内存中的分层 sprite 和 cue 播放替换 <c>NAncientEventLayout</c> 中的 packed 背景场景
    ///     （见 <see cref="AncientEventStageProceduralVisualSet" />）。
    /// </param>
    public sealed record AncientEventPresentationAssetProfile(
        string? MapIconPath = null,
        string? MapIconOutlinePath = null,
        string? RunHistoryIconPath = null,
        string? RunHistoryIconOutlinePath = null,
        AncientEventStageProceduralVisualSet? StageProcedural = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        ///     默认空 profile（无自定义路径）。
        /// </summary>
        public static AncientEventPresentationAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional rest site option icon path for mod-added campfire buttons.
    ///     Mod 添加的营火按钮可选休息点选项图标路径。
    /// </summary>
    /// <param name="IconPath">
    ///     Custom icon texture path (<c>res://</c> or PCK-relative).
    ///     自定义图标贴图路径（<c>res://</c> 或相对 PCK）。
    /// </param>
    public sealed record RestSiteOptionAssetProfile(
        string? IconPath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        ///     默认空 profile（无自定义路径）。
        /// </summary>
        public static RestSiteOptionAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional timeline epoch portrait paths.
    ///     可选时间线纪元肖像路径。
    /// </summary>
    /// <param name="PackedPortraitPath">
    ///     Atlas sprite resource path for the small timeline portrait.
    ///     小型时间线肖像的 atlas sprite 资源路径。
    /// </param>
    /// <param name="BigPortraitPath">
    ///     Large epoch portrait texture path.
    ///     大型纪元肖像贴图路径。
    /// </param>
    public sealed record EpochAssetProfile(
        string? PackedPortraitPath = null,
        string? BigPortraitPath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        ///     默认空 profile（无自定义路径）。
        /// </summary>
        public static EpochAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Factory methods that build <strong>base-game</strong> (<c>res://</c>) asset paths from vanilla folder and atlas
    ///     entry names. They do not infer paths from mod model ids — mod-only art uses explicit profile fields or your PCK
    ///     layout. Act borrowing mirrors <see cref="Characters.CharacterAssetProfiles.FromCharacterId" /> (vanilla id in,
    ///     vanilla paths out).
    ///     根据原版文件夹名和 atlas 条目名构建 <strong>基础游戏</strong>（<c>res://</c>）资源路径的工厂方法。
    ///     它们不会从 mod 模型 id 推断路径；mod 专属美术应使用显式 profile 字段或你的 PCK 布局。
    ///     借用章节美术的行为与 <see cref="Characters.CharacterAssetProfiles.FromCharacterId" /> 一致
    ///     （输入原版 id，输出原版路径）。
    /// </summary>
    public static class ContentAssetProfiles
    {
        /// <summary>
        ///     Builds default portrait and overlay paths for a card in <paramref name="poolEntry" /> /
        ///     <paramref name="cardEntry" />.
        ///     为 <paramref name="poolEntry" /> / <paramref name="cardEntry" /> 中的卡牌构建默认肖像和覆盖层路径。
        /// </summary>
        public static CardAssetProfile Card(string poolEntry, string cardEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(poolEntry);
            ArgumentException.ThrowIfNullOrWhiteSpace(cardEntry);

            var normalizedPool = Normalize(poolEntry);
            var normalizedCard = Normalize(cardEntry);
            return new(
                ImageHelper.GetImagePath($"packed/card_portraits/{normalizedPool}/{normalizedCard}.png"),
                ImageHelper.GetImagePath($"packed/card_portraits/{normalizedPool}/beta/{normalizedCard}.png"),
                OverlayScenePath: SceneHelper.GetScenePath($"cards/overlays/{normalizedCard}"));
        }

        /// <summary>
        ///     Builds default relic icon paths for <paramref name="relicEntry" />.
        ///     为 <paramref name="relicEntry" /> 构建默认遗物图标路径。
        /// </summary>
        public static RelicAssetProfile Relic(string relicEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relicEntry);

            var normalized = Normalize(relicEntry);
            return new(
                ImageHelper.GetImagePath($"atlases/relic_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"atlases/relic_outline_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"relics/{normalized}.png"));
        }

        /// <summary>
        ///     Builds default power icon paths for <paramref name="powerEntry" />.
        ///     为 <paramref name="powerEntry" /> 构建默认能力图标路径。
        /// </summary>
        public static PowerAssetProfile Power(string powerEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(powerEntry);

            var normalized = Normalize(powerEntry);
            return new(
                ImageHelper.GetImagePath($"atlases/power_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"powers/{normalized}.png"));
        }

        /// <summary>
        ///     Builds default orb icon and visuals scene paths for <paramref name="orbEntry" />.
        ///     为 <paramref name="orbEntry" /> 构建默认充能球图标和视觉场景路径。
        /// </summary>
        public static OrbAssetProfile Orb(string orbEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(orbEntry);

            var normalized = Normalize(orbEntry);
            return new(
                ImageHelper.GetImagePath($"orbs/{normalized}.png"),
                SceneHelper.GetScenePath($"orbs/orb_visuals/{normalized}"));
        }

        /// <summary>
        ///     Builds default potion image paths for <paramref name="potionEntry" />.
        ///     为 <paramref name="potionEntry" /> 构建默认药水图片路径。
        /// </summary>
        public static PotionAssetProfile Potion(string potionEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(potionEntry);

            var normalized = Normalize(potionEntry);
            return new(
                ImageHelper.GetImagePath($"atlases/potion_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"atlases/potion_outline_atlas.sprites/{normalized}.tres"));
        }

        /// <summary>
        ///     Builds default affliction overlay scene path for <paramref name="afflictionEntry" />.
        ///     为 <paramref name="afflictionEntry" /> 构建默认苦痛覆盖场景路径。
        /// </summary>
        public static AfflictionAssetProfile Affliction(string afflictionEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(afflictionEntry);

            var normalized = Normalize(afflictionEntry);
            return new(
                SceneHelper.GetScenePath($"cards/overlays/afflictions/{normalized}"));
        }

        /// <summary>
        ///     Builds default enchantment icon path for <paramref name="enchantmentEntry" />.
        ///     为 <paramref name="enchantmentEntry" /> 构建默认附魔图标路径。
        /// </summary>
        public static EnchantmentAssetProfile Enchantment(string enchantmentEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(enchantmentEntry);

            var normalized = Normalize(enchantmentEntry);
            return new(
                ImageHelper.GetImagePath($"enchantments/{normalized}.png"));
        }

        /// <summary>
        ///     Builds default modifier icon path for <paramref name="modifierEntry" />.
        ///     为 <paramref name="modifierEntry" /> 构建默认修饰符图标路径。
        /// </summary>
        public static ModifierAssetProfile Modifier(string modifierEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modifierEntry);

            var normalized = Normalize(modifierEntry);
            return new(
                ImageHelper.GetImagePath($"packed/modifiers/{normalized}.png"));
        }

        /// <summary>
        ///     Builds a full <see cref="ActAssetProfile" /> for a <strong>vanilla</strong> act folder name (e.g. <c>hive</c>,
        ///     <c>ship</c>): main background scene, <c>scenes/backgrounds/&lt;id&gt;/layers</c>, map parallax images, rest
        ///     site, chest Spine. Pass the base-game directory name, not a mod <see cref="ActModel" /> <c>Id.Entry</c>.
        ///     为 <strong>原版</strong>章节文件夹名（例如 <c>hive</c>、
        ///     <c>ship</c>）构建完整的 <see cref="ActAssetProfile" />：主背景场景、<c>scenes/backgrounds/&lt;id&gt;/layers</c>、地图视差图片、
        ///     休息点和宝箱 Spine。请传入基础游戏目录名，而不是 mod <see cref="ActModel" /> 的 <c>Id.Entry</c>。
        /// </summary>
        public static ActAssetProfile FromVanillaActId(string vanillaActId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(vanillaActId);

            var normalized = Normalize(vanillaActId);
            return new(
                SceneHelper.GetScenePath($"backgrounds/{normalized}/{normalized}_background"),
                SceneHelper.GetScenePath($"rest_site/{normalized}_rest_site"),
                ImageHelper.GetImagePath($"packed/map/map_bgs/{normalized}/map_top_{normalized}.png"),
                ImageHelper.GetImagePath($"packed/map/map_bgs/{normalized}/map_middle_{normalized}.png"),
                ImageHelper.GetImagePath($"packed/map/map_bgs/{normalized}/map_bottom_{normalized}.png"),
                $"res://animations/backgrounds/treasure_room/chest_room_act_{normalized}_skel_data.tres",
                ActVanillaBackgroundLayersDirectory(normalized));
        }

        /// <summary>
        ///     Short alias for <see cref="FromVanillaActId" />.
        ///     <see cref="FromVanillaActId" /> 的短别名。
        /// </summary>
        public static ActAssetProfile Act(string actEntry)
        {
            return FromVanillaActId(actEntry);
        }

        /// <summary>
        ///     Base-game combat background layers directory for <paramref name="vanillaActFolderName" />
        ///     (<c>res://scenes/backgrounds/&lt;act&gt;/layers</c>).
        /// </summary>
        /// <param name="vanillaActFolderName">
        ///     Vanilla act directory name (e.g. <c>hive</c>), not a mod act model id.
        ///     原版章节目录名（例如 <c>hive</c>），不是 mod 章节模型 id。
        /// </param>
        public static string ActVanillaBackgroundLayersDirectory(string vanillaActFolderName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(vanillaActFolderName);
            return $"res://scenes/backgrounds/{Normalize(vanillaActFolderName)}/layers";
        }

        /// <summary>
        ///     Builds default encounter asset paths for <paramref name="encounterEntry" /> (vanilla <c>EncounterModel</c> layout),
        ///     including concrete run-history texture paths under <c>ui/run_history/</c> (same files vanilla uses for that slug).
        ///     为 <paramref name="encounterEntry" /> 构建默认遭遇ResourcePath（原版 <c>EncounterModel</c> 布局），包括
        ///     <c>ui/run_history/</c> 下的具体运行历史贴图路径（与原版该 slug 使用的文件相同）。
        /// </summary>
        /// <param name="encounterEntry">
        ///     Vanilla encounter folder / animation slug (normalized to lowercase).
        ///     原版遭遇文件夹 / 动画 slug（会规范化为小写）。
        /// </param>
        /// <param name="runHistoryIconPath">
        ///     When non-null, overrides the main run-history icon path; otherwise
        ///     <see cref="ImageHelper.GetImagePath" /><c>($\"ui/run_history/{normalized}.png\")</c>.
        ///     非 null 时覆盖运行历史主图标路径；否则使用
        /// </param>
        /// <param name="runHistoryIconOutlinePath">
        ///     When non-null, overrides the outline path; otherwise
        ///     <see cref="ImageHelper.GetImagePath" /><c>($\"ui/run_history/{normalized}_outline.png\")</c>.
        ///     非 null 时覆盖轮廓路径；否则使用
        /// </param>
        public static EncounterAssetProfile Encounter(string encounterEntry,
            string? runHistoryIconPath = null,
            string? runHistoryIconOutlinePath = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(encounterEntry);

            var normalized = Normalize(encounterEntry);
            var rhMain = runHistoryIconPath ?? ImageHelper.GetImagePath($"ui/run_history/{normalized}.png");
            var rhOut = runHistoryIconOutlinePath ??
                        ImageHelper.GetImagePath($"ui/run_history/{normalized}_outline.png");
            return new(
                SceneHelper.GetScenePath($"encounters/{normalized}"),
                SceneHelper.GetScenePath($"backgrounds/{normalized}/{normalized}_background"),
                $"res://scenes/backgrounds/{normalized}/layers",
                $"res://animations/map/{normalized}/{normalized}_node_skel_data.tres",
                RunHistoryIconPath: rhMain,
                RunHistoryIconOutlinePath: rhOut);
        }

        /// <summary>
        ///     Vanilla per-encounter combat layers directory (<c>res://scenes/backgrounds/&lt;encounter&gt;/layers</c>).
        ///     原版逐遭遇战斗图层目录（<c>res://scenes/backgrounds/&lt;encounter&gt;/layers</c>）。
        /// </summary>
        public static string EncounterVanillaBackgroundLayersDirectory(string encounterEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(encounterEntry);
            return $"res://scenes/backgrounds/{Normalize(encounterEntry)}/layers";
        }

        /// <summary>
        ///     Builds default creature visuals scene path for <paramref name="monsterEntry" />.
        ///     为 <paramref name="monsterEntry" /> 构建默认生物视觉场景路径。
        /// </summary>
        public static MonsterAssetProfile Monster(string monsterEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(monsterEntry);
            return new(SceneHelper.GetScenePath($"creature_visuals/{Normalize(monsterEntry)}"));
        }

        /// <summary>
        ///     Builds default event asset paths for <paramref name="eventEntry" /> (default / combat style events).
        ///     为 <paramref name="eventEntry" /> 构建默认事件ResourcePath（默认 / 战斗风格事件）。
        /// </summary>
        public static EventAssetProfile Event(string eventEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(eventEntry);

            var normalized = Normalize(eventEntry);
            return new(
                InitialPortraitPath: ImageHelper.GetImagePath($"events/{normalized}.png"),
                BackgroundScenePath: SceneHelper.GetScenePath($"events/background_scenes/{normalized}"),
                VfxScenePath: SceneHelper.GetScenePath($"vfx/events/{normalized}_vfx"));
        }

        /// <summary>
        ///     Builds the vanilla custom-layout scene path for <paramref name="eventEntry" /> (custom event layout type).
        ///     为 <paramref name="eventEntry" /> 构建原版自定义布局场景路径（自定义事件布局类型）。
        /// </summary>
        public static string EventCustomLayoutScenePath(string eventEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(eventEntry);
            return SceneHelper.GetScenePath($"events/custom/{Normalize(eventEntry)}");
        }

        /// <summary>
        ///     Builds default ancient presentation paths for <paramref name="ancientEntry" />.
        ///     为 <paramref name="ancientEntry" /> 构建默认 ancient 表现路径。
        /// </summary>
        public static AncientEventPresentationAssetProfile AncientPresentation(string ancientEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ancientEntry);

            var normalized = Normalize(ancientEntry);
            return new(
                ImageHelper.GetImagePath($"packed/map/ancients/ancient_node_{normalized}.png"),
                ImageHelper.GetImagePath($"packed/map/ancients/ancient_node_{normalized}_outline.png"),
                ImageHelper.GetImagePath($"ui/run_history/{normalized}.png"),
                ImageHelper.GetImagePath($"ui/run_history/{normalized}_outline.png"));
        }

        /// <summary>
        ///     Builds default epoch portrait paths for <paramref name="epochId" /> (matches <c>EpochModel</c> conventions).
        ///     为 <paramref name="epochId" /> 构建默认纪元肖像路径（匹配 <c>EpochModel</c> 约定）。
        /// </summary>
        public static EpochAssetProfile Epoch(string epochId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);

            var normalized = Normalize(epochId);
            return new(
                ImageHelper.GetImagePath($"atlases/epoch_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"timeline/epoch_portraits/{normalized}.png"));
        }

        private static string Normalize(string value)
        {
            return value.Trim().ToLowerInvariant();
        }
    }
}
