using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

if (args.Length != 4 || !string.Equals(args[0], "generate", StringComparison.Ordinal))
{
    Console.Error.WriteLine("Usage: dotnet run GenerateModManifest.cs -- generate <source.json> <dest.json> <displayName>");
    return 2;
}

var sourcePath = args[1];
var destPath = args[2];
var displayName = args[3];

if (!File.Exists(sourcePath))
{
    Console.Error.WriteLine($"Source manifest not found: {sourcePath}");
    return 3;
}

var dstDir = Path.GetDirectoryName(destPath);
if (!string.IsNullOrEmpty(dstDir))
{
    Directory.CreateDirectory(dstDir);
}

var jsonText = File.ReadAllText(sourcePath, Encoding.UTF8);
JsonNode root;
try
{
    root = JsonNode.Parse(jsonText)
        ?? throw new InvalidOperationException("Parsed manifest JSON produced null.");
}
catch (JsonException ex)
{
    Console.Error.WriteLine($"Invalid JSON in source manifest '{sourcePath}': {ex.Message}");
    return 4;
}

root["name"] = displayName;

var opts = new JsonSerializerOptions { WriteIndented = true };
var updated = root.ToJsonString(opts);
File.WriteAllText(destPath, updated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
return 0;
