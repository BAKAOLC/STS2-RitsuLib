using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Bundle of optional resource paths for mod card portraits, frames, energy icon, overlay scene, and banner.
    /// </summary>
    /// <param name="PortraitPath">Main card portrait image path.</param>
    /// <param name="BetaPortraitPath">Alternate “beta” portrait path, if any.</param>
    /// <param name="FramePath">Card frame texture path.</param>
    /// <param name="PortraitBorderPath">Portrait border / frame accent texture.</param>
    /// <param name="EnergyIconPath">Small energy icon texture for this card.</param>
    /// <param name="FrameMaterialPath">Material resource path for the card frame.</param>
    /// <param name="OverlayScenePath">Packed scene path for built-in card overlay UI.</param>
    /// <param name="BannerTexturePath">Texture used on run-summary or banner UI.</param>
    /// <param name="BannerMaterialPath">Material path for banner rendering.</param>
    public sealed record CardAssetProfile(
        string? PortraitPath = null,
        string? BetaPortraitPath = null,
        string? FramePath = null,
        string? PortraitBorderPath = null,
        string? EnergyIconPath = null,
        string? FrameMaterialPath = null,
        string? OverlayScenePath = null,
        string? BannerTexturePath = null,
        string? BannerMaterialPath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static CardAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional relic icon paths (atlas entries and large shop/detail image).
    /// </summary>
    /// <param name="IconPath">Primary relic icon texture path.</param>
    /// <param name="IconOutlinePath">Outline / silhouette icon path.</param>
    /// <param name="BigIconPath">Large relic art path.</param>
    public sealed record RelicAssetProfile(
        string? IconPath = null,
        string? IconOutlinePath = null,
        string? BigIconPath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static RelicAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional power icon paths (atlas and large illustration).
    /// </summary>
    /// <param name="IconPath">Power icon texture path.</param>
    /// <param name="BigIconPath">Large power art path.</param>
    public sealed record PowerAssetProfile(
        string? IconPath = null,
        string? BigIconPath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static PowerAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional orb HUD icon and combat visuals scene paths.
    /// </summary>
    /// <param name="IconPath">Orb icon texture path.</param>
    /// <param name="VisualsScenePath">Scene path for orb combat presentation.</param>
    public sealed record OrbAssetProfile(
        string? IconPath = null,
        string? VisualsScenePath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static OrbAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional potion bottle image and outline atlas paths.
    /// </summary>
    /// <param name="ImagePath">Main potion image texture path.</param>
    /// <param name="OutlinePath">Outline texture path.</param>
    public sealed record PotionAssetProfile(
        string? ImagePath = null,
        string? OutlinePath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static PotionAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional affliction card overlay scene path.
    /// </summary>
    /// <param name="OverlayScenePath">Packed scene path for the affliction overlay.</param>
    public sealed record AfflictionAssetProfile(
        string? OverlayScenePath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static AfflictionAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional enchantment icon texture path.
    /// </summary>
    /// <param name="IconPath">Enchantment icon image path.</param>
    public sealed record EnchantmentAssetProfile(
        string? IconPath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static EnchantmentAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional act-level background, map layer, rest site, and treasure chest Spine resource paths.
    /// </summary>
    /// <param name="BackgroundScenePath">Main act background scene.</param>
    /// <param name="RestSiteBackgroundPath">Rest site background scene.</param>
    /// <param name="MapTopBgPath">Top layer of the act map background image.</param>
    /// <param name="MapMidBgPath">Middle layer of the act map background image.</param>
    /// <param name="MapBotBgPath">Bottom layer of the act map background image.</param>
    /// <param name="ChestSpineResourcePath">Treasure room chest Spine data resource path.</param>
    /// <param name="BackgroundLayersDirectoryPath">
    ///     Optional <c>res://</c> directory scanned like vanilla <c>scenes/backgrounds/&lt;act&gt;/layers</c> (files must
    ///     contain
    ///     <c>_bg_</c> or <c>_fg_</c> in the name).
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
        /// </summary>
        public static ActAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional creature visuals scene path for <see cref="MegaCrit.Sts2.Core.Models.MonsterModel" /> (<c>VisualsPath</c>
    ///     ).
    /// </summary>
    /// <param name="VisualsScenePath">Packed scene root under <c>creature_visuals/</c> convention.</param>
    public sealed record MonsterAssetProfile(string? VisualsScenePath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static MonsterAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional encounter combat scene, background (main scene + parallax layers dir), boss map node spine, and extra
    ///     preload paths (vanilla <c>EncounterModel</c> pipeline).
    /// </summary>
    /// <param name="EncounterScenePath">
    ///     Packed scene for <c>EncounterModel.CreateScene</c> when
    ///     <see cref="EncounterModel.HasScene" /> is used.
    /// </param>
    /// <param name="BackgroundScenePath">Main combat background scene when using encounter-specific backgrounds.</param>
    /// <param name="BackgroundLayersDirectoryPath"><c>res://</c> layers directory (<c>_bg_</c> / <c>_fg_</c> file names).</param>
    /// <param name="BossNodeSpinePath">
    ///     Spine skeleton resource for boss/elite map node (see <c>EncounterModel.BossNodePath</c>
    ///     ).
    /// </param>
    /// <param name="ExtraAssetPaths">Additional paths merged into <c>GetAssetPaths</c> preload.</param>
    /// <param name="MapNodeAssetPaths">When non-empty, replaces <c>MapNodeAssetPaths</c> enumeration for this encounter.</param>
    /// <param name="RunHistoryIconPath">
    ///     Full <c>res://images/…</c> path for run-history / top-bar main icon (see
    ///     <see cref="ImageHelper.GetImagePath" />).
    /// </param>
    /// <param name="RunHistoryIconOutlinePath">
    ///     Outline texture path, same conventions as
    ///     <paramref name="RunHistoryIconPath" />.
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
        /// </summary>
        public static EncounterAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional event layout scene, portrait, background scene, and VFX scene paths (vanilla <c>EventModel</c> pipeline).
    /// </summary>
    /// <param name="LayoutScenePath">Packed scene for the event layout root (<c>CreateScene</c>).</param>
    /// <param name="InitialPortraitPath">Texture path for the initial portrait (<c>CreateInitialPortrait</c>).</param>
    /// <param name="BackgroundScenePath">Packed scene path for the background (<c>CreateBackgroundScene</c>).</param>
    /// <param name="VfxScenePath">Packed scene path for optional event VFX (<c>CreateVfx</c>).</param>
    public sealed record EventAssetProfile(
        string? LayoutScenePath = null,
        string? InitialPortraitPath = null,
        string? BackgroundScenePath = null,
        string? VfxScenePath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static EventAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional ancient map node and run-history icon paths (vanilla <c>AncientEventModel</c> presentation), plus
    ///     optional procedural stage layers (background / foreground cues) for the ancient encounter backdrop.
    /// </summary>
    /// <param name="MapIconPath">Compressed texture for map node icon.</param>
    /// <param name="MapIconOutlinePath">Compressed texture for map node outline.</param>
    /// <param name="RunHistoryIconPath">Run history main icon texture.</param>
    /// <param name="RunHistoryIconOutlinePath">Run history outline texture.</param>
    /// <param name="StageProcedural">
    ///     When set, replaces the packed background scene in <c>NAncientEventLayout</c> with in-memory layered sprites and
    ///     cue playback (see <see cref="AncientEventStageProceduralVisualSet" />).
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
        /// </summary>
        public static AncientEventPresentationAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional timeline epoch portrait paths (vanilla <c>EpochModel.PackedPortraitPath</c> / <c>BigPortraitPath</c>).
    /// </summary>
    /// <param name="PackedPortraitPath">Atlas sprite resource path for the small timeline portrait.</param>
    /// <param name="BigPortraitPath">Large epoch portrait texture path.</param>
    public sealed record EpochAssetProfile(
        string? PackedPortraitPath = null,
        string? BigPortraitPath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static EpochAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Factory methods that build <strong>base-game</strong> (<c>res://</c>) asset paths from vanilla folder and atlas
    ///     entry names. They do not infer paths from mod model ids — mod-only art uses explicit profile fields or your PCK
    ///     layout. Act borrowing mirrors <see cref="Characters.CharacterAssetProfiles.FromCharacterId" /> (vanilla id in,
    ///     vanilla paths out).
    /// </summary>
    public static class ContentAssetProfiles
    {
        /// <summary>
        ///     Builds default portrait and overlay paths for a card in <paramref name="poolEntry" /> /
        ///     <paramref name="cardEntry" />.
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
        /// </summary>
        public static EnchantmentAssetProfile Enchantment(string enchantmentEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(enchantmentEntry);

            var normalized = Normalize(enchantmentEntry);
            return new(
                ImageHelper.GetImagePath($"enchantments/{normalized}.png"));
        }

        /// <summary>
        ///     Builds a full <see cref="ActAssetProfile" /> for a <strong>vanilla</strong> act folder name (e.g. <c>hive</c>,
        ///     <c>ship</c>): main background scene, <c>scenes/backgrounds/&lt;id&gt;/layers</c>, map parallax images, rest
        ///     site, chest Spine. Pass the base-game directory name, not a mod <see cref="ActModel" /> <c>Id.Entry</c>.
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
        /// </summary>
        public static ActAssetProfile Act(string actEntry)
        {
            return FromVanillaActId(actEntry);
        }

        /// <summary>
        ///     Base-game combat background layers directory for <paramref name="vanillaActFolderName" />
        ///     (<c>res://scenes/backgrounds/&lt;act&gt;/layers</c>).
        /// </summary>
        /// <param name="vanillaActFolderName">Vanilla act directory name (e.g. <c>hive</c>), not a mod act model id.</param>
        public static string ActVanillaBackgroundLayersDirectory(string vanillaActFolderName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(vanillaActFolderName);
            return $"res://scenes/backgrounds/{Normalize(vanillaActFolderName)}/layers";
        }

        /// <summary>
        ///     Builds default encounter asset paths for <paramref name="encounterEntry" /> (vanilla <c>EncounterModel</c> layout),
        ///     including concrete run-history texture paths under <c>ui/run_history/</c> (same files vanilla uses for that slug).
        /// </summary>
        /// <param name="encounterEntry">Vanilla encounter folder / animation slug (normalized to lowercase).</param>
        /// <param name="runHistoryIconPath">
        ///     When non-null, overrides the main run-history icon path; otherwise
        ///     <see cref="ImageHelper.GetImagePath" /><c>($\"ui/run_history/{normalized}.png\")</c>.
        /// </param>
        /// <param name="runHistoryIconOutlinePath">
        ///     When non-null, overrides the outline path; otherwise
        ///     <see cref="ImageHelper.GetImagePath" /><c>($\"ui/run_history/{normalized}_outline.png\")</c>.
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
        /// </summary>
        public static string EncounterVanillaBackgroundLayersDirectory(string encounterEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(encounterEntry);
            return $"res://scenes/backgrounds/{Normalize(encounterEntry)}/layers";
        }

        /// <summary>
        ///     Builds default creature visuals scene path for <paramref name="monsterEntry" />.
        /// </summary>
        public static MonsterAssetProfile Monster(string monsterEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(monsterEntry);
            return new(SceneHelper.GetScenePath($"creature_visuals/{Normalize(monsterEntry)}"));
        }

        /// <summary>
        ///     Builds default event asset paths for <paramref name="eventEntry" /> (default / combat style events).
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
        /// </summary>
        public static string EventCustomLayoutScenePath(string eventEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(eventEntry);
            return SceneHelper.GetScenePath($"events/custom/{Normalize(eventEntry)}");
        }

        /// <summary>
        ///     Builds default ancient presentation paths for <paramref name="ancientEntry" />.
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
