using Godot;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace STS2RitsuLib.RuntimeInput
{
    internal sealed partial class RuntimeHotkeyRouterNode : Node
    {
        private readonly List<RuntimeHotkeyRegistration> _registrations = [];

        public override void _EnterTree()
        {
            SetProcessUnhandledKeyInput(true);
        }

        public RuntimeHotkeyHandle Register(RuntimeHotkeyBinding binding, Action callback,
            RuntimeHotkeyOptions? options)
        {
            return Register([binding], callback, options);
        }

        public RuntimeHotkeyHandle Register(IReadOnlyList<RuntimeHotkeyBinding> bindings, Action callback,
            RuntimeHotkeyOptions? options)
        {
            var registration = new RuntimeHotkeyRegistration(bindings, callback, options ?? new RuntimeHotkeyOptions());
            _registrations.Add(registration);
            return new(this, registration);
        }

        public bool TryRebind(RuntimeHotkeyRegistration registration, string bindingText, out string normalizedBinding)
        {
            if (!RuntimeHotkeyParser.TryParse(bindingText, out var binding, out normalizedBinding))
                return false;
            registration.SetBindings([binding]);
            return true;
        }

        public bool TryRebind(RuntimeHotkeyRegistration registration, IEnumerable<string> bindingTexts,
            out IReadOnlyList<string> normalizedBindings)
        {
            var bindings = new List<RuntimeHotkeyBinding>();
            var normalized = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var bindingText in bindingTexts)
            {
                if (!RuntimeHotkeyParser.TryParse(bindingText, out var binding, out var normalizedBinding))
                {
                    normalizedBindings = [];
                    return false;
                }

                if (!seen.Add(normalizedBinding))
                    continue;

                bindings.Add(binding);
                normalized.Add(normalizedBinding);
            }

            if (bindings.Count == 0)
            {
                normalizedBindings = [];
                return false;
            }

            registration.SetBindings(bindings);
            normalizedBindings = normalized;
            return true;
        }

        public bool TryGetRegistrationInfo(RuntimeHotkeyRegistration registration,
            out RuntimeHotkeyRegistrationInfo registrationInfo)
        {
            if (!_registrations.Contains(registration))
            {
                registrationInfo = null!;
                return false;
            }

            registrationInfo = registration.ToRegistrationInfo();
            return true;
        }

        public IReadOnlyList<RuntimeHotkeyRegistrationInfo> GetRegistrationInfos()
        {
            var infos = new RuntimeHotkeyRegistrationInfo[_registrations.Count];
            for (var i = 0; i < _registrations.Count; i++)
                infos[i] = _registrations[i].ToRegistrationInfo();
            return infos;
        }

        public IReadOnlyList<RuntimeHotkeyRegistrationDetails> GetRegistrationDetails()
        {
            var infos = new RuntimeHotkeyRegistrationDetails[_registrations.Count];
            for (var i = 0; i < _registrations.Count; i++)
                infos[i] = _registrations[i].ToRegistrationDetails();
            return infos;
        }

        public RuntimeHotkeyRegistrationInfo? GetRegistrationInfoById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            for (var i = _registrations.Count - 1; i >= 0; i--)
            {
                var registration = _registrations[i];
                if (string.Equals(registration.Options.Id, id, StringComparison.Ordinal))
                    return registration.ToRegistrationInfo();
            }

            return null;
        }

        public void Unregister(RuntimeHotkeyRegistration registration)
        {
            _registrations.Remove(registration);
        }

        public override void _UnhandledKeyInput(InputEvent @event)
        {
            if (@event is not InputEventKey { Pressed: true } keyEvent || keyEvent.IsEcho())
                return;

            for (var i = _registrations.Count - 1; i >= 0; i--)
            {
                var registration = _registrations[i];
                if (!ShouldConsider(registration.Options))
                    continue;
                if (!registration.Matches(keyEvent))
                    continue;

                registration.Callback();
                if (registration.Options.MarkInputHandled)
                    GetViewport()?.SetInputAsHandled();
                return;
            }
        }

        private static bool ShouldConsider(RuntimeHotkeyOptions options)
        {
            if (options.SuppressWhenDevConsoleVisible && NDevConsole.Instance.Visible)
                return false;

            if (!options.SuppressWhenTextInputFocused)
                return true;

            if (Engine.GetMainLoop() is not SceneTree { Root: { } root })
                return true;

            var control = root.GetViewport()?.GuiGetFocusOwner();
            return control == null || !((control is LineEdit lineEdit && lineEdit.IsEditing()) ||
                                        (control is NMegaTextEdit nMegaTextEdit && nMegaTextEdit.IsEditing()));
        }
    }

    internal sealed class RuntimeHotkeyRegistration(
        IReadOnlyList<RuntimeHotkeyBinding> bindings,
        Action callback,
        RuntimeHotkeyOptions options)
    {
        private readonly List<RuntimeHotkeyBinding> _bindings = bindings.ToList();

        public IReadOnlyList<RuntimeHotkeyBinding> Bindings => _bindings;
        public Action Callback { get; } = callback;
        public RuntimeHotkeyOptions Options { get; } = options;

        public void SetBindings(IEnumerable<RuntimeHotkeyBinding> bindings)
        {
            _bindings.Clear();
            _bindings.AddRange(bindings);
        }

        public bool Matches(InputEventKey keyEvent)
        {
            for (var i = 0; i < _bindings.Count; i++)
                if (_bindings[i].Matches(keyEvent))
                    return true;

            return false;
        }

        public RuntimeHotkeyRegistrationDetails ToRegistrationDetails()
        {
            var bindingTexts = _bindings.Select(static binding => binding.CanonicalString).ToArray();
            var modifierStates = _bindings.Select(static binding => binding.IsModifierOnly).ToArray();
            return new(
                bindingTexts,
                modifierStates,
                Options.Id,
                ResolveText(Options.DisplayName),
                ResolveText(Options.Description),
                Options.Purpose,
                ResolveText(Options.Category),
                Options.MarkInputHandled,
                Options.SuppressWhenTextInputFocused,
                Options.SuppressWhenDevConsoleVisible,
                Options.DebugName);
        }

        public RuntimeHotkeyRegistrationInfo ToRegistrationInfo()
        {
            return ToRegistrationDetails().ToRegistrationInfo();
        }

        private static string? ResolveText(RuntimeHotkeyText? text)
        {
            return text?.Resolve();
        }
    }

    internal sealed class RuntimeHotkeyHandle(RuntimeHotkeyRouterNode owner, RuntimeHotkeyRegistration registration)
        : IRuntimeHotkeyHandle
    {
        private RuntimeHotkeyRouterNode? _owner = owner;
        private RuntimeHotkeyRegistration? _registration = registration;

        public string CurrentBinding => CurrentBindings.FirstOrDefault() ?? string.Empty;

        public IReadOnlyList<string> CurrentBindings =>
            _registration?.Bindings.Select(static binding => binding.CanonicalString).ToArray() ?? [];

        public bool IsRegistered => _owner != null && _registration != null;

        public bool TryRebind(string bindingText, out string normalizedBinding)
        {
            if (_owner != null && _registration != null)
                return _owner.TryRebind(_registration, bindingText, out normalizedBinding);
            normalizedBinding = string.Empty;
            return false;
        }

        public bool TryRebind(IEnumerable<string> bindingTexts, out IReadOnlyList<string> normalizedBindings)
        {
            if (_owner != null && _registration != null)
                return _owner.TryRebind(_registration, bindingTexts, out normalizedBindings);
            normalizedBindings = [];
            return false;
        }

        public bool TryGetRegistrationInfo(out RuntimeHotkeyRegistrationInfo registrationInfo)
        {
            if (_owner != null && _registration != null)
                return _owner.TryGetRegistrationInfo(_registration, out registrationInfo);
            registrationInfo = null!;
            return false;
        }

        public void Unregister()
        {
            if (_owner == null || _registration == null)
                return;
            _owner.Unregister(_registration);
            _owner = null;
            _registration = null;
        }

        public void Dispose()
        {
            Unregister();
        }
    }
}
