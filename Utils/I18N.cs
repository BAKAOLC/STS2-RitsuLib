using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Loads merged JSON translation dictionaries from the file system, embedded resources, and PCK paths,
    ///     reacting to game locale changes when possible.
    /// </summary>
    public class I18N : IDisposable
    {
        private readonly string[] _fsFolders;
        private readonly string _instanceName;
        private readonly string[] _pckFolders;
        private readonly Assembly _resourceAssembly;
        private readonly string[] _resourceFolders;
        private bool _disposed;
        private string? _loadedLanguage;
        private bool _subscribed;
        private Dictionary<string, string> _translations = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Creates an instance, optionally wiring locale change subscription when sources are configured.
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
        ///     Raised after translations are reloaded (locale change or <see cref="ForceReload" />).
        /// </summary>
        public event Action? Changed;

        /// <summary>
        ///     Returns the translation for <paramref name="key" /> or <paramref name="fallback" /> if missing.
        /// </summary>
        public string Get(string key, string fallback)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureLoaded();
            return _translations.GetValueOrDefault(key) ?? fallback;
        }

        /// <summary>
        ///     Reloads translations for the current resolved language and raises <see cref="Changed" />.
        /// </summary>
        public void ForceReload()
        {
            var language = ResolveLanguage();
            _translations = LoadTranslations(language);
            _loadedLanguage = language;
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
                RitsuLibFramework.Logger.Error(
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
                RitsuLibFramework.Logger.Error(
                    $"[{_instanceName}] Deserialization resulted in null object for embedded resource '{resourceName}'");
                return null;
            }
            catch (JsonException ex)
            {
                RitsuLibFramework.Logger.Error(
                    $"[{_instanceName}] JSON parsing error in embedded resource '{resourceName}': {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error(
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

        private static string NormalizeLanguageCode(string? language)
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
