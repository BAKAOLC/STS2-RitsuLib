using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Telemetry
{
    internal static class TelemetryConsentStore
    {
        private static readonly Lock Sync = new();
        private static TelemetryConsentDocument? _document;

        internal static TelemetryConsentDocument Snapshot()
        {
            lock (Sync)
            {
                EnsureLoaded();
                return Normalize(_document!);
            }
        }

        internal static TelemetryApplicantConsent GetApplicantConsent(string applicantId)
        {
            lock (Sync)
            {
                EnsureLoaded();
                return CloneConsent(GetOrCreate(applicantId));
            }
        }

        internal static bool IsRequestGranted(TelemetryApplicant applicant, TelemetryRequest request)
        {
            lock (Sync)
            {
                EnsureLoaded();
                var consent = GetOrCreate(applicant.ApplicantId);
                return consent.Consent == TelemetryConsentState.Granted &&
                       consent.GrantedRequests.Contains(request.RequestId);
            }
        }

        internal static bool IsSharedContributionGranted(
            string applicantId,
            string contributorModId,
            string contributionId)
        {
            lock (Sync)
            {
                EnsureLoaded();
                var consent = GetOrCreate(applicantId);
                return consent.Consent == TelemetryConsentState.Granted &&
                       consent.SharedContributionSources.TryGetValue(contributorModId, out var ids) &&
                       ids.Contains(contributionId);
            }
        }

        public static void SetApplicantConsent(
            string applicantId,
            TelemetryConsentState state,
            IEnumerable<string>? grantedRequests = null)
        {
            lock (Sync)
            {
                EnsureLoaded();
                var consent = GetOrCreate(applicantId);
                consent.Consent = state;
                consent.GrantedRequests = grantedRequests == null
                    ? []
                    : new(grantedRequests, StringComparer.OrdinalIgnoreCase);
                if (state != TelemetryConsentState.Granted)
                    consent.SharedContributionSources.Clear();
                Save();
                RitsuLibFramework.Logger.Info(
                    $"[Telemetry] Applicant consent updated: {applicantId} -> {state} ({consent.GrantedRequests.Count} granted request(s)).");
            }

            if (state != TelemetryConsentState.Granted)
                return;

            TelemetryRuntime.ReplayStartupSnapshotToApplicant(applicantId);
            TelemetryTaskRunner.Forget(
                TelemetryQueue.FlushApplicantAsync(applicantId),
                "flush_applicant_after_consent");
        }

        public static void SetSharedContributionConsent(
            string applicantId,
            string contributorModId,
            string contributionId,
            bool granted)
        {
            lock (Sync)
            {
                EnsureLoaded();
                var consent = GetOrCreate(applicantId);
                if (!consent.SharedContributionSources.TryGetValue(contributorModId, out var ids))
                {
                    ids = new(StringComparer.OrdinalIgnoreCase);
                    consent.SharedContributionSources[contributorModId] = ids;
                }

                if (granted)
                    ids.Add(contributionId);
                else
                    ids.Remove(contributionId);

                if (ids.Count == 0)
                    consent.SharedContributionSources.Remove(contributorModId);

                Save();
                RitsuLibFramework.Logger.Info(
                    $"[Telemetry] Shared contribution consent updated: applicant={applicantId}, source={contributorModId}/{contributionId}, granted={granted}.");
            }
        }

        private static void EnsureLoaded()
        {
            if (_document != null)
                return;

            var result = FileOperations.ReadJson<TelemetryConsentDocument>(
                TelemetryPaths.ConsentPath,
                TelemetryJson.Options,
                "TelemetryConsent");
            _document = Normalize(result is { Success: true, Data: not null } ? result.Data : new());
        }

        private static TelemetryApplicantConsent GetOrCreate(string applicantId)
        {
            if (_document!.Applicants.TryGetValue(applicantId, out var consent)) return consent;
            consent = new();
            _document.Applicants[applicantId] = consent;

            return consent;
        }

        private static void Save()
        {
            FileOperations.WriteJson(TelemetryPaths.ConsentPath, _document, TelemetryJson.Options, "TelemetryConsent");
        }

        private static TelemetryConsentDocument Normalize(TelemetryConsentDocument document)
        {
            var applicants = new Dictionary<string, TelemetryApplicantConsent>(StringComparer.OrdinalIgnoreCase);
            foreach (var (applicantId, consent) in document.Applicants ?? [])
            {
                if (string.IsNullOrWhiteSpace(applicantId) || consent == null)
                    continue;

                var sharedSources =
                    new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var (contributorModId, contributionIds) in consent.SharedContributionSources ?? [])
                {
                    if (string.IsNullOrWhiteSpace(contributorModId))
                        continue;

                    sharedSources[contributorModId] = new(
                        (contributionIds ?? [])
                        .Where(id => !string.IsNullOrWhiteSpace(id)),
                        StringComparer.OrdinalIgnoreCase);
                }

                applicants[applicantId] = new()
                {
                    Consent = Enum.IsDefined(consent.Consent)
                        ? consent.Consent
                        : TelemetryConsentState.Unknown,
                    GrantedRequests = new(
                        (consent.GrantedRequests ?? [])
                        .Where(id => !string.IsNullOrWhiteSpace(id)),
                        StringComparer.OrdinalIgnoreCase),
                    SharedContributionSources = sharedSources,
                };
            }

            return new()
            {
                SchemaVersion = 1,
                Applicants = applicants,
            };
        }

        private static TelemetryApplicantConsent CloneConsent(TelemetryApplicantConsent consent)
        {
            var sharedSources =
                new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (contributorModId, contributionIds) in consent.SharedContributionSources)
                sharedSources[contributorModId] = new(contributionIds, StringComparer.OrdinalIgnoreCase);

            return new()
            {
                Consent = consent.Consent,
                GrantedRequests = new(consent.GrantedRequests, StringComparer.OrdinalIgnoreCase),
                SharedContributionSources = sharedSources,
            };
        }
    }
}
