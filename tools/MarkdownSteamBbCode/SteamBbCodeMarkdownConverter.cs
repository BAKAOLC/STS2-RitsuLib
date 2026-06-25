using System.Text;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace ModUploader
{
    public static class SteamBbCodeMarkdownConverter
    {
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        private static readonly HashSet<string> KnownTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "b",
            "i",
            "u",
            "strike",
            "code",
            "quote",
            "url",
            "img",
            "h1",
            "h2",
            "h3",
            "h4",
            "h5",
            "h6",
            "list",
            "olist",
            "table",
            "tr",
            "th",
            "td",
        };

        public static string Convert(string markdown)
        {
            return MarkdownToSteamBbCode(markdown);
        }

        public static string MarkdownToSteamBbCode(string markdown)
        {
            MarkdownDocument document = Markdown.Parse(markdown ?? string.Empty, Pipeline);
            return NormalizeLineEndings(RenderMarkdownBlocks(document)).Trim();
        }

        public static string SteamBbCodeToMarkdown(string bbcode)
        {
            if (string.IsNullOrEmpty(bbcode)) return string.Empty;

            var root = ParseBbCode(bbcode.Replace("\r\n", "\n").Replace('\r', '\n'));
            return NormalizeLineEndings(RenderBbBlocks(root.Children, 0)).Trim();
        }

        private static string RenderMarkdownBlocks(ContainerBlock container)
        {
            List<string> rendered = [];

            foreach (Block block in container)
            {
                var text = RenderMarkdownBlock(block).Trim();
                if (text.Length > 0) rendered.Add(text);
            }

            return string.Join("\n\n", rendered);
        }

        private static string RenderMarkdownBlock(Block block)
        {
            return block switch
            {
                HeadingBlock heading => RenderMarkdownHeading(heading),
                ParagraphBlock paragraph => RenderMarkdownInline(paragraph.Inline),
                ListBlock list => RenderMarkdownList(list),
                QuoteBlock quote => RenderMarkdownQuote(quote),
                FencedCodeBlock code => RenderMarkdownCodeBlock(code),
                CodeBlock code => RenderMarkdownCodeBlock(code),
                ThematicBreakBlock => "----",
                Table table => RenderMarkdownTable(table),
                HtmlBlock html => EscapeSteamText(html.Lines.ToString()),
                LeafBlock leaf => EscapeSteamText(leaf.Lines.ToString()),
                ContainerBlock container => RenderMarkdownBlocks(container),
                _ => string.Empty,
            };
        }

        private static string RenderMarkdownHeading(HeadingBlock heading)
        {
            var tag = heading.Level switch
            {
                1 => "h1",
                2 => "h2",
                _ => "h3",
            };

            return $"[{tag}]{RenderMarkdownInline(heading.Inline).Trim()}[/{tag}]";
        }

        private static string RenderMarkdownList(ListBlock list)
        {
            var tag = list.IsOrdered ? "olist" : "list";
            StringBuilder builder = new();
            builder.Append('[').Append(tag).AppendLine("]");

            foreach (Block item in list)
            {
                if (item is not ListItemBlock listItem) continue;

                var text = RenderMarkdownListItem(listItem);
                if (text.Length > 0) builder.Append("[*]").AppendLine(text);
            }

            builder.Append("[/").Append(tag).Append(']');
            return builder.ToString();
        }

        private static string RenderMarkdownListItem(ListItemBlock listItem)
        {
            List<string> parts = [];

            foreach (Block child in listItem)
            {
                var text = RenderMarkdownBlock(child).Trim();
                if (text.Length > 0) parts.Add(text);
            }

            return string.Join("\n", parts);
        }

        private static string RenderMarkdownQuote(QuoteBlock quote)
        {
            var content = RenderMarkdownBlocks(quote).Trim();
            return content.Length == 0 ? string.Empty : $"[quote]\n{content}\n[/quote]";
        }

        private static string RenderMarkdownCodeBlock(LeafBlock code)
        {
            return $"[code]\n{EscapeSteamCode(code.Lines.ToString().TrimEnd())}\n[/code]";
        }

        private static string RenderMarkdownTable(Table table)
        {
            StringBuilder builder = new();
            builder.AppendLine("[table]");

            foreach (Block child in table)
            {
                if (child is not TableRow row) continue;

                builder.AppendLine("[tr]");
                foreach (Block cellBlock in row)
                {
                    var tag = row.IsHeader ? "th" : "td";
                    var content = cellBlock is TableCell cell
                        ? RenderMarkdownTableCell(cell).Trim()
                        : RenderMarkdownBlock(cellBlock).Trim();
                    builder.Append('[').Append(tag).Append(']');
                    builder.Append(content);
                    builder.Append("[/").Append(tag).AppendLine("]");
                }

                builder.AppendLine("[/tr]");
            }

            builder.Append("[/table]");
            return builder.ToString();
        }

        private static string RenderMarkdownTableCell(TableCell cell)
        {
            List<string> parts = [];

            foreach (Block block in cell)
            {
                var text = RenderMarkdownBlock(block).Trim();
                if (text.Length > 0) parts.Add(text);
            }

            return string.Join("\n", parts);
        }

        private static string RenderMarkdownInline(ContainerInline? container)
        {
            if (container == null) return string.Empty;

            StringBuilder builder = new();
            Inline? inline = container.FirstChild;

            while (inline != null)
            {
                builder.Append(RenderMarkdownInline(inline));
                inline = inline.NextSibling;
            }

            return builder.ToString();
        }

        private static string RenderMarkdownInline(Inline inline)
        {
            return inline switch
            {
                LiteralInline literal => EscapeSteamText(literal.Content.ToString()),
                CodeInline code => $"[code]{EscapeSteamCode(code.Content)}[/code]",
                LineBreakInline => "\n",
                HtmlInline html => EscapeSteamText(html.Tag),
                LinkInline { IsImage: true } image => RenderMarkdownImage(image),
                LinkInline link => RenderMarkdownLink(link),
                EmphasisInline emphasis => RenderMarkdownEmphasis(emphasis),
                ContainerInline nested => RenderMarkdownInline(nested),
                _ => EscapeSteamText(inline.ToString() ?? string.Empty),
            };
        }

        private static string RenderMarkdownImage(LinkInline image)
        {
            if (string.IsNullOrWhiteSpace(image.Url)) return RenderMarkdownInline(image);

            return $"[img]{EscapeSteamUrl(image.Url)}[/img]";
        }

        private static string RenderMarkdownLink(LinkInline link)
        {
            var label = RenderMarkdownInline(link).Trim();
            if (string.IsNullOrWhiteSpace(link.Url)) return label;

            return $"[url={EscapeSteamUrl(link.Url)}]{label}[/url]";
        }

        private static string RenderMarkdownEmphasis(EmphasisInline emphasis)
        {
            var content = RenderMarkdownInline(emphasis);
            return emphasis.DelimiterChar switch
            {
                '*' or '_' when emphasis.DelimiterCount >= 2 => $"[b]{content}[/b]",
                '*' or '_' => $"[i]{content}[/i]",
                '~' => $"[strike]{content}[/strike]",
                _ => content,
            };
        }

        private static BbNode ParseBbCode(string text)
        {
            var root = BbNode.Element("root");
            List<BbNode> stack = [root];
            var index = 0;

            while (index < text.Length)
            {
                var open = text.IndexOf('[', index);
                if (open < 0)
                {
                    AppendBbText(CurrentBbNode(stack), text[index..]);
                    break;
                }

                if (open > index) AppendBbText(CurrentBbNode(stack), text[index..open]);

                var close = text.IndexOf(']', open + 1);
                if (close < 0)
                {
                    AppendBbText(CurrentBbNode(stack), text[open..]);
                    break;
                }

                var rawTag = text[(open + 1)..close];
                var tag = ParseBbTag(rawTag);
                if (tag == null)
                {
                    AppendBbText(CurrentBbNode(stack), text[open..(close + 1)]);
                    index = close + 1;
                    continue;
                }

                if (tag.Name.Equals("lb", StringComparison.OrdinalIgnoreCase))
                {
                    AppendBbText(CurrentBbNode(stack), "[");
                    index = close + 1;
                    continue;
                }

                if (tag.Name.Equals("rb", StringComparison.OrdinalIgnoreCase))
                {
                    AppendBbText(CurrentBbNode(stack), "]");
                    index = close + 1;
                    continue;
                }

                if (tag.IsListItem)
                {
                    StartBbListItem(stack);
                    index = close + 1;
                    continue;
                }

                if (!KnownTags.Contains(tag.Name))
                {
                    AppendBbText(CurrentBbNode(stack), text[open..(close + 1)]);
                    index = close + 1;
                    continue;
                }

                if (tag.IsClosing)
                {
                    if (!CloseBbTag(stack, tag.Name)) AppendBbText(CurrentBbNode(stack), text[open..(close + 1)]);

                    index = close + 1;
                    continue;
                }

                if (tag.Name.Equals("code", StringComparison.OrdinalIgnoreCase))
                {
                    var closingStart = IndexOfClosingBbTag(text, "code", close + 1);
                    var contentEnd = closingStart < 0 ? text.Length : closingStart;
                    CurrentBbNode(stack).Children.Add(BbNode.Element("code", text[(close + 1)..contentEnd]));
                    index = closingStart < 0 ? text.Length : contentEnd + "[/code]".Length;
                    continue;
                }

                var node = BbNode.Element(tag.Name, attribute: tag.Attribute);
                CurrentBbNode(stack).Children.Add(node);
                stack.Add(node);
                index = close + 1;
            }

            return root;
        }

        private static TagToken? ParseBbTag(string rawTag)
        {
            var tag = rawTag.Trim();
            if (tag.Length == 0) return null;

            if (tag == "*") return TagToken.ListItem();

            var isClosing = tag[0] == '/';
            if (isClosing) tag = tag[1..].TrimStart();

            var equals = tag.IndexOf('=');
            var nameEnd = equals >= 0 ? equals : tag.IndexOfAny([' ', '\t']);
            if (nameEnd < 0) nameEnd = tag.Length;

            var name = tag[..nameEnd].Trim().ToLowerInvariant();
            if (name.Length == 0) return null;

            var attribute = equals >= 0 ? tag[(equals + 1)..].Trim() : null;
            return new(name, attribute, isClosing, false);
        }

        private static void StartBbListItem(List<BbNode> stack)
        {
            var listIndex = -1;
            for (var i = stack.Count - 1; i >= 0; i--)
                if (IsBbListTag(stack[i].Tag))
                {
                    listIndex = i;
                    break;
                }

            if (listIndex < 0)
            {
                AppendBbText(CurrentBbNode(stack), "[*]");
                return;
            }

            while (stack.Count > listIndex + 1) stack.RemoveAt(stack.Count - 1);

            var item = BbNode.Element("*");
            CurrentBbNode(stack).Children.Add(item);
            stack.Add(item);
        }

        private static bool CloseBbTag(List<BbNode> stack, string tag)
        {
            for (var i = stack.Count - 1; i > 0; i--)
            {
                if (!stack[i].Tag.Equals(tag, StringComparison.OrdinalIgnoreCase)) continue;

                while (stack.Count > i) stack.RemoveAt(stack.Count - 1);

                return true;
            }

            return false;
        }

        private static int IndexOfClosingBbTag(string text, string tag, int start)
        {
            return text.IndexOf($"[/{tag}]", start, StringComparison.OrdinalIgnoreCase);
        }

        private static BbNode CurrentBbNode(List<BbNode> stack)
        {
            return stack[^1];
        }

        private static void AppendBbText(BbNode node, string text)
        {
            if (text.Length == 0) return;

            if (node.Children.LastOrDefault() is { Tag: "text", Text: not null } last)
                last.Text += text;
            else
                node.Children.Add(BbNode.TextNode(text));
        }

        private static string RenderBbBlocks(IReadOnlyList<BbNode> nodes, int indent)
        {
            List<string> blocks = [];
            StringBuilder inline = new();

            foreach (var node in nodes)
            {
                if (!IsBbBlockNode(node))
                {
                    inline.Append(RenderBbInline(node));
                    continue;
                }

                FlushInline();
                var block = RenderBbBlock(node, indent).Trim();
                if (block.Length > 0) blocks.Add(block);
            }

            FlushInline();
            return string.Join("\n\n", blocks);

            void FlushInline()
            {
                var text = NormalizeInlineWhitespace(inline.ToString()).Trim();
                if (text.Length > 0) blocks.Add(text);

                inline.Clear();
            }
        }

        private static bool IsBbBlockNode(BbNode node)
        {
            return BbCodeLooksBlock(node) || node.Tag is "quote" or "list" or "olist" or "table" ||
                   IsBbHeadingTag(node.Tag);
        }

        private static string RenderBbBlock(BbNode node, int indent)
        {
            return node.Tag switch
            {
                "code" => BbCodeLooksBlock(node)
                    ? RenderBbCodeBlock(node.Text ?? RenderBbInlineChildren(node))
                    : RenderBbInline(node),
                "quote" => RenderBbQuote(node, indent),
                "list" => RenderBbList(node, indent, false),
                "olist" => RenderBbList(node, indent, true),
                "table" => RenderBbTable(node),
                _ when IsBbHeadingTag(node.Tag) => RenderBbHeading(node),
                _ => RenderBbInline(node),
            };
        }

        private static string RenderBbHeading(BbNode node)
        {
            var level = int.TryParse(node.Tag.AsSpan(1), out var value) ? Math.Clamp(value, 1, 6) : 1;
            var content = RenderBbInlineChildren(node).Trim();
            if (content.Length == 0) return string.Empty;

            if (!content.Contains('\n')) return $"{new string('#', level)} {content}";

            if (level <= 2)
            {
                var underline = level == 1 ? '=' : '-';
                return $"{content}\n{new string(underline, Math.Max(3, LongestLineLength(content)))}";
            }

            return $"{new string('#', level)} {content.Replace("\n", " ", StringComparison.Ordinal)}";
        }

        private static string RenderBbQuote(BbNode node, int indent)
        {
            var content = RenderBbBlocks(node.Children, indent).Trim();
            return content.Length == 0 ? string.Empty : PrefixLines(content, "> ");
        }

        private static string RenderBbList(BbNode node, int indent, bool ordered)
        {
            StringBuilder builder = new();
            var index = 1;

            foreach (var item in node.Children.Where(static child => child.Tag == "*"))
            {
                var marker = ordered ? $"{index}. " : "- ";
                var itemText = RenderBbListItem(item, indent + marker.Length);
                if (itemText.Length == 0) continue;

                var lines = itemText.Split('\n');
                builder.Append(new string(' ', indent)).Append(marker).AppendLine(lines[0]);
                for (var i = 1; i < lines.Length; i++)
                    builder.Append(new string(' ', indent + marker.Length)).AppendLine(lines[i]);

                index++;
            }

            return builder.ToString().TrimEnd();
        }

        private static string RenderBbListItem(BbNode item, int indent)
        {
            List<string> parts = [];
            StringBuilder inline = new();

            foreach (var child in item.Children)
            {
                if (!IsBbBlockNode(child))
                {
                    inline.Append(RenderBbInline(child));
                    continue;
                }

                FlushInline();
                var block = RenderBbBlock(child, indent).Trim();
                if (block.Length > 0) parts.Add(block);
            }

            FlushInline();
            return string.Join("\n", parts);

            void FlushInline()
            {
                var text = NormalizeInlineWhitespace(inline.ToString()).Trim();
                if (text.Length > 0) parts.Add(text);

                inline.Clear();
            }
        }

        private static string RenderBbTable(BbNode table)
        {
            var rows = table.Children.Where(static child => child.Tag == "tr").ToList();
            if (rows.Count == 0) return RenderBbInlineChildren(table).Trim();

            var cells = rows.Select(row => row.Children
                    .Where(static child => child.Tag is "th" or "td")
                    .Select(cell => RenderBbInlineChildren(cell).Replace("\n", "<br>", StringComparison.Ordinal).Trim())
                    .ToList())
                .ToList();

            var columnCount = cells.Count == 0 ? 0 : cells.Max(static row => row.Count);
            if (columnCount == 0) return string.Empty;

            StringBuilder builder = new();
            AppendMarkdownTableRow(builder, cells[0], columnCount);
            builder.Append('|');
            for (var i = 0; i < columnCount; i++) builder.Append(" --- |");

            builder.AppendLine();

            foreach (var row in cells.Skip(1)) AppendMarkdownTableRow(builder, row, columnCount);

            return builder.ToString().TrimEnd();
        }

        private static void AppendMarkdownTableRow(StringBuilder builder, IReadOnlyList<string> row, int columnCount)
        {
            builder.Append('|');
            for (var i = 0; i < columnCount; i++)
                builder.Append(' ').Append(EscapeMarkdownTableCell(i < row.Count ? row[i] : string.Empty)).Append(" |");

            builder.AppendLine();
        }

        private static string RenderBbCodeBlock(string content)
        {
            var normalized = DecodeSteamEscapes(content).Replace("\r\n", "\n").Replace('\r', '\n').Trim('\n');
            var fence = "```";
            while (normalized.Contains(fence, StringComparison.Ordinal)) fence += "`";

            return $"{fence}\n{normalized}\n{fence}";
        }

        private static string RenderBbInline(BbNode node)
        {
            if (node is { Tag: "text", Text: not null } textNode) return DecodeSteamEscapes(textNode.Text);

            return node.Tag switch
            {
                "b" => WrapMarkdownInline("**", RenderBbInlineChildren(node)),
                "i" => WrapMarkdownInline("*", RenderBbInlineChildren(node)),
                "u" => $"<u>{RenderBbInlineChildren(node)}</u>",
                "strike" => WrapMarkdownInline("~~", RenderBbInlineChildren(node)),
                "code" => RenderBbInlineCode(node.Text ?? RenderBbInlineChildren(node)),
                "url" => RenderBbUrl(node),
                "img" => RenderBbImage(node),
                "tr" or "th" or "td" => RenderBbInlineChildren(node),
                _ when IsBbBlockNode(node) => RenderBbBlock(node, 0),
                _ => RenderBbInlineChildren(node),
            };
        }

        private static string RenderBbInlineChildren(BbNode node)
        {
            StringBuilder builder = new();
            foreach (var child in node.Children) builder.Append(RenderBbInline(child));

            return builder.ToString();
        }

        private static string RenderBbUrl(BbNode node)
        {
            var label = RenderBbInlineChildren(node).Trim();
            var url = DecodeSteamEscapes(node.Attribute ?? label).Trim();
            if (url.Length == 0) return label;

            return label.Length == 0 || string.Equals(label, url, StringComparison.Ordinal)
                ? $"<{url}>"
                : $"[{EscapeMarkdownLinkLabel(label)}]({url})";
        }

        private static string RenderBbImage(BbNode node)
        {
            var url = RenderBbInlineChildren(node).Trim();
            return url.Length == 0 ? string.Empty : $"![]({url})";
        }

        private static string WrapMarkdownInline(string wrapper, string content)
        {
            return content.Length == 0 ? string.Empty : $"{wrapper}{content}{wrapper}";
        }

        private static string RenderBbInlineCode(string content)
        {
            var decoded = DecodeSteamEscapes(content).Replace("\n", " ", StringComparison.Ordinal);
            var marker = "`";
            while (decoded.Contains(marker, StringComparison.Ordinal)) marker += "`";

            return $"{marker}{decoded}{marker}";
        }

        private static string EscapeSteamText(string text)
        {
            return EscapeSteamBrackets(text);
        }

        private static string EscapeSteamCode(string text)
        {
            return EscapeSteamBrackets(text);
        }

        private static string EscapeSteamUrl(string url)
        {
            return url
                .Replace("[", "%5B", StringComparison.Ordinal)
                .Replace("]", "%5D", StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal);
        }

        private static string EscapeSteamBrackets(string text)
        {
            StringBuilder builder = new(text.Length);
            foreach (var c in text)
                builder.Append(c switch
                {
                    '[' => "[lb]",
                    ']' => "[rb]",
                    _ => c,
                });

            return builder.ToString();
        }

        private static string DecodeSteamEscapes(string text)
        {
            return text
                .Replace("[lb]", "[", StringComparison.OrdinalIgnoreCase)
                .Replace("[rb]", "]", StringComparison.OrdinalIgnoreCase)
                .Replace("&#91;", "[", StringComparison.Ordinal)
                .Replace("&#93;", "]", StringComparison.Ordinal);
        }

        private static string NormalizeInlineWhitespace(string text)
        {
            return text.Replace("\n\n\n", "\n\n", StringComparison.Ordinal);
        }

        private static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        }

        private static string PrefixLines(string text, string prefix)
        {
            return string.Join("\n", text.Split('\n').Select(line => prefix + line));
        }

        private static string EscapeMarkdownLinkLabel(string label)
        {
            return label.Replace("[", "\\[", StringComparison.Ordinal).Replace("]", "\\]", StringComparison.Ordinal);
        }

        private static string EscapeMarkdownTableCell(string text)
        {
            return text.Replace("|", "\\|", StringComparison.Ordinal);
        }

        private static int LongestLineLength(string text)
        {
            return text.Split('\n').Max(static line => line.Length);
        }

        private static bool IsBbHeadingTag(string tag)
        {
            return tag is "h1" or "h2" or "h3" or "h4" or "h5" or "h6";
        }

        private static bool BbCodeLooksBlock(BbNode node)
        {
            if (node.Tag != "code") return false;

            var content = node.Text ?? RenderBbInlineChildren(node);
            return content.Contains('\n') || content.Length > 80;
        }

        private static bool IsBbListTag(string tag)
        {
            return tag is "list" or "olist";
        }

        private sealed class BbNode
        {
            private BbNode(string tag, string? attribute = null, string? text = null)
            {
                Tag = tag;
                Attribute = attribute;
                Text = text;
            }

            public string Tag { get; }
            public string? Attribute { get; }
            public string? Text { get; set; }
            public List<BbNode> Children { get; } = [];

            public static BbNode Element(string tag, string? text = null, string? attribute = null)
            {
                return new(tag, attribute, text);
            }

            public static BbNode TextNode(string text)
            {
                return new("text", text: text);
            }
        }

        private sealed record TagToken(string Name, string? Attribute, bool IsClosing, bool IsListItem)
        {
            public static TagToken ListItem()
            {
                return new("*", null, false, true);
            }
        }
    }
}
