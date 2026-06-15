using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Godot;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Telemetry.RunHistory;
using STS2RitsuLib.Utils;
using Environment = System.Environment;

namespace STS2RitsuLib.Telemetry
{
    internal static class TelemetryEnvelopeFactory
    {
        internal const string BasePayloadOverrideKey = "__ritsulib_base_payload";
        private static readonly string SessionId = Guid.NewGuid().ToString("N");

        internal static TelemetryEnvelope Create(
            TelemetryApplicant applicant,
            TelemetryRequest request,
            string eventName,
            JsonNode? applicantPayload,
            IReadOnlyDictionary<string, object?>? properties)
        {
            var mergedProperties = properties == null
                ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                : new(properties, StringComparer.OrdinalIgnoreCase);
            AddCommonProperties(mergedProperties, applicant);

            var payload = BuildPayload(applicant, request, eventName, applicantPayload);
            return new()
            {
                ApplicantId = applicant.ApplicantId,
                EventName = eventName,
                RequestId = request.RequestId,
                Category = request.Category,
                Properties = mergedProperties,
                Payload = payload,
            };
        }

        private static void AddCommonProperties(Dictionary<string, object?> properties, TelemetryApplicant applicant)
        {
            foreach (var kvp in BuildCommonProperties(applicant))
                properties[kvp.Key] = kvp.Value;
        }

        private static Dictionary<string, object?> BuildCommonProperties(TelemetryApplicant applicant)
        {
            return new(StringComparer.OrdinalIgnoreCase)
            {
                ["anonymous_install_id"] = TelemetryIdentityStore.AnonymousInstallId,
                ["session_id"] = SessionId,
                ["ritsulib_version"] = Const.Version,
                ["ritsulib_informational_version"] = RitsuLibBuildInfo.InformationalVersion,
                ["ritsulib_build_channel"] = ResolveBuildChannel(),
                ["ritsulib_build_configuration"] = ResolveBuildConfiguration(),
                ["applicant_id"] = applicant.ApplicantId,
                ["owner_mod_id"] = applicant.OwnerModId,
                ["applicant_display_name"] = applicant.ResolveDisplayName(),
                ["game_version"] = ResolveGameVersion(),
                ["game_release_label"] = Sts2HostVersion.ReleaseLabel,
                ["platform"] = Environment.OSVersion.Platform.ToString(),
                ["os_name"] = ResolveGodotOsName(),
                ["os_version"] = Environment.OSVersion.VersionString,
                ["process_architecture"] = RuntimeInformation.ProcessArchitecture.ToString(),
                ["dotnet_runtime"] = RuntimeInformation.FrameworkDescription,
                ["game_language"] = ResolveGameLanguage(),
            };
        }

        private static string? ResolveGameVersion()
        {
            return Sts2HostVersion.Numeric?.ToString() ?? Sts2HostVersion.ReleaseLabel;
        }

        private static string? ResolveGodotOsName()
        {
            try
            {
                return OS.GetName();
            }
            catch
            {
                return null;
            }
        }

        private static string? ResolveGameLanguage()
        {
            try
            {
                return I18N.ResolveCurrentLanguageCode();
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveBuildChannel()
        {
            if (RitsuLibBuildInfo.Metadata.TryGetValue("RitsuLibTelemetryBuildChannel", out var channel) &&
                !string.IsNullOrWhiteSpace(channel))
                return channel;

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (RitsuLibBuildInfo.IsDevBuild)
                return "dev";

#if DEBUG
            return "local_debug";
#else
            return "release";
#endif
        }

        private static string ResolveBuildConfiguration()
        {
            if (RitsuLibBuildInfo.Metadata.TryGetValue("RitsuLibTelemetryBuildConfiguration", out var configuration) &&
                !string.IsNullOrWhiteSpace(configuration))
                return configuration;

#if DEBUG
            return "Debug";
#else
            return "Release";
#endif
        }

        private static JsonObject BuildPayload(
            TelemetryApplicant applicant,
            TelemetryRequest request,
            string eventName,
            JsonNode? applicantPayload)
        {
            var root = new JsonObject();
            var payloadForEnvelope = applicantPayload?.DeepClone();
            var basePayloadOverride = ExtractBasePayloadOverride(payloadForEnvelope);

            var basePayload = basePayloadOverride ?? BuildBasePayload(request);
            if (basePayload.Count > 0)
                root["base_payload"] = basePayload;

            var privateContributions = BuildContributions(
                TelemetryRegistry.ResolvePrivateContributions(applicant, request),
                applicant,
                request,
                eventName,
                basePayload,
                "Private");
            if (privateContributions.Count > 0)
                root["private_contributions"] = privateContributions;

            var shared = BuildContributions(
                TelemetryRegistry.ResolveSharedContributions(applicant, request),
                applicant,
                request,
                eventName,
                basePayload,
                "Shared");
            if (shared.Count > 0)
                root["shared_contributions"] = shared;

            if (payloadForEnvelope is JsonObject { Count: 0 })
                return root;

            if (payloadForEnvelope != null)
                root["applicant_payload"] = payloadForEnvelope;

            return root;
        }

        private static JsonObject? ExtractBasePayloadOverride(JsonNode? payload)
        {
            if (payload is not JsonObject obj)
                return null;

            if (obj[BasePayloadOverrideKey] is not JsonObject overridePayload)
                return null;

            obj.Remove(BasePayloadOverrideKey);
            return overridePayload.DeepClone().AsObject();
        }

        private static JsonObject BuildBasePayload(TelemetryRequest request)
        {
            return request.Category switch
            {
                TelemetryDataCategory.ModInventory => new()
                {
                    ["mods"] = RunHistoryTelemetryCollector.BuildModInventoryList(),
                    ["loaded_mods"] = RunHistoryTelemetryCollector.BuildLoadedModList(),
                },
                _ => new(),
            };
        }

        private static JsonObject BuildContributions(
            IReadOnlyList<ITelemetryContributionProvider> providers,
            TelemetryApplicant applicant,
            TelemetryRequest request,
            string eventName,
            JsonNode? basePayload,
            string logPrefix)
        {
            var root = new JsonObject();
            foreach (var provider in providers)
            {
                JsonNode? node;
                try
                {
                    node = provider.Build(new()
                    {
                        ApplicantId = applicant.ApplicantId,
                        RequestId = request.RequestId,
                        EventName = eventName,
                        BasePayload = basePayload,
                    });
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Telemetry] {logPrefix} contribution '{provider.ContributorModId}/{provider.ContributionId}' failed: {ex.Message}");
                    continue;
                }

                if (node == null)
                    continue;

                if (root[provider.ContributorModId] is not JsonObject byMod)
                {
                    byMod = new();
                    root[provider.ContributorModId] = byMod;
                }

                byMod[provider.ContributionId] = node.DeepClone();
            }

            return root;
        }
    }
}
