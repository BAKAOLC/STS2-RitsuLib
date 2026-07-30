using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Models.Identity;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     <para xml:lang="en">Ensures a mutable model has a runtime identity at a deterministic synchronization entry point.</para>
        ///     <para xml:lang="zh-CN">在确定性的同步入口确保可变模型具有运行时身份。</para>
        /// </summary>
        public static ModModelIdentity EnsureModelIdentity(AbstractModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            return ModModelIdentityRegistry.EnsureRegistered(model);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get a mutable model's runtime identity token.</para>
        ///     <para xml:lang="zh-CN">尝试获取可变模型的运行时身份令牌。</para>
        /// </summary>
        public static bool TryGetModelIdentity(AbstractModel model, out ModModelIdentityToken token)
        {
            ArgumentNullException.ThrowIfNull(model);
            return ModModelIdentityRegistry.TryGetToken(model, out token);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to resolve a runtime identity token to the current local model instance.</para>
        ///     <para xml:lang="zh-CN">尝试将运行时身份令牌解析为当前本地模型实例。</para>
        /// </summary>
        public static bool TryResolveModelIdentity(ModModelIdentityToken token, out AbstractModel model)
        {
            return ModModelIdentityRegistry.TryResolve(token, out model);
        }
    }
}
