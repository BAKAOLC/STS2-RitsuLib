using STS2RitsuLib.Utils.Persistence.Context;

namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     Resolves local storage targets for different <see cref="SaveScope" /> domains.
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
                SaveScope.RunSidecar => ResolveRunSidecarBasePathUser(modId, context, accountBase, profileId),
                _ => accountBase,
            };
        }

        public static string ResolveFilePathUser(string modId, string fileName, SaveScope scope,
            StorageContext? context = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            var basePath = ResolveBasePathUser(modId, scope, context);
            return $"{basePath}/{fileName}";
        }

        private static int ResolveProfileId(StorageContext context)
        {
            return context.TryGet(StorageContextKeys.ProfileId, out var pid)
                ? pid
                : ProfileManager.Instance.CurrentProfileId;
        }

        private static string ResolveRunSidecarBasePathUser(string modId, StorageContext context, string accountBase,
            int profileId)
        {
            if (!context.TryGet(StorageContextKeys.RunFingerprintStem, out var stem) || string.IsNullOrWhiteSpace(stem))
                throw new InvalidOperationException(
                    $"SaveScope.RunSidecar requires StorageContextKeys.RunFingerprintStem. ({modId})");

            var profileBase = $"{accountBase}/{ProfileManager.GetProfileDirectory(profileId)}";
            return $"{profileBase}/{RunSidecarSegment}/{stem.Trim()}";
        }
    }
}
