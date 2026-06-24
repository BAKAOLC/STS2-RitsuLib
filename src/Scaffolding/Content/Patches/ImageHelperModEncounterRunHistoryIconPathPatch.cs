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
    ///     Run history and top bar call <see cref="ImageHelper.GetRoomIconPath" /> with a <see cref="ModelId" /> that may be
    ///     an
    ///     encounter, ancient, event, etc. Only when the id resolves to an <see cref="EncounterModel" /> with mod overrides do
    ///     we
    ///     remap paths (otherwise vanilla resolution runs). Mod encounters without this would hit missing
    ///     <c>ui/run_history/&lt;mod_entry&gt;.png</c>. This prefix returns
    ///     <see cref="IModEncounterAssetOverrides.CustomRunHistoryIconPath" /> /
    ///     <see cref="IModEncounterAssetOverrides.CustomRunHistoryIconOutlinePath" />
    ///     when those paths exist (same pattern as <see cref="ImageHelperAncientModRunHistoryIconPathPatch" /> for ancients).
    ///     跑局历史和顶部栏会使用一个 <see cref="ModelId" /> 调用 <see cref="ImageHelper.GetRoomIconPath" />，该 id 可能是
    ///     一个
    ///     遭遇、远古事件、事件等。只有当 id 解析为带有 mod 覆盖的 <see cref="EncounterModel" /> 时，
    ///     我们
    ///     才会重映射路径（否则走原版解析）。没有此前缀的 mod 遭遇会命中缺失的
    ///     <c>ui/run_history/&lt;mod_entry&gt;.png</c>。此前缀会返回
    ///     <see cref="IModEncounterAssetOverrides.CustomRunHistoryIconPath" /> /
    ///     <see cref="IModEncounterAssetOverrides.CustomRunHistoryIconOutlinePath" />
    ///     中存在的路径（模式与远古事件的 <see cref="ImageHelperAncientModRunHistoryIconPathPatch" /> 相同）。
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
            return ExternalAssetOverrideRegistry.TryGetEncounterRunHistoryIconPath(encounter, out var externalPath)
                ? externalPath
                : (encounter as IModEncounterAssetOverrides)?.CustomRunHistoryIconPath;
        }

        private static string? ResolveOutlineIconPath(EncounterModel encounter)
        {
            return ExternalAssetOverrideRegistry.TryGetEncounterRunHistoryIconOutlinePath(encounter,
                out var externalPath)
                ? externalPath
                : (encounter as IModEncounterAssetOverrides)?.CustomRunHistoryIconOutlinePath;
        }
    }
}
