using System.Reflection;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Run history and top bar call <see cref="ImageHelper.GetRoomIconPath" /> with a <see cref="ModelId" /> that may be
    ///     an
    ///     encounter, ancient, event, etc. Only when the id resolves to an <see cref="EncounterModel" /> with mod overrides do
    ///     we
    ///     remap paths (otherwise vanilla resolution runs). Mod encounters without this would hit missing
    ///     <c>ui/run_history/&lt;mod_entry&gt;.png</c>. This prefix returns
    ///     <see cref="IModEncounterAssetOverrides.CustomRunHistoryIconPath" /> /
    ///     <see cref="IModEncounterAssetOverrides.CustomRunHistoryIconOutlinePath" />
    ///     when those paths exist (same pattern as <see cref="ImageHelperAncientModRunHistoryIconPathPatch" /> for ancients).
    /// </summary>
    public sealed class ImageHelperModEncounterRunHistoryIconPathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "image_helper_mod_encounter_run_history_icon_path";

        /// <inheritdoc />
        public static string Description =>
            "Route encounter run-history icon paths through IModEncounterAssetOverrides custom texture paths";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ImageHelper), nameof(ImageHelper.GetRoomIconPath)),
                new(typeof(ImageHelper), nameof(ImageHelper.GetRoomIconOutlinePath)),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Harmony prefix: return the configured <c>res://images/…</c> path when present on disk / in the resource loader.
        /// </summary>
        public static bool Prefix(
                MethodBase __originalMethod,
                MapPointType mapPointType,
                RoomType roomType,
                ModelId? modelId,
                ref string? __result)
            // ReSharper restore InconsistentNaming
        {
            if (modelId is null)
                return true;

            if (ModelDb.GetByIdOrNull<AbstractModel>(modelId!) is not EncounterModel encounter)
                return true;

            if (encounter is not IModEncounterAssetOverrides overrides)
                return true;

            var path = __originalMethod.Name switch
            {
                nameof(ImageHelper.GetRoomIconPath) => overrides.CustomRunHistoryIconPath,
                nameof(ImageHelper.GetRoomIconOutlinePath) => overrides.CustomRunHistoryIconOutlinePath,
                _ => null,
            };

            var memberLabel = __originalMethod.Name == nameof(ImageHelper.GetRoomIconPath)
                ? nameof(IModEncounterAssetOverrides.CustomRunHistoryIconPath)
                : nameof(IModEncounterAssetOverrides.CustomRunHistoryIconOutlinePath);

            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, encounter, memberLabel))
                return true;

            __result = path;
            return false;
        }
    }
}
