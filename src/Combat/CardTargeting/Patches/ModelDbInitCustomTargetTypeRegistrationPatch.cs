using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Diagnostics;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     Registers built-in custom target predicates after <see cref="ModelDb.Init" />.
    ///     在 <see cref="ModelDb.Init" /> 完成后注册内置自定义目标谓词。
    /// </summary>
    internal sealed class ModelDbInitCustomTargetTypeRegistrationPatch : IPatchMethod
    {
        public static string PatchId => "card_target_model_db_init_custom_target_type_registration";

        public static string Description => "Register RitsuLib custom TargetType filters";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), nameof(ModelDb.Init))];
        }

        public static void Postfix()
        {
            CustomTargetTypeRegistry.RegisterBuiltIns();
            RitsuLibStartupAudit.Measure("modelDb.validateBaseLibDynamicEnums",
                RegistrationConflictDetector.ValidateAndLogBaseLibDynamicEnumValueCollisions);
        }
    }
}
