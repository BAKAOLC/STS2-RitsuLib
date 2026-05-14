using STS2RitsuLib.Utils.Persistence.Context;

namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     Resolves local storage targets for different <see cref="SaveScope" /> domains.
    ///     解析 local 存储 targets 用于 different <see cref="SaveScope" /> domains.
    /// </summary>
    internal static class StoragePathResolver
    {
        private const string RunSidecarSegment = "run_sidecar/v1";

        public static string ResolveBasePathUser(string modId, SaveScope scope, StorageContext? context = null)
        {
            context ??= StorageContext.Empty;
            var profileId = ResolveProfileId(context);
            var accountBase = ProfileManager.GetAccountBasePath(modId);

            return scope switch
            {
                SaveScope.Global => accountBase,
                SaveScope.Profile => $"{accountBase}/{ProfileManager.GetProfileDirectory(profileId)}",
                SaveScope.RunSidecar => ResolveRunSidecarBasePathUser(modId, context, profileId),
                _ => accountBase,
            };
        }

        public static string ResolveFilePathUser(string modId, string fileName, SaveScope scope,
            StorageContext? context = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            var basePath = ResolveBasePathUser(modId, scope, context);
            if (scope != SaveScope.RunSidecar)
                return $"{basePath}/{fileName}";

            // RunSidecar is rooted under the framework's profile tree. Each consumer mod gets its own subdirectory
            // to avoid collisions (different mods can reuse the same fileName).
            var safeModId = SanitizePathSegment(modId);
            return $"{basePath}/{safeModId}/{fileName}";
        }

        private static int ResolveProfileId(StorageContext context)
        {
            return context.TryGet(StorageContextKeys.ProfileId, out var pid)
                ? pid
                : ProfileManager.Instance.CurrentProfileId;
        }

        private static string ResolveRunSidecarBasePathUser(string modId, StorageContext context, int profileId)
        {
            if (!context.TryGet(StorageContextKeys.RunFingerprintStem, out var stem) || string.IsNullOrWhiteSpace(stem))
                throw new InvalidOperationException(
                    $"SaveScope.RunSidecar requires StorageContextKeys.RunFingerprintStem. ({modId})");

            var frameworkAccountBase = ProfileManager.GetAccountBasePath();
            var frameworkProfileBase = $"{frameworkAccountBase}/{ProfileManager.GetProfileDirectory(profileId)}";
            return $"{frameworkProfileBase}/{RunSidecarSegment}/{stem.Trim()}";
        }

        private static string SanitizePathSegment(string id)
        {
            var chars = id
                .Select(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-' ? ch : '_')
                .ToArray();
            var s = new string(chars).Trim('_');
            return string.IsNullOrEmpty(s) ? "mod" : s;
        }
    }
}
