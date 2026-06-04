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
    }
}
