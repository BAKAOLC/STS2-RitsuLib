using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Makes direct <see cref="ImageHelper.GetRoomIconPath" /> and
    ///         <see cref="ImageHelper.GetRoomIconOutlinePath" /> calls honor encounter run-history icon overrides.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使直接调用 <see cref="ImageHelper.GetRoomIconPath" /> 和
    ///         <see cref="ImageHelper.GetRoomIconOutlinePath" /> 时识别遭遇的游戏历史图标覆盖。
    ///     </para>
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal sealed class ImageHelperModEncounterRunHistoryIconPathPatch : IPatchMethod
    {
        public static string PatchId => "image_helper_mod_encounter_run_history_icon_path";

        public static string Description =>
            "Route encounter run-history icon paths through IModEncounterAssetOverrides custom texture paths";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ImageHelper), nameof(ImageHelper.GetRoomIconPath)),
                new(typeof(ImageHelper), nameof(ImageHelper.GetRoomIconOutlinePath)),
            ];
        }

        public static bool Prefix(
            MethodBase __originalMethod,
            MapPointType mapPointType,
            RoomType roomType,
            ModelId? modelId,
            ref string? __result)
        {
            if (modelId is null)
                return true;

            if (ModelDb.GetByIdOrNull<AbstractModel>(modelId) is not EncounterModel encounter)
                return true;

            var path = __originalMethod.Name switch
            {
                nameof(ImageHelper.GetRoomIconPath) => ResolveMainIconPath(encounter),
                nameof(ImageHelper.GetRoomIconOutlinePath) => ResolveOutlineIconPath(encounter),
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

        private static string? ResolveMainIconPath(EncounterModel encounter)
        {
            if (ExternalAssetOverrideRegistry.TryGetEncounterRunHistoryIconPath(encounter, out var externalPath) &&
                AssetPathDiagnostics.Exists(
                    externalPath,
                    encounter,
                    "ExternalAssetOverrideRegistry.EncounterRunHistoryIconPath"))
                return externalPath;

            return (encounter as IModEncounterAssetOverrides)?.CustomRunHistoryIconPath;
        }

        private static string? ResolveOutlineIconPath(EncounterModel encounter)
        {
            if (ExternalAssetOverrideRegistry.TryGetEncounterRunHistoryIconOutlinePath(
                    encounter,
                    out var externalPath) &&
                AssetPathDiagnostics.Exists(
                    externalPath,
                    encounter,
                    "ExternalAssetOverrideRegistry.EncounterRunHistoryIconOutlinePath"))
                return externalPath;

            return (encounter as IModEncounterAssetOverrides)?.CustomRunHistoryIconOutlinePath;
        }
    }
}
