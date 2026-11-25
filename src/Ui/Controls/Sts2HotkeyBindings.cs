using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace STS2RitsuLib.Ui.Controls
{
    internal static class Sts2HotkeyBindings
    {
        internal delegate void PushBinding(NHotkeyManager manager, string hotkey, Node owner, Action action,
            long sequence = 0L);

        internal delegate long RemoveBinding(NHotkeyManager manager, string hotkey, Node owner, Action action);

        internal static readonly PushBinding PushPressed = ResolvePush(nameof(NHotkeyManager.PushHotkeyPressedBinding));

        internal static readonly PushBinding PushReleased =
            ResolvePush(nameof(NHotkeyManager.PushHotkeyReleasedBinding));

        internal static readonly RemoveBinding RemovePressed =
            ResolveRemove(nameof(NHotkeyManager.RemoveHotkeyPressedBinding));

        internal static readonly RemoveBinding RemoveReleased =
            ResolveRemove(nameof(NHotkeyManager.RemoveHotkeyReleasedBinding));

        private static PushBinding ResolvePush(string name)
        {
            var type = typeof(NHotkeyManager);
            var owned = type.GetMethod(name, [typeof(string), typeof(Node), typeof(Action), typeof(long)]);
            if (owned != null)
                return owned.CreateDelegate<PushBinding>();

            var sequenced = type.GetMethod(name, [typeof(string), typeof(Action), typeof(long)]);
            if (sequenced != null)
            {
                var bind = sequenced.CreateDelegate<Action<NHotkeyManager, string, Action, long>>();
                return (manager, hotkey, _, action, sequence) => bind(manager, hotkey, action, sequence);
            }

            var legacy = type.GetMethod(name, [typeof(string), typeof(Action)])
                         ?? throw new MissingMethodException(type.FullName, name);
            var push = legacy.CreateDelegate<Action<NHotkeyManager, string, Action>>();
            return (manager, hotkey, _, action, _) => push(manager, hotkey, action);
        }

        private static RemoveBinding ResolveRemove(string name)
        {
            var type = typeof(NHotkeyManager);
            var owned = type.GetMethod(name, [typeof(string), typeof(Node), typeof(Action)]);
            if (owned != null)
                return owned.CreateDelegate<RemoveBinding>();

            var method = type.GetMethod(name, [typeof(string), typeof(Action)])
                         ?? throw new MissingMethodException(type.FullName, name);
            if (method.ReturnType == typeof(long))
            {
                var remove = method.CreateDelegate<Func<NHotkeyManager, string, Action, long>>();
                return (manager, hotkey, _, action) => remove(manager, hotkey, action);
            }

            var legacy = method.CreateDelegate<Action<NHotkeyManager, string, Action>>();
            return (manager, hotkey, _, action) =>
            {
                legacy(manager, hotkey, action);
                return 0L;
            };
        }
    }
}
