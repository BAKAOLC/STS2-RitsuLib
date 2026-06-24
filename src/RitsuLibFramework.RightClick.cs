using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     Registers a synced right-click binding for models of type <typeparamref name="TModel" />.
        ///     为 <typeparamref name="TModel" /> 类型的模型注册同步右键绑定。
        /// </summary>
        /// <param name="modId">Owning mod id. 所属 mod id。</param>
        /// <param name="localStem">Local binding id stem. 本地 binding id stem。</param>
        /// <param name="canHandle">
        ///     Execution-time guard. It runs after the synced action resolves the model on each peer. Do not use this
        ///     delegate for local-only UI filtering.
        ///     执行期判定：同步动作在各端解析模型后调用。不要将它用于仅本地 UI 过滤。
        /// </param>
        /// <param name="execute">Synced right-click behavior. 同步右键行为。</param>
        /// <param name="priority">Binding priority; higher values run first. 优先级越高越先运行。</param>
        /// <returns>
        ///     A disposable registration handle.
        ///     可释放的注册句柄。
        /// </returns>
        public static IDisposable RegisterRightClick<TModel>(
            string modId,
            string localStem,
            Func<ModRightClickContext, bool> canHandle,
            Func<ModRightClickExecutionContext, Task> execute,
            int priority = 0)
            where TModel : AbstractModel
        {
            return ModRightClickRegistry.Register<TModel>(modId, localStem, canHandle, execute, priority);
        }

        /// <summary>
        ///     Registers a synced right-click binding for models of type <typeparamref name="TModel" />.
        ///     为 <typeparamref name="TModel" /> 类型的模型注册同步右键绑定。
        /// </summary>
        /// <param name="modId">Owning mod id. 所属 mod id。</param>
        /// <param name="localStem">Local binding id stem. 本地 binding id stem。</param>
        /// <param name="execute">Synced right-click behavior. 同步右键行为。</param>
        /// <param name="priority">Binding priority; higher values run first. 优先级越高越先运行。</param>
        /// <param name="canHandleLocal">
        ///     Optional local-only fast filter. Use only stable, local UI facts here; mutable gameplay state should be
        ///     checked in <paramref name="canExecute" /> or <paramref name="execute" />.
        ///     可选的仅本地快速过滤。这里只应使用稳定的本地 UI 信息；可变游戏状态应在
        ///     <paramref name="canExecute" /> 或 <paramref name="execute" /> 中检查。
        /// </param>
        /// <param name="canExecute">
        ///     Optional execution-time guard. It runs after the synced action resolves the model on each peer.
        ///     可选执行期判定：同步动作在各端解析模型后调用。
        /// </param>
        /// <returns>
        ///     A disposable registration handle.
        ///     可释放的注册句柄。
        /// </returns>
        public static IDisposable RegisterRightClick<TModel>(
            string modId,
            string localStem,
            Func<ModRightClickExecutionContext, Task> execute,
            int priority = 0,
            Func<ModRightClickContext, bool>? canHandleLocal = null,
            Func<ModRightClickExecutionContext, bool>? canExecute = null)
            where TModel : AbstractModel
        {
            return ModRightClickRegistry.Register<TModel>(
                modId,
                localStem,
                execute,
                priority,
                canHandleLocal,
                canExecute);
        }
    }
}
