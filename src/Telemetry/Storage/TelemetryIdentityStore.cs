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
            _document = result is { Success: true, Data: not null } &&
                        !string.IsNullOrWhiteSpace(result.Data.AnonymousInstallId)
                ? result.Data
                : new();

            FileOperations.WriteJson(TelemetryPaths.IdentityPath, _document, TelemetryJson.Options,
                "TelemetryIdentity");
        }
    }
}
