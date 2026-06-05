using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Models.Patches
{
    /// <summary>
    ///     Dispatches vanilla model clone operations to <see cref="ModelCloneRegistry" />.
    ///     将原版模型复制操作分发到 <see cref="ModelCloneRegistry" />。
    /// </summary>
    internal sealed class ModelCloneRegistryPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "model_clone_registry";

        /// <inheritdoc />
        public static string Description => "Notify registered listeners after vanilla model cloning";

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(AbstractModel), nameof(AbstractModel.MutableClone), Type.EmptyTypes),
            ];
        }

        /// <summary>
        ///     Dispatches clone context after vanilla clone state has been initialized.
        ///     在原版复制状态初始化完成后分发复制上下文。
        /// </summary>
        /// <param name="__instance">
        ///     Original model instance.
        ///     原始模型实例。
        /// </param>
        /// <param name="__result">
        ///     Cloned model instance.
        ///     复制出的模型实例。
        /// </param>
        public static void Postfix(AbstractModel __instance, AbstractModel __result)
        {
            ModelSavedDataRegistry.NotifyCloned(__instance, __result);
            ModelCapabilities.NotifyCloned(__instance, __result);
            ModelCloneRegistry.NotifyCloned(__instance, __result);
        }
    }
}
