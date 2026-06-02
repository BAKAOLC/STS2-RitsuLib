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

        public static string Description => "注册 RitsuLib 自定义 TargetType 过滤器";

        public static bool IsCritical => false;

        /// <summary>
        ///     Targets the model database initialization entry point.
        ///     目标为模型数据库初始化入口。
        /// </summary>
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), nameof(ModelDb.Init))];
        }

        /// <summary>
        ///     Performs registry bootstrap for built-in custom target types.
        ///     执行内置自定义目标类型注册表引导。
        /// </summary>
        public static void Postfix()
        {
            CustomTargetTypeRegistry.RegisterBuiltIns();
            RitsuLibStartupAudit.Measure("modelDb.validateBaseLibDynamicEnums",
                RegistrationConflictDetector.ValidateAndLogBaseLibDynamicEnumValueCollisions);
        }
    }
}
