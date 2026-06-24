using STS2RitsuLib.Interactions.RightClick;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Controls whether a right-click capability stops or continues the capability chain after it runs.
    ///     控制右键能力执行后是否停止或继续能力链。
    /// </summary>
    public enum ModelRightClickCapabilityRunMode
    {
        /// <summary>
        ///     Run the first matching capability and stop. This is the default action-style behavior.
        ///     执行第一个匹配能力后停止；这是默认的 action 风格行为。
        /// </summary>
        Exclusive,

        /// <summary>
        ///     Continue checking later matching capabilities after this one runs.
        ///     执行后继续检查后续匹配能力。
        /// </summary>
        Continue,
    }

    /// <summary>
    ///     Optional model capability that handles synced right-click interactions through RitsuLib.
    ///     可选模型能力：通过 RitsuLib 处理同步右键交互。
    /// </summary>
    public interface IModelRightClickCapability
    {
        /// <summary>
        ///     Higher priority capabilities are checked first; ties keep the attached capability order.
        ///     优先级越高越先检查；相同优先级保持附加能力顺序。
        /// </summary>
        int RightClickPriority => 0;

        /// <summary>
        ///     Controls whether execution stops after this capability handles the right-click.
        ///     控制此能力处理右键后是否停止执行链。
        /// </summary>
        ModelRightClickCapabilityRunMode RightClickRunMode => ModelRightClickCapabilityRunMode.Exclusive;

        /// <summary>
        ///     Optional local-only fast filter. Use only stable, local UI facts here; mutable gameplay state should be
        ///     checked in <see cref="CanExecuteRightClick" /> or <see cref="OnRightClick" />.
        ///     可选的仅本地快速过滤。这里只应使用稳定的本地 UI 信息；可变游戏状态应在
        ///     <see cref="CanExecuteRightClick" /> 或 <see cref="OnRightClick" /> 中检查。
        /// </summary>
        bool CanHandleRightClickLocal(ModRightClickContext context)
        {
            return true;
        }

        /// <summary>
        ///     Execution-time guard. It runs after the synced action resolves the model on each peer.
        ///     执行期判定：同步动作在各端解析模型后调用。
        /// </summary>
        bool CanExecuteRightClick(ModRightClickExecutionContext context)
        {
            return true;
        }

        /// <summary>
        ///     Runs the synced right-click behavior.
        ///     执行同步后的右键行为。
        /// </summary>
        Task OnRightClick(ModRightClickExecutionContext context);
    }
}
