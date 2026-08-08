using STS2RitsuLib.Search.Pinyin;

namespace STS2RitsuLib.Search
{
    internal static class RitsuSearchService
    {
        private static readonly Lock SyncRoot = new();
        private static RitsuSearchExpansionRegistration? _pinyinRegistration;
        private static bool _initialized;

        internal static void Initialize()
        {
            lock (SyncRoot)
            {
                if (_initialized)
                    return;
                _pinyinRegistration = RitsuSearchExpansionRegistry.Register(
                    Const.ModId,
                    new PinyinSearchExpansionProvider());
                PinyinSearchDataManager.DataChanged += OnPinyinDataChanged;
                _initialized = true;
            }

            _ = Task.Run(InitializePinyinDataAsync);
        }

        private static async Task InitializePinyinDataAsync()
        {
            try
            {
                await PinyinSearchDataManager.TryLoadCachedAsync().ConfigureAwait(false);
                if (PinyinSearchDataManager.Data == null &&
                    RitsuSearchSettingsStore.IsProviderEnabled(PinyinSearchExpansionProvider.ProviderId, false) &&
                    RitsuSearchSettingsStore.GetAutomaticPinyinDataDownloads())
                    await PinyinSearchOperationRunner
                        .RunAsync(() => PinyinSearchDataManager.DownloadAndInstallAsync(false))
                        .ConfigureAwait(false);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[Search/Pinyin] Background initialization failed: {ex.Message}");
            }
        }

        private static void OnPinyinDataChanged()
        {
            _pinyinRegistration?.Invalidate();
        }
    }
}
