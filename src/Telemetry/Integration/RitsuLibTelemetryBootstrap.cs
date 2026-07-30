using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry.Diagnostics;
using STS2RitsuLib.Telemetry.Integration;
using STS2RitsuLib.Telemetry.RunHistory;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Bootstraps RitsuLib's built-in telemetry applicant and runtime hooks.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         引导初始化 RitsuLib 内置的遥测申请方和运行时钩子。
    ///     </para>
    /// </summary>
    internal static class RitsuLibTelemetryBootstrap
    {
        private static bool _initialized;
        private static bool _mainMenuInitialized;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attaches telemetry runtime callbacks. Registration of the user-visible applicant is deferred until
        ///         the first main menu.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         挂接遥测运行时回调。向用户显示的申请方会延迟到首次进入主菜单时注册。
        ///     </para>
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
            RunMainMenuStep("register_applicant", RegisterRitsuLibApplicant);
            RunMainMenuStep("ensure_settings_page", TelemetrySettingsPages.EnsureRootPage);
            RunMainMenuStep("capture_startup_snapshot", TelemetryRuntime.CaptureStartupSnapshot);
            RunMainMenuStep("first_main_menu_flow", TelemetryConsentPromptCoordinator.TryRunFirstMainMenuFlow);
        }

        private static void RunMainMenuStep(string operation, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Telemetry] Main menu step '{operation}' failed: {ex.Message}");
                DiagnosticsTelemetryCollector.CaptureExceptionForAuthorizedApplicants(
                    ex,
                    $"telemetry_{operation}");
            }
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
                            "Session start time, framework and game versions, build channel, platform, language, and anonymous install ID.")),
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
