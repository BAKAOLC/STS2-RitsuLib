using System.Collections;
using System.Reflection;
using System.Text;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Ui.Toast;

namespace STS2RitsuLib.Diagnostics
{
    internal static partial class RuntimeDetourCompatibilityScanner
    {
        private const string Prefix = "[RuntimeDetourCompat]";

        private static readonly string[] InfrastructureAssemblyNamePrefixes =
        [
            "0Harmony",
            "HarmonyLib",
            "Mono.Cecil",
            "MonoMod.",
            "System.",
            "Microsoft.",
            "netstandard",
        ];

        private static int _scanIssuedForSession;

        internal static void Initialize()
        {
            RitsuLibFramework.SubscribeLifecycleOnce<DeferredInitializationCompletedEvent>(_ =>
                RitsuLibStartupAudit.Measure("runtimeDetourCompatibilityScan", ScanAndWarn));
        }

        private static void ScanAndWarn()
        {
            if (Interlocked.CompareExchange(ref _scanIssuedForSession, 1, 0) != 0)
                return;

            var riskMods = FindRuntimeDetourRiskMods();
            if (riskMods.Count == 0)
                return;

            RitsuLibFramework.Logger.Warn(BuildRiskDependencyWarning(riskMods));

            var bridge = RuntimeDetourReflectionBridge.TryCreate();
            if (bridge == null)
            {
                RitsuLibFramework.Logger.Warn(
                    $"{Prefix} Risky hook dependency detected, but MonoMod.RuntimeDetour query API is not available. RuntimeDetour/Harmony overlap cannot be checked.");
                ShowCompatibilityToast(riskMods, [], true);
                return;
            }

            var conflicts = FindHarmonyRuntimeDetourConflicts(bridge);
            if (conflicts.Count == 0)
            {
                RitsuLibFramework.Logger.Info(
                    $"{Prefix} RuntimeDetour dependency detected, but no RuntimeDetour hook currently overlaps a Harmony-patched method.");
                ShowCompatibilityToast(riskMods, conflicts, false);
                return;
            }

            RitsuLibFramework.Logger.Warn(BuildConflictWarning(conflicts));
            ShowCompatibilityToast(riskMods, conflicts, false);
        }

        private static IReadOnlyList<RuntimeDetourRiskMod> FindRuntimeDetourRiskMods()
        {
            IReadOnlyList<Sts2LoadedModAssemblyEntry> mods;
            try
            {
                mods = Sts2ModManagerCompat.BuildLoadedModAssemblyEntries();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"{Prefix} Failed to enumerate loaded mod assemblies: {ex.Message}");
                return [];
            }

            return
            [
                .. mods
                    .Select(TryBuildRiskMod)
                    .Where(risk => risk != null)
                    .Select(risk => risk!),
            ];
        }

        private static RuntimeDetourRiskMod? TryBuildRiskMod(Sts2LoadedModAssemblyEntry mod)
        {
            var referencingAssemblies = EnumerateModOwnedAssemblies(mod)
                .Where(ReferencesRuntimeDetour)
                .Select(assembly => assembly.GetName().Name ?? assembly.FullName ?? "<unknown>")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return referencingAssemblies.Length == 0
                ? null
                : new RuntimeDetourRiskMod(mod, referencingAssemblies);
        }

        private static IEnumerable<Assembly> EnumerateModOwnedAssemblies(Sts2LoadedModAssemblyEntry mod)
        {
            var primaryAssembly = mod.Assembly;
            var modDirectory = TryGetAssemblyDirectory(primaryAssembly);
            var yielded = new HashSet<Assembly>();

            if (ShouldInspectAssembly(primaryAssembly) && yielded.Add(primaryAssembly))
                yield return primaryAssembly;

            if (string.IsNullOrWhiteSpace(modDirectory))
                yield break;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!ShouldInspectAssembly(assembly) || !yielded.Add(assembly))
                    continue;

