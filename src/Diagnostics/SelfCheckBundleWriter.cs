using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Content;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Diagnostics
{
    internal static partial class SelfCheckBundleWriter
    {
        private static readonly Regex KeywordId = KeywordIdRegex();
        private static readonly Regex PublicEntry = PublicEntryRegex();

        internal static string? TryResolveOutputDirectory(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                return null;

            var trimmed = rawPath.Trim();
            try
            {
                if (trimmed.StartsWith("user://", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
                    return ProjectSettings.GlobalizePath(trimmed);
                return Path.GetFullPath(trimmed);
            }
            catch
            {
                return null;
            }
        }

        internal static bool TryWriteBundle(string outputDirectory, out string? zipPath, out string? errorMessage)
        {
            zipPath = null;
            errorMessage = null;
            string? bundleDir = null;
            try
            {
                Directory.CreateDirectory(outputDirectory);
                var runId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                bundleDir = Path.Combine(outputDirectory, $"ritsulib_self_check_{runId}");
                Directory.CreateDirectory(bundleDir);

                var reportPath = Path.Combine(bundleDir, "self_check_report.log");
                var dumpPath = Path.Combine(bundleDir, "harmony_patch_dump.log");

                var dumpOk = HarmonyPatchDumpWriter.TryWrite(dumpPath, out var dumpErr);
                var runtime = RitsuLibFramework.CaptureRuntimeSnapshot();
                var artifactResult = CollectSupportArtifacts(bundleDir);
                var keywordDefs = ModKeywordRegistry.GetDefinitionsSnapshot();
                var modelSnapshots = ModContentRegistry.GetRegisteredTypeSnapshots();
                var replacementLayerSnapshots = ModContentRegistry.GetCharacterAssetReplacementLayerSnapshots();
                var replacementResolvedSnapshots =
                    ModContentRegistry.GetCharacterAssetReplacementResolvedPropertySnapshots();
                var charCheck = CheckCharacterAssets();
                var locCheck = CheckLocalization(keywordDefs, modelSnapshots);

                File.WriteAllText(reportPath,
                    BuildReport(runtime, dumpOk, dumpPath, dumpErr, artifactResult, charCheck, locCheck,
                        keywordDefs, modelSnapshots, replacementLayerSnapshots, replacementResolvedSnapshots),
                    new UTF8Encoding(false));

                zipPath = Path.Combine(outputDirectory, $"{Path.GetFileName(bundleDir)}.zip");
                if (File.Exists(zipPath))
                    File.Delete(zipPath);
                ZipFile.CreateFromDirectory(bundleDir, zipPath, CompressionLevel.Optimal, false);
                if (dumpOk) return true;
                errorMessage = $"Harmony dump failed: {dumpErr}";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
            finally
            {
                if (!string.IsNullOrEmpty(bundleDir) && Directory.Exists(bundleDir))
                    try
                    {
                        Directory.Delete(bundleDir, true);
                    }
                    catch
                    {
                        // ignored
                    }
            }
        }

        private static string BuildReport(FrameworkRuntimeSnapshot runtime, bool dumpOk, string dumpPath,
            string? dumpErr,
            ArtifactCollectionResult artifactResult, CheckResult charCheck, CheckResult locCheck,
            IReadOnlyList<ModKeywordDefinition> keywordDefs,
            IReadOnlyList<ModContentRegistry.ModContentRegisteredTypeSnapshot> modelSnapshots,
            IReadOnlyList<ModContentRegistry.CharacterAssetReplacementLayerSnapshot> replacementLayerSnapshots,
            IReadOnlyList<ModContentRegistry.CharacterAssetReplacementResolvedPropertySnapshot>
                replacementResolvedSnapshots)
        {
            var sb = new StringBuilder();
            var perMod = BuildPerModSummary(charCheck, locCheck, keywordDefs, modelSnapshots);
            sb.AppendLine("=== RitsuLib Self Check Report ===");
            sb.AppendLine($"Generated: {DateTime.Now:O}");
            sb.AppendLine($"Version: {Const.Version}");
            sb.AppendLine($"Framework Active: {runtime.IsActive}");
            sb.AppendLine($"Framework Initialized: {runtime.IsInitialized}");
            sb.AppendLine(
                $"Host Version: {Sts2HostVersion.ReleaseLabel ?? Sts2HostVersion.Numeric?.ToString() ?? "unknown"}");
            sb.AppendLine($"Compat Branch: {RitsuLibFramework.GetCompatBranchLabel()}");
            sb.AppendLine($"Profile Services Initialized: {runtime.ProfileServicesInitialized}");
            sb.AppendLine($"Registered Mod Settings: {runtime.HasRegisteredModSettings}");
            sb.AppendLine($"Lifecycle Observers: {runtime.LifecycleObserverCount}");
            sb.AppendLine($"Registered Script Assemblies: {runtime.RegisteredScriptAssemblyCount}");
            sb.AppendLine();
            sb.AppendLine("Framework Patcher Areas:");
            foreach (var area in runtime.PatcherAreas.OrderBy(x => x.AreaName, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine(
                    $"- {area.AreaName}: registered={area.IsRegistered}, applied={area.IsApplied}, patches={area.RegisteredPatchCount}, dynamic={area.RegisteredDynamicPatchCount}, appliedPatches={area.AppliedPatchCount}");
            sb.AppendLine();
            sb.AppendLine("Per Mod Summary:");
            foreach (var mod in perMod.OrderBy(m => m.ModId, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine(
                    $"- {mod.ModId}: registeredModels={mod.RegisteredModelCount}, registeredKeywords={mod.RegisteredKeywordCount}, checks={mod.TotalChecks}, PASS={mod.PassCount}, WARN={mod.WarnCount}, FAIL={mod.FailCount}");
            if (perMod.Count == 0)
                sb.AppendLine("- none");
            sb.AppendLine();
            sb.AppendLine($"Character Asset Runtime Check: FAIL={charCheck.Failures}, WARN={charCheck.Warnings}");
            foreach (var i in charCheck.Issues
                         .OrderBy(x => x.Source, StringComparer.Ordinal)
                         .ThenBy(x => x.Level, StringComparer.Ordinal)
                         .ThenBy(x => x.Reason, StringComparer.Ordinal)
                         .Take(120))
                sb.AppendLine($"- [{i.Level}] [{i.ModId}] {i.Source}: {i.Reason} | diagnosis={i.Diagnosis}");

            if (charCheck.Issues.Count == 0) sb.AppendLine("- none");
            sb.AppendLine();
            sb.AppendLine("Character Asset Replacement Layers:");
            if (replacementLayerSnapshots.Count == 0)
                sb.AppendLine("- none");
            else
                foreach (var layer in replacementLayerSnapshots
                             .OrderBy(x => x.WriteOrder)
                             .ThenBy(x => x.Scope, StringComparer.Ordinal)
                             .ThenBy(x => x.CharacterEntry, StringComparer.OrdinalIgnoreCase)
                             .ThenBy(x => x.ModId, StringComparer.OrdinalIgnoreCase))
                {
                    sb.AppendLine(
                        $"- order={layer.WriteOrder} scope={layer.Scope} target={layer.CharacterEntry} mod={layer.ModId}");
                    foreach (var field in EnumerateCharacterAssetProfilePathFields(layer.Profile))
                        sb.AppendLine($"  - {field.PropertyPath} = {field.Value}");
                }

            sb.AppendLine();
            sb.AppendLine("Character Asset Replacement Effective Properties:");
            if (replacementResolvedSnapshots.Count == 0)
                sb.AppendLine("- none");
            else
                foreach (var group in replacementResolvedSnapshots
                             .GroupBy(x => x.CharacterEntry, StringComparer.OrdinalIgnoreCase)
                             .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"- character={group.Key}");
                    foreach (var item in group
                                 .OrderBy(x => x.PropertyPath, StringComparer.Ordinal)
                                 .ThenBy(x => x.SourceWriteOrder))
                        sb.AppendLine(
                            $"  - {item.PropertyPath} = {item.Value} [source: scope={item.SourceScope}, mod={item.SourceModId}, order={item.SourceWriteOrder}]");
                }

            sb.AppendLine();
            sb.AppendLine($"Localization/Entry Runtime Check: FAIL={locCheck.Failures}, WARN={locCheck.Warnings}");
            foreach (var i in locCheck.Issues
                         .OrderBy(x => x.Source, StringComparer.Ordinal)
                         .ThenBy(x => x.Level, StringComparer.Ordinal)
                         .ThenBy(x => x.Reason, StringComparer.Ordinal)
                         .Take(160))
                sb.AppendLine($"- [{i.Level}] [{i.ModId}] {i.Source}: {i.Reason} | diagnosis={i.Diagnosis}");

            if (locCheck.Issues.Count == 0) sb.AppendLine("- none");
            sb.AppendLine();
            foreach (var line in SelfCheckIlCallGraphAnalyzer.BuildReportLines())
                sb.AppendLine(line);
            sb.AppendLine();
            sb.AppendLine($"Harmony Dump: {(dumpOk ? "PASS" : "FAIL")} {dumpPath} {dumpErr}");
            sb.AppendLine();
            sb.AppendLine("Support Artifacts:");
            sb.AppendLine($"- total={artifactResult.Artifacts.Count}");
            foreach (var group in artifactResult.Artifacts
                         .GroupBy(x => x.Kind, StringComparer.OrdinalIgnoreCase)
                         .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"- {group.Key}: {group.Count()}");
            if (artifactResult.Artifacts.Count == 0)
                sb.AppendLine("- none");
            else
                foreach (var item in artifactResult.Artifacts
                             .OrderBy(x => x.EntryName, StringComparer.OrdinalIgnoreCase)
                             .Take(500))
                    sb.AppendLine(
                        $"- [{item.Kind}] {item.EntryName} size={FormatByteCount(item.SizeBytes)} source={item.SourcePath} {item.Note}");
            foreach (var warning in artifactResult.Warnings)
                sb.AppendLine($"- [WARN] {warning}");
            return sb.ToString();
        }

        private static CheckResult CheckCharacterAssets()
        {
            var counters = new Dictionary<string, MutableModCheckCounter>(StringComparer.OrdinalIgnoreCase);
            var issues = new List<Issue>();
            foreach (var c in ModContentRegistry.GetModCharacters().OrderBy(x => x.Id.Entry, StringComparer.Ordinal))
            {
                var modId = ModContentRegistry.TryGetOwnerModId(c.GetType(), out var ownerModId)
                    ? ownerModId
                    : "<unknown-mod>";
                if (c is not IModCharacterAssetOverrides o) continue;
                Check(modId, c.Id.Entry, "VisualsPath", o.CustomVisualsPath, () => c.VisualsPath, true);
                Check(modId, c.Id.Entry, "EnergyCounterPath", o.CustomEnergyCounterPath, () => c.EnergyCounterPath,
                    true);
                Check(modId, c.Id.Entry, "IconTexturePath", o.CustomIconTexturePath, () => c.IconTexturePath, true);
                Check(modId, c.Id.Entry, "IconOutlineTexturePath", o.CustomIconOutlineTexturePath,
                    () => c.IconOutlineTexturePath,
                    true);
                Check(modId, c.Id.Entry, "CharacterSelectBg", o.CustomCharacterSelectBgPath, () => c.CharacterSelectBg,
                    true);
                Check(modId, c.Id.Entry, "CharacterSelectTransitionPath", o.CustomCharacterSelectTransitionPath,
                    () => c.CharacterSelectTransitionPath, true);
                Check(modId, c.Id.Entry, "TrailPath", o.CustomTrailPath, () => c.TrailPath, true);
                Check(modId, c.Id.Entry, "AttackSfx", o.CustomAttackSfx, () => c.AttackSfx, false);
                Check(modId, c.Id.Entry, "CastSfx", o.CustomCastSfx, () => c.CastSfx, false);
                Check(modId, c.Id.Entry, "DeathSfx", o.CustomDeathSfx, () => c.DeathSfx, false);
            }

            return new(issues, [.. counters.Select(kvp => kvp.Value.ToSnapshot(kvp.Key))]);

            void Check(string modId, string charId, string name, string? overrideValue, Func<string> resolveValue,
                bool requireResource)
            {
                if (string.IsNullOrWhiteSpace(overrideValue)) return;
                var counter = GetCounter(modId);
                counter.Total++;
                string resolved;
                try
                {
                    resolved = resolveValue();
                }
                catch (Exception ex)
                {
                    counter.Fail++;
                    issues.Add(new("FAIL", modId, $"{charId}.{name}", "resolved_value_read_failed",
                        $"Failed to read runtime value: {ex.GetType().Name}"));
                    return;
                }

                var selected = string.Equals(overrideValue, resolved, StringComparison.Ordinal);
                var exists = !requireResource || GodotResourcePath.ResourceExists(overrideValue);
                switch (selected)
                {
                    case true when exists:
                        counter.Pass++;
                        return;
                    case true when !exists:
                        counter.Fail++;
                        issues.Add(new("FAIL", modId, $"{charId}.{name}", "override_selected_but_file_missing",
                            Diagnose("override_selected_but_file_missing")));
                        return;
                    case false when !exists:
                        counter.Warn++;
                        issues.Add(new("WARN", modId, $"{charId}.{name}", "override_file_missing_fallback_to_vanilla",
                            Diagnose("override_file_missing_fallback_to_vanilla")));
                        return;
                    default:
                        counter.Fail++;
                        issues.Add(new("FAIL", modId, $"{charId}.{name}",
                            "override_not_applied_possible_patch_skip_or_overwrite",
                            Diagnose("override_not_applied_possible_patch_skip_or_overwrite")));
                        break;
                }
            }

            MutableModCheckCounter GetCounter(string modId)
            {
                if (counters.TryGetValue(modId, out var existing))
                    return existing;
                var created = new MutableModCheckCounter();
                counters[modId] = created;
                return created;
            }
        }

        private static CheckResult CheckLocalization(
            IReadOnlyList<ModKeywordDefinition> keywordDefinitions,
            IReadOnlyList<ModContentRegistry.ModContentRegisteredTypeSnapshot> modelSnapshots)
        {
            var counters = new Dictionary<string, MutableModCheckCounter>(StringComparer.OrdinalIgnoreCase);
            var issues = new List<Issue>();
            foreach (var d in keywordDefinitions)
            {
                var counter = GetCounter(d.ModId);
                counter.Total++;
                var hasFail = false;
                var hasWarn = false;

                if (!KeywordId.IsMatch(d.Id))
                {
                    hasFail = true;
                    issues.Add(new("FAIL", d.ModId, $"keyword:{d.Id}", "keyword_id_invalid_format",
                        Diagnose("keyword_id_invalid_format")));
                }

                var expectedPrefix = ModContentRegistry.GetQualifiedKeywordId(d.ModId, "probe_suffix");
                expectedPrefix = expectedPrefix[..^"probe_suffix".Length];
                if (!d.Id.StartsWith(expectedPrefix, StringComparison.Ordinal))
                {
                    hasWarn = true;
                    issues.Add(new("WARN", d.ModId, $"keyword:{d.Id}",
                        $"keyword_id_not_owned_pattern_expected_prefix={expectedPrefix}",
                        Diagnose("keyword_id_not_owned_pattern_expected_prefix")));
                }

                if (!LocString.Exists(d.TitleTable, d.TitleKey))
                {
                    hasFail = true;
                    issues.Add(new("FAIL", d.ModId, $"keyword:{d.Id}",
                        $"title_loc_missing table={d.TitleTable} key={d.TitleKey}",
                        Diagnose("title_loc_missing")));
                }

                if (!LocString.Exists(d.DescriptionTable, d.DescriptionKey))
                {
                    hasFail = true;
                    issues.Add(new("FAIL", d.ModId, $"keyword:{d.Id}",
                        $"description_loc_missing table={d.DescriptionTable} key={d.DescriptionKey}",
                        Diagnose("description_loc_missing")));
                }

                ApplyStatus(counter, hasFail, hasWarn);
            }

            foreach (var s in modelSnapshots)
            {
                var counter = GetCounter(s.ModId);
                counter.Total++;
                var hasFail = false;
                var hasWarn = false;
                var typeName = s.ModelType.FullName ?? s.ModelType.Name;
                if (s.ModelDbId == null)
                {
                    hasFail = true;
                    issues.Add(new("FAIL", s.ModId, $"model:{typeName}", "modeldb_id_missing",
                        Diagnose("modeldb_id_missing")));
                    ApplyStatus(counter, hasFail, false);
                    continue;
                }

                var actual = s.ModelDbId.Entry;
                if (!PublicEntry.IsMatch(actual))
                {
                    hasFail = true;
                    issues.Add(new("FAIL", s.ModId, $"model:{typeName}", $"entry_invalid_format entry={actual}",
                        Diagnose("entry_invalid_format")));
                }

                if (!string.IsNullOrWhiteSpace(s.ExpectedPublicEntry) &&
                    !actual.Equals(s.ExpectedPublicEntry, StringComparison.Ordinal))
                {
                    var reason = $"entry_mismatch expected={s.ExpectedPublicEntry} actual={actual}";
                    var diagnosis = AnalyzeEntryMismatch(s, actual);
                    if (IsLegacyCompatibleEntryMismatch(s, actual))
                    {
                        hasWarn = true;
                        issues.Add(new("WARN", s.ModId, $"model:{typeName}", reason, diagnosis));
                    }
                    else
                    {
                        hasFail = true;
                        issues.Add(new("FAIL", s.ModId, $"model:{typeName}", reason, diagnosis));
                    }
                }

                ApplyStatus(counter, hasFail, hasWarn);
            }

            return new(issues, [.. counters.Select(kvp => kvp.Value.ToSnapshot(kvp.Key))]);

            MutableModCheckCounter GetCounter(string modId)
            {
                if (counters.TryGetValue(modId, out var existing))
                    return existing;
                var created = new MutableModCheckCounter();
                counters[modId] = created;
                return created;
            }

            static void ApplyStatus(MutableModCheckCounter counter, bool fail, bool warn)
            {
                if (fail)
                {
                    counter.Fail++;
                    return;
                }

                if (warn)
                {
                    counter.Warn++;
                    return;
                }

                counter.Pass++;
            }
        }

        [GeneratedRegex("^[A-Z0-9_]+$", RegexOptions.Compiled)]
        private static partial Regex KeywordIdRegex();

        [GeneratedRegex("^[A-Z0-9_]+$", RegexOptions.Compiled)]
        private static partial Regex PublicEntryRegex();

        private static string Diagnose(string reasonCode)
        {
            return reasonCode switch
            {
                "override_selected_but_file_missing" =>
                    "Override is selected and the target resource file is missing.",
                "override_file_missing_fallback_to_vanilla" =>
                    "Custom resource file is missing; runtime fallback to vanilla is active.",
                "override_not_applied_possible_patch_skip_or_overwrite" =>
                    "Resource exists but resolved value does not match override.",
                "keyword_id_invalid_format" =>
                    "Keyword ID does not match uppercase underscore format.",
                "keyword_id_not_owned_pattern_expected_prefix" =>
                    "Keyword ID prefix does not match this mod's ownership prefix.",
                "title_loc_missing" => "Title localization key is not found.",
                "description_loc_missing" => "Description localization key is not found.",
                "modeldb_id_missing" =>
                    "Type does not have a mapped ModelDbId.",
                "entry_invalid_format" =>
                    "ModelId.Entry does not match uppercase underscore format.",
                "entry_mismatch" =>
                    "Actual entry is different from expected entry.",
                _ => "Unclassified reason.",
            };
        }

        private static bool IsLegacyCompatibleEntryMismatch(
            ModContentRegistry.ModContentRegisteredTypeSnapshot snapshot,
            string actualEntry)
        {
            return !snapshot.HasExplicitPublicEntryOverride &&
                   !string.IsNullOrWhiteSpace(snapshot.TypeNamePublicEntry) &&
                   actualEntry.Equals(snapshot.TypeNamePublicEntry, StringComparison.Ordinal);
        }

        private static string AnalyzeEntryMismatch(
            ModContentRegistry.ModContentRegisteredTypeSnapshot snapshot,
            string actualEntry)
        {
            var notes = new List<string>(6)
            {
                $"explicitOverride={snapshot.HasExplicitPublicEntryOverride}",
                $"legacyTypeNameEntry={snapshot.TypeNamePublicEntry}",
                $"legacyCompatible={IsLegacyCompatibleEntryMismatch(snapshot, actualEntry)}",
            };
            if (snapshot.ModelDbId == null) return string.Join("; ", notes);
            var resolvedType = TryResolveModelType(snapshot.ModelDbId, out var resolvedOwnerModId);
            notes.Add(resolvedType == null ? "reverseLookupType=<null>" : $"reverseLookupType={resolvedType.FullName}");
            notes.Add($"reverseLookupOwner={resolvedOwnerModId ?? "unknown"}");

            return string.Join("; ", notes);
        }

        private static Type? TryResolveModelType(ModelId modelId, out string? ownerModId)
        {
            ownerModId = null;
            try
            {
                var model = ModelDb.GetById<AbstractModel>(modelId);
                var resolvedType = model.GetType();
                if (ModContentRegistry.TryGetOwnerModId(resolvedType, out var modId))
                    ownerModId = modId;

                return resolvedType;
            }
            catch
            {
                return null;
            }
        }

        private static List<PerModSummary> BuildPerModSummary(
            CheckResult charCheck,
            CheckResult locCheck,
            IReadOnlyList<ModKeywordDefinition> keywordDefs,
            IReadOnlyList<ModContentRegistry.ModContentRegisteredTypeSnapshot> modelSnapshots)
        {
            var summaries = new Dictionary<string, PerModSummary>(StringComparer.OrdinalIgnoreCase);

            foreach (var group in keywordDefs.GroupBy(k => k.ModId, StringComparer.OrdinalIgnoreCase))
            {
                Ensure(group.Key);
                summaries[group.Key] = summaries[group.Key] with { RegisteredKeywordCount = group.Count() };
            }

            foreach (var group in modelSnapshots.GroupBy(m => m.ModId, StringComparer.OrdinalIgnoreCase))
            {
                Ensure(group.Key);
                summaries[group.Key] = summaries[group.Key] with { RegisteredModelCount = group.Count() };
            }

            foreach (var mod in charCheck.Counters.Concat(locCheck.Counters))
            {
                Ensure(mod.ModId);
                var current = summaries[mod.ModId];
                summaries[mod.ModId] = current with
                {
                    TotalChecks = current.TotalChecks + mod.Total,
                    PassCount = current.PassCount + mod.Pass,
                    WarnCount = current.WarnCount + mod.Warn,
                    FailCount = current.FailCount + mod.Fail,
                };
            }

            return [.. summaries.Values];

            void Ensure(string modId)
            {
                if (!summaries.ContainsKey(modId))
                    summaries[modId] = new(modId, 0, 0, 0, 0, 0, 0);
            }
        }

        private static IEnumerable<(string PropertyPath, string Value)> EnumerateCharacterAssetProfilePathFields(
            CharacterAssetProfile profile)
        {
            if (!string.IsNullOrWhiteSpace(profile.Scenes?.VisualsPath))
                yield return ("Scenes.VisualsPath", profile.Scenes.VisualsPath);
            if (!string.IsNullOrWhiteSpace(profile.Scenes?.EnergyCounterPath))
                yield return ("Scenes.EnergyCounterPath", profile.Scenes.EnergyCounterPath);
            if (!string.IsNullOrWhiteSpace(profile.Scenes?.MerchantAnimPath))
                yield return ("Scenes.MerchantAnimPath", profile.Scenes.MerchantAnimPath);
            if (!string.IsNullOrWhiteSpace(profile.Scenes?.RestSiteAnimPath))
                yield return ("Scenes.RestSiteAnimPath", profile.Scenes.RestSiteAnimPath);
            if (!string.IsNullOrWhiteSpace(profile.Ui?.IconTexturePath))
                yield return ("Ui.IconTexturePath", profile.Ui.IconTexturePath);
            if (!string.IsNullOrWhiteSpace(profile.Ui?.IconOutlineTexturePath))
                yield return ("Ui.IconOutlineTexturePath", profile.Ui.IconOutlineTexturePath);
            if (!string.IsNullOrWhiteSpace(profile.Ui?.IconPath))
                yield return ("Ui.IconPath", profile.Ui.IconPath);
            if (!string.IsNullOrWhiteSpace(profile.Ui?.CharacterSelectBgPath))
                yield return ("Ui.CharacterSelectBgPath", profile.Ui.CharacterSelectBgPath);
            if (!string.IsNullOrWhiteSpace(profile.Ui?.CharacterSelectIconPath))
                yield return ("Ui.CharacterSelectIconPath", profile.Ui.CharacterSelectIconPath);
            if (!string.IsNullOrWhiteSpace(profile.Ui?.CharacterSelectLockedIconPath))
                yield return ("Ui.CharacterSelectLockedIconPath", profile.Ui.CharacterSelectLockedIconPath);
            if (!string.IsNullOrWhiteSpace(profile.Ui?.CharacterSelectTransitionPath))
                yield return ("Ui.CharacterSelectTransitionPath", profile.Ui.CharacterSelectTransitionPath);
            if (!string.IsNullOrWhiteSpace(profile.Ui?.MapMarkerPath))
                yield return ("Ui.MapMarkerPath", profile.Ui.MapMarkerPath);
            if (!string.IsNullOrWhiteSpace(profile.Vfx?.TrailPath))
                yield return ("Vfx.TrailPath", profile.Vfx.TrailPath);
            if (!string.IsNullOrWhiteSpace(profile.Spine?.CombatSkeletonDataPath))
                yield return ("Spine.CombatSkeletonDataPath", profile.Spine.CombatSkeletonDataPath);
            if (!string.IsNullOrWhiteSpace(profile.Audio?.CharacterSelectSfx))
                yield return ("Audio.CharacterSelectSfx", profile.Audio.CharacterSelectSfx);
            if (!string.IsNullOrWhiteSpace(profile.Audio?.CharacterTransitionSfx))
                yield return ("Audio.CharacterTransitionSfx", profile.Audio.CharacterTransitionSfx);
            if (!string.IsNullOrWhiteSpace(profile.Audio?.AttackSfx))
                yield return ("Audio.AttackSfx", profile.Audio.AttackSfx);
            if (!string.IsNullOrWhiteSpace(profile.Audio?.CastSfx))
                yield return ("Audio.CastSfx", profile.Audio.CastSfx);
            if (!string.IsNullOrWhiteSpace(profile.Audio?.DeathSfx))
                yield return ("Audio.DeathSfx", profile.Audio.DeathSfx);
            if (!string.IsNullOrWhiteSpace(profile.Multiplayer?.ArmPointingTexturePath))
                yield return ("Multiplayer.ArmPointingTexturePath", profile.Multiplayer.ArmPointingTexturePath);
            if (!string.IsNullOrWhiteSpace(profile.Multiplayer?.ArmRockTexturePath))
                yield return ("Multiplayer.ArmRockTexturePath", profile.Multiplayer.ArmRockTexturePath);
            if (!string.IsNullOrWhiteSpace(profile.Multiplayer?.ArmPaperTexturePath))
                yield return ("Multiplayer.ArmPaperTexturePath", profile.Multiplayer.ArmPaperTexturePath);
            if (!string.IsNullOrWhiteSpace(profile.Multiplayer?.ArmScissorsTexturePath))
                yield return ("Multiplayer.ArmScissorsTexturePath", profile.Multiplayer.ArmScissorsTexturePath);
        }

        private sealed class MutableModCheckCounter
        {
            internal int Fail;
            internal int Pass;
            internal int Total;
            internal int Warn;

            internal ModCheckCounter ToSnapshot(string modId)
            {
                return new(modId, Total, Pass, Warn, Fail);
            }
        }

        private readonly record struct Issue(
            string Level,
            string ModId,
            string Source,
            string Reason,
            string Diagnosis);

        private readonly record struct ModCheckCounter(
            string ModId,
            int Total,
            int Pass,
            int Warn,
            int Fail);

        private readonly record struct PerModSummary(
            string ModId,
            int RegisteredModelCount,
            int RegisteredKeywordCount,
            int TotalChecks,
            int PassCount,
            int WarnCount,
            int FailCount);

        private readonly record struct CheckResult
        {
            internal CheckResult(IReadOnlyList<Issue> issues, IReadOnlyList<ModCheckCounter> counters)
            {
                Issues = issues;
                Counters = counters;
                Failures = issues.Count(i => i.Level == "FAIL");
                Warnings = issues.Count(i => i.Level == "WARN");
            }

            internal int Failures { get; }
            internal int Warnings { get; }
            internal IReadOnlyList<ModCheckCounter> Counters { get; } = [];
            internal IReadOnlyList<Issue> Issues { get; } = [];
        }
    }
}
