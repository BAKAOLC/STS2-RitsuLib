using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Timeline.Patches
{
    /// <summary>
    ///     Applies era-axis icon policy with default behavior that hides icons when a custom era has no texture.
    ///     应用 纪元轴图标策略 默认行为是在以下情况隐藏图标： a 自定义纪元没有纹理。
    /// </summary>
    internal sealed class NTimelineScreenGetEraIconPolicyPatch : IPatchMethod
    {
        public static string PatchId => "n_timeline_screen_get_era_icon_policy";

        public static string Description =>
            "Apply configurable era-axis icon policy and default-hide missing custom era icons";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NTimelineScreen), nameof(NTimelineScreen.GetEraIcon), [typeof(EpochEra)]),
            ];
        }

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
            return Enum.IsDefined(era) && ResourceLoader.Exists(GetEraTexturePath(era));
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
    ///     未解析到纹理时隐藏 era 图标节点。
    /// </summary>
    internal sealed class NEraColumnHideEmptyIconPatch : IPatchMethod
    {
        public static string PatchId => "n_era_column_hide_empty_icon";
        public static string Description => "Hide era-axis icon node when texture is null";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NEraColumn), nameof(NEraColumn.Init), [typeof(EpochSlotData)]),
            ];
        }

        public static void Postfix(NEraColumn __instance)
        {
            var icon = __instance.GetNode<TextureRect>("%Icon");
            if (icon.Texture == null)
                icon.Visible = false;
        }
    }
}
