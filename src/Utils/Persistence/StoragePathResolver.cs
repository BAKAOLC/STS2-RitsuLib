using STS2RitsuLib.Utils.Persistence.Context;

namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     <para xml:lang="en">Resolves local user-storage paths for each <see cref="SaveScope" />.</para>
    ///     <para xml:lang="zh-CN">根据 <see cref="SaveScope" /> 解析本地用户存储路径。</para>
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