                var assemblyDirectory = TryGetAssemblyDirectory(assembly);
                if (string.Equals(assemblyDirectory, modDirectory, StringComparison.OrdinalIgnoreCase))
                    yield return assembly;
            }
        }

        private static bool ShouldInspectAssembly(Assembly assembly)
        {
            var assemblyName = assembly.GetName().Name;
            return !string.IsNullOrWhiteSpace(assemblyName) && !IsInfrastructureAssemblyName(assemblyName);
        }

        private static bool ReferencesRuntimeDetour(Assembly assembly)
        {
            try
            {
                return assembly
                    .GetReferencedAssemblies()
                    .Any(static reference => IsRuntimeDetourAssemblyName(reference.Name));
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"{Prefix} Failed to inspect assembly references for {FormatAssemblyName(assembly)}: {ex.Message}");
                return false;
            }
        }

        private static IReadOnlyList<RuntimeDetourHarmonyConflict> FindHarmonyRuntimeDetourConflicts(
            RuntimeDetourReflectionBridge bridge)
        {
            var conflicts = new List<RuntimeDetourHarmonyConflict>();

            foreach (var group in BuildHarmonyPatchIndex())
            {
                IReadOnlyList<RuntimeDetourHook> hooks;
                try
                {
                    hooks = bridge.GetRuntimeDetourHooks(group.OriginalMethod);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"{Prefix} Failed to query RuntimeDetour hooks for {FormatMethod(group.OriginalMethod)}: {ex.Message}");
                    continue;
                }

                if (hooks.Count > 0)
                    conflicts.Add(new(group, hooks));
            }

            return
            [
                .. conflicts
                    .OrderBy(conflict => FormatMethod(conflict.HarmonyPatchGroup.OriginalMethod),
                        StringComparer.Ordinal),
            ];
        }

        private static IReadOnlyList<HarmonyPatchedMethodGroup> BuildHarmonyPatchIndex()
        {
            var result = new List<HarmonyPatchedMethodGroup>();

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var method in Harmony.GetAllPatchedMethods()
                         .OrderBy(static method => method.DeclaringType?.FullName ?? "", StringComparer.Ordinal)
                         .ThenBy(static method => method.Name, StringComparer.Ordinal))
            {
                var patchInfo = Harmony.GetPatchInfo(method);
                if (patchInfo == null)
                    continue;

                var patches = FormatPatches("Prefix", patchInfo.Prefixes)
                    .Concat(FormatPatches("Postfix", patchInfo.Postfixes))
                    .Concat(FormatPatches("Transpiler", patchInfo.Transpilers))
                    .Concat(FormatPatches("Finalizer", patchInfo.Finalizers))
                    .Order(StringComparer.Ordinal)
                    .ToArray();

                if (patches.Length > 0)
                    result.Add(new(method, patches));
            }

            return result;
        }

        private static IEnumerable<string> FormatPatches(string kind, IEnumerable<Patch> patches)
        {
            return patches
                .OrderBy(static patch => patch.priority)
                .ThenBy(static patch => patch.owner, StringComparer.Ordinal)
                .ThenBy(static patch => FormatMethod(patch.PatchMethod), StringComparer.Ordinal)
                .Select(patch =>
                    $"{kind} owner={patch.owner} priority={patch.priority} method={FormatMethod(patch.PatchMethod)}");
        }

        private static string BuildRiskDependencyWarning(IReadOnlyList<RuntimeDetourRiskMod> riskMods)
        {
            var text = new StringBuilder()
                .AppendLine(
                    $"{Prefix} Risky hook dependency detected: loaded mod assemblies reference MonoMod.RuntimeDetour.")
                .AppendLine(
                    "If a MonoMod.RuntimeDetour hook targets a Harmony-patched method, it redirects the call path and the Harmony patches on that method are expected to be skipped.")
                .AppendLine("Referencing mods:");

            foreach (var riskMod in riskMods.OrderBy(static mod => mod.Mod.Id, StringComparer.OrdinalIgnoreCase))
                text.AppendLine(
                    $"  - {FormatMod(riskMod.Mod)} references: {string.Join(", ", riskMod.ReferencingAssemblies)}");

            return text.ToString().TrimEnd();
        }

        private static string BuildConflictWarning(IReadOnlyList<RuntimeDetourHarmonyConflict> conflicts)
        {
            var text = new StringBuilder()
                .AppendLine($"{Prefix} WARNING: RuntimeDetour hook overlaps Harmony patches.")
                .AppendLine(
                    "These RuntimeDetour hooks redirect methods that already have Harmony patches, so the following Harmony patches are expected to be skipped instead of executing normally:")
                .AppendLine($"Conflicts: {conflicts.Count}");

            foreach (var conflict in conflicts)
            {
                text.AppendLine($"  * {FormatMethod(conflict.HarmonyPatchGroup.OriginalMethod)}")
                    .AppendLine("    Harmony patches:");

                foreach (var patch in conflict.HarmonyPatchGroup.Patches)
                    text.AppendLine($"      - {patch}");

                text.AppendLine("    RuntimeDetour hooks:");
                foreach (var hook in conflict.Hooks)
                    text.AppendLine($"      - {hook.Kind} {hook.Target} config={hook.Config}");
            }

            return text.ToString().TrimEnd();
        }

        private static void ShowCompatibilityToast(
            IReadOnlyList<RuntimeDetourRiskMod> riskMods,
            IReadOnlyList<RuntimeDetourHarmonyConflict> conflicts,
            bool queryUnavailable)
        {
            try
            {
                var title = conflicts.Count > 0
                    ? T("ritsulib.runtimeDetourCompat.toast.conflictTitle", "RuntimeDetour/Harmony conflict")
                    : T("ritsulib.runtimeDetourCompat.toast.riskTitle", "RuntimeDetour hook risk");
                var body = conflicts.Count > 0
                    ? L(
                        "ritsulib.runtimeDetourCompat.toast.conflictBody",
                        "{0} Harmony-patched method(s) are overlapped by RuntimeDetour hooks. Click for details.",
                        conflicts.Count)
                    : queryUnavailable
                        ? T(
                            "ritsulib.runtimeDetourCompat.toast.queryUnavailableBody",
                            "RuntimeDetour dependency detected, but hook overlap could not be checked. Click for details.")
                        : T(
                            "ritsulib.runtimeDetourCompat.toast.noOverlapBody",
                            "RuntimeDetour dependency detected. No Harmony overlap is currently known. Click for details.");

                var capturedRiskMods = riskMods.ToArray();
                var capturedConflicts = conflicts.ToArray();
                RitsuToastService.Show(RitsuToastRequest.Warning(body, title)
                    .Persistent()
                    .WithClick(() => ShowCompatibilityDialog(capturedRiskMods, capturedConflicts, queryUnavailable)));
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"{Prefix} Failed to show compatibility toast: {ex.Message}");
            }
        }

        private static void ShowCompatibilityDialog(
            IReadOnlyList<RuntimeDetourRiskMod> riskMods,
            IReadOnlyList<RuntimeDetourHarmonyConflict> conflicts,
            bool queryUnavailable)
        {
            try
            {
                if (NModalContainer.Instance == null)
                    return;

                NModalContainer.Instance.Clear();
                NModalContainer.Instance.Add(new RuntimeDetourCompatibilityPanel(
                    [.. riskMods],
                    [.. conflicts],
                    queryUnavailable));
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"{Prefix} Failed to show compatibility dialog: {ex.Message}");
            }
        }

        private static string BuildDialogSummary(
            IReadOnlyList<RuntimeDetourRiskMod> riskMods,
            IReadOnlyList<RuntimeDetourHarmonyConflict> conflicts,
            bool queryUnavailable)
        {
            if (conflicts.Count > 0)
                return L(
                    "ritsulib.runtimeDetourCompat.dialog.summary.conflict",
                    "{0} mod(s) reference MonoMod.RuntimeDetour. {1} Harmony-patched method(s) currently overlap RuntimeDetour hooks, so those Harmony patches are expected to be skipped instead of executing normally.",
                    riskMods.Count,
                    conflicts.Count);

            return queryUnavailable
                ? L(
                    "ritsulib.runtimeDetourCompat.dialog.summary.queryUnavailable",
                    "{0} mod(s) reference MonoMod.RuntimeDetour, but the RuntimeDetour runtime query API was not available. Harmony overlap could not be checked.",
                    riskMods.Count)
                : L(
                    "ritsulib.runtimeDetourCompat.dialog.summary.noOverlap",
                    "{0} mod(s) reference MonoMod.RuntimeDetour. No RuntimeDetour hook currently overlaps a Harmony-patched method.",
                    riskMods.Count);
        }

        private static Control BuildProblemModsBody(IReadOnlyList<RuntimeDetourRiskMod> riskMods)
        {
            var report = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            report.AddThemeConstantOverride("separation", 8);

            foreach (var riskMod in riskMods.OrderBy(static mod => mod.Mod.Id, StringComparer.OrdinalIgnoreCase))
            {
                var card = CreateReportCard();
                var body = new VBoxContainer();
                body.AddThemeConstantOverride("separation", 5);
                card.AddChild(body);

                body.AddChild(CreateDialogLabel($"{riskMod.Mod.Name} [{riskMod.Mod.Id}]", 17,
                    RitsuShellTheme.Current.Text.RichTitle, true));
                body.AddChild(CreateDialogLabel(
                    L(
                        "ritsulib.runtimeDetourCompat.dialog.modAssembly",
                        "Assembly: {0}",
                        FormatAssemblyName(riskMod.Mod.Assembly)) +
                    (string.IsNullOrWhiteSpace(riskMod.Mod.Version)
                        ? ""
                        : " | " + L(
                            "ritsulib.runtimeDetourCompat.dialog.modVersion",
                            "Version: {0}",
                            riskMod.Mod.Version)),
                    14,
                    RitsuShellTheme.Current.Text.RichSecondary));
                body.AddChild(CreateDialogLabel(
                    L(
                        "ritsulib.runtimeDetourCompat.dialog.references",
                        "References: {0}",
                        string.Join(", ", riskMod.ReferencingAssemblies)),
                    14,
                    RitsuShellTheme.Current.Text.RichBody));

                report.AddChild(card);
            }

            return report;
        }

        private static Control BuildAffectedPatchesBody(
            IReadOnlyList<RuntimeDetourHarmonyConflict> conflicts,
            bool queryUnavailable)
        {
            var report = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            report.AddThemeConstantOverride("separation", 8);

            if (conflicts.Count == 0)
            {
                report.AddChild(CreateDialogLabel(
                    queryUnavailable
                        ? T(
                            "ritsulib.runtimeDetourCompat.dialog.affectedUnknown",
                            "RuntimeDetour hooks could not be queried, so affected Harmony patches are unknown.")
                        : T(
                            "ritsulib.runtimeDetourCompat.dialog.affectedNone",
                            "No RuntimeDetour hook currently overlaps a Harmony-patched method."),
                    15,
                    RitsuShellTheme.Current.Text.RichSecondary));
                return report;
            }

            foreach (var conflict in conflicts)
            {
                var card = CreateReportCard();
                var body = new VBoxContainer();
                body.AddThemeConstantOverride("separation", 8);
                card.AddChild(body);

                body.AddChild(CreateDialogLabel(FormatMethod(conflict.HarmonyPatchGroup.OriginalMethod), 16,
                    RitsuShellTheme.Current.Text.RichTitle, true));

                body.AddChild(CreateDialogLabel(T(
                        "ritsulib.runtimeDetourCompat.dialog.harmonyPatchesSkipped",
                        "Harmony patches expected to be skipped:"),
                    14,
                    RitsuShellTheme.Current.Text.RichBody, true));
                foreach (var patch in conflict.HarmonyPatchGroup.Patches)
                    body.AddChild(CreateDialogLabel($"- {patch}", 13, RitsuShellTheme.Current.Text.RichSecondary));

                body.AddChild(CreateDialogLabel(T(
                        "ritsulib.runtimeDetourCompat.dialog.runtimeDetourHooks",
                        "RuntimeDetour hooks:"),
                    14,
                    RitsuShellTheme.Current.Text.RichBody, true));
                foreach (var hook in conflict.Hooks)
                    body.AddChild(CreateDialogLabel($"- {hook.Kind} {hook.Target} config={hook.Config}", 13,
                        RitsuShellTheme.Current.Text.RichSecondary));

                report.AddChild(card);
            }

            return report;
        }

        private static Label CreateSectionTitle(string text)
        {
            return CreateDialogLabel(text, 18, RitsuShellTheme.Current.Text.RichTitle, true);
        }

        private static PanelContainer CreateReportCard()
        {
            var card = new PanelContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            card.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListItemCardStyle());
            return card;
        }

        private static VBoxContainer CreateInsetVBox(
            Container parent,
            int left,
            int top,
            int right,
            int bottom,
            int separation)
        {
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", left);
            margin.AddThemeConstantOverride("margin_top", top);
            margin.AddThemeConstantOverride("margin_right", right);
            margin.AddThemeConstantOverride("margin_bottom", bottom);
            parent.AddChild(margin);

            var box = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            box.AddThemeConstantOverride("separation", separation);
            margin.AddChild(box);
            return box;
        }

        private static Label CreateDialogLabel(string text, int fontSize, Color color, bool bold = false)
        {
            var label = new Label
            {
                Text = text,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            label.AddThemeFontOverride("font",
                bold ? RitsuShellTheme.Current.Font.BodyBold : RitsuShellTheme.Current.Font.Body);
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeColorOverride("font_color", color);
            return label;
        }

        private static string T(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }

        private static string L(string key, string fallback, params object?[] args)
        {
            return string.Format(T(key, fallback), args);
        }

        private static string FormatMod(Sts2LoadedModAssemblyEntry mod)
        {
            var version = string.IsNullOrWhiteSpace(mod.Version) ? "" : $", version={mod.Version}";
            return $"{mod.Name} [{mod.Id}] (assembly={FormatAssemblyName(mod.Assembly)}{version})";
        }

        private static string FormatAssemblyName(Assembly assembly)
        {
            try
            {
                return assembly.GetName().Name ?? assembly.FullName ?? "<unknown>";
            }
            catch
            {
                return assembly.FullName ?? "<unknown>";
            }
        }

        private static string FormatMethod(MethodBase method)
        {
            var declaringType = method.DeclaringType?.FullName ?? "<unknown>";
            var parameterList = string.Join(", ", method.GetParameters().Select(static parameter =>
                parameter.ParameterType.FullName ?? parameter.ParameterType.Name));
            return $"{declaringType}.{method.Name}({parameterList})";
        }

        private static string? TryGetAssemblyDirectory(Assembly assembly)
        {
            try
            {
                var location = assembly.Location;
                return string.IsNullOrWhiteSpace(location) ? null : Path.GetDirectoryName(location);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsInfrastructureAssemblyName(string? assemblyName)
        {
            return !string.IsNullOrWhiteSpace(assemblyName) &&
                   InfrastructureAssemblyNamePrefixes.Any(prefix =>
                       assemblyName.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                       assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsRuntimeDetourAssemblyName(string? assemblyName)
        {
            return !string.IsNullOrWhiteSpace(assemblyName) &&
                   (assemblyName.Equals("MonoMod.RuntimeDetour", StringComparison.OrdinalIgnoreCase) ||
                    assemblyName.StartsWith("MonoMod.RuntimeDetour.", StringComparison.OrdinalIgnoreCase));
        }

        private sealed partial class RuntimeDetourCompatibilityPanel : Control, IScreenContext
        {
            private const int ControllerScrollStep = 72;

            private readonly IReadOnlyList<RuntimeDetourHarmonyConflict> _conflicts;
            private readonly bool _queryUnavailable;
            private readonly IReadOnlyList<RuntimeDetourRiskMod> _riskMods;
            private ScrollContainer? _mainScroll;

            internal RuntimeDetourCompatibilityPanel(
                IReadOnlyList<RuntimeDetourRiskMod> riskMods,
                IReadOnlyList<RuntimeDetourHarmonyConflict> conflicts,
                bool queryUnavailable)
            {
                _riskMods = riskMods;
                _conflicts = conflicts;
                _queryUnavailable = queryUnavailable;
                Name = "RitsuRuntimeDetourCompatibilityPanel";
                MouseFilter = MouseFilterEnum.Stop;
            }

            public RuntimeDetourCompatibilityPanel()
            {
                _riskMods = [];
                _conflicts = [];
            }

            public Control? DefaultFocusedControl { get; private set; }

            public override void _Ready()
            {
                SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                SetProcessUnhandledInput(true);
                Build();
                Callable.From(() => DefaultFocusedControl?.GrabFocus()).CallDeferred();
            }

            public override void _UnhandledInput(InputEvent @event)
            {
                if (!@event.IsEcho() &&
                    (@event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack)))
                {
                    Close();
                    GetViewport()?.SetInputAsHandled();
                    return;
                }

                if (!@event.IsEcho() && TryScrollFromInput(@event))
                {
                    GetViewport()?.SetInputAsHandled();
                    return;
                }

                base._UnhandledInput(@event);
            }

            private void Build()
            {
                var viewportMargin = new MarginContainer
                {
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                viewportMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                viewportMargin.AddThemeConstantOverride("margin_left", 32);
                viewportMargin.AddThemeConstantOverride("margin_top", 28);
                viewportMargin.AddThemeConstantOverride("margin_right", 32);
                viewportMargin.AddThemeConstantOverride("margin_bottom", 28);
                AddChild(viewportMargin);

                var panel = new PanelContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    SizeFlagsVertical = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Stop,
                };
                panel.AddThemeStyleboxOverride("panel",
                    RitsuShellPanelStyles.CreateFramedSurface(
                        RitsuShellTheme.Current.Surface.Content,
                        RitsuShellTheme.Current.Metric.Radius.Default));
                viewportMargin.AddChild(panel);

                var margin = new MarginContainer();
                margin.AddThemeConstantOverride("margin_left", 24);
                margin.AddThemeConstantOverride("margin_top", 22);
                margin.AddThemeConstantOverride("margin_right", 24);
                margin.AddThemeConstantOverride("margin_bottom", 20);
                panel.AddChild(margin);

                var root = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    SizeFlagsVertical = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                root.AddThemeConstantOverride("separation", 16);
                margin.AddChild(root);

                root.AddChild(BuildHeader());
                root.AddChild(BuildSections());
                root.AddChild(BuildFooter());
            }

            private Control BuildHeader()
            {
                var row = new HBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                row.AddThemeConstantOverride("separation", 16);

                var title = CreateDialogLabel(
                    _conflicts.Count > 0
                        ? T("ritsulib.runtimeDetourCompat.dialog.conflictTitle",
                            "RuntimeDetour/Harmony conflict")
                        : T("ritsulib.runtimeDetourCompat.dialog.riskTitle", "RuntimeDetour hook risk"),
                    28,
                    RitsuShellTheme.Current.Text.RichTitle,
                    true);
                title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                row.AddChild(title);

                var copy = new ModSettingsTextButton(
                    T("ritsulib.runtimeDetourCompat.dialog.copyReport", "Copy report"),
                    ModSettingsButtonTone.Normal,
                    CopyReportToClipboard)
                {
                    CustomMinimumSize = new(190f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
                };
                row.AddChild(copy);

                var close = new ModSettingsTextButton(
                    T("clipboard.pasteErrorOk", "OK"),
                    ModSettingsButtonTone.Normal,
                    Close)
                {
                    CustomMinimumSize = new(150f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
                };
                row.AddChild(close);
                DefaultFocusedControl = close;
                return row;
            }

            private Control BuildSections()
            {
                var scroll = new ScrollContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    SizeFlagsVertical = SizeFlags.ExpandFill,
                    HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                    MouseFilter = MouseFilterEnum.Stop,
                    FocusMode = FocusModeEnum.None,
                };
                ModSettingsUiControlTheming.ApplySettingsScrollContainerTheme(scroll);
                _mainScroll = scroll;

                var scrollMargin = new MarginContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                scrollMargin.AddThemeConstantOverride("margin_right",
                    ModSettingsUiControlTheming.ResolveSettingsScrollContentRightGutter(scroll));
                scroll.AddChild(scrollMargin);

                var box = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                box.AddThemeConstantOverride("separation", 12);
                scrollMargin.AddChild(box);

                var summary = BuildDialogSummary(_riskMods, _conflicts, _queryUnavailable);
                box.AddChild(new ModSettingsCollapsibleSection(
                    T("ritsulib.runtimeDetourCompat.dialog.summary", "Summary"),
                    "runtime_detour_compat_summary",
                    null,
                    false,
                    [CreateInfoCard(summary, RitsuShellTheme.Current.Text.RichBody)]));

                box.AddChild(new ModSettingsCollapsibleSection(
                    T("ritsulib.runtimeDetourCompat.dialog.problemMods", "Problem mods"),
                    "runtime_detour_compat_problem_mods",
                    null,
                    false,
                    [BuildProblemModsBody(_riskMods)]));

                box.AddChild(new ModSettingsCollapsibleSection(
                    T("ritsulib.runtimeDetourCompat.dialog.affectedPatches", "Affected Harmony patches"),
                    "runtime_detour_compat_affected_patches",
                    null,
                    _conflicts.Count == 0,
                    [BuildAffectedPatchesBody(_conflicts, _queryUnavailable)]));

                return scroll;
            }

            private Control BuildFooter()
            {
                var row = new HBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                row.AddThemeConstantOverride("separation", 10);
                var label = CreateDialogLabel(
                    L("ritsulib.runtimeDetourCompat.dialog.footer",
                        "RuntimeDetour mods: {0}  Overlapped Harmony methods: {1}",
                        _riskMods.Count,
                        _conflicts.Count),
                    14,
                    RitsuShellTheme.Current.Text.LabelSecondary);
                label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                row.AddChild(label);
                return row;
            }

            private bool TryScrollFromInput(InputEvent inputEvent)
            {
                if (_mainScroll == null || !IsInstanceValid(_mainScroll))
                    return false;

                var direction = inputEvent.IsActionPressed("ui_down") || inputEvent.IsActionPressed(MegaInput.down)
                    ? 1
                    : inputEvent.IsActionPressed("ui_up") || inputEvent.IsActionPressed(MegaInput.up)
                        ? -1
                        : 0;
                if (direction == 0)
                    return false;

                var bar = _mainScroll.GetVScrollBar();
                if (!IsInstanceValid(bar))
                    return false;

                bar.Value = Math.Clamp(
                    bar.Value + direction * ControllerScrollStep,
                    bar.MinValue,
                    bar.MaxValue);
                return true;
            }

            private void Close()
            {
                NModalContainer.Instance?.Clear();
            }

            private void CopyReportToClipboard()
            {
                DisplayServer.ClipboardSet(BuildExportReport());
                ModSettingsClipboardAccess.InvalidateCache();
                RitsuToastService.ShowInfo(
                    T("ritsulib.runtimeDetourCompat.toast.reportCopied.body",
                        "RuntimeDetour compatibility report copied to clipboard."),
                    T("ritsulib.runtimeDetourCompat.toast.reportCopied.title",
                        "RuntimeDetour compatibility"));
            }

            private string BuildExportReport()
            {
                var builder = new StringBuilder();
                var title = _conflicts.Count > 0
                    ? T("ritsulib.runtimeDetourCompat.dialog.conflictTitle", "RuntimeDetour/Harmony conflict")
                    : T("ritsulib.runtimeDetourCompat.dialog.riskTitle", "RuntimeDetour hook risk");
                builder.AppendLine(title);
                builder.AppendLine(new('=', title.Length));
                builder.AppendLine();
                builder.AppendLine(BuildDialogSummary(_riskMods, _conflicts, _queryUnavailable));
                builder.AppendLine();
                builder.AppendLine(T("ritsulib.runtimeDetourCompat.dialog.problemMods", "Problem mods"));
                foreach (var riskMod in _riskMods)
                    builder.AppendLine(
                        $"- {FormatMod(riskMod.Mod)} references: {string.Join(", ", riskMod.ReferencingAssemblies)}");

                builder.AppendLine();
                builder.AppendLine(T("ritsulib.runtimeDetourCompat.dialog.affectedPatches",
                    "Affected Harmony patches"));
                if (_conflicts.Count == 0)
                {
                    builder.AppendLine(_queryUnavailable
                        ? T("ritsulib.runtimeDetourCompat.dialog.affectedUnknown",
                            "RuntimeDetour hooks could not be queried, so affected Harmony patches are unknown.")
                        : T("ritsulib.runtimeDetourCompat.dialog.affectedNone",
                            "No RuntimeDetour hook currently overlaps a Harmony-patched method."));
                    return builder.ToString();
                }

                foreach (var conflict in _conflicts)
                {
                    builder.AppendLine($"- {FormatMethod(conflict.HarmonyPatchGroup.OriginalMethod)}");
                    foreach (var patch in conflict.HarmonyPatchGroup.Patches)
                        builder.AppendLine($"  Harmony: {patch}");
                    foreach (var hook in conflict.Hooks)
                        builder.AppendLine($"  RuntimeDetour: {hook.Kind} {hook.Target} config={hook.Config}");
                }

                return builder.ToString();
            }

            private static Control CreateInfoCard(string text, Color color)
            {
                var panel = new PanelContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                panel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateInsetSurfaceStyle());
                var box = CreateInsetVBox(panel, 12, 9, 12, 9, 4);
                box.AddChild(CreateDialogLabel(text, 16, color));
                return panel;
            }
        }

        private sealed class RuntimeDetourReflectionBridge
        {
            private const BindingFlags PublicInstanceFlags = BindingFlags.Instance | BindingFlags.Public;
            private readonly PropertyInfo _detoursProperty;

            private readonly MethodInfo _getDetourInfoMethod;
            private readonly PropertyInfo _ilHooksProperty;

            private RuntimeDetourReflectionBridge(
                MethodInfo getDetourInfoMethod,
                PropertyInfo detoursProperty,
                PropertyInfo ilHooksProperty)
            {
                _getDetourInfoMethod = getDetourInfoMethod;
                _detoursProperty = detoursProperty;
                _ilHooksProperty = ilHooksProperty;
            }

            internal static RuntimeDetourReflectionBridge? TryCreate()
            {
                var managerType = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Select(static assembly => assembly.GetType("MonoMod.RuntimeDetour.DetourManager", false))
                    .FirstOrDefault(static type => type != null);
                if (managerType == null)
                    return null;

                var getDetourInfoMethod = managerType.GetMethod(
                    "GetDetourInfo",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(MethodBase)],
                    null);
                if (getDetourInfoMethod == null)
                    return null;

                var returnType = getDetourInfoMethod.ReturnType;
                var detoursProperty = returnType.GetProperty("Detours", PublicInstanceFlags);
                var ilHooksProperty = returnType.GetProperty("ILHooks", PublicInstanceFlags);
                if (detoursProperty == null || ilHooksProperty == null)
                    return null;

                return new(getDetourInfoMethod, detoursProperty, ilHooksProperty);
            }

            internal IReadOnlyList<RuntimeDetourHook> GetRuntimeDetourHooks(MethodBase originalMethod)
            {
                var detourInfo = _getDetourInfoMethod.Invoke(null, [originalMethod]);
                if (detourInfo == null)
                    return [];

                return
                [
                    .. EnumerateDetourHooks(_detoursProperty.GetValue(detourInfo)),
                    .. EnumerateIlHooks(_ilHooksProperty.GetValue(detourInfo)),
                ];
            }

            private static IEnumerable<RuntimeDetourHook> EnumerateDetourHooks(object? detours)
            {
                return from detour in EnumerateObjects(detours)
                    let target = FormatReflectedMember(detour.GetType().GetProperty("Entry", PublicInstanceFlags)
                        ?.GetValue(detour))
                    let config = FormatDetourConfig(detour.GetType().GetProperty("Config", PublicInstanceFlags)
                        ?.GetValue(detour))
                    select new RuntimeDetourHook("Detour", $"entry={target}", config);
            }

            private static IEnumerable<RuntimeDetourHook> EnumerateIlHooks(object? ilHooks)
            {
                return from ilHook in EnumerateObjects(ilHooks)
                    let target = FormatReflectedMember(ilHook.GetType()
                        .GetProperty("ManipulatorMethod", PublicInstanceFlags)?.GetValue(ilHook))
                    let config = FormatDetourConfig(ilHook.GetType().GetProperty("Config", PublicInstanceFlags)
                        ?.GetValue(ilHook))
                    select new RuntimeDetourHook("ILHook", $"manipulator={target}", config);
            }

            private static IEnumerable<object> EnumerateObjects(object? value)
            {
                if (value is not IEnumerable enumerable)
                    yield break;

                foreach (var item in enumerable)
                    if (item != null)
                        yield return item;
            }

            private static string FormatReflectedMember(object? value)
            {
                return value is MethodBase method ? FormatMethod(method) : value?.ToString() ?? "<unknown>";
            }

            private static string FormatDetourConfig(object? config)
            {
                if (config == null)
                    return "<none>";

                var type = config.GetType();
                return string.Join(
                    ", ",
                    ReadConfigValue(type, config, "Id"),
                    ReadConfigValue(type, config, "Priority"),
                    ReadConfigValue(type, config, "Before"),
                    ReadConfigValue(type, config, "After"));
            }

            private static string ReadConfigValue(Type type, object config, string propertyName)
            {
                var property = type.GetProperty(propertyName, PublicInstanceFlags);
                if (property == null)
                    return $"{propertyName}=<unknown>";

                try
                {
                    var value = property.GetValue(config);
                    return $"{propertyName}={FormatConfigValue(value)}";
                }
                catch
                {
                    return $"{propertyName}=<unreadable>";
                }
            }

            private static string FormatConfigValue(object? value)
            {
                if (value == null)
                    return "<null>";

                return value is IEnumerable enumerable and not string
                    ? "[" + string.Join(", ", enumerable.Cast<object>().Select(static item => item?.ToString())) + "]"
                    : value.ToString() ?? "<unknown>";
            }
        }

        private sealed record RuntimeDetourRiskMod(
            Sts2LoadedModAssemblyEntry Mod,
            IReadOnlyList<string> ReferencingAssemblies);

        private sealed record HarmonyPatchedMethodGroup(
            MethodBase OriginalMethod,
            IReadOnlyList<string> Patches);

        private sealed record RuntimeDetourHook(
            string Kind,
            string Target,
            string Config);

        private sealed record RuntimeDetourHarmonyConflict(
            HarmonyPatchedMethodGroup HarmonyPatchGroup,
            IReadOnlyList<RuntimeDetourHook> Hooks);
    }
}
