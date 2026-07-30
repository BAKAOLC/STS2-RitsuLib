using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Models.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Dispatches base-game model clone operations to model saved data, capabilities, and
    ///         <see cref="ModelCloneRegistry" /> listeners.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将游戏原版模型复制操作分发给模型保存数据、模型能力及
    ///         <see cref="ModelCloneRegistry" /> 监听器。
    ///     </para>
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
