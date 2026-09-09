using STS2RitsuLib.Search;

namespace STS2RitsuLib.Ui.Catalog
{
    internal sealed class RitsuCatalogSearchRequest
    {
        private readonly RitsuCatalogQuery? _expression;
        private readonly RitsuCatalogSearchFields _fields;
        private readonly RitsuSearchOptions _options;
        private readonly string[] _terms;

        private RitsuCatalogSearchRequest(RitsuCatalogQuery? expression, string[] terms,
            RitsuCatalogSearchFields fields, RitsuSearchOptions options)
        {
            _expression = expression;
            _terms = terms;
            _fields = fields;
            _options = options;
        }

        internal static bool TryCreate(bool advanced, string text, RitsuCatalogSearchFields fields,
            RitsuCatalogSearchFields availableFields, RitsuSearchOptions normalOptions,
            out RitsuCatalogSearchRequest? request, out string? error, out int position)
        {
            request = null;
            if (advanced)
            {
                if (!RitsuCatalogQuery.TryParse(text, RitsuCatalogSearchFields.None, availableFields,
                        out var expression, out error, out position))
                    return false;
                request = new(expression, [], RitsuCatalogSearchFields.None, RitsuSearchOptions.Literal);
                return true;
            }

            error = null;
            position = 0;
            var terms = text.Split((char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            request = new(null, terms, fields & availableFields, normalOptions);
            return true;
        }

        internal bool Matches(RitsuCatalogItem item, Func<RitsuCatalogItem, bool> normalFilters,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _expression?.Matches(item, _options) ??
                   (normalFilters(item) && item.Matches(_terms, _fields, _options));
        }
    }
}
