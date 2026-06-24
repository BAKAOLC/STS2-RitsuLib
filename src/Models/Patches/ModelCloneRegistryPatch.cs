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
        public static string PatchId => "model_clone_registry";
        public static string Description => "Notify registered listeners after vanilla model cloning";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(AbstractModel), nameof(AbstractModel.MutableClone), Type.EmptyTypes),
            ];
        }

        public static void Postfix(AbstractModel __instance, AbstractModel __result)
        {
            ModelSavedDataRegistry.NotifyCloned(__instance, __result);
            ModelCapabilities.NotifyCloned(__instance, __result);
            ModelCloneRegistry.NotifyCloned(__instance, __result);
        }
    }
}
