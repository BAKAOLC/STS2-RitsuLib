using System.Text;
using ModUploader;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = new UTF8Encoding(false);

try
{
    var options = CliOptions.Parse(args);
    if (options.ShowHelp)
    {
        CliOptions.WriteHelp(Console.Out);
        return 0;
    }

    var input = options.InputPath == null
        ? await Console.In.ReadToEndAsync()
        : await File.ReadAllTextAsync(options.InputPath, Encoding.UTF8);

    var output = options.Mode switch
    {
        ConversionMode.MarkdownToBbCode => SteamBbCodeMarkdownConverter.MarkdownToSteamBbCode(input),
        ConversionMode.BbCodeToMarkdown => SteamBbCodeMarkdownConverter.SteamBbCodeToMarkdown(input),
        _ => throw new InvalidOperationException("Unsupported conversion mode."),
    };

    if (options.OutputPath == null)
    {
        Console.Write(output);
    }
    else
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(options.OutputPath));
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(options.OutputPath, output, new UTF8Encoding(false));
    }

    return 0;
}
catch (CliException e)
{
    Console.Error.WriteLine(e.Message);
    Console.Error.WriteLine();
    CliOptions.WriteHelp(Console.Error);
    return 2;
}

internal enum ConversionMode
{
    MarkdownToBbCode,
    BbCodeToMarkdown,
}

internal sealed record CliOptions(
    ConversionMode Mode,
    string? InputPath,
    string? OutputPath,
    bool ShowHelp)
{
    public static CliOptions Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0 || IsHelp(args[0])) return new(ConversionMode.MarkdownToBbCode, null, null, true);

        var mode = ParseMode(args[0]);
        string? input = null;
        string? output = null;

        for (var i = 1; i < args.Count; i++)
        {
            var arg = args[i];
            if (IsHelp(arg)) return new(mode, input, output, true);

            switch (arg)
            {
                case "-i":
                case "--input":
                    input = ReadValue(args, ref i, arg);
                    break;
                case "-o":
                case "--output":
                    output = ReadValue(args, ref i, arg);
                    break;
                default:
                    if (input == null)
                        input = arg;
                    else
                        throw new CliException($"Unexpected argument: {arg}");

                    break;
            }
        }

        return new(mode, input, output, false);
    }

    public static void WriteHelp(TextWriter writer)
    {
        writer.WriteLine("MarkdownSteamBbCode converts between Markdown and Steam Workshop BBCode.");
        writer.WriteLine();
        writer.WriteLine("Usage:");
        writer.WriteLine("  MarkdownSteamBbCode md2bb [input.md] [-o output.txt]");
        writer.WriteLine("  MarkdownSteamBbCode bb2md [input.txt] [-o output.md]");
        writer.WriteLine("  MarkdownSteamBbCode markdown-to-bbcode < input.md > output.txt");
        writer.WriteLine("  MarkdownSteamBbCode bbcode-to-markdown < input.txt > output.md");
        writer.WriteLine();
        writer.WriteLine("Modes:");
        writer.WriteLine("  md2bb, markdown-to-bbcode, to-bbcode");
        writer.WriteLine("  bb2md, bbcode-to-markdown, to-markdown");
    }

    private static ConversionMode ParseMode(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "md2bb" or "markdown-to-bbcode" or "to-bbcode" => ConversionMode.MarkdownToBbCode,
            "bb2md" or "bbcode-to-markdown" or "to-markdown" => ConversionMode.BbCodeToMarkdown,
            _ => throw new CliException($"Unknown mode: {value}"),
        };
    }

    private static string ReadValue(IReadOnlyList<string> args, ref int index, string option)
    {
        index++;
        if (index >= args.Count) throw new CliException($"Missing value for {option}.");

        return args[index];
    }

    private static bool IsHelp(string value)
    {
        return value is "-h" or "--help" or "help";
    }
}

internal sealed class CliException(string message) : Exception(message);
