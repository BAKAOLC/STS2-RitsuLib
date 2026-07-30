using System.Collections;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">Provides external asset overrides for non-card content.</para>
    ///     <para xml:lang="zh-CN">为非卡牌内容提供外部资源覆盖。</para>
    /// </summary>
    public static class ExternalAssetOverrideRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, Func<RelicModel, string?>> RelicIconPathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<RelicModel, string?>> RelicIconOutlinePathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<RelicModel, Texture2D?>> RelicIconTextureProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<RelicModel, Texture2D?>> RelicIconOutlineTextureProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<RelicModel, Texture2D?>> RelicBigIconTextureProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<PowerModel, string?>> PowerIconPathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<PowerModel, Texture2D?>> PowerIconTextureProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<PowerModel, Texture2D?>> PowerBigIconTextureProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<PotionModel, string?>> PotionImagePathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<PotionModel, string?>> PotionOutlinePathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<PotionModel, Texture2D?>> PotionImageTextureProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<PotionModel, Texture2D?>> PotionOutlineTextureProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<OrbModel, string?>> OrbIconPathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<OrbModel, CompressedTexture2D?>> OrbIconTextureProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<OrbModel, string?>> OrbVisualsScenePathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<ActModel, string?>> ActBackgroundScenePathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<ActModel, string?>> ActRestSiteBackgroundPathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<ActModel, string?>> ActMapTopBgPathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<ActModel, string?>> ActMapMidBgPathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<ActModel, string?>> ActMapBotBgPathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<EventModel, string?>> EventBackgroundScenePathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<EventModel, string?>> EventLayoutScenePathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<EventModel, Texture2D?>> EventInitialPortraitTextureProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<EventModel, PackedScene?>> EventBackgroundSceneProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<EventModel, PackedScene?>> EventVfxSceneProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<EncounterModel, string?>> EncounterScenePathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<EncounterModel, string?>>
            EncounterBackgroundScenePathProviders =
                new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<EncounterModel, string?>>
            EncounterBackgroundLayersDirProviders =
                new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<EncounterModel, string?>> EncounterBossNodePathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<EncounterModel, IEnumerable<string>?>>
            EncounterMapNodeAssetPathProviders =
                new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<EncounterModel, string?>> EncounterRunHistoryIconPathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<EncounterModel, string?>>
            EncounterRunHistoryIconOutlinePathProviders =
                new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<AncientEventModel, string?>> AncientMapIconPathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<AncientEventModel, string?>>
            AncientMapIconOutlinePathProviders =
                new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<AncientEventModel, string?>>
            AncientRunHistoryIconPathProviders =
                new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<AncientEventModel, string?>>
            AncientRunHistoryIconOutlinePathProviders =
                new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<AfflictionModel, string?>> AfflictionOverlayPathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<AfflictionModel, PackedScene?>>
            AfflictionOverlaySceneProviders =
                new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<EnchantmentModel, string?>> EnchantmentIconPathProviders =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Func<ModifierModel, string?>> ModifierIconPathProviders =
            new(StringComparer.Ordinal);

        private static readonly (IDictionary Map, RuntimeAssetRefreshScope Scope)[] ProviderMaps =
        [
            (RelicIconPathProviders, RuntimeAssetRefreshScope.Relics),
            (RelicIconOutlinePathProviders, RuntimeAssetRefreshScope.Relics),
            (RelicIconTextureProviders, RuntimeAssetRefreshScope.Relics),
            (RelicIconOutlineTextureProviders, RuntimeAssetRefreshScope.Relics),
            (RelicBigIconTextureProviders, RuntimeAssetRefreshScope.Relics),
            (PowerIconPathProviders, RuntimeAssetRefreshScope.Powers),
            (PowerIconTextureProviders, RuntimeAssetRefreshScope.Powers),
            (PowerBigIconTextureProviders, RuntimeAssetRefreshScope.Powers),
            (PotionImagePathProviders, RuntimeAssetRefreshScope.Potions),
            (PotionOutlinePathProviders, RuntimeAssetRefreshScope.Potions),
            (PotionImageTextureProviders, RuntimeAssetRefreshScope.Potions),
            (PotionOutlineTextureProviders, RuntimeAssetRefreshScope.Potions),
            (OrbIconPathProviders, RuntimeAssetRefreshScope.Orbs),
            (OrbIconTextureProviders, RuntimeAssetRefreshScope.Orbs),
            (OrbVisualsScenePathProviders, RuntimeAssetRefreshScope.Orbs),
            (ActBackgroundScenePathProviders, RuntimeAssetRefreshScope.None),
            (ActRestSiteBackgroundPathProviders, RuntimeAssetRefreshScope.None),
            (ActMapTopBgPathProviders, RuntimeAssetRefreshScope.None),
            (ActMapMidBgPathProviders, RuntimeAssetRefreshScope.None),
            (ActMapBotBgPathProviders, RuntimeAssetRefreshScope.None),
            (EventBackgroundScenePathProviders, RuntimeAssetRefreshScope.None),
            (EventLayoutScenePathProviders, RuntimeAssetRefreshScope.None),
            (EventInitialPortraitTextureProviders, RuntimeAssetRefreshScope.None),
            (EventBackgroundSceneProviders, RuntimeAssetRefreshScope.None),
            (EventVfxSceneProviders, RuntimeAssetRefreshScope.None),
            (EncounterScenePathProviders, RuntimeAssetRefreshScope.None),
            (EncounterBackgroundScenePathProviders, RuntimeAssetRefreshScope.None),
            (EncounterBackgroundLayersDirProviders, RuntimeAssetRefreshScope.None),
            (EncounterBossNodePathProviders, RuntimeAssetRefreshScope.None),
            (EncounterMapNodeAssetPathProviders, RuntimeAssetRefreshScope.None),
            (EncounterRunHistoryIconPathProviders, RuntimeAssetRefreshScope.None),
            (EncounterRunHistoryIconOutlinePathProviders, RuntimeAssetRefreshScope.None),
            (AncientMapIconPathProviders, RuntimeAssetRefreshScope.None),
            (AncientMapIconOutlinePathProviders, RuntimeAssetRefreshScope.None),
            (AncientRunHistoryIconPathProviders, RuntimeAssetRefreshScope.None),
            (AncientRunHistoryIconOutlinePathProviders, RuntimeAssetRefreshScope.None),
            (AfflictionOverlayPathProviders, RuntimeAssetRefreshScope.None),
            (AfflictionOverlaySceneProviders, RuntimeAssetRefreshScope.None),
            (EnchantmentIconPathProviders, RuntimeAssetRefreshScope.None),
            (ModifierIconPathProviders, RuntimeAssetRefreshScope.None),
        ];

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a relic icon-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换遗物图标路径提供器。</para>
        /// </summary>
        public static void RegisterRelicIconPathProvider(string key, Func<RelicModel, string?> provider)
        {
            Register(RelicIconPathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a relic outline-icon path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换遗物轮廓图标路径提供器。</para>
        /// </summary>
        public static void RegisterRelicIconOutlinePathProvider(string key, Func<RelicModel, string?> provider)
        {
            Register(RelicIconOutlinePathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a relic icon-texture provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换遗物图标纹理提供器。</para>
        /// </summary>
        public static void RegisterRelicIconTextureProvider(string key, Func<RelicModel, Texture2D?> provider)
        {
            Register(RelicIconTextureProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a relic outline-icon texture provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换遗物轮廓图标纹理提供器。</para>
        /// </summary>
        public static void RegisterRelicIconOutlineTextureProvider(string key, Func<RelicModel, Texture2D?> provider)
        {
            Register(RelicIconOutlineTextureProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a large relic-icon texture provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换遗物大图标纹理提供器。</para>
        /// </summary>
        public static void RegisterRelicBigIconTextureProvider(string key, Func<RelicModel, Texture2D?> provider)
        {
            Register(RelicBigIconTextureProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a power icon-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换能力图标路径提供器。</para>
        /// </summary>
        public static void RegisterPowerIconPathProvider(string key, Func<PowerModel, string?> provider)
        {
            Register(PowerIconPathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a power icon-texture provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换能力图标纹理提供器。</para>
        /// </summary>
        public static void RegisterPowerIconTextureProvider(string key, Func<PowerModel, Texture2D?> provider)
        {
            Register(PowerIconTextureProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a large power-icon texture provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换能力大图标纹理提供器。</para>
        /// </summary>
        public static void RegisterPowerBigIconTextureProvider(string key, Func<PowerModel, Texture2D?> provider)
        {
            Register(PowerBigIconTextureProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a potion image-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换药水图像路径提供器。</para>
        /// </summary>
        public static void RegisterPotionImagePathProvider(string key, Func<PotionModel, string?> provider)
        {
            Register(PotionImagePathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a potion outline-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换药水轮廓路径提供器。</para>
        /// </summary>
        public static void RegisterPotionOutlinePathProvider(string key, Func<PotionModel, string?> provider)
        {
            Register(PotionOutlinePathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a potion image-texture provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换药水图像纹理提供器。</para>
        /// </summary>
        public static void RegisterPotionImageTextureProvider(string key, Func<PotionModel, Texture2D?> provider)
        {
            Register(PotionImageTextureProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a potion outline-texture provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换药水轮廓纹理提供器。</para>
        /// </summary>
        public static void RegisterPotionOutlineTextureProvider(string key, Func<PotionModel, Texture2D?> provider)
        {
            Register(PotionOutlineTextureProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an orb icon-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换充能球图标路径提供器。</para>
        /// </summary>
        public static void RegisterOrbIconPathProvider(string key, Func<OrbModel, string?> provider)
        {
            Register(OrbIconPathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an orb icon-texture provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换充能球图标纹理提供器。</para>
        /// </summary>
        public static void RegisterOrbIconTextureProvider(string key, Func<OrbModel, CompressedTexture2D?> provider)
        {
            Register(OrbIconTextureProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an orb combat-visual scene-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换充能球战斗视觉场景路径提供器。</para>
        /// </summary>
        public static void RegisterOrbVisualsScenePathProvider(string key, Func<OrbModel, string?> provider)
        {
            Register(OrbVisualsScenePathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an act main-background scene-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换章节主背景场景路径提供器。</para>
        /// </summary>
        public static void RegisterActBackgroundScenePathProvider(string key, Func<ActModel, string?> provider)
        {
            Register(ActBackgroundScenePathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an act rest-site background-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换章节休息处背景路径提供器。</para>
        /// </summary>
        public static void RegisterActRestSiteBackgroundPathProvider(string key, Func<ActModel, string?> provider)
        {
            Register(ActRestSiteBackgroundPathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an act-map top-background path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换章节地图顶层背景路径提供器。</para>
        /// </summary>
        public static void RegisterActMapTopBgPathProvider(string key, Func<ActModel, string?> provider)
        {
            Register(ActMapTopBgPathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an act-map middle-background path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换章节地图中层背景路径提供器。</para>
        /// </summary>
        public static void RegisterActMapMidBgPathProvider(string key, Func<ActModel, string?> provider)
        {
            Register(ActMapMidBgPathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an act-map bottom-background path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换章节地图底层背景路径提供器。</para>
        /// </summary>
        public static void RegisterActMapBotBgPathProvider(string key, Func<ActModel, string?> provider)
        {
            Register(ActMapBotBgPathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an event background scene-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换事件背景场景路径提供器。</para>
        /// </summary>
        public static void RegisterEventBackgroundScenePathProvider(string key, Func<EventModel, string?> provider)
        {
            Register(EventBackgroundScenePathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an event layout scene-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换事件布局场景路径提供器。</para>
        /// </summary>
        public static void RegisterEventLayoutScenePathProvider(string key, Func<EventModel, string?> provider)
        {
            Register(EventLayoutScenePathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an event initial-portrait texture provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换事件初始立绘纹理提供器。</para>
        /// </summary>
        public static void RegisterEventInitialPortraitTextureProvider(string key,
            Func<EventModel, Texture2D?> provider)
        {
            Register(EventInitialPortraitTextureProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an event background <see cref="PackedScene" /> provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换事件背景 <see cref="PackedScene" /> 提供器。</para>
        /// </summary>
        public static void RegisterEventBackgroundSceneProvider(string key, Func<EventModel, PackedScene?> provider)
        {
            Register(EventBackgroundSceneProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an event VFX <see cref="PackedScene" /> provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换事件 VFX <see cref="PackedScene" /> 提供器。</para>
        /// </summary>
        public static void RegisterEventVfxSceneProvider(string key, Func<EventModel, PackedScene?> provider)
        {
            Register(EventVfxSceneProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an encounter scene-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换遭遇场景路径提供器。</para>
        /// </summary>
        public static void RegisterEncounterScenePathProvider(string key, Func<EncounterModel, string?> provider)
        {
            Register(EncounterScenePathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an encounter background scene-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换遭遇背景场景路径提供器。</para>
        /// </summary>
        public static void RegisterEncounterBackgroundScenePathProvider(string key,
            Func<EncounterModel, string?> provider)
        {
            Register(EncounterBackgroundScenePathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an encounter background-layer directory provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换遭遇背景图层目录提供器。</para>
        /// </summary>
        public static void RegisterEncounterBackgroundLayersDirectoryProvider(string key,
            Func<EncounterModel, string?> provider)
        {
            Register(EncounterBackgroundLayersDirProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an encounter boss map-node path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换遭遇首领地图节点路径提供器。</para>
        /// </summary>
        public static void RegisterEncounterBossNodePathProvider(string key, Func<EncounterModel, string?> provider)
        {
            Register(EncounterBossNodePathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an encounter map-node asset-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换遭遇地图节点资源路径提供器。</para>
        /// </summary>
        public static void RegisterEncounterMapNodeAssetPathsProvider(string key,
            Func<EncounterModel, IEnumerable<string>?> provider)
        {
            Register(EncounterMapNodeAssetPathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an encounter run-history icon-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换遭遇游戏历史图标路径提供器。</para>
        /// </summary>
        public static void RegisterEncounterRunHistoryIconPathProvider(string key,
            Func<EncounterModel, string?> provider)
        {
            Register(EncounterRunHistoryIconPathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an encounter run-history outline-icon path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换遭遇游戏历史轮廓图标路径提供器。</para>
        /// </summary>
        public static void RegisterEncounterRunHistoryIconOutlinePathProvider(string key,
            Func<EncounterModel, string?> provider)
        {
            Register(EncounterRunHistoryIconOutlinePathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an Ancient-event map-node icon-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换先古事件地图节点图标路径提供器。</para>
        /// </summary>
        public static void RegisterAncientMapIconPathProvider(string key, Func<AncientEventModel, string?> provider)
        {
            Register(AncientMapIconPathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an Ancient-event map-node outline-icon path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换先古事件地图节点轮廓图标路径提供器。</para>
        /// </summary>
        public static void RegisterAncientMapIconOutlinePathProvider(string key,
            Func<AncientEventModel, string?> provider)
        {
            Register(AncientMapIconOutlinePathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an Ancient-event run-history icon-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换先古事件游戏历史图标路径提供器。</para>
        /// </summary>
        public static void RegisterAncientRunHistoryIconPathProvider(string key,
            Func<AncientEventModel, string?> provider)
        {
            Register(AncientRunHistoryIconPathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an Ancient-event run-history outline-icon path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换先古事件游戏历史轮廓图标路径提供器。</para>
        /// </summary>
        public static void RegisterAncientRunHistoryIconOutlinePathProvider(string key,
            Func<AncientEventModel, string?> provider)
        {
            Register(AncientRunHistoryIconOutlinePathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an affliction overlay-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换侵蚀覆盖层路径提供器。</para>
        /// </summary>
        public static void RegisterAfflictionOverlayPathProvider(string key, Func<AfflictionModel, string?> provider)
        {
            Register(AfflictionOverlayPathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an affliction overlay <see cref="PackedScene" /> provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换侵蚀覆盖层 <see cref="PackedScene" /> 提供器。</para>
        /// </summary>
        public static void RegisterAfflictionOverlaySceneProvider(string key,
            Func<AfflictionModel, PackedScene?> provider)
        {
            Register(AfflictionOverlaySceneProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an enchantment icon-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换附魔图标路径提供器。</para>
        /// </summary>
        public static void RegisterEnchantmentIconPathProvider(string key, Func<EnchantmentModel, string?> provider)
        {
            Register(EnchantmentIconPathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a modifier icon-path provider.</para>
        ///     <para xml:lang="zh-CN">注册或替换修饰符图标路径提供器。</para>
        /// </summary>
        public static void RegisterModifierIconPathProvider(string key, Func<ModifierModel, string?> provider)
        {
            Register(ModifierIconPathProviders, key, provider);
        }

        /// <summary>
        ///     <para xml:lang="en">Removes every provider registered with the specified key.</para>
        ///     <para xml:lang="zh-CN">移除使用指定键注册的所有提供器。</para>
        /// </summary>
        public static bool Unregister(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            bool removed;
            RuntimeAssetRefreshScope scope;
            lock (SyncRoot)
            {
                removed = UnregisterFromAllBuckets(key, out scope);
            }

            if (removed && scope != RuntimeAssetRefreshScope.None)
                RuntimeAssetRefreshCoordinator.Request(scope);
            return removed;
        }

        /// <summary>
        ///     <para xml:lang="en">Removes all registered external providers.</para>
        ///     <para xml:lang="zh-CN">移除所有已注册的外部提供器。</para>
        /// </summary>
        public static void Clear()
        {
            RuntimeAssetRefreshScope scope;
            lock (SyncRoot)
            {
                scope = ClearAllBuckets();
            }

            if (scope != RuntimeAssetRefreshScope.None)
                RuntimeAssetRefreshCoordinator.Request(scope);
        }

        private static bool UnregisterFromAllBuckets(string key, out RuntimeAssetRefreshScope scope)
        {
            scope = RuntimeAssetRefreshScope.None;
            var removed = false;
            foreach (var (map, mapScope) in ProviderMaps)
            {
                if (!map.Contains(key))
                    continue;
                map.Remove(key);
                removed = true;
                scope |= mapScope;
            }

            return removed;
        }

        private static RuntimeAssetRefreshScope ClearAllBuckets()
        {
            var scope = RuntimeAssetRefreshScope.None;
            foreach (var (map, mapScope) in ProviderMaps)
            {
                if (map.Count == 0)
                    continue;
                map.Clear();
                scope |= mapScope;
            }

            return scope;
        }

        internal static bool TryGetRelicIconPath(RelicModel model, out string value)
        {
            return TryGet(RelicIconPathProviders, model, out value);
        }

        internal static bool TryGetRelicIconOutlinePath(RelicModel model, out string value)
        {
            return TryGet(RelicIconOutlinePathProviders, model, out value);
        }

        internal static bool TryGetRelicIconTexture(RelicModel model, out Texture2D value)
        {
            return TryGet(RelicIconTextureProviders, model, out value);
        }

        internal static bool TryGetRelicIconOutlineTexture(RelicModel model, out Texture2D value)
        {
            return TryGet(RelicIconOutlineTextureProviders, model, out value);
        }

        internal static bool TryGetRelicBigIconTexture(RelicModel model, out Texture2D value)
        {
            return TryGet(RelicBigIconTextureProviders, model, out value);
        }

        internal static bool TryGetPowerIconPath(PowerModel model, out string value)
        {
            return TryGet(PowerIconPathProviders, model, out value);
        }

        internal static bool TryGetPowerIconTexture(PowerModel model, out Texture2D value)
        {
            return TryGet(PowerIconTextureProviders, model, out value);
        }

        internal static bool TryGetPowerBigIconTexture(PowerModel model, out Texture2D value)
        {
            return TryGet(PowerBigIconTextureProviders, model, out value);
        }

        internal static bool TryGetPotionImagePath(PotionModel model, out string value)
        {
            return TryGet(PotionImagePathProviders, model, out value);
        }

        internal static bool TryGetPotionOutlinePath(PotionModel model, out string value)
        {
            return TryGet(PotionOutlinePathProviders, model, out value);
        }

        internal static bool TryGetPotionImageTexture(PotionModel model, out Texture2D value)
        {
            return TryGet(PotionImageTextureProviders, model, out value);
        }

        internal static bool TryGetPotionOutlineTexture(PotionModel model, out Texture2D value)
        {
            return TryGet(PotionOutlineTextureProviders, model, out value);
        }

        internal static bool TryGetOrbIconPath(OrbModel model, out string value)
        {
            return TryGet(OrbIconPathProviders, model, out value);
        }

        internal static bool TryGetOrbIconTexture(OrbModel model, out CompressedTexture2D value)
        {
            return TryGet(OrbIconTextureProviders, model, out value);
        }

        internal static bool TryGetOrbVisualsScenePath(OrbModel model, out string value)
        {
            return TryGet(OrbVisualsScenePathProviders, model, out value);
        }

        internal static bool TryGetActBackgroundScenePath(ActModel model, out string value)
        {
            return TryGet(ActBackgroundScenePathProviders, model, out value);
        }

        internal static bool TryGetActRestSiteBackgroundPath(ActModel model, out string value)
        {
            return TryGet(ActRestSiteBackgroundPathProviders, model, out value);
        }

        internal static bool TryGetActMapTopBgPath(ActModel model, out string value)
        {
            return TryGet(ActMapTopBgPathProviders, model, out value);
        }

        internal static bool TryGetActMapMidBgPath(ActModel model, out string value)
        {
            return TryGet(ActMapMidBgPathProviders, model, out value);
        }

        internal static bool TryGetActMapBotBgPath(ActModel model, out string value)
        {
            return TryGet(ActMapBotBgPathProviders, model, out value);
        }

        internal static bool TryGetEventBackgroundScenePath(EventModel model, out string value)
        {
            return TryGet(EventBackgroundScenePathProviders, model, out value);
        }

        internal static bool TryGetEventLayoutScenePath(EventModel model, out string value)
        {
            return TryGet(EventLayoutScenePathProviders, model, out value);
        }

        internal static bool TryGetEventInitialPortraitTexture(EventModel model, out Texture2D value)
        {
            return TryGet(EventInitialPortraitTextureProviders, model, out value);
        }

        internal static bool TryGetEventBackgroundScene(EventModel model, out PackedScene value)
        {
            return TryGet(EventBackgroundSceneProviders, model, out value);
        }

        internal static bool TryGetEventBackgroundScene(EventModel model, out PackedScene value,
            out string providerKey)
        {
            return TryGet(EventBackgroundSceneProviders, model, out value, out providerKey);
        }

        internal static bool TryGetEventVfxScene(EventModel model, out PackedScene value)
        {
            return TryGet(EventVfxSceneProviders, model, out value);
        }

        internal static bool TryGetEventVfxScene(EventModel model, out PackedScene value, out string providerKey)
        {
            return TryGet(EventVfxSceneProviders, model, out value, out providerKey);
        }

        internal static bool TryGetEncounterScenePath(EncounterModel model, out string value)
        {
            return TryGet(EncounterScenePathProviders, model, out value);
        }

        internal static bool TryGetEncounterBackgroundScenePath(EncounterModel model, out string value)
        {
            return TryGet(EncounterBackgroundScenePathProviders, model, out value);
        }

        internal static bool TryGetEncounterBackgroundLayersDirectory(EncounterModel model, out string value)
        {
            return TryGet(EncounterBackgroundLayersDirProviders, model, out value);
        }

        internal static bool TryGetEncounterBossNodePath(EncounterModel model, out string value)
        {
            return TryGet(EncounterBossNodePathProviders, model, out value);
        }

        internal static bool TryGetEncounterMapNodeAssetPaths(EncounterModel model, out IEnumerable<string> values)
        {
            if (!TryGet(EncounterMapNodeAssetPathProviders, model, out var raw, out var providerKey) || raw == null)
            {
                values = [];
                return false;
            }

            try
            {
                values = raw.ToArray();
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Assets] External provider '{providerKey}' failed while enumerating encounter map-node asset paths: {ex.Message}");
                values = [];
                return false;
            }
        }

        internal static bool TryGetEncounterRunHistoryIconPath(EncounterModel model, out string value)
        {
            return TryGet(EncounterRunHistoryIconPathProviders, model, out value);
        }

        internal static bool TryGetEncounterRunHistoryIconOutlinePath(EncounterModel model, out string value)
        {
            return TryGet(EncounterRunHistoryIconOutlinePathProviders, model, out value);
        }

        internal static bool TryGetAncientMapIconPath(AncientEventModel model, out string value)
        {
            return TryGet(AncientMapIconPathProviders, model, out value);
        }

        internal static bool TryGetAncientMapIconOutlinePath(AncientEventModel model, out string value)
        {
            return TryGet(AncientMapIconOutlinePathProviders, model, out value);
        }

        internal static bool TryGetAncientRunHistoryIconPath(AncientEventModel model, out string value)
        {
            return TryGet(AncientRunHistoryIconPathProviders, model, out value);
        }

        internal static bool TryGetAncientRunHistoryIconOutlinePath(AncientEventModel model, out string value)
        {
            return TryGet(AncientRunHistoryIconOutlinePathProviders, model, out value);
        }

        internal static bool TryGetAfflictionOverlayPath(AfflictionModel model, out string value)
        {
            return TryGet(AfflictionOverlayPathProviders, model, out value);
        }

        internal static bool TryGetAfflictionOverlayPath(AfflictionModel model, out string value,
            out string providerKey)
        {
            return TryGet(AfflictionOverlayPathProviders, model, out value, out providerKey);
        }

        internal static bool TryGetAfflictionOverlayScene(AfflictionModel model, out PackedScene value)
        {
            return TryGet(AfflictionOverlaySceneProviders, model, out value);
        }

        internal static bool TryGetAfflictionOverlayScene(AfflictionModel model, out PackedScene value,
            out string providerKey)
        {
            return TryGet(AfflictionOverlaySceneProviders, model, out value, out providerKey);
        }

        internal static bool TryGetEnchantmentIconPath(EnchantmentModel model, out string value)
        {
            return TryGet(EnchantmentIconPathProviders, model, out value);
        }

        internal static bool TryGetModifierIconPath(ModifierModel model, out string value)
        {
            return TryGet(ModifierIconPathProviders, model, out value);
        }

        private static void Register<TModel, TValue>(
            Dictionary<string, Func<TModel, TValue?>> map,
            string key,
            Func<TModel, TValue?> provider)
            where TModel : class
            where TValue : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(provider);
            lock (SyncRoot)
            {
                map[key] = provider;
            }

            var scope = GetScopeForMap(map);
            if (scope != RuntimeAssetRefreshScope.None)
                RuntimeAssetRefreshCoordinator.Request(scope);
        }

        private static RuntimeAssetRefreshScope GetScopeForMap(IDictionary map)
        {
            foreach (var (candidate, scope) in ProviderMaps)
                if (ReferenceEquals(candidate, map))
                    return scope;

            return RuntimeAssetRefreshScope.None;
        }

        private static bool TryGet<TModel>(
            Dictionary<string, Func<TModel, string?>> map,
            TModel model,
            out string value)
            where TModel : class
        {
            return TryGet(map, model, out value, out _);
        }

        private static bool TryGet<TModel>(
            Dictionary<string, Func<TModel, string?>> map,
            TModel model,
            out string value,
            out string providerKey)
            where TModel : class
        {
            foreach (var pair in SnapshotWithKeys(map))
            {
                string? candidate;
                try
                {
                    candidate = pair.Value(model);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Assets] External provider '{pair.Key}' failed: {ex.Message}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                value = candidate;
                providerKey = pair.Key;
                return true;
            }

            value = string.Empty;
            providerKey = string.Empty;
            return false;
        }

        private static bool TryGet<TModel, TValue>(
            Dictionary<string, Func<TModel, TValue?>> map,
            TModel model,
            out TValue value)
            where TModel : class
            where TValue : class
        {
            return TryGet(map, model, out value, out _);
        }

        private static bool TryGet<TModel, TValue>(
            Dictionary<string, Func<TModel, TValue?>> map,
            TModel model,
            out TValue value,
            out string providerKey)
            where TModel : class
            where TValue : class
        {
            foreach (var pair in SnapshotWithKeys(map))
            {
                TValue? candidate;
                try
                {
                    candidate = pair.Value(model);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Assets] External provider '{pair.Key}' failed: {ex.Message}");
                    continue;
                }

                if (candidate == null)
                    continue;

                value = candidate;
                providerKey = pair.Key;
                return true;
            }

            value = null!;
            providerKey = string.Empty;
            return false;
        }

        private static KeyValuePair<string, Func<TModel, TValue?>>[] SnapshotWithKeys<TModel, TValue>(
            Dictionary<string, Func<TModel, TValue?>> providers)
            where TModel : class
        {
            lock (SyncRoot)
            {
                return [.. providers];
            }
        }
    }
}
