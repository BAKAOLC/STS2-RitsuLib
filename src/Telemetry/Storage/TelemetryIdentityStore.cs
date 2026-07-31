using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Telemetry
{
    internal static class TelemetryIdentityStore
    {
        private static readonly Lock Sync = new();
        private static TelemetryIdentityDocument? _document;

        internal static string AnonymousInstallId
        {
            get
            {
                lock (Sync)
                {
                    EnsureLoaded();
                    return _document!.AnonymousInstallId;
                }
            }
        }

        private static void EnsureLoaded()
        {
            if (_document != null)
                return;

            var result = FileOperations.ReadJson<TelemetryIdentityDocument>(
                TelemetryPaths.IdentityPath,
                TelemetryJson.Options,
                "TelemetryIdentity");
            if (result is { Success: true, Data: not null } &&
                Guid.TryParseExact(result.Data.AnonymousInstallId, "N", out var installId))
            {
                _document = new()
                {
                    SchemaVersion = 1,
                    AnonymousInstallId = installId.ToString("N"),
                };
                return;
            }

            var generated = new TelemetryIdentityDocument();
            var write = FileOperations.WriteJson(TelemetryPaths.IdentityPath, generated, TelemetryJson.Options,
                "TelemetryIdentity");
            if (!write.Success)
                throw new InvalidOperationException(
                    $"Failed to persist telemetry identity: {write.ErrorMessage}");

            _document = generated;
        }
    }
}
