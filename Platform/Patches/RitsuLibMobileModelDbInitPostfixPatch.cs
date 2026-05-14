using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Lifecycle.Patches;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Unlocks.Patches;

namespace STS2RitsuLib.Platform.Patches
{
    /// <summary>
    ///     After <see cref="ModelDb.Init" /> (including STS2Mobile's replaced init), applies Harmony patches that must
    ///     not run during the first mod-load pass because they touch static init that depends on the model registry.
    ///     在 <see cref="ModelDb.Init" /> 之后（包括 STS2Mobile 替换后的 init）应用必须
    ///     不在第一次 mod 加载阶段运行的 Harmony patch，因为它们会触及依赖模型注册表的静态初始化。
    /// </summary>
    internal sealed class RitsuLibMobileModelDbInitPostfixPatch : IPatchMethod
    {
        private static int _applied;

        public static string PatchId => "mobile_model_db_init_apply_deferred_patches";

        public static string Description =>
            "Android/iOS: after ModelDb.Init, apply deferred NDailyRun + UnlockState patches";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), nameof(ModelDb.Init))];
        }

        public static void Postfix()
        {
            if (!RitsuLibMobileSteamRuntime.SuppressNativeSteamIntegration)
                return;

            if (Interlocked.Exchange(ref _applied, 1) != 0)
                return;

            try
            {
                var core = RitsuLibFramework.GetFrameworkPatcher(RitsuLibFramework.FrameworkPatcherArea.Core);
                core.ApplyLateStaticPatches(
                    IPatchMethod.CreatePatchInfos<NDailyRunLoadScreenBeginRunMissingCharacterPatch>());

                var unlockPatches = new List<ModPatchInfo>();
                unlockPatches.AddRange(IPatchMethod.CreatePatchInfos<CharacterUnlockFilterPatch>());
                unlockPatches.AddRange(IPatchMethod.CreatePatchInfos<SharedAncientUnlockFilterPatch>());
                var unlocks = RitsuLibFramework.GetFrameworkPatcher(RitsuLibFramework.FrameworkPatcherArea.Unlocks);
                unlocks.ApplyLateStaticPatches(unlockPatches.ToArray());
                RitsuLibFramework.Logger.Info(
                    "[MobileDefer] Applied deferred patches after ModelDb.Init (daily run load + unlock filters).");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error(
                    $"[MobileDefer] Failed to apply deferred patches after ModelDb.Init: {ex}");
            }
        }
    }
}
