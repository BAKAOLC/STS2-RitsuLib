using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick.Patches;
using STS2RitsuLib.Lifecycle.Patches;
using STS2RitsuLib.Models.Identity.Patches;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Unlocks.Patches;

namespace STS2RitsuLib.Platform.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies deferred Harmony patches after <see cref="ModelDb.Init" />. These patches cannot resolve their
    ///         targets during the first mod-load pass because resolution can trigger static initialization that
    ///         depends on <see cref="ModelDb" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <see cref="ModelDb.Init" /> 后应用延迟的 Harmony 补丁。这些补丁不能在首次加载模组时解析目标，
    ///         因为解析可能触发依赖 <see cref="ModelDb" /> 的静态初始化。
    ///     </para>
    /// </summary>
    internal sealed class RitsuLibModelDbInitPostfixPatch : IPatchMethod
    {
        private static int _applied;

        public static string PatchId => "model_db_init_apply_deferred_patches";

        public static string Description =>
            "After ModelDb.Init, apply deferred patches whose target resolution can trigger model-dependent static init";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), nameof(ModelDb.Init))];
        }

        public static void Postfix()
        {
            if (Interlocked.Exchange(ref _applied, 1) != 0)
                return;

            try
            {
                var core = RitsuLibFramework.GetFrameworkPatcher(RitsuLibFramework.FrameworkPatcherArea.Core);
                core.ApplyLateStaticPatches(
                [
                    .. IPatchMethod.CreatePatchInfos<NDailyRunLoadScreenBeginRunMissingCharacterPatch>(),
                    .. IPatchMethod.CreatePatchInfos<ModModelIdentityRunStateCreatePatch>(),
                    .. IPatchMethod.CreatePatchInfos<ModRightClickCardHolderPatch>(),
                    .. IPatchMethod.CreatePatchInfos<ModRightClickCardPilePatch>(),
                    .. IPatchMethod.CreatePatchInfos<ModRightClickRelicPatch>(),
                    .. IPatchMethod.CreatePatchInfos<ModRightClickPowerPatch>(),
                    .. IPatchMethod.CreatePatchInfos<ModRightClickPotionPatch>(),
                    .. IPatchMethod.CreatePatchInfos<ModRightClickOrbPatch>(),
                ]);

                var unlockPatches = new List<ModPatchInfo>();
                unlockPatches.AddRange(IPatchMethod.CreatePatchInfos<CharacterUnlockFilterPatch>());
                unlockPatches.AddRange(IPatchMethod.CreatePatchInfos<SharedAncientUnlockFilterPatch>());
                unlockPatches.AddRange(IPatchMethod.CreatePatchInfos<EliteEpochAfterCombatFallbackPatch>());
                var unlocks = RitsuLibFramework.GetFrameworkPatcher(RitsuLibFramework.FrameworkPatcherArea.Unlocks);
                unlocks.ApplyLateStaticPatches([.. unlockPatches]);
                RitsuLibFramework.Logger.Info(
                    "[ModelDbDefer] Applied deferred patches after ModelDb.Init.");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[ModelDbDefer] Failed to apply deferred patches after ModelDb.Init: {ex}");
            }
        }
    }
}
