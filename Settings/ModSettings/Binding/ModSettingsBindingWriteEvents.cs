using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Multicast notifications raised after <see cref="IModSettingsValueBinding{TValue}.Write" /> completes on built-in
    ///     binding implementations. Subscribe from UI or tools that mirror settings elsewhere; use
    ///     <see cref="SubscribeValueWrittenWhileNodeAlive" /> so subscriptions drop when a host node leaves the tree.
    ///     内置绑定实现上的 <see cref="IModSettingsValueBinding{TValue}.Write" /> 完成后触发的多播通知。
    ///     可从 UI 或将设置镜像到其它位置的工具订阅；使用
    ///     <see cref="SubscribeValueWrittenWhileNodeAlive" />，使宿主节点离开树时自动取消订阅。
    /// </summary>
    public static class ModSettingsBindingWriteEvents
    {
        /// <summary>
        ///     Raised synchronously from binding <c>Write</c> bodies after the backing store has been updated.
        ///     在后备存储更新后，由绑定 <c>Write</c> 方法体同步触发。
        /// </summary>
        public static event Action<IModSettingsBinding>? ValueWritten;

        internal static void NotifyValueWritten(IModSettingsBinding binding)
        {
            ValueWritten?.Invoke(binding);
        }

        /// <summary>
        ///     Subscribes while <paramref name="anchor" /> remains in the scene tree and unsubscribes automatically when it
        ///     exits (same delegate identity used for removal).
        ///     在 <paramref name="anchor" /> 留在场景树中期间订阅，并在它
        ///     退出时自动取消订阅（移除时使用同一委托标识）。
        /// </summary>
        public static void SubscribeValueWrittenWhileNodeAlive(Node anchor, Action<IModSettingsBinding> listener)
        {
            ArgumentNullException.ThrowIfNull(anchor);
            ArgumentNullException.ThrowIfNull(listener);

            ValueWritten += Wrapped;

            anchor.Connect(Node.SignalName.TreeExiting, Callable.From(() => ValueWritten -= Wrapped),
                (uint)GodotObject.ConnectFlags.OneShot);
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
