using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Loads merged JSON translation dictionaries from the file system, embedded resources, and PCK
    ///         paths, reacting to game locale changes when possible.
    ///     </para>
    ///     <para xml:lang="zh-CN">从文件系统、嵌入资源和 PCK 路径加载并合并 JSON 翻译字典，并在可行时响应游戏语言切换。</para>
    /// </summary>
    public class I18N : IDisposable, IEnumerable<KeyValuePair<string, string>>
    {
        private readonly string? _fallbackLanguage;
        private readonly string[] _fsFolders;
        private readonly string _instanceName;
        private readonly string[] _pckFolders;
        private readonly Assembly _resourceAssembly;
        private readonly string[] _resourceFolders;
        private IReadOnlyList<string>? _availableLanguagesCache;
        private bool _disposed;
        private string? _loadedLanguage;
        private HashSet<string> _localKeys = new(StringComparer.OrdinalIgnoreCase);
        private bool _subscribed;
        private Dictionary<string, string> _translations = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     <para xml:lang="en">Creates an instance, optionally wiring locale change subscription when sources are configured.</para>
        ///     <para xml:lang="zh-CN">创建实例；当配置了翻译来源时，可自动接入语言切换订阅。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         This overload falls back from non-English languages to <c>eng</c>, matching the game's
        ///         <c>LocTable</c> fallback behavior.
        ///     </para>
        ///     <para xml:lang="zh-CN">此重载会让非英语语言回退到 <c>eng</c>，与游戏 <c>LocTable</c> 的回退行为一致。</para>
        /// </remarks>
        public I18N(string? instanceName = null,
            string[]? fsFolders = null,
            string[]? resourceFolders = null,
            string[]? pckFolders = null,
            Assembly? resourceAssembly = null)
            : this(instanceName, fsFolders, resourceFolders, pckFolders, resourceAssembly, null)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an instance with an explicit fallback language when
        ///         <paramref name="fallbackLanguage" /> is non-blank.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建实例；<paramref name="fallbackLanguage" /> 非空白时将其用作显式回退语言。</para>
        /// </summary>
        public I18N(string? instanceName,
            string[]? fsFolders,
            string[]? resourceFolders,
            string[]? pckFolders,
            Assembly? resourceAssembly,
            string? fallbackLanguage)
        {
            _instanceName = instanceName ?? "I18N";
            _resourceFolders = resourceFolders?.Where(f => !string.IsNullOrWhiteSpace(f)).ToArray() ?? [];
            _fsFolders = fsFolders?.Where(f => !string.IsNullOrWhiteSpace(f)).ToArray() ?? [];
            _pckFolders = pckFolders?.Where(f => !string.IsNullOrWhiteSpace(f)).ToArray() ?? [];
            _resourceAssembly = resourceAssembly ?? Assembly.GetCallingAssembly();
            _fallbackLanguage = string.IsNullOrWhiteSpace(fallbackLanguage)
                ? null
                : NormalizeLanguageCode(fallbackLanguage);

            if (_resourceFolders.Length == 0 && _fsFolders.Length == 0 && _pckFolders.Length == 0)
                RitsuLibFramework.Logger.Warn($"[{_instanceName}] Initialized with no translation sources");
            else
                Initialize();
        }

        /// <summary>
        ///     <para xml:lang="en">Releases subscriptions and clears loaded translations.</para>
        ///     <para xml:lang="zh-CN">释放订阅并清空已经加载的翻译。</para>
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            TryUnsubscribe();
            _translations.Clear();
            _localKeys.Clear();
            Changed = null;
            RitsuLibFramework.Logger.Info($"[{_instanceName}] Instance disposed and resources released");
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     <para xml:lang="en">Enumerates the current merged translations as key-value pairs.</para>
        ///     <para xml:lang="zh-CN">以键值对形式枚举当前已合并的翻译。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Enumeration uses a snapshot copy to avoid collection-modified exceptions if a reload occurs
        ///         during iteration.
        ///     </para>
        ///     <para xml:lang="zh-CN">枚举使用快照副本，避免迭代期间重新加载翻译造成集合已修改异常。</para>
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
        ///     <para xml:lang="en">
        ///         Raised by <see cref="ForceReload" />, including reloads initiated by a subscribed locale-change
        ///         notification.
        ///     </para>
        ///     <para xml:lang="zh-CN">由 <see cref="ForceReload" /> 触发，包括已订阅语言变化通知所发起的重新加载。</para>
        /// </summary>
        public event Action? Changed;

        /// <summary>
        ///     <para xml:lang="en">Returns the translation for <paramref name="key" /> or <paramref name="fallback" /> if missing.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="key" /> 的翻译；缺失时返回 <paramref name="fallback" />。</para>
        /// </summary>
        public string Get(string key, string fallback)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureLoaded();
            return _translations.GetValueOrDefault(key) ?? fallback;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns true and outputs the translation when <paramref name="key" /> exists.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="key" /> 存在时返回 true 并输出翻译。</para>
        /// </summary>
        public bool TryGet(string key, out string value)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureLoaded();
            return _translations.TryGetValue(key, out value!);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns true when <paramref name="key" /> exists in the current merged dictionary.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="key" /> 存在于当前合并字典中时返回 true。</para>
        /// </summary>
        public bool ContainsKey(string key)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureLoaded();
            return _translations.ContainsKey(key);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns true when <paramref name="key" /> exists in the current language before fallback
        ///         language entries are considered.
        ///     </para>
        ///     <para xml:lang="zh-CN">在考虑回退语言条目前，如果 <paramref name="key" /> 已存在于当前语言中，则返回 true。</para>
        /// </summary>
        public bool ContainsLocalKey(string key)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureLoaded();
            return _localKeys.Contains(key);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a stable snapshot view of the current merged translations.</para>
        ///     <para xml:lang="zh-CN">返回当前合并翻译的稳定快照视图。</para>
        /// </summary>
        public IReadOnlyDictionary<string, string> Snapshot()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureLoaded();
            return new ReadOnlyDictionary<string, string>(_translations);
        }

        /// <summary>
        ///     <para xml:lang="en">Enumerates translation keys in the current merged dictionary.</para>
        ///     <para xml:lang="zh-CN">枚举当前合并字典中的翻译键。</para>
        /// </summary>
        /// <param name="prefix">
        ///     <para xml:lang="en">
        ///         When non-empty, limits results to keys beginning with this prefix using ordinal
        ///         case-insensitive comparison.
        ///     </para>
        ///     <para xml:lang="zh-CN">非空时，仅返回以此输入前缀开头的键，比较时按序号忽略大小写。</para>
        /// </param>
        /// <param name="orderByKey">
        ///     <para xml:lang="en">When true, keys are ordered with ordinal ignore case.</para>
        ///     <para xml:lang="zh-CN">为 true 时，按键名以序号忽略大小写排序。</para>
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
        ///     <para xml:lang="en">Returns all keys from the current merged dictionary as a new list, optionally sorted.</para>
        ///     <para xml:lang="zh-CN">将当前合并字典中的全部键作为新列表返回，并可选择排序。</para>
        /// </summary>
        public IReadOnlyList<string> GetAllKeys(bool orderByKey = true)
        {
            return [.. EnumerateKeys(null, orderByKey)];
        }

        /// <summary>
        ///     <para xml:lang="en">Returns known language codes discoverable from configured sources.</para>
        ///     <para xml:lang="zh-CN">返回可从已配置来源中发现的已知语言代码。</para>
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
        ///     <para xml:lang="en">Reloads translations for the current resolved language and raises <see cref="Changed" />.</para>
        ///     <para xml:lang="zh-CN">重新加载当前解析语言的翻译，并触发 <see cref="Changed" />。</para>
        /// </summary>
        public void ForceReload()
        {
            var language = ResolveLanguage();
            var loaded = LoadTranslations(language);
            _translations = loaded.Translations;
            _localKeys = loaded.LocalKeys;
            _loadedLanguage = language;
            _availableLanguagesCache = null;
            RitsuLibFramework.Logger.Debug(
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
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
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
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
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

            var loaded = LoadTranslations(language);
            _translations = loaded.Translations;
            _localKeys = loaded.LocalKeys;
            _loadedLanguage = language;
            RitsuLibFramework.Logger.Debug(
                $"[{_instanceName}] Successfully loaded translations for language '{_loadedLanguage}' ({_translations.Count} entries)");
        }

        private LoadedTranslations LoadTranslations(string language)
        {
            var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var localKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sourceCount = 0;
            var primarySourceCount = 0;
            var fallbackSourceCount = 0;
            string? fallbackLanguage = null;

            foreach (var step in ResolveLanguageLoadSteps(language))
            {
                if (!step.IsPrimary)
                    fallbackLanguage = step.Language;

                foreach (var folder in _fsFolders)
                {
                    var path = $"{folder}/{step.Language}.json";
                    var dictionary = TryLoadFromFileSystem(path);
                    if (dictionary is not { Count: > 0 }) continue;

                    var newKeys = MergeTranslations(dictionary, step.IsPrimary);
                    sourceCount++;
                    CountSource(step.IsPrimary);
                    RitsuLibFramework.Logger.Debug(
                        $"[{_instanceName}] Merged {DescribeLoadStep(step)} from FS: {path} ({dictionary.Count} entries, {newKeys} new)");
                }

                foreach (var res in _resourceFolders)
                {
                    var dictionary = TryLoadEmbedded(res, step.Language);
                    if (dictionary is not { Count: > 0 }) continue;

                    var newKeys = MergeTranslations(dictionary, step.IsPrimary);
                    sourceCount++;
                    CountSource(step.IsPrimary);
                    RitsuLibFramework.Logger.Debug(
                        $"[{_instanceName}] Merged {DescribeLoadStep(step)} from embedded: {res}.{step.Language}.json ({dictionary.Count} entries, {newKeys} new)");
                }

                foreach (var res in _pckFolders)
                {
                    var path = $"{res}/{step.Language}.json";
                    var dictionary = TryLoadFromPck(path);
                    if (dictionary is not { Count: > 0 }) continue;

                    var newKeys = MergeTranslations(dictionary, step.IsPrimary);
                    sourceCount++;
                    CountSource(step.IsPrimary);
                    RitsuLibFramework.Logger.Debug(
                        $"[{_instanceName}] Merged {DescribeLoadStep(step)} from PCK: {path} ({dictionary.Count} entries, {newKeys} new)");
                }
            }

            if (merged.Count == 0)
                RitsuLibFramework.Logger.Warn($"[{_instanceName}] No translations found for '{language}'");
            else
                RitsuLibFramework.Logger.Info(
                    $"[{_instanceName}] Loaded translations: language='{NormalizeLanguageCode(language)}', " +
                    $"fallback='{fallbackLanguage ?? "<none>"}', entries={merged.Count}, local={localKeys.Count}, " +
                    $"fallbackEntries={merged.Count - localKeys.Count}, sources={sourceCount} " +
                    $"(local={primarySourceCount}, fallback={fallbackSourceCount})");

            return new(merged, localKeys);

            void CountSource(bool isPrimary)
            {
                if (isPrimary)
                    primarySourceCount++;
                else
                    fallbackSourceCount++;
            }

            int MergeTranslations(IReadOnlyDictionary<string, string> dictionary, bool isPrimary)
            {
                var newKeys = 0;
                foreach (var kvp in dictionary)
                {
                    if (isPrimary)
                        localKeys.Add(kvp.Key);
                    if (merged.TryAdd(kvp.Key, kvp.Value))
                        newKeys++;
                }

                return newKeys;
            }
        }

        private IEnumerable<LanguageLoadStep> ResolveLanguageLoadSteps(string language)
        {
            var normalizedLanguage = NormalizeLanguageCode(language);
            yield return new(normalizedLanguage, true);

            var fallback = ResolveFallbackLanguage(normalizedLanguage);
            if (!string.IsNullOrWhiteSpace(fallback) &&
                !string.Equals(normalizedLanguage, fallback, StringComparison.OrdinalIgnoreCase))
                yield return new(fallback, false);
        }

        private string? ResolveFallbackLanguage(string language)
        {
            if (_fallbackLanguage != null)
                return _fallbackLanguage;

            return string.Equals(language, "eng", StringComparison.OrdinalIgnoreCase) ? null : "eng";
        }

        private static string DescribeLoadStep(LanguageLoadStep step)
        {
            return step.IsPrimary ? "current language" : $"fallback language '{step.Language}'";
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
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
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
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                return [];
            }

            var prefix = resourceFolder + ".";

            return
            [
                .. from name in names
                where name.StartsWith(prefix, StringComparison.Ordinal)
                where name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                select name[prefix.Length..^5]
                into core
                let dot = core.IndexOf('.')
                where dot < 0
                where !string.IsNullOrWhiteSpace(core)
                select core,
            ];
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
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
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
        ///     <para xml:lang="en">Resolves the current game locale to a normalized language code.</para>
        ///     <para xml:lang="zh-CN">将当前游戏语言设置解析为规范化语言代码。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Resolution tries <c>LocManager.Instance.Language</c> first, then
        ///         <see cref="TranslationServer.GetLocale" />. The result is normalized by
        ///         <see cref="NormalizeLanguageCode" /> and falls back to <c>eng</c> when unavailable or blank.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析会先尝试 <c>LocManager.Instance.Language</c>，再尝试
        ///         <see cref="TranslationServer.GetLocale" />。结果由 <see cref="NormalizeLanguageCode" /> 规范化，
        ///         无法取得或为空白时回退到 <c>eng</c>。
        ///     </para>
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
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                // Silently ignore LocManager access errors
            }

            if (!string.IsNullOrWhiteSpace(language)) return NormalizeLanguageCode(language);

            try
            {
                language = TranslationServer.GetLocale();
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                // Silently ignore TranslationServer access errors
            }

            return NormalizeLanguageCode(language);
        }

        /// <summary>
        ///     <para xml:lang="en">Normalizes a locale or language input to RitsuLib's stable language-code identifiers.</para>
        ///     <para xml:lang="zh-CN">将区域设置或语言输入规范化为 RitsuLib 使用的稳定语言代码标识符。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Examples include <c>zh-CN</c> to <c>zhs</c>, <c>en-US</c> to <c>eng</c>, and <c>ja-JP</c> to
        ///         <c>jpn</c>. Unrecognized values are lower-cased with <c>-</c> replaced by <c>_</c>; null or
        ///         whitespace values fall back to <c>eng</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         例如，<c>zh-CN</c> 会变为 <c>zhs</c>，<c>en-US</c> 会变为 <c>eng</c>，<c>ja-JP</c> 会变为
        ///         <c>jpn</c>。无法识别的值会转为小写并将 <c>-</c> 替换为 <c>_</c>；null 或空白值会回退到
        ///         <c>eng</c>。
        ///     </para>
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

        private readonly record struct LoadedTranslations(
            Dictionary<string, string> Translations,
            HashSet<string> LocalKeys);

        private readonly record struct LanguageLoadStep(string Language, bool IsPrimary);
    }
}
