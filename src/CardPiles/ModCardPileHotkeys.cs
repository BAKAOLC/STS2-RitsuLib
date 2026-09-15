using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace STS2RitsuLib.CardPiles
{
    internal sealed class ModCardPileHotkeys : IDisposable
    {
        private readonly Control _owner;
        private readonly Dictionary<string, (Action Pressed, Action Released)> _bindings = new(StringComparer.Ordinal);
        private readonly HashSet<string> _pressed = new(StringComparer.Ordinal);
        private readonly Func<bool> _canOpen;
        private readonly Action _open;
        private NHotkeyManager? _manager;
        private bool _disposed;

        internal ModCardPileHotkeys(Control owner, ModCardPileDefinition definition, Func<bool> canOpen, Action open)
        {
            _owner = owner;
            foreach (var action in definition.Hotkeys ?? [])
                _bindings.Add(action, (() => OnPressed(action), () => OnReleased(action)));
            _canOpen = canOpen;
            _open = open;
            owner.VisibilityChanged += Refresh;
            ActiveScreenContext.Instance.Updated += Refresh;
            Refresh();
        }

        internal void Refresh()
        {
            if (_disposed || _bindings.Count == 0)
                return;

            if (!NGame.IsGameFocusedWindow())
                _pressed.Clear();

            var manager = CanActivate() ? NHotkeyManager.Instance : null;
            if (ReferenceEquals(manager, _manager))
                return;

            Unregister();
            _manager = manager;
            if (_manager == null)
                return;

            foreach (var (action, binding) in _bindings)
            {
                _manager.PushHotkeyPressedBinding(action, binding.Pressed);
                _manager.PushHotkeyReleasedBinding(action, binding.Released);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _owner.VisibilityChanged -= Refresh;
            ActiveScreenContext.Instance.Updated -= Refresh;
            Unregister();
        }

        private bool CanActivate()
        {
            return !_disposed && GodotObject.IsInstanceValid(_owner) && _owner.IsInsideTree()
                   && _owner.IsVisibleInTree() && _owner.CanProcess() && _canOpen()
                   && NRun.Instance is { } run
                   && NCapstoneContainer.Instance is { InUse: false }
                   && NModalContainer.Instance?.OpenModal == null
                   && NOverlayStack.Instance is { ScreenCount: 0 }
                   && ActiveScreenContext.Instance.GetCurrentScreen() is Node active
                   && run.IsAncestorOf(active);
        }

        private void OnPressed(string action)
        {
            if (CanActivate() && NGame.IsGameFocusedWindow())
                _pressed.Add(action);
        }

        private void OnReleased(string action)
        {
            if (_pressed.Remove(action) && CanActivate() && NGame.IsGameFocusedWindow())
                _open();
        }

        private void Unregister()
        {
            _pressed.Clear();
            if (_manager != null && GodotObject.IsInstanceValid(_manager))
                foreach (var (action, binding) in _bindings)
                {
                    _manager.RemoveHotkeyPressedBinding(action, binding.Pressed);
                    _manager.RemoveHotkeyReleasedBinding(action, binding.Released);
                }

            _manager = null;
        }
    }
}
