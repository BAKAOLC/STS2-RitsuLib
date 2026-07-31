using STS2RitsuLib.Interactions.RightClick;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Controls whether a right-click capability stops or continues the capability chain after it
    ///         runs.
    ///     </para>
    ///     <para xml:lang="zh-CN">控制右键能力执行后是否停止或继续能力链。</para>
    /// </summary>
    public enum ModelRightClickCapabilityRunMode
    {
        /// <summary>
        ///     <para xml:lang="en">Run the first matching capability and stop. This is the default action-style behavior.</para>
        ///     <para xml:lang="zh-CN">执行第一个匹配能力后停止；这是面向操作的默认行为。</para>
        /// </summary>
        Exclusive,

        /// <summary>
        ///     <para xml:lang="en">Continue checking later matching capabilities after this one runs.</para>
        ///     <para xml:lang="zh-CN">执行后继续检查后续匹配能力。</para>
        /// </summary>
        Continue,
    }

    /// <summary>
    ///     <para xml:lang="en">Optional model capability that handles synchronized right-click interactions through RitsuLib.</para>
    ///     <para xml:lang="zh-CN">可选模型能力：通过 RitsuLib 处理同步的右键交互。</para>
    /// </summary>
    public interface IModelRightClickCapability
    {
        /// <summary>
        ///     <para xml:lang="en">Higher priority capabilities are checked first; ties keep the attached capability order.</para>
        ///     <para xml:lang="zh-CN">优先级越高越先检查；相同优先级保持附加能力顺序。</para>
        /// </summary>
        int RightClickPriority => 0;

        /// <summary>
        ///     <para xml:lang="en">Controls whether execution stops after this capability handles the right-click.</para>
        ///     <para xml:lang="zh-CN">控制此能力处理右键后是否停止执行链。</para>
        /// </summary>
        ModelRightClickCapabilityRunMode RightClickRunMode => ModelRightClickCapabilityRunMode.Exclusive;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional local-only fast filter. Use only stable, local UI facts here; mutable gameplay state should be
        ///         checked in <see cref="CanExecuteRightClick" /> or <see cref="OnRightClick" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的仅本地快速过滤。这里只应使用稳定的本地界面信息；可变游戏状态应在
        ///         <see cref="CanExecuteRightClick" /> 或 <see cref="OnRightClick" /> 中检查。
        ///     </para>
        /// </summary>
        bool CanHandleRightClickLocal(ModRightClickContext context)
        {
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Execution-time guard. It runs after the synchronized action resolves the model on each peer.</para>
        ///     <para xml:lang="zh-CN">执行期判定：同步动作在各端解析模型后调用。</para>
        /// </summary>
        bool CanExecuteRightClick(ModRightClickExecutionContext context)
        {
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Runs the synchronized right-click behavior.</para>
        ///     <para xml:lang="zh-CN">执行同步右键行为。</para>
        /// </summary>
        Task OnRightClick(ModRightClickExecutionContext context);
    }
}
