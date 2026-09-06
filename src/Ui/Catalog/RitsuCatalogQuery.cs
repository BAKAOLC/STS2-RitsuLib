using System.Text;
using STS2RitsuLib.Search;

namespace STS2RitsuLib.Ui.Catalog
{
    internal sealed class RitsuCatalogQuery
    {
        internal const int MaximumLength = 2048;
        private readonly Node? _root;

        private RitsuCatalogQuery(Node? root)
        {
            _root = root;
        }

        internal bool Matches(RitsuCatalogItem item, RitsuSearchOptions options)
        {
            return _root?.Matches(item, options) ?? true;
        }

        internal static bool TryParse(string text, RitsuCatalogSearchFields defaultFields,
            RitsuCatalogSearchFields availableFields, out RitsuCatalogQuery query, out string? error,
            out int position)
        {
            try
            {
                var parser = new Parser(text, availableFields);
                query = new(parser.Parse(defaultFields));
                error = null;
                position = 0;
                return true;
            }
            catch (QueryException ex)
            {
                query = new(null);
                error = ex.Message;
                position = ex.Position + 1;
                return false;
            }
        }

        internal static string Quote(string value)
        {
            return '"' + value.Replace(@"\", @"\\").Replace("\"", "\\\"") + '"';
        }

        private abstract class Node
        {
            internal abstract bool Matches(RitsuCatalogItem item, RitsuSearchOptions options);
        }

        private sealed class Term(string text, RitsuCatalogSearchFields fields) : Node
        {
            private readonly string[] _terms = [text];

            internal override bool Matches(RitsuCatalogItem item, RitsuSearchOptions options)
            {
                return item.Matches(_terms, fields, options);
            }
        }

        private sealed class Negation(Node operand) : Node
        {
            internal override bool Matches(RitsuCatalogItem item, RitsuSearchOptions options)
            {
                return !operand.Matches(item, options);
            }
        }

        private sealed class Group(Node[] operands, bool any) : Node
        {
            internal override bool Matches(RitsuCatalogItem item, RitsuSearchOptions options)
            {
                foreach (var operand in operands)
                    if (operand.Matches(item, options) == any)
                        return any;
                return !any;
            }
        }

        private sealed class QueryException(string error, int position) : Exception(error)
        {
            internal int Position { get; } = position;
        }

        private enum Kind
        {
            End,
            Word,
            Phrase,
            Open,
            Close,
            And,
            Or,
            Not,
        }

        private readonly record struct Token(Kind Kind, string Text, int Position);

        private sealed class Parser
        {
            private readonly string _text;
            private readonly RitsuCatalogSearchFields _availableFields;
            private int _offset;
            private int _termCount;
            private Token _current;

            internal Parser(string text, RitsuCatalogSearchFields availableFields)
            {
                if (text.Length > MaximumLength)
                    throw new QueryException("tooLong", MaximumLength);
                _text = text;
                _availableFields = availableFields;
                Advance();
            }

            internal Node? Parse(RitsuCatalogSearchFields fields)
            {
                if (_current.Kind == Kind.End)
                    return null;
                var result = ParseOr(fields, 0);
                if (_current.Kind != Kind.End)
                    throw Error("unexpected");
                return result;
            }

            private Node ParseOr(RitsuCatalogSearchFields fields, int depth, bool recognizeFields = true)
            {
                var first = ParseAnd(fields, depth, recognizeFields);
                if (_current.Kind != Kind.Or)
                    return first;
                var operands = new List<Node> { first };
                while (_current.Kind == Kind.Or)
                {
                    Advance();
                    operands.Add(ParseAnd(fields, depth, recognizeFields));
                }

                return new Group([.. operands], true);
            }

            private Node ParseAnd(RitsuCatalogSearchFields fields, int depth, bool recognizeFields)
            {
                var first = ParseUnary(fields, depth, recognizeFields);
                if (_current.Kind is Kind.End or Kind.Close or Kind.Or)
                    return first;
                var operands = new List<Node> { first };
                while (_current.Kind is not (Kind.End or Kind.Close or Kind.Or))
                {
                    if (_current.Kind == Kind.And)
                        Advance();
                    operands.Add(ParseUnary(fields, depth, recognizeFields));
                }

                return new Group([.. operands], false);
            }

