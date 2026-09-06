using STS2RitsuLib.Search;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Ui.Catalog
{
    [Flags]
    internal enum RitsuCatalogSearchFields
    {
        None = 0,
        Name = 1,
        Id = 2,
        Description = 4,
        Keywords = 8,
        Metadata = 16,
        Default = Name | Id,
        All = Name | Id | Description | Keywords | Metadata,
    }

    internal sealed class RitsuCatalogSearchDocument
    {
        private readonly Func<string?>? _description;
        private readonly Func<string?>? _keywords;
        private readonly Dictionary<(string Text, RitsuSearchOptions Options), RitsuSearchText> _prepared = [];
        private string? _descriptionText;
        private string? _keywordText;
        private string _language = string.Empty;

        internal RitsuCatalogSearchDocument(
            string? modelId = null,
            Func<string?>? description = null,
            Func<string?>? keywords = null)
        {
            ModelId = modelId;
            _description = description;
            _keywords = keywords;
        }

        internal string? ModelId { get; }

        internal RitsuCatalogSearchFields AvailableFields =>
            (_description == null ? RitsuCatalogSearchFields.None : RitsuCatalogSearchFields.Description) |
            (_keywords == null ? RitsuCatalogSearchFields.None : RitsuCatalogSearchFields.Keywords);

        internal bool Matches(
            RitsuCatalogItem item,
            IReadOnlyList<string> terms,
            RitsuCatalogSearchFields fields,
            RitsuSearchOptions options)
        {
            if (terms.Count == 0)
                return true;
            if (fields == RitsuCatalogSearchFields.None)
                return false;
            var language = I18N.ResolveCurrentLanguageCode();
            if (!string.Equals(_language, language, StringComparison.Ordinal))
            {
                _language = language;
                _descriptionText = null;
                _keywordText = null;
                _prepared.Clear();
            }


            foreach (var term in terms)
            {
                if (!MatchesTerm(term))
                    return false;
            }

            return true;

            bool MatchesTerm(string term)
            {
                // Cheap fields first. Expensive model text is only requested after these fail.
                if (Direct(item.Title, RitsuCatalogSearchFields.Name) ||
                    Direct(ModelId ?? item.Id, RitsuCatalogSearchFields.Id) ||
                    Direct(item.Subtitle, RitsuCatalogSearchFields.Metadata) ||
                    Direct(item.SearchText, RitsuCatalogSearchFields.Metadata))
                    return true;
                if ((fields & RitsuCatalogSearchFields.Description) != 0)
                {
                    _descriptionText ??= Resolve(_description);
                    if (Direct(_descriptionText, RitsuCatalogSearchFields.Description))
                        return true;
                }

                if ((fields & RitsuCatalogSearchFields.Keywords) != 0)
                {
                    _keywordText ??= Resolve(_keywords);
                    if (Direct(_keywordText, RitsuCatalogSearchFields.Keywords))
                        return true;
                }

                if (options == RitsuSearchOptions.Literal)
                    return false;
                return Expanded(item.Title, RitsuCatalogSearchFields.Name) ||
                       Expanded(ModelId ?? item.Id, RitsuCatalogSearchFields.Id) ||
                       Expanded(item.Subtitle, RitsuCatalogSearchFields.Metadata) ||
                       Expanded(item.SearchText, RitsuCatalogSearchFields.Metadata) ||
                       Expanded(_descriptionText, RitsuCatalogSearchFields.Description) ||
                       Expanded(_keywordText, RitsuCatalogSearchFields.Keywords);

                bool Direct(string? text, RitsuCatalogSearchFields field)
                {
                    return (fields & field) != 0 &&
                           text?.Contains(term, StringComparison.CurrentCultureIgnoreCase) == true;
                }

                bool Expanded(string? text, RitsuCatalogSearchFields field)
                {
                    if ((fields & field) == 0 || string.IsNullOrEmpty(text))
                        return false;
                    var key = (text, options);
                    if (!_prepared.TryGetValue(key, out var prepared))
                    {
                        prepared = RitsuSearch.Prepare(text, options);
                        if (_prepared.Count >= 32)
                            _prepared.Clear();
                        _prepared.Add(key, prepared);
                    }

                    return prepared.Contains(term);
                }
            }
        }

        private static string Resolve(Func<string?>? factory)
        {
            if (factory == null)
                return string.Empty;
            try
            {
                return StripMarkup(factory() ?? string.Empty);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[Catalog] Could not read search text: {ex.Message}");
                return string.Empty;
            }
        }

        internal static string StripMarkup(string text)
        {
            var result = new System.Text.StringBuilder(text.Length);
            for (var index = 0; index < text.Length; index++)
            {
                if (text[index] == '[')
                {
                    var end = text.IndexOf(']', index + 1);
                    if (end >= 0)
                    {
                        index = end;
                        continue;
                    }
                }

                result.Append(text[index]);
            }

            return result.ToString();
        }
    }
}
