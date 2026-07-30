using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     <para xml:lang="en">Registers a synchronized right-click binding for models of type <typeparamref name="TModel" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TModel" /> 类型的模型注册同步右键绑定。</para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">Owning mod ID.</para>
        ///     <para xml:lang="zh-CN">所属模组 ID。</para>
        /// </param>
        /// <param name="localStem">
        ///     <para xml:lang="en">Local binding-ID stem.</para>
        ///     <para xml:lang="zh-CN">本地绑定 ID 词干。</para>
        /// </param>
        /// <param name="canHandle">
        ///     <para xml:lang="en">
        ///         Execution-time guard, invoked after the synchronized action resolves the model on each peer; do
        ///         not use it for local-only UI filtering.
        ///     </para>
        ///     <para xml:lang="zh-CN">执行期判定，在同步动作于各端解析模型后调用；不要将其用于仅本地的界面过滤。</para>
        /// </param>
        /// <param name="execute">
        ///     <para xml:lang="en">Synchronized right-click behavior.</para>
        ///     <para xml:lang="zh-CN">同步右键行为。</para>
        /// </param>
        /// <param name="priority">
        ///     <para xml:lang="en">Binding priority; higher values run first.</para>
        ///     <para xml:lang="zh-CN">绑定优先级；值越高越先运行。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">Disposable registration handle.</para>
        ///     <para xml:lang="zh-CN">可释放的注册句柄。</para>
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
        ///     <para xml:lang="en">Registers a synchronized right-click binding for models of type <typeparamref name="TModel" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TModel" /> 类型的模型注册同步右键绑定。</para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">Owning mod ID.</para>
        ///     <para xml:lang="zh-CN">所属模组 ID。</para>
        /// </param>
        /// <param name="localStem">
        ///     <para xml:lang="en">Local binding-ID stem.</para>
        ///     <para xml:lang="zh-CN">本地绑定 ID 词干。</para>
        /// </param>
        /// <param name="execute">
        ///     <para xml:lang="en">Synchronized right-click behavior.</para>
        ///     <para xml:lang="zh-CN">同步右键行为。</para>
        /// </param>
        /// <param name="priority">
        ///     <para xml:lang="en">Binding priority; higher values run first.</para>
        ///     <para xml:lang="zh-CN">绑定优先级；值越高越先运行。</para>
        /// </param>
        /// <param name="canHandleLocal">
        ///     <para xml:lang="en">
        ///         Optional local-only fast filter; use only stable local UI facts, and check mutable gameplay
        ///         state in <paramref name="canExecute" /> or <paramref name="execute" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的仅本地快速过滤；只应使用稳定的本地界面信息，可变游戏状态应在 <paramref name="canExecute" /> 或
        ///         <paramref name="execute" /> 中检查。
        ///     </para>
        /// </param>
        /// <param name="canExecute">
        ///     <para xml:lang="en">
        ///         Optional execution-time guard, invoked after the synchronized action resolves the model on each
        ///         peer.
        ///     </para>
        ///     <para xml:lang="zh-CN">可选执行期判定，在同步动作于各端解析模型后调用。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">Disposable registration handle.</para>
        ///     <para xml:lang="zh-CN">可释放的注册句柄。</para>
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
