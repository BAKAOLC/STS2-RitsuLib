using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Models.Identity;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     Ensures a runtime identity exists for a mutable model at a deterministic synchronization entry point.
        ///     在确定性的同步入口为 mutable model 确保存在运行时身份。
        /// </summary>
        public static ModModelIdentity EnsureModelIdentity(AbstractModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            return ModModelIdentityRegistry.EnsureRegistered(model);
        }

        /// <summary>
        ///     Tries to get the runtime identity token for a mutable model.
        ///     尝试获取 mutable model 的运行时身份令牌。
        /// </summary>
        public static bool TryGetModelIdentity(AbstractModel model, out ModModelIdentityToken token)
        {
            ArgumentNullException.ThrowIfNull(model);
            return ModModelIdentityRegistry.TryGetToken(model, out token);
        }

        /// <summary>
        ///     Tries to resolve a runtime identity token to the current local model instance.
        ///     尝试将运行时身份令牌解析为当前本地 model 实例。
        /// </summary>
        public static bool TryResolveModelIdentity(ModModelIdentityToken token, out AbstractModel model)
        {
            return ModModelIdentityRegistry.TryResolve(token, out model);
        }
    }
}
