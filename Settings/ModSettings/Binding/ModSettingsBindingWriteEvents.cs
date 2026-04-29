using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Multicast notifications raised after <see cref="IModSettingsValueBinding{TValue}.Write" /> completes on built-in
    ///     binding implementations. Subscribe from UI or tools that mirror settings elsewhere; use
    ///     <see cref="SubscribeValueWrittenWhileNodeAlive" /> so subscriptions drop when a host node leaves the tree.
    /// </summary>
    public static class ModSettingsBindingWriteEvents
    {
        /// <summary>
        ///     Raised synchronously from binding <c>Write</c> bodies after the backing store has been updated.
        /// </summary>
        public static event Action<IModSettingsBinding>? ValueWritten;

        internal static void NotifyValueWritten(IModSettingsBinding binding)
        {
            ValueWritten?.Invoke(binding);
        }

        /// <summary>
        ///     Subscribes while <paramref name="anchor" /> remains in the scene tree and unsubscribes automatically when it
        ///     exits (same delegate identity used for removal).
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
