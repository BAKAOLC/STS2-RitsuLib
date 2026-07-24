using STS2RitsuLib.Telemetry.Diagnostics;
using STS2RitsuLib.Telemetry.Integration;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     Process-wide registry for telemetry applicants and contribution providers.
    ///     进程级 telemetry 申请方和 contribution provider 注册表。
    /// </summary>
    public static class TelemetryRegistry
    {
        private static readonly Lock Sync = new();

        private static readonly Dictionary<string, TelemetryApplicant> Applicants =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ITelemetryContributionProvider> ContributionProviders =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Registers or replaces a telemetry applicant.
        ///     注册或替换一个 telemetry 申请方。
        /// </summary>
        public static void RegisterApplicant(TelemetryApplicant applicant)
        {
            ArgumentNullException.ThrowIfNull(applicant);

            ArgumentException.ThrowIfNullOrWhiteSpace(applicant.ApplicantId);
            ArgumentException.ThrowIfNullOrWhiteSpace(applicant.OwnerModId);
            ArgumentException.ThrowIfNullOrWhiteSpace(applicant.DisplayName);
            ArgumentNullException.ThrowIfNull(applicant.Adapter);

            lock (Sync)
            {
                Applicants[applicant.ApplicantId] = applicant;
            }

            RitsuLibFramework.Logger.Info(
                $"[Telemetry] Registered applicant '{applicant.ApplicantId}' -> {applicant.Adapter.EndpointDescription} ({applicant.Requests.Count} request(s)).");
            try
            {
                TelemetrySettingsPages.RegisterApplicantPage(applicant);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Telemetry] Failed to refresh settings page for applicant '{applicant.ApplicantId}': {ex.Message}");
                DiagnosticsTelemetryCollector.CaptureExceptionForAuthorizedApplicants(
                    ex,
                    "telemetry_settings_page_refresh");
            }
        }

        /// <summary>
        ///     Registers or replaces a telemetry contribution provider.
        ///     注册或替换一个 telemetry contribution provider。
        /// </summary>
        public static void RegisterContributionProvider(ITelemetryContributionProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            ArgumentException.ThrowIfNullOrWhiteSpace(provider.ContributorModId);
            ArgumentException.ThrowIfNullOrWhiteSpace(provider.ContributionId);
            lock (Sync)
            {
                ContributionProviders[BuildContributionKey(provider.ContributorModId, provider.ContributionId)] =
                    provider;
            }

            RitsuLibFramework.Logger.Info(
                $"[Telemetry] Registered contribution '{provider.ContributorModId}/{provider.ContributionId}' ({provider.Category}, {provider.Visibility}).");
        }

        /// <summary>
        ///     Returns a snapshot of registered telemetry applicants.
        ///     返回已注册 telemetry 申请方快照。
        /// </summary>
        public static IReadOnlyList<TelemetryApplicant> GetApplicants()
        {
            lock (Sync)
            {
                return
                [
                    .. Applicants.Values
                        .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => x.ApplicantId, StringComparer.OrdinalIgnoreCase),
                ];
            }
        }

        /// <summary>
        ///     Returns a snapshot of registered contribution providers.
        ///     返回已注册 contribution provider 快照。
        /// </summary>
        public static IReadOnlyList<ITelemetryContributionProvider> GetContributionProviders()
        {
            lock (Sync)
            {
                return
                [
                    .. ContributionProviders.Values
                        .OrderBy(x => x.ContributorModId, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => x.ContributionId, StringComparer.OrdinalIgnoreCase),
                ];
            }
        }

        internal static bool TryGetApplicant(string applicantId, out TelemetryApplicant applicant)
        {
            lock (Sync)
            {
                return Applicants.TryGetValue(applicantId, out applicant!);
            }
        }

        internal static bool TryGetRequest(
            TelemetryApplicant applicant,
            string requestId,
            out TelemetryRequest request)
        {
            request = applicant.Requests.FirstOrDefault(r =>
                string.Equals(r.RequestId, requestId, StringComparison.OrdinalIgnoreCase))!;
            return request != null;
        }

        internal static IReadOnlyList<ITelemetryContributionProvider> ResolveSharedContributions(
            TelemetryApplicant applicant,
            TelemetryRequest request)
        {
            var subscriptions = request.ContributionSubscriptions;
            if (subscriptions.Count == 0)
                return [];

            lock (Sync)
            {
                return
                [
                    .. ContributionProviders.Values
                        .Where(provider =>
                            provider.Visibility == TelemetryContributionVisibility.SharedToAuthorizedSubscribers)
                        .Where(provider => provider.Category == request.Category)
                        .Where(provider => SubscriptionMatches(provider, applicant, subscriptions, false))
                        .Where(provider => TelemetryConsentStore.IsSharedContributionGranted(
                            applicant.ApplicantId,
                            provider.ContributorModId,
                            provider.ContributionId)),
                ];
            }
        }

        internal static IReadOnlyList<ITelemetryContributionProvider> ResolvePrivateContributions(
            TelemetryApplicant applicant,
            TelemetryRequest request)
        {
            var subscriptions = request.ContributionSubscriptions;
            if (subscriptions.Count == 0)
                return [];

            lock (Sync)
            {
                return
                [
                    .. ContributionProviders.Values
                        .Where(provider => provider.Visibility == TelemetryContributionVisibility.PrivateToApplicant)
                        .Where(provider => provider.Category == request.Category)
                        .Where(provider => SubscriptionMatches(provider, applicant, subscriptions, true))
                        .Where(provider => IsOwnedByApplicant(provider, applicant)),
                ];
            }
        }

        internal static IReadOnlyList<ITelemetryContributionProvider> GetRequestedSharedContributions(
            TelemetryApplicant applicant)
        {
            lock (Sync)
            {
                return
                [
                    .. ContributionProviders.Values
                        .Where(provider =>
                            provider.Visibility == TelemetryContributionVisibility.SharedToAuthorizedSubscribers)
                        .Where(provider => applicant.Requests.Any(request =>
                            request.Category == provider.Category &&
                            SubscriptionMatches(provider, applicant, request.ContributionSubscriptions, false)))
                        .OrderBy(provider => provider.ContributorModId, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(provider => provider.ContributionId, StringComparer.OrdinalIgnoreCase),
                ];
            }
        }

        private static bool IsOwnedByApplicant(
            ITelemetryContributionProvider provider,
            TelemetryApplicant applicant)
        {
            return string.Equals(provider.ContributorModId, applicant.OwnerModId, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(provider.ContributorModId, applicant.ApplicantId, StringComparison.OrdinalIgnoreCase);
        }

        private static bool SubscriptionMatches(
            ITelemetryContributionProvider provider,
            TelemetryApplicant applicant,
            IReadOnlyList<string> subscriptions,
            bool allowUnqualifiedOwnedContribution)
        {
            foreach (var subscription in subscriptions)
            {
                var value = subscription.Trim();
                if (string.IsNullOrEmpty(value))
                    continue;

                if (allowUnqualifiedOwnedContribution &&
                    IsOwnedByApplicant(provider, applicant) &&
                    string.Equals(value, provider.ContributionId, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (string.Equals(
                        value,
                        $"{provider.ContributorModId}/{provider.ContributionId}",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        value,
                        $"{provider.ContributorModId}:{provider.ContributionId}",
                        StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string BuildContributionKey(string contributorModId, string contributionId)
        {
            return $"{contributorModId}\n{contributionId}";
        }
    }
}
