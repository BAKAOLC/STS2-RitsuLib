using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick.Patches;
using STS2RitsuLib.Lifecycle.Patches;
using STS2RitsuLib.Models.Identity.Patches;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Unlocks.Patches;

namespace STS2RitsuLib.Platform.Patches
{
    /// <summary>
    ///     After <see cref="ModelDb.Init" />, applies Harmony patches that must not resolve target methods during the
    ///     first mod-load pass because target resolution can trigger static initialization that depends on ModelDb.
    ///     在 <see cref="ModelDb.Init" /> 之后应用不应在第一次 mod 加载阶段解析目标方法的 Harmony patch，
    ///     因为目标解析可能触发依赖 ModelDb 的静态初始化。
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
