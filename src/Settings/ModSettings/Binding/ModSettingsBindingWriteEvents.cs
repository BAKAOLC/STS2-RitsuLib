using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Publishes notifications after built-in <see cref="IModSettingsValueBinding{TValue}" /> implementations
    ///         finish writing their backing values. This supports settings UI refresh and external value mirrors.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在内置 <see cref="IModSettingsValueBinding{TValue}" /> 实现完成底层值写入后发布通知，
    ///         用于设置界面刷新以及与外部值保持同步。
    ///     </para>
    /// </summary>
    public static class ModSettingsBindingWriteEvents
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Occurs synchronously after a binding updates its backing value. Subscriber failures are logged and do
        ///         not prevent later subscribers from running.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在绑定更新底层值后同步发生。订阅者失败会被记录，且不会阻止后续订阅者运行。
        ///     </para>
        /// </summary>
        public static event Action<IModSettingsBinding>? ValueWritten;

        internal static void NotifyValueWritten(IModSettingsBinding binding)
        {
            var handlers = ValueWritten?.GetInvocationList();
            if (handlers == null)
                return;

            foreach (var handler in handlers)
                try
                {
                    ((Action<IModSettingsBinding>)handler).Invoke(binding);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[ModSettings] ValueWritten callback failed for '{binding.GetType().FullName}': {ex}");
                }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Subscribes a listener while a Godot node remains in the scene tree and automatically removes the same
        ///         delegate when that node exits the tree.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 Godot 节点留在场景树中期间订阅监听器，并在该节点退出场景树时自动移除同一委托。
        ///     </para>
        /// </summary>
        /// <param name="anchor">
        ///     <para xml:lang="en">The node whose scene-tree lifetime controls the subscription.</para>
        ///     <para xml:lang="zh-CN">以其场景树生命周期控制订阅的节点。</para>
        /// </param>
        /// <param name="listener">
        ///     <para xml:lang="en">The listener invoked after a binding value is written.</para>
        ///     <para xml:lang="zh-CN">绑定值写入后调用的监听器。</para>
        /// </param>
        public static void SubscribeValueWrittenWhileNodeAlive(Node anchor, Action<IModSettingsBinding> listener)
        {
            ArgumentNullException.ThrowIfNull(anchor);
            ArgumentNullException.ThrowIfNull(listener);

            ValueWritten += Wrapped;

            try
            {
                anchor.Connect(Node.SignalName.TreeExiting, Callable.From(() => ValueWritten -= Wrapped),
                    (uint)GodotObject.ConnectFlags.OneShot);
            }
            catch
            {
                ValueWritten -= Wrapped;
                throw;
            }

            return;

            void Wrapped(IModSettingsBinding binding)
            {
                if (!GodotObject.IsInstanceValid(anchor))
                    return;
                listener(binding);
            }
        }
    }
}