            private Node ParseUnary(RitsuCatalogSearchFields fields, int depth, bool recognizeFields)
            {
                if (depth >= 16)
                    throw Error("tooComplex");
                switch (_current.Kind)
                {
                    case Kind.Not:
                        Advance();
                        return new Negation(ParseUnary(fields, depth + 1, recognizeFields));
                    case Kind.Open:
                        Advance();
                        var group = ParseOr(fields, depth + 1, recognizeFields);
                        if (_current.Kind != Kind.Close)
                            throw Error("closingParenthesis");
                        Advance();
                        return group;
                }

                if (_current.Kind is not (Kind.Word or Kind.Phrase))
                    throw Error("expectedTerm");
                var token = _current;
                Advance();
                if (recognizeFields && token.Kind == Kind.Word && token.Text.IndexOf(':') is var colon and >= 0)
                {
                    fields = token.Text[..colon].ToLowerInvariant() switch
                    {
                        "name" => RitsuCatalogSearchFields.Name,
                        "id" => RitsuCatalogSearchFields.Id,
                        "desc" => RitsuCatalogSearchFields.Description,
                        "keyword" => RitsuCatalogSearchFields.Keywords,
                        "attr" => RitsuCatalogSearchFields.Metadata,
                        _ => throw new QueryException("unknownField", token.Position),
                    };
                    if ((fields & _availableFields) == 0)
                        throw new QueryException("unavailableField", token.Position);
                    if (colon == token.Text.Length - 1)
                        // ReSharper disable once TailRecursiveCall
                        return ParseUnary(fields, depth + 1, false);
                    token = token with { Text = token.Text[(colon + 1)..] };
                }

                if (string.IsNullOrWhiteSpace(token.Text))
                    throw new QueryException("expectedTerm", token.Position);
                if (fields == RitsuCatalogSearchFields.None)
                    throw new QueryException("fieldRequired", token.Position);
                if (++_termCount > 128)
                    throw new QueryException("tooComplex", token.Position);
                return new Term(token.Text, fields);
            }

            private void Advance()
            {
                while (_offset < _text.Length && char.IsWhiteSpace(_text[_offset]))
                    _offset++;
                var start = _offset;
                if (_offset == _text.Length)
                {
                    _current = new(Kind.End, string.Empty, start);
                    return;
                }

                var first = _text[_offset++];
                var kind = first switch
                {
                    '(' => Kind.Open,
                    ')' => Kind.Close,
                    '-' => Kind.Not,
                    _ => Kind.Word,
                };
                if (kind != Kind.Word)
                {
                    _current = new(kind, string.Empty, start);
                    return;
                }

                if (first == '"')
                {
                    var value = new StringBuilder();
                    while (_offset < _text.Length)
                    {
                        var character = _text[_offset++];
                        switch (character)
                        {
                            case '"':
                                _current = new(Kind.Phrase, value.ToString(), start);
                                return;
                            case '\\':
                                if (_offset == _text.Length || _text[_offset] is not ('"' or '\\'))
                                    throw new QueryException("escape", _offset - 1);
                                character = _text[_offset++];
                                break;
                        }

                        value.Append(character);
                    }

                    throw new QueryException("closingQuote", start);
                }

                while (_offset < _text.Length && !char.IsWhiteSpace(_text[_offset]) &&
                       _text[_offset] is not ('(' or ')' or '"'))
                    _offset++;
                var text = _text[start.._offset];
                kind = text switch
                {
                    "AND" => Kind.And,
                    "OR" => Kind.Or,
                    "NOT" => Kind.Not,
                    _ => Kind.Word,
                };
                _current = new(kind, text, start);
            }

            private QueryException Error(string error)
            {
                return new(error, _current.Position);
            }
        }
    }
}
