namespace STS2RitsuLib.Telemetry
{
    internal static class RitsuLibTelemetryConfiguration
    {
        private const string DefaultIngestEndpoint = "https://ritsulib-telemetry.ritsukage.com/v1/ingest";

        internal static ITelemetryAdapter CreateAdapter()
        {
            return new HttpJsonTelemetryAdapter(DefaultIngestEndpoint);
        }
    }
}
