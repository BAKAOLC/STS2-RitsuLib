using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Timeline.Scaffolding;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Adds obtained mod epoch templates omitted by the base game's Neow-rooted expansion traversal to
    ///         <see cref="ProgressSaveManager.GetRevealableEpochs" />. This lets independent mod story roots contribute to
    ///         discovery counts and timeline notifications.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将游戏本体以涅奥为根的扩展遍历所遗漏、但已获得的模组纪元模板加入
    ///         <see cref="ProgressSaveManager.GetRevealableEpochs" />，使独立的模组故事根节点可计入发现数量和时间线通知。
    ///     </para>
    /// </summary>
    internal sealed class ProgressSaveManagerGetRevealableEpochsModTemplatePatch : IPatchMethod
    {
        public static string PatchId => "progress_save_manager_get_revealable_epochs_mod_template";

        public static string Description =>
            "Union ModEpochTemplate rows in Obtained/ObtainedNoSlot into GetRevealableEpochs when vanilla omits them";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ProgressSaveManager), nameof(ProgressSaveManager.GetRevealableEpochs), Type.EmptyTypes)];
        }

        public static IEnumerable<SerializableEpoch> Postfix(
            IEnumerable<SerializableEpoch> __result,
            ProgressSaveManager __instance)
        {
            var list = __result.ToList();
            var seen = new HashSet<string>(list.Select(e => e.Id));

            foreach (var epoch in __instance.Progress.Epochs)
            {
                var st = epoch.State;
                if (st != EpochState.Obtained && st != EpochState.ObtainedNoSlot)
                    continue;
                if (!seen.Add(epoch.Id))
                    continue;

                EpochModel model;
                try
                {
                    model = EpochModel.Get(epoch.Id);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Unlocks] Could not resolve obtained epoch '{epoch.Id}' while extending revealable epochs: {ex}");
                    seen.Remove(epoch.Id);
                    continue;
                }

                if (model is not ModEpochTemplate)
                {
                    seen.Remove(epoch.Id);
                    continue;
                }

                list.Add(epoch);
            }

            return list;
        }
    }
}
