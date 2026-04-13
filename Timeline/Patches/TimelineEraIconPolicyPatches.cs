using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Timeline.Patches
{
    /// <summary>
    ///     Applies era-axis icon policy with default behavior that hides icons when a custom era has no texture.
    /// </summary>
    public sealed class NTimelineScreenGetEraIconPolicyPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "n_timeline_screen_get_era_icon_policy";

        /// <inheritdoc />
        public static string Description =>
            "Apply configurable era-axis icon policy and default-hide missing custom era icons";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NTimelineScreen), nameof(NTimelineScreen.GetEraIcon), [typeof(EpochEra)]),
            ];
        }

        /// <summary>
        ///     Uses <see cref="ModTimelineEraIconRegistry" /> when configured; otherwise keeps vanilla icon resolution if
        ///     resources exist and hides icon when they do not.
        /// </summary>
        public static bool Prefix(EpochEra era, ref (Texture2D Texture, string Name) __result)
        {
            if (ModTimelineEraIconRegistry.TryResolve(era, out var enabled, out var texturePath))
            {
                if (enabled == false)
                {
                    __result = (null!, ResolveEraLocKey(era));
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(texturePath) && ResourceLoader.Exists(texturePath))
                {
                    __result = (ResourceLoader.Load<Texture2D>(texturePath), ResolveEraLocKey(era));
                    return false;
                }
            }

            if (HasVanillaEraIconResource(era))
                return true;

            __result = (null!, ResolveEraLocKey(era));
            return false;
        }

        private static bool HasVanillaEraIconResource(EpochEra era)
        {
            if (!Enum.IsDefined(era))
                return false;

            return ResourceLoader.Exists(GetEraTexturePath(era));
        }

        private static string ResolveEraLocKey(EpochEra era)
        {
            if (Enum.IsDefined(era))
                return StringHelper.Slugify(era.ToString());

            var fallbackEra = (int)era < 0 ? EpochEra.Prehistoria0 : EpochEra.Seeds0;
            return StringHelper.Slugify(fallbackEra.ToString());
        }

        private static string GetEraTexturePath(EpochEra era)
        {
            var eraInt = (int)era;
            return eraInt >= (int)EpochEra.Seeds0
                ? $"res://images/atlases/era_atlas.sprites/era_{eraInt}.tres"
                : $"res://images/atlases/era_atlas.sprites/era_minus_{Math.Abs(eraInt)}.tres";
        }
    }

    /// <summary>
    ///     Hides the era icon node when no texture was resolved.
    /// </summary>
    public sealed class NEraColumnHideEmptyIconPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "n_era_column_hide_empty_icon";

        /// <inheritdoc />
        public static string Description => "Hide era-axis icon node when texture is null";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NEraColumn), nameof(NEraColumn.Init), [typeof(EpochSlotData)]),
            ];
        }

        /// <summary>
        ///     Ensures no empty placeholder icon remains visible when texture is absent.
        /// </summary>
        public static void Postfix(NEraColumn __instance)
        {
            var icon = __instance.GetNode<TextureRect>("%Icon");
            if (icon.Texture == null)
                icon.Visible = false;
        }
    }
}
