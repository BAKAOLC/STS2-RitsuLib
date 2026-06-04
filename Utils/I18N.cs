using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Loads merged JSON translation dictionaries from the file system, embedded resources, and PCK paths,
    ///     reacting to game locale changes when possible.
    ///     从文件系统、嵌入资源和 PCK 路径加载并合并 JSON 翻译字典，并在可行时响应游戏语言切换。
    /// </summary>
    public class I18N : IDisposable, IEnumerable<KeyValuePair<string, string>>
    {
        private readonly string[] _fsFolders;
        private readonly string _instanceName;
        private readonly string[] _pckFolders;
        private readonly Assembly _resourceAssembly;
        private readonly string[] _resourceFolders;
        private IReadOnlyList<string>? _availableLanguagesCache;
        private bool _disposed;
        private string? _loadedLanguage;
        private bool _subscribed;
        private Dictionary<string, string> _translations = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Creates an instance, optionally wiring locale change subscription when sources are configured.
        ///     创建实例；当配置了翻译来源时，可自动接入语言切换订阅。
        /// </summary>
        public I18N(string? instanceName = null,
            string[]? fsFolders = null,
            string[]? resourceFolders = null,
            string[]? pckFolders = null,
            Assembly? resourceAssembly = null)
        {
            _instanceName = instanceName ?? "I18N";
            _resourceFolders = resourceFolders?.Where(f => !string.IsNullOrWhiteSpace(f)).ToArray() ?? [];
            _fsFolders = fsFolders?.Where(f => !string.IsNullOrWhiteSpace(f)).ToArray() ?? [];
            _pckFolders = pckFolders?.Where(f => !string.IsNullOrWhiteSpace(f)).ToArray() ?? [];
            _resourceAssembly = resourceAssembly ?? Assembly.GetCallingAssembly();

            if (_resourceFolders.Length == 0 && _fsFolders.Length == 0 && _pckFolders.Length == 0)
                RitsuLibFramework.Logger.Warn($"[{_instanceName}] Initialized with no translation sources");
            else
                Initialize();
        }

        /// <summary>
        ///     Releases subscriptions and clears loaded translations.
        ///     释放订阅并清空已经加载的翻译。
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            TryUnsubscribe();
            _translations.Clear();
            Changed = null;
            RitsuLibFramework.Logger.Info($"[{_instanceName}] Instance disposed and resources released");
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Enumerates the current merged translations as key-value pairs.
        ///     以键值对形式枚举当前已合并的翻译。
        /// </summary>
        /// <remarks>
        ///     Enumeration uses a snapshot copy to avoid collection-modified exceptions if reload happens during iteration.
        ///     枚举会使用快照副本，避免迭代期间重载翻译导致集合被修改异常。
        /// </remarks>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureLoaded();
            return _translations.ToArray().AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Raised after translations are reloaded (locale change or <see cref="ForceReload" />).
        ///     在翻译重新加载后触发（区域设置变化或 <see cref="ForceReload" />）。
        /// </summary>
        public event Action? Changed;

        /// <summary>
        ///     Returns the translation for <paramref name="key" /> or <paramref name="fallback" /> if missing.
        ///     返回 <paramref name="key" /> 的翻译；缺失时返回 <paramref name="fallback" />。
        /// </summary>
        public string Get(string key, string fallback)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureLoaded();
            return _translations.GetValueOrDefault(key) ?? fallback;
        }

        /// <summary>
        ///     Returns true and outputs the translation when <paramref name="key" /> exists.
        ///     当 <paramref name="key" /> 存在时返回 true 并输出翻译。
        /// </summary>
        public bool TryGet(string key, out string value)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureLoaded();
            return _translations.TryGetValue(key, out value!);
        }

        /// <summary>
        ///     Returns true when <paramref name="key" /> exists in the current merged dictionary.
        ///     当 <paramref name="key" /> 存在于当前合并字典中时返回 true。
        /// </summary>
        public bool ContainsKey(string key)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureLoaded();
            return _translations.ContainsKey(key);
        }

        /// <summary>
        ///     Returns a stable snapshot view of the current merged translations.
        ///     返回当前合并翻译的稳定快照视图。
        /// </summary>
        public IReadOnlyDictionary<string, string> Snapshot()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureLoaded();
            return new ReadOnlyDictionary<string, string>(_translations);
        }

        /// <summary>
        ///     Enumerates translation keys in the current merged dictionary.
        ///     枚举当前合并字典中的翻译键。
        /// </summary>
        /// <param name="prefix">
        ///     When non-empty, only keys that start with this prefix (ordinal ignore case).
        ///     非空时，仅返回以该前缀开头的键（序号忽略大小写）。
        /// </param>
        /// <param name="orderByKey">
        ///     When true, keys are ordered with ordinal ignore case.
        ///     为 true 时，按键名以序号忽略大小写排序。
        /// </param>
        public IEnumerable<string> EnumerateKeys(string? prefix = null, bool orderByKey = true)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureLoaded();

            IEnumerable<string> keys = _translations.Keys;
            if (!string.IsNullOrWhiteSpace(prefix))
                keys = keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            return orderByKey
                ? keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                : keys;
        }

        /// <summary>
        ///     Returns all keys from the current merged dictionary as a new list, optionally sorted.
        ///     将当前合并字典中的全部键作为新列表返回，并可选择排序。
        /// </summary>
        public IReadOnlyList<string> GetAllKeys(bool orderByKey = true)
        {
            return EnumerateKeys(null, orderByKey).ToArray();
        }

        /// <summary>
        ///     Returns known language codes discoverable from configured sources.
        ///     返回可从已配置来源中发现的已知语言代码。
        /// </summary>
        public IReadOnlyList<string> EnumerateAvailableLanguages(bool useCache = true)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (useCache && _availableLanguagesCache != null)
                return _availableLanguagesCache;

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in _fsFolders)
            foreach (var lang in EnumerateJsonLanguagesInFolder(folder))
                set.Add(lang);

            foreach (var folder in _pckFolders)
            foreach (var lang in EnumerateJsonLanguagesInFolder(folder))
                set.Add(lang);

            foreach (var res in _resourceFolders)
            foreach (var lang in EnumerateEmbeddedLanguages(res))
                set.Add(lang);

            var list = set
                .Select(NormalizeLanguageCode)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _availableLanguagesCache = list;
            return list;
        }

        /// <summary>
        ///     Reloads translations for the current resolved language and raises <see cref="Changed" />.
        ///     重新加载当前解析语言的翻译，并触发 <see cref="Changed" />。
        /// </summary>
        public void ForceReload()
        {
            var language = ResolveLanguage();
            _translations = LoadTranslations(language);
            _loadedLanguage = language;
            _availableLanguagesCache = null;
            RitsuLibFramework.Logger.Info(
                $"[{_instanceName}] Successfully reloaded translations for language '{language}' ({_translations.Count} entries)");
            BroadcastChange();
        }

        private void Initialize()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ForceReload();
            TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;

            try
            {
                var instance = LocManager.Instance;
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (instance == null)
                {
                    RitsuLibFramework.Logger.Debug(
                        $"[{_instanceName}] LocManager not available, will detect language changes lazily");
                    return;
                }

                instance.SubscribeToLocaleChange(OnLocaleChanged);
                _subscribed = true;
                RitsuLibFramework.Logger.Info($"[{_instanceName}] Subscribed to locale change notifications");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[{_instanceName}] Unable to subscribe to locale changes, falling back to lazy detection: {ex.Message}");
            }
        }

        private void TryUnsubscribe()
        {
            if (!_subscribed) return;

            try
            {
                var instance = LocManager.Instance;
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (instance == null) return;

                instance.UnsubscribeToLocaleChange(OnLocaleChanged);
                _subscribed = false;
                RitsuLibFramework.Logger.Info(
                    $"[{_instanceName}] Successfully unsubscribed from locale change notifications");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[{_instanceName}] Error during locale change unsubscription: {ex.Message}");
            }
        }

        private void BroadcastChange()
        {
            Changed?.Invoke();
        }

        private void OnLocaleChanged()
        {
            if (_disposed) return;
            var language = ResolveLanguage();
            RitsuLibFramework.Logger.Info(
                $"[{_instanceName}] Locale change detected, switching to language: {language}");
            _loadedLanguage = null;
            ForceReload();
        }

        private void EnsureLoaded()
        {
            if (!_subscribed) TrySubscribe();

            var language = ResolveLanguage();
            if (string.Equals(_loadedLanguage, language, StringComparison.OrdinalIgnoreCase)) return;

            _translations = LoadTranslations(language);
            _loadedLanguage = language;
            RitsuLibFramework.Logger.Info(
                $"[{_instanceName}] Successfully loaded translations for language '{_loadedLanguage}' ({_translations.Count} entries)");
        }

        private Dictionary<string, string> LoadTranslations(string language)
        {
            var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var sourceCount = 0;

            foreach (var folder in _fsFolders)
            {
                var path = $"{folder}/{language}.json";
                var dictionary = TryLoadFromFileSystem(path);
                if (dictionary is not { Count: > 0 }) continue;

                var newKeys = dictionary.Count(kvp => merged.TryAdd(kvp.Key, kvp.Value));
                sourceCount++;
                RitsuLibFramework.Logger.Info(
                    $"[{_instanceName}] Merged from FS: {path} ({dictionary.Count} entries, {newKeys} new)");
            }

            foreach (var res in _resourceFolders)
            {
                var dictionary = TryLoadEmbedded(res, language);
                if (dictionary is not { Count: > 0 }) continue;

                var newKeys = dictionary.Count(kvp => merged.TryAdd(kvp.Key, kvp.Value));
                sourceCount++;
                RitsuLibFramework.Logger.Info(
                    $"[{_instanceName}] Merged from embedded: {res}.{language}.json ({dictionary.Count} entries, {newKeys} new)");
            }

            foreach (var res in _pckFolders)
            {
                var path = $"{res}/{language}.json";
                var dictionary = TryLoadFromPck(path);
                if (dictionary is not { Count: > 0 }) continue;

                var newKeys = dictionary.Count(kvp => merged.TryAdd(kvp.Key, kvp.Value));
                sourceCount++;
                RitsuLibFramework.Logger.Info(
                    $"[{_instanceName}] Merged from PCK: {path} ({dictionary.Count} entries, {newKeys} new)");
            }

            if (merged.Count == 0)
                RitsuLibFramework.Logger.Warn($"[{_instanceName}] No translations found for '{language}'");
            else
                RitsuLibFramework.Logger.Info(
                    $"[{_instanceName}] Total: {merged.Count} entries from {sourceCount} source(s)");

            return merged;
        }

        private static IReadOnlyList<string> EnumerateJsonLanguagesInFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return [];

            var list = new List<string>();
            try
            {
                using var dir = DirAccess.Open(folder);
                if (dir == null) return [];

                list.AddRange(from file in dir.GetFiles()
                    where file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                    select file[..^5]
                    into name
                    where !string.IsNullOrWhiteSpace(name)
                    select name);
            }
            catch
            {
                return [];
            }

            return list;
        }

        private IReadOnlyList<string> EnumerateEmbeddedLanguages(string resourceFolder)
        {
            if (string.IsNullOrWhiteSpace(resourceFolder)) return [];

            string[]? names;
            try
            {
                names = _resourceAssembly.GetManifestResourceNames();
            }
            catch
            {
                return [];
            }

            var prefix = resourceFolder + ".";

            return (from name in names
                where name.StartsWith(prefix, StringComparison.Ordinal)
                where name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                select name[prefix.Length..^5]
                into core
                let dot = core.IndexOf('.')
                where dot < 0
                where !string.IsNullOrWhiteSpace(core)
                select core).ToList();
        }

        private Dictionary<string, string>? TryLoadEmbedded(string resourceFolder, string language)
        {
            var resourceName = $"{resourceFolder}.{language}.json";

            try
            {
                using var stream = _resourceAssembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    RitsuLibFramework.Logger.Debug(
                        $"[{_instanceName}] Embedded resource not found: '{resourceName}' in assembly '{_resourceAssembly.GetName().Name}'");
                    return null;
                }

                var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);

                if (translations != null) return translations;
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[{_instanceName}] Deserialization resulted in null object for embedded resource '{resourceName}'");
                return null;
            }
            catch (JsonException ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[{_instanceName}] JSON parsing error in embedded resource '{resourceName}': {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[{_instanceName}] Unexpected error loading embedded resource '{resourceName}': {ex.Message}");
                return null;
            }
        }


        private Dictionary<string, string>? TryLoadFromPck(string path)
        {
            var result = FileOperations.ReadJson<Dictionary<string, string>>(path, null, _instanceName);
            if (!result.Success || result.Data == null) return null;
            return result.Data;
        }

        private Dictionary<string, string>? TryLoadFromFileSystem(string path)
        {
            if (!FileOperations.FileExists(path))
            {
                RitsuLibFramework.Logger.Debug($"[{_instanceName}] FS file not found: '{path}'");
                return null;
            }

            var result = FileOperations.ReadJson<Dictionary<string, string>>(path, null, _instanceName);
            if (!result.Success || result.Data == null) return null;
            return result.Data;
        }

        private static string ResolveLanguage()
        {
            return ResolveCurrentLanguageCode();
        }

        /// <summary>
        ///     Resolves the current game locale to a normalized language code.
        ///     将当前游戏区域语言解析为规范化语言代码。
        /// </summary>
        /// <remarks>
        ///     Resolution tries (in order):
        ///     <list type="number">
        ///         <item>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <c>Godot.TranslationServer.GetLocale()</c>；<c>Godot.TranslationServer.GetLocale()</c>
        ///             </description>
        ///         </item>
        ///     </list>
        ///     The returned value is always normalized by <see cref="NormalizeLanguageCode" /> and falls back to
        ///     <c>eng</c> when unknown.
        ///     解析会按顺序尝试：
        ///     <list type="number">
        ///         <item>
        ///         </item>
        ///         <item>
        ///             <description>
        ///             </description>
        ///         </item>
        ///     </list>
        ///     返回值始终由 <see cref="NormalizeLanguageCode" /> 规范化，并在未知时回退到
        ///     <c>eng</c>。
        /// </remarks>
        public static string ResolveCurrentLanguageCode()
        {
            string? language = null;
            try
            {
                var instance = LocManager.Instance;
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                language = instance?.Language;
            }
            catch
            {
                // Silently ignore LocManager access errors
            }

            if (!string.IsNullOrWhiteSpace(language)) return NormalizeLanguageCode(language);

            try
            {
                language = TranslationServer.GetLocale();
            }
            catch
            {
                // Silently ignore TranslationServer access errors
            }

            return NormalizeLanguageCode(language);
        }

        /// <summary>
        ///     Normalizes a locale / language input to RitsuLib's stable three-letter-ish codes.
        ///     将区域/语言输入规范化为 RitsuLib 稳定使用的近似三字母代码。
        /// </summary>
        /// <remarks>
        ///     Examples:
        ///     <list type="bullet">
        ///         <item>
        ///         </item>
        ///         <item>
        ///         </item>
        ///         <item>
        ///         </item>
        ///     </list>
        ///     Unrecognized values are lower-cased, with <c>-</c> replaced by <c>_</c>.
        ///     Null or whitespace inputs fall back to <c>eng</c>.
        ///     示例：
        ///     <list type="bullet">
        ///         <item>
        ///         </item>
        ///         <item>
        ///         </item>
        ///         <item>
        ///         </item>
        ///     </list>
        ///     无法识别的值会转为小写，并将 <c>-</c> 替换为 <c>_</c>。
        ///     null 或空白输入会回退到 <c>eng</c>。
        /// </remarks>
        public static string NormalizeLanguageCode(string? language)
        {
            if (string.IsNullOrWhiteSpace(language)) return "eng";
            var text = language.Trim().Replace('-', '_').ToLowerInvariant();
            return text switch
            {
                "zh_cn" or "zh_hans" or "zh_sg" or "zh" => "zhs",
                "en_us" or "en_gb" or "en" or "eng" => "eng",
                "ja" or "ja_jp" or "jpn" => "jpn",
                "ko" or "ko_kr" or "kor" => "kor",
                "de" or "de_de" or "deu" => "deu",
                "es" or "es_es" or "esp" => "esp",
                "fr" or "fr_fr" or "fra" => "fra",
                "it" or "it_it" or "ita" => "ita",
                "pl" or "pl_pl" or "pol" => "pol",
                "pt" or "pt_br" or "ptb" => "ptb",
                "ru" or "ru_ru" or "rus" => "rus",
                "th" or "th_th" or "tha" => "tha",
                "tr" or "tr_tr" or "tur" => "tur",
                _ => text,
            };
        }
    }
}
