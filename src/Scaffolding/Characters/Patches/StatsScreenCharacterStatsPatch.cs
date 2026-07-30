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
    ///     <para xml:lang="en">
    ///         Appends saved statistics for playable mod characters to the general statistics screen. The base
    ///         <see cref="NGeneralStatsGrid.LoadStats" /> implementation creates sections only for the five built-in
    ///         characters.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将可玩模组角色的已保存统计数据追加到综合统计界面。游戏本体的
    ///         <see cref="NGeneralStatsGrid.LoadStats" /> 实现只会为五名内置角色创建区段。
    ///     </para>
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
            "Append playable non-vanilla character progress records to NGeneralStatsGrid character history sections";

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

                if (ModelDb.GetByIdOrNull<CharacterModel>(id) is not { IsPlayable: true })
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
