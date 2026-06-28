using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Godot;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Appends mod character history sections to the general stats screen.
    ///     Base game NGeneralStatsGrid.LoadStats hard-codes five vanilla characters,
    ///     so non-vanilla character records never render without this patch.
    ///     向通用统计界面追加 mod 角色历史区段。
    ///     基础游戏 NGeneralStatsGrid.LoadStats 硬编码了五个原版角色，
    ///     没有此补丁时，mod 角色记录永远不会渲染。
    /// </summary>
    internal class StatsScreenCharacterStatsPatch : IPatchMethod
    {
        private static readonly StringName RitsuLibCharacterStatsIdMeta = new("RitsuLibCharacterStatsId");

        private static readonly AccessTools.FieldRef<NGeneralStatsGrid, Control> CharacterStatContainerRef =
            AccessTools.FieldRefAccess<NGeneralStatsGrid, Control>("_characterStatContainer");

        private static readonly AccessTools.FieldRef<NCharacterStats, CharacterStats> CharacterStatsRef =
            AccessTools.FieldRefAccess<NCharacterStats, CharacterStats>("_characterStats");

        public static string PatchId => "stats_screen_mod_character_sections";

        public static string Description =>
            "Append non-vanilla character progress records to NGeneralStatsGrid character history sections";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NGeneralStatsGrid), nameof(NGeneralStatsGrid.LoadStats))];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(NGeneralStatsGrid __instance)
        {
            var progressSave = SaveManager.Instance.Progress;
            var container = CharacterStatContainerRef(__instance);
            if (container == null)
                return;

            var visibleCharacterIds = GetVisibleCharacterIds(container);
            foreach (var stats in progressSave.CharacterStats.Values.OrderBy(static stats => stats.Id?.Entry,
                         StringComparer.Ordinal))
            {
                var id = stats.Id;
                if ((object?)id == null || id == ModelId.none || visibleCharacterIds.Contains(id))
                    continue;

                if (ModelDb.GetByIdOrNull<CharacterModel>(id) == null)
                    continue;

                var child = NCharacterStats.Create(stats);
                child.SetMeta(RitsuLibCharacterStatsIdMeta, id.ToString());
                RitsuGodotTreeCompat.AddChildSafely(container, child);
                visibleCharacterIds.Add(id);
            }
        }

        private static HashSet<ModelId> GetVisibleCharacterIds(Node container)
        {
            var result = new HashSet<ModelId>();
            foreach (var child in container.GetChildren())
            {
                if (child is not NCharacterStats characterStatsNode)
                    continue;

                var stats = CharacterStatsRef(characterStatsNode);

                var id = stats?.Id;
                if ((object?)id != null && id != ModelId.none)
                    result.Add(id);
            }

            return result;
        }
    }
}
