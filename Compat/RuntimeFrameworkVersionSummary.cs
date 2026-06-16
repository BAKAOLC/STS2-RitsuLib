using System.Reflection;

namespace STS2RitsuLib.Compat
{
    internal static class RuntimeFrameworkVersionSummary
    {
        internal static string BuildRitsuLibDisplayLine()
        {
            var devBuildText = RitsuLibBuildInfo.IsDevBuild
                ? $" [dev build: {RitsuLibBuildInfo.InformationalVersion}]"
                : "";
            return $"RitsuLib: v{Const.Version}{devBuildText} (compat {RitsuLibFramework.GetCompatBranchLabel()})";
        }

        internal static bool TryBuildBaseLibDisplayLine(out string line)
        {
            if (TryGetBaseLibVersionText(out var versionText))
            {
                line = $"BaseLib: {versionText}";
                return true;
            }

            line = "";
            return false;
        }

        internal static string BuildUiText(bool includeMissingBaseLib)
        {
            var lines = BuildDisplayLines(includeMissingBaseLib);
            return string.Join("\n", lines);
        }

        internal static string BuildInlineUiText(bool includeMissingBaseLib)
        {
            var lines = BuildDisplayLines(includeMissingBaseLib);
            return string.Join("  |  ", lines);
        }

        internal static IReadOnlyList<string> BuildDisplayLines(bool includeMissingBaseLib)
        {
            var lines = new List<string> { BuildRitsuLibDisplayLine() };
            if (TryBuildBaseLibDisplayLine(out var baseLibLine))
                lines.Add(baseLibLine);
            else if (includeMissingBaseLib)
                lines.Add("BaseLib: not detected");

            return lines;
        }

        private static bool TryGetBaseLibVersionText(out string versionText)
        {
            if (TryGetBaseLibModInfo(out var info))
            {
                versionText = FormatVersionText(info.Version, info.AssemblyVersion);
                return true;
            }

            if (TryGetBaseLibAssemblyVersion(out var assemblyVersion))
            {
                versionText = string.IsNullOrWhiteSpace(assemblyVersion)
                    ? "detected, version unknown"
                    : $"assembly {assemblyVersion}";
                return true;
            }

            versionText = "";
            return false;
        }

        private static bool TryGetBaseLibModInfo(out RitsuModInfo info)
        {
            info = null!;

            try
            {
                if (RitsuModManager.TryGetModInfo("BaseLib", out var exact) && exact != null)
                {
                    info = exact;
                    return true;
                }

                info = RitsuModManager.GetKnownMods()
                    .Where(IsBaseLibModInfo)
                    .OrderBy(GetModInfoRank)
                    .FirstOrDefault()!;
                return info != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsBaseLibModInfo(RitsuModInfo info)
        {
            return string.Equals(info.Id, "BaseLib", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(info.Id, ExternalFrameworkIds.BaseLib, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(info.AssemblyName, "BaseLib", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetModInfoRank(RitsuModInfo info)
        {
            return info.State switch
            {
                RitsuModLoadState.Loaded => 0,
                RitsuModLoadState.Pending => 1,
                RitsuModLoadState.AddedAtRuntime => 2,
                RitsuModLoadState.Failed => 3,
                RitsuModLoadState.Disabled => 4,
                RitsuModLoadState.DisabledDuplicate => 5,
                _ => 6,
            };
        }

        private static bool TryGetBaseLibAssemblyVersion(out string? assemblyVersion)
        {
            assemblyVersion = null;

            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(static item =>
                        string.Equals(item.GetName().Name, "BaseLib", StringComparison.OrdinalIgnoreCase));
                if (assembly == null && !ExternalFrameworkRegistry.IsFrameworkPresent(ExternalFrameworkIds.BaseLib))
                    return false;

                assembly ??= AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(static item =>
                        item.GetType("BaseLib.BaseLibMain", false) != null);
                assemblyVersion = assembly == null ? null : ResolveAssemblyVersion(assembly);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string FormatVersionText(string? manifestVersion, string? assemblyVersion)
        {
            if (!string.IsNullOrWhiteSpace(manifestVersion))
                return manifestVersion.Trim();

            return string.IsNullOrWhiteSpace(assemblyVersion)
                ? "detected, version unknown"
                : $"assembly {assemblyVersion.Trim()}";
        }

        private static string? ResolveAssemblyVersion(Assembly assembly)
        {
            var informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informationalVersion))
                return informationalVersion.Trim();

            return assembly.GetName().Version?.ToString();
        }
    }
}
