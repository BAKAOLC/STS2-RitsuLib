using System.Globalization;
using System.IO.Hashing;
using System.Text;

namespace STS2RitsuLib.RuntimeInput
{
    /// <summary>
    ///     <para xml:lang="en">Registers optional Steam Input actions for runtime hotkeys.</para>
    ///     <para xml:lang="zh-CN">为运行时热键注册可选的 Steam Input 动作。</para>
    /// </summary>
    public static class RitsuSteamInputActionRegistry
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<string, Registration> Registrations = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, int> ReferenceCounts = new(StringComparer.Ordinal);

        internal static event Action? ActionsChanged;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a Godot input action for exposure as a Steam Input digital action when Steam is available.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个 Godot 输入动作，以便在 Steam 可用时将其公开为 Steam Input 数字动作。
        ///     </para>
        /// </summary>
        public static IDisposable RegisterAction(
            string actionName,
            RuntimeHotkeyText displayName,
            RuntimeHotkeyText? description = null,
            string? registrationId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
            ArgumentNullException.ThrowIfNull(displayName);

            var normalizedActionName = actionName.Trim();
            lock (SyncRoot)
            {
                if (Registrations.TryGetValue(normalizedActionName, out _))
                {
                    ReferenceCounts[normalizedActionName]++;
                    return new RegistrationHandle(normalizedActionName);
                }

                var steamActionId = BuildSteamActionId(normalizedActionName);
                if (Registrations.Values.Any(existing =>
                        string.Equals(existing.SteamActionId, steamActionId, StringComparison.Ordinal)))
                    throw new InvalidOperationException(
                        $"Steam Input action id collision for '{normalizedActionName}'.");

                Registrations[normalizedActionName] = new(
                    normalizedActionName,
                    steamActionId,
                    displayName,
                    description,
                    registrationId);
                ReferenceCounts[normalizedActionName] = 1;
            }

            ActionsChanged?.Invoke();
            return new RegistrationHandle(normalizedActionName);
        }

        internal static IReadOnlyList<RitsuSteamInputActionDescriptor> GetActions()
        {
            lock (SyncRoot)
            {
                return
                [
                    .. Registrations.Values
                        .Select(static registration => registration.ToDescriptor())
                        .OrderBy(static action => action.SteamActionId, StringComparer.Ordinal),
                ];
            }
        }

        private static void Unregister(string actionName)
        {
            bool changed;
            lock (SyncRoot)
            {
                if (!ReferenceCounts.TryGetValue(actionName, out var count))
                    return;

                if (count > 1)
                {
                    ReferenceCounts[actionName] = count - 1;
                    return;
                }

                ReferenceCounts.Remove(actionName);
                Registrations.Remove(actionName);
                changed = true;
            }

            if (changed)
                ActionsChanged?.Invoke();
        }

        private static string BuildSteamActionId(string actionName)
        {
            var builder = new StringBuilder();
            foreach (var ch in actionName)
            {
                if (char.IsAsciiLetterOrDigit(ch))
                {
                    builder.Append(char.ToLowerInvariant(ch));
                    continue;
                }

                builder.Append('_');
            }

            var stem = string.Join('_', builder.ToString()
                .Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            if (string.Equals(stem, actionName, StringComparison.Ordinal))
                return $"ritsu_{stem}";

            if (stem.Length == 0)
                stem = "action";
            if (stem.Length > 32)
                stem = stem[..32].TrimEnd('_');

            var hash = XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(actionName));
            return string.Create(
                CultureInfo.InvariantCulture,
                $"ritsu_{stem}_{hash:x16}");
        }

        private sealed record Registration(
            string InputActionName,
            string SteamActionId,
            RuntimeHotkeyText DisplayName,
            RuntimeHotkeyText? Description,
            string? RegistrationId)
        {
            public RitsuSteamInputActionDescriptor ToDescriptor()
            {
                return new(InputActionName, SteamActionId, DisplayName, Description, RegistrationId);
            }
        }

        private sealed class RegistrationHandle(string actionName) : IDisposable
        {
            private string? _actionName = actionName;

            public void Dispose()
            {
                var actionNameToRelease = Interlocked.Exchange(ref _actionName, null);
                if (actionNameToRelease != null)
                    Unregister(actionNameToRelease);
            }
        }
    }
}
