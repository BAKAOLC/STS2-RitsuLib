using Godot;
using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Telemetry.Integration
{
    /// <summary>
    ///     Registers the single-page telemetry permission manager.
    ///     注册单页 telemetry 授权管理界面。
    /// </summary>
    internal static class TelemetrySettingsPages
    {
        private const string RootPageId = "telemetry";
        private static readonly Lock Sync = new();
        private static bool _rootRegistered;

        /// <summary>
        ///     Ensures the telemetry settings page exists.
        ///     确保 telemetry 设置页已存在。
        /// </summary>
        internal static void EnsureRootPage()
        {
            var shouldRegister = false;
            lock (Sync)
            {
                if (!_rootRegistered)
                {
                    _rootRegistered = true;
                    shouldRegister = true;
                }
            }

            if (shouldRegister)
                RegisterRootPage();
        }

        /// <summary>
        ///     Refreshes the single telemetry page after an applicant is registered.
        ///     在申请方注册后刷新单页 telemetry 界面。
        /// </summary>
        internal static void RegisterApplicantPage(TelemetryApplicant applicant)
        {
            ArgumentNullException.ThrowIfNull(applicant);
            EnsureRootPage();
            RegisterRootPage();
        }

        private static void RegisterRootPage()
        {
            var applicants = TelemetryRegistry.GetApplicants();
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page =>
                {
                    page.AsChildOf(Const.ModId)
                        .WithSortOrder(-240)
                        .WithTitle(T("ritsulib.telemetry.page.title", "Telemetry"))
                        .WithDescription(T("ritsulib.telemetry.page.description", "Manage data sharing permissions."));

                    if (applicants.Count == 0)
                    {
                        page.AddSection("empty", section => section
                            .AddParagraph(
                                "empty",
                                T("ritsulib.telemetry.applicants.empty", "No telemetry applicants are registered.")));
                        return;
                    }

                    foreach (var applicant in applicants)
                    {
                        var applicantId = applicant.ApplicantId;
                        page.AddSection(
                            $"applicant_{BuildSafeId(applicantId)}",
                            section => section
                                .WithTitle(ModSettingsText.DynamicFullRefreshOnly(() =>
                                    BuildApplicantSectionTitle(applicantId)))
                                .WithDescription(ModSettingsText.DynamicFullRefreshOnly(() =>
                                    BuildApplicantSectionDescription(applicantId)))
                                .Collapsible(!string.Equals(
                                    applicantId,
                                    Const.ModId,
                                    StringComparison.OrdinalIgnoreCase))
                                .AddCustom(
                                    "actions",
                                    T("ritsulib.telemetry.actions.label", "Permission"),
                                    host => BuildApplicantActionButtons(applicantId, host))
                                .AddParagraph(
                                    "status",
                                    ModSettingsText.DynamicFullRefreshOnly(() => BuildApplicantStatus(applicantId)))
                                .AddParagraph(
                                    "requests",
                                    ModSettingsText.DynamicFullRefreshOnly(() => BuildApplicantRequests(applicantId))));
                    }
                },
                RootPageId);
        }

        private static Control BuildApplicantActionButtons(string applicantId, IModSettingsUiActionHost host)
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                Alignment = BoxContainer.AlignmentMode.End,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", 10);

            RefreshActionRow();
            return row;

            void RefreshActionRow()
            {
                ClearChildren(row);

                var consent = TelemetryConsentStore.GetApplicantConsent(applicantId).Consent;
                var isGranted = consent == TelemetryConsentState.Granted;
                var consentButton = new ModSettingsTextButton(
                    isGranted
                        ? L("ritsulib.telemetry.revoke.button", "Revoke")
                        : L("ritsulib.telemetry.allow.button", "Allow"),
                    isGranted ? ModSettingsButtonTone.Danger : ModSettingsButtonTone.Accent,
                    () =>
                    {
                        if (isGranted)
                            Revoke(applicantId);
                        else
                            GrantAll(applicantId);
                        RefreshActionRow();
                        host.RequestRefresh();
                    });
                row.AddChild(consentButton);

                if (TelemetryQueue.GetQueuedEventCount(applicantId) <= 0) return;
                var retryButton = new ModSettingsTextButton(
                    L("ritsulib.telemetry.retry.button", "Retry now"),
                    ModSettingsButtonTone.Normal,
                    () =>
                    {
                        TelemetryTaskRunner.Forget(
                            TelemetryQueue.FlushApplicantAsync(applicantId),
                            "flush_applicant_from_settings");
                        RefreshActionRow();
                        host.RequestRefresh();
                    });
                row.AddChild(retryButton);
            }
        }

        private static void ClearChildren(Node node)
        {
            while (node.GetChildCount() > 0)
            {
                var child = node.GetChild(0);
                node.RemoveChild(child);
                child.QueueFree();
            }
        }

        private static string BuildApplicantSectionTitle(string applicantId)
        {
            if (!TelemetryRegistry.TryGetApplicant(applicantId, out var applicant))
                return applicantId;

            return string.Format(
                L("ritsulib.telemetry.applicant.row.label", "{0}"),
                applicant.ResolveDisplayName());
        }

        private static string BuildApplicantSectionDescription(string applicantId)
        {
            if (!TelemetryRegistry.TryGetApplicant(applicantId, out var applicant))
                return L("ritsulib.telemetry.applicant.missing", "Telemetry applicant is no longer registered.");

            var consent = TelemetryConsentStore.GetApplicantConsent(applicantId).Consent;
            return string.Format(
                L("ritsulib.telemetry.applicant.row.description", "{0} · {1} request(s)"),
                ResolveConsent(consent),
                applicant.Requests.Count);
        }

        private static string BuildApplicantStatus(string applicantId)
        {
            if (!TelemetryRegistry.TryGetApplicant(applicantId, out var applicant))
                return L("ritsulib.telemetry.applicant.missing", "Telemetry applicant is no longer registered.");

            var consent = TelemetryConsentStore.GetApplicantConsent(applicantId);
            var granted = consent.GrantedRequests.Count == 0
                ? L("empty.none", "None")
                : string.Join(", ", consent.GrantedRequests.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
            var grantedShared = CountGrantedSharedContributions(consent).ToString();
            return string.Join(
                "\n",
                FormatLine("ritsulib.telemetry.field.consent", "Consent", ResolveConsent(consent.Consent)),
                FormatLine("ritsulib.telemetry.field.endpoint", "Endpoint", applicant.Adapter.EndpointDescription),
                FormatLine("ritsulib.telemetry.field.grantedRequests", "Granted requests", granted),
                FormatLine("ritsulib.telemetry.field.grantedSharedContributions", "Shared sources", grantedShared),
                FormatLine(
                    "ritsulib.telemetry.field.queuedEvents",
                    "Queued events",
                    TelemetryQueue.GetQueuedEventCount(applicantId).ToString()));
        }

        private static string BuildApplicantRequests(string applicantId)
        {
            if (!TelemetryRegistry.TryGetApplicant(applicantId, out var applicant))
                return L("ritsulib.telemetry.applicant.missing", "Telemetry applicant is no longer registered.");

            var requests = string.Join(
                "\n",
                applicant.Requests.Select(request =>
                    string.Format(
                        L("ritsulib.telemetry.request.line", "- {0} ({1}): {2}"),
                        ResolveCategory(request.Category),
                        request.RequestId,
                        request.ResolveDescription())));

            var shared = BuildSharedContributionRequests(applicantId, applicant);
            var combined = string.Join(
                "\n\n",
                new[] { requests, shared }.Where(text => !string.IsNullOrWhiteSpace(text)));

            return string.IsNullOrWhiteSpace(combined)
                ? L("ritsulib.telemetry.requests.empty", "This applicant has no data requests.")
                : combined;
        }

        private static void GrantAll(string applicantId)
        {
            if (!TelemetryRegistry.TryGetApplicant(applicantId, out var applicant))
                return;

            GrantRequestedSharedContributions(applicant);
            TelemetryConsentStore.SetApplicantConsent(
                applicantId,
                TelemetryConsentState.Granted,
                applicant.Requests.Select(r => r.RequestId));
        }

        private static void Revoke(string applicantId)
        {
            TelemetryConsentStore.SetApplicantConsent(applicantId, TelemetryConsentState.Denied);
            TelemetryQueue.ClearApplicant(applicantId);
        }

        private static string BuildSafeId(string value)
        {
            var safe = new string(value
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
                .ToArray());
            return string.IsNullOrWhiteSpace(safe) ? "unknown" : safe;
        }

        private static string FormatLine(string key, string fallbackLabel, string value)
        {
            return string.Format(L("ritsulib.telemetry.field.line", "{0}: {1}"), L(key, fallbackLabel), value);
        }

        private static string ResolveConsent(TelemetryConsentState state)
        {
            return state switch
            {
                TelemetryConsentState.Granted => L("ritsulib.telemetry.consent.granted", "Granted"),
                TelemetryConsentState.Denied => L("ritsulib.telemetry.consent.denied", "Denied"),
                _ => L("ritsulib.telemetry.consent.unknown", "Not decided"),
            };
        }

        private static string ResolveCategory(TelemetryDataCategory category)
        {
            return L($"ritsulib.telemetry.category.{category}", category.ToString());
        }

        private static string BuildSharedContributionRequests(string applicantId, TelemetryApplicant applicant)
        {
            var providers = TelemetryRegistry.GetRequestedSharedContributions(applicant);
            if (providers.Count == 0)
                return "";

            var consent = TelemetryConsentStore.GetApplicantConsent(applicantId);
            var lines = new List<string>
            {
                L("ritsulib.telemetry.shared.heading", "Additional shared data sources:"),
            };
            lines.AddRange(providers.Select(provider =>
                string.Format(
                    L("ritsulib.telemetry.shared.line", "- {0}/{1} ({2}): {3}"),
                    provider.ContributorModId,
                    provider.ContributionId,
                    ResolveCategory(provider.Category),
                    ResolveSharedContributionState(consent, provider))));
            return string.Join("\n", lines);
        }

        private static string ResolveSharedContributionState(
            TelemetryApplicantConsent consent,
            ITelemetryContributionProvider provider)
        {
            return consent.Consent == TelemetryConsentState.Granted &&
                   consent.SharedContributionSources.TryGetValue(provider.ContributorModId, out var ids) &&
                   ids.Contains(provider.ContributionId)
                ? L("ritsulib.telemetry.shared.granted", "Granted")
                : L("ritsulib.telemetry.shared.notGranted", "Not granted");
        }

        private static int CountGrantedSharedContributions(TelemetryApplicantConsent consent)
        {
            return consent.SharedContributionSources.Values.Sum(ids => ids.Count);
        }

        private static void GrantRequestedSharedContributions(TelemetryApplicant applicant)
        {
            foreach (var provider in TelemetryRegistry.GetRequestedSharedContributions(applicant))
                TelemetryConsentStore.SetSharedContributionConsent(
                    applicant.ApplicantId,
                    provider.ContributorModId,
                    provider.ContributionId,
                    true);
        }

        private static ModSettingsText T(string key, string fallback)
        {
            return ModSettingsLocalization.Text(key, fallback);
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }
    }
}
