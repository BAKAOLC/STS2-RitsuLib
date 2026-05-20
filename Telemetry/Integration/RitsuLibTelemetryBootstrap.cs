using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry.Diagnostics;
using STS2RitsuLib.Telemetry.Integration;
using STS2RitsuLib.Telemetry.RunHistory;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     Bootstraps RitsuLib's built-in telemetry applicant and runtime hooks.
    ///     启动 RitsuLib 内置 telemetry 申请方和运行时钩子。
    /// </summary>
    internal static class RitsuLibTelemetryBootstrap
    {
        private static bool _initialized;
        private static bool _mainMenuInitialized;

        /// <summary>
        ///     Hooks telemetry runtime callbacks; user-facing applicant registration is deferred until the first main menu.
        ///     挂接 telemetry 运行时回调；面向用户的申请方注册延后到首次主菜单。
        /// </summary>
        internal static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            DiagnosticsTelemetryCollector.InitializeGlobalExceptionHandlers();
            RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(RunHistoryTelemetryCollector.CaptureEndedRun);
            RitsuLibFramework.SubscribeLifecycleOnce<MainMenuReadyEvent>(_ => InitializeMainMenuTelemetry());
        }

        private static void InitializeMainMenuTelemetry()
        {
            if (_mainMenuInitialized)
                return;

            _mainMenuInitialized = true;
            RegisterRitsuLibApplicant();
            TelemetrySettingsPages.EnsureRootPage();
            TelemetryRuntime.CaptureStartupSnapshot();
            TelemetryConsentPromptCoordinator.TryRunFirstMainMenuFlow();
        }

        private static void RegisterRitsuLibApplicant()
        {
            TelemetryRegistry.RegisterApplicant(new()
            {
                ApplicantId = Const.ModId,
                OwnerModId = Const.ModId,
                DisplayName = "RitsuLib",
                Adapter = RitsuLibTelemetryConfiguration.CreateAdapter(),
                Requests =
                [
                    TelemetryRequest.BasicUsage(
                        T(
                            "ritsulib.telemetry.request.basicUsage.description",
                            "Session start, framework/game versions, build channel, platform, language, and anonymous install id.")),
                    TelemetryRequest.ModInventory(T(
                        "ritsulib.telemetry.request.modInventory.description",
                        "Registered mod inventory, load states, versions, and gameplay flags for compatibility analysis.")),
                    TelemetryRequest.Diagnostics(T(
                        "ritsulib.telemetry.request.diagnostics.description",
                        "Exception reports and framework runtime diagnostics.")),
                ],
            });
        }

        private static ModSettingsText T(string key, string fallback)
        {
            return ModSettingsLocalization.Text(key, fallback);
        }
    }
}
