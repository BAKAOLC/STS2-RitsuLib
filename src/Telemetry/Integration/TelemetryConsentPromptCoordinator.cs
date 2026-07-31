using Godot;
using MegaCrit.Sts2.Core.Nodes;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Toast;

namespace STS2RitsuLib.Telemetry.Integration
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Shows the telemetry consent notification on the first main menu and starts delivery of authorized
    ///         startup data.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         首次进入主菜单时显示遥测授权通知，并开始投递已获授权的启动数据。
    ///     </para>
    /// </summary>
    internal static class TelemetryConsentPromptCoordinator
    {
        private static bool _handledFirstMainMenu;

        /// <summary>
        ///     <para xml:lang="en">Runs the first-main-menu flow at most once per process.</para>
        ///     <para xml:lang="zh-CN">每个进程最多运行一次首次主菜单流程。</para>
        /// </summary>
        internal static void TryRunFirstMainMenuFlow()
        {
            if (_handledFirstMainMenu)
                return;

            _handledFirstMainMenu = true;
            TelemetryRuntime.RefreshStartupModInventorySnapshot(nameof(MainMenuReadyEvent));
            TelemetryRuntime.ReplayStartupSnapshotToAuthorizedApplicants();

            var pending = TelemetryRegistry.GetApplicants()
                .Where(applicant => TelemetryConsentStore.GetApplicantConsent(applicant.ApplicantId).Consent ==
                                    TelemetryConsentState.Unknown)
                .ToArray();

            if (pending.Length == 0)
            {
                RitsuLibFramework.Logger.Debug(
                    "[Telemetry] No pending first-run consent prompt; flushing authorized startup telemetry.");
                TelemetryTaskRunner.Forget(TelemetryQueue.FlushAllAsync(), "flush_all_after_startup");
                return;
            }

            var host = ResolveHostNode();
            if (host == null)
            {
                RitsuLibFramework.Logger.Warn(
                    "[Telemetry] Cannot show consent prompt: no active Godot scene tree host was found.");
                return;
            }

            RitsuLibFramework.Logger.Debug(
                $"[Telemetry] Showing persistent consent toast for {pending.Length} applicant(s).");
            ShowPendingConsentToast();
        }

        private static void ShowPendingConsentToast()
        {
            var pending = ResolvePendingApplicants();
            if (pending.Length == 0)
                return;

            RitsuToastService.Show(new(
                string.Format(
                    L("ritsulib.telemetry.toast.body", "{0} telemetry applicant(s) need review."),
                    pending.Length),
                L("ritsulib.telemetry.toast.title", "Telemetry permission"),
                null,
                RitsuToastLevel.Warning,
                null,
                ShowPendingConsentPrompt)
            {
                IsPersistent = true,
            });
        }

        private static void ShowPendingConsentPrompt()
        {
            var pending = ResolvePendingApplicants();
            if (pending.Length == 0)
                return;

            var applicant = pending[0];
            var host = ResolveHostNode();
            if (host == null)
            {
                RitsuLibFramework.Logger.Warn(
                    "[Telemetry] Cannot show consent prompt: no active Godot scene tree host was found.");
                return;
            }

            RitsuLibFramework.Logger.Debug(
                $"[Telemetry] Showing consent prompt for applicant '{applicant.ApplicantId}'.");
            ModSettingsUiFactory.ShowStyledConfirm(
                host,
                L("ritsulib.telemetry.prompt.title", "Telemetry consent"),
                BuildPromptBody(applicant),
                L("ritsulib.telemetry.prompt.cancel", "Reject this applicant"),
                L("ritsulib.telemetry.prompt.confirm", "Allow this applicant"),
                false,
                () => GrantApplicant(applicant),
                true,
                () => DenyApplicant(applicant),
                ShowPendingConsentToast,
                false,
                true);
        }

        private static TelemetryApplicant[] ResolvePendingApplicants()
        {
            return
            [
                .. TelemetryRegistry.GetApplicants()
                    .Where(applicant => TelemetryConsentStore.GetApplicantConsent(applicant.ApplicantId).Consent ==
                                        TelemetryConsentState.Unknown),
            ];
        }

        private static void GrantApplicant(TelemetryApplicant applicant)
        {
            GrantRequestedSharedContributions(applicant);
            TelemetryConsentStore.SetApplicantConsent(
                applicant.ApplicantId,
                TelemetryConsentState.Granted,
                applicant.Requests.Select(r => r.RequestId));

            RitsuLibFramework.Logger.Info(
                $"[Telemetry] User granted first-run telemetry consent for applicant '{applicant.ApplicantId}'.");
            ShowPendingConsentToast();
        }

        private static void DenyApplicant(TelemetryApplicant applicant)
        {
            TelemetryConsentStore.SetApplicantConsent(applicant.ApplicantId, TelemetryConsentState.Denied);
            TelemetryQueue.ClearApplicant(applicant.ApplicantId);

            RitsuLibFramework.Logger.Info(
                $"[Telemetry] User rejected first-run telemetry consent for applicant '{applicant.ApplicantId}'.");
            ShowPendingConsentToast();
        }

        private static string BuildPromptBody(TelemetryApplicant applicant)
        {
            var lines = new List<string>
            {
                L(
                    "ritsulib.telemetry.prompt.bodyIntro",
                    "This mod requests permission to send telemetry to its fixed endpoint. Startup information has already been sampled locally and will only be sent after consent."),
                "",
                $"{applicant.ResolveDisplayName()} ({applicant.ApplicantId})",
                string.Format(
                    L("ritsulib.telemetry.prompt.endpoint", "Endpoint: {0}"),
                    applicant.Adapter.EndpointDescription),
            };

            lines.AddRange(applicant.Requests.Select(request =>
                string.Format(L("ritsulib.telemetry.prompt.requestLine", "- {0}: {1}"),
                    ResolveCategory(request.Category),
                    request.ResolveDescription())));

            var sharedContributions = TelemetryRegistry.GetRequestedSharedContributions(applicant);
            if (sharedContributions.Count > 0)
            {
                lines.Add("");
                lines.Add(L("ritsulib.telemetry.prompt.shared.heading", "Additional shared data sources:"));
                lines.AddRange(sharedContributions.Select(provider =>
                    string.Format(
                        L("ritsulib.telemetry.prompt.shared.line", "- {0}/{1} ({2})"),
                        provider.ContributorModId,
                        provider.ContributionId,
                        ResolveCategory(provider.Category))));
            }

            lines.Add("");

            lines.Add(L(
                "ritsulib.telemetry.prompt.footer",
                "You can manage each applicant later from RitsuLib > Telemetry."));
            return string.Join("\n", lines);
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

        private static Node? ResolveHostNode()
        {
            if (NGame.Instance != null)
                return NGame.Instance;

            return Engine.GetMainLoop() is SceneTree tree ? tree.Root : null;
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }

        private static string ResolveCategory(TelemetryDataCategory category)
        {
            return L($"ritsulib.telemetry.category.{category}", category.ToString());
        }
    }
}
