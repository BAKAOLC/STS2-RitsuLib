using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Diagnostics
{
    internal static class RegistrationFreezeDiagnostics
    {
        private static readonly Lock Sync = new();
        private static readonly List<RegistrationFailure> Failures = [];
        private static bool _reportedFailures;

        internal static void RecordFailure(string system, string? modId, string description, Exception exception)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(system);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentNullException.ThrowIfNull(exception);

            lock (Sync)
            {
                Failures.Add(new(system, modId, description, exception.GetType().Name, exception.Message));
            }
        }

        internal static void WarnRecordedFailures(string reason)
        {
            RegistrationFailure[] snapshot;
            lock (Sync)
            {
                if (_reportedFailures)
                    return;

                _reportedFailures = true;
                snapshot = [.. Failures];
            }

            foreach (var failure in snapshot)
                RitsuLibFramework.Logger.Warn(
                    $"[{failure.System}] Registration failed before final freeze ({reason})" +
                    $"{FormatMod(failure.ModId)}: {failure.Description} ({failure.ExceptionType}: {failure.Message})");
        }

        internal static void WarnMissingModelType(string system, string? modId, string description, Type modelType,
            Type expectedBaseType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(system);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentNullException.ThrowIfNull(modelType);
            ArgumentNullException.ThrowIfNull(expectedBaseType);

            if (TryResolveModelType(modelType, expectedBaseType, out var note))
                return;

            RitsuLibFramework.Logger.Warn(
                $"[{system}] Registered reference was not resolved in ModelDb after final freeze" +
                $"{FormatMod(modId)}: {description}; type={modelType.FullName}; {note}");
        }

        internal static void WarnMissingModelId(string system, string? modId, string description, ModelId modelId,
            Type expectedBaseType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(system);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentNullException.ThrowIfNull(expectedBaseType);

            if (TryResolveModelId(modelId, expectedBaseType, out var note))
                return;

            RitsuLibFramework.Logger.Warn(
                $"[{system}] Registered ModelDb id reference was not resolved after final freeze" +
                $"{FormatMod(modId)}: {description}; id={modelId}; {note}");
        }

        private static bool TryResolveModelType(Type modelType, Type expectedBaseType, out string note)
        {
            ModelId id;
            try
            {
                id = ModelDb.GetId(modelType);
            }
            catch (Exception ex)
            {
                note = $"ModelDb.GetId failed: {ex.GetType().Name}: {ex.Message}";
                return false;
            }

            if (!TryResolveModelId(id, expectedBaseType, out note, out var resolved))
                return false;

            if (resolved == null || resolved.GetType() == modelType)
                return true;

            note = $"id resolves to '{resolved.GetType().FullName}', not the registered type";
            return false;
        }

        private static bool TryResolveModelId(ModelId modelId, Type expectedBaseType, out string note)
        {
            return TryResolveModelId(modelId, expectedBaseType, out note, out _);
        }

        private static bool TryResolveModelId(ModelId modelId, Type expectedBaseType, out string note,
            out AbstractModel? resolved)
        {
            try
            {
                resolved = ModelDb.GetByIdOrNull<AbstractModel>(modelId);
            }
            catch (Exception ex)
            {
                resolved = null;
                note = $"ModelDb lookup failed: {ex.GetType().Name}: {ex.Message}";
                return false;
            }

            if (resolved == null)
            {
                note = "ModelDb returned no model for this id";
                return false;
            }

            if (expectedBaseType.IsInstanceOfType(resolved))
            {
                note = string.Empty;
                return true;
            }

            note = $"id resolves to '{resolved.GetType().FullName}', not a '{expectedBaseType.FullName}'";
            return false;
        }

        private static string FormatMod(string? modId)
        {
            return string.IsNullOrWhiteSpace(modId) ? "" : $" (mod '{modId}')";
        }

        private sealed record RegistrationFailure(
            string System,
            string? ModId,
            string Description,
            string ExceptionType,
            string Message);
    }
}
