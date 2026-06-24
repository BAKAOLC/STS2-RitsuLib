using STS2RitsuLib.Utils.Persistence.Context;

namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     Resolves local storage targets for different <see cref="SaveScope" /> domains.
    ///     解析 local 存储 targets 用于 different <see cref="SaveScope" /> domains.
    /// </summary>
    internal static class StoragePathResolver
    {
        public static string ResolveBasePathUser(string modId, SaveScope scope, StorageContext? context = null)
        {
            context ??= StorageContext.Empty;
            var profileId = ResolveProfileId(context);
            var accountBase = ProfileManager.GetAccountBasePath(modId);

            return scope switch
            {
                SaveScope.Global => accountBase,
                SaveScope.Profile => $"{accountBase}/{ProfileManager.GetProfileDirectory(profileId)}",
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
    }
}
