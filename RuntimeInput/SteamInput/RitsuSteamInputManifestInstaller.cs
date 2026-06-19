using System.Text;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using Environment = System.Environment;

namespace STS2RitsuLib.RuntimeInput
{
    internal static class RitsuSteamInputManifestInstaller
    {
        private const string OriginalManifestFileName = "game_actions_2868840.vdf";
        private const string GeneratedManifestFileName = "steam_input_manifest.ritsulib.vdf";

        private static int _installAttempted;
        private static int _actionsChangedAfterInstallLogged;

        static RitsuSteamInputManifestInstaller()
        {
            RitsuSteamInputActionRegistry.ActionsChanged += OnActionsChanged;
        }

        public static bool IsManifestInstalled { get; private set; }
        public static string? GeneratedManifestPath { get; private set; }

        public static void InstallBeforeSteamInputInit()
        {
            _ = typeof(RitsuSteamInputManifestInstaller);
            if (Interlocked.Exchange(ref _installAttempted, 1) == 1)
                return;

            if (!RitsuSteamInputInterop.IsSteamAvailable)
            {
                RitsuLibFramework.Logger.Debug(
                    "[SteamInput] Steam is not available; optional RitsuLib actions are disabled.");
                return;
            }

            var actions = RitsuSteamInputActionRegistry.GetActions();
            if (actions.Count == 0)
            {
                RitsuLibFramework.Logger.Debug("[SteamInput] No RitsuLib actions requested; using the game manifest.");
                return;
            }

            try
            {
                if (!TryFindOriginalManifest(out var originalPath))
                {
                    RitsuLibFramework.Logger.Warn(
                        "[SteamInput] Original game action manifest was not found; optional RitsuLib Steam Input actions are disabled.");
                    return;
                }

                var originalText = File.ReadAllText(originalPath, Encoding.UTF8);
                if (!TryMerge(originalText, actions, out var mergedText))
                {
                    RitsuLibFramework.Logger.Warn(
                        "[SteamInput] Original manifest shape was not recognized; optional RitsuLib Steam Input actions are disabled.");
                    return;
                }

                var generatedPath = ResolveGeneratedManifestPath();
                Directory.CreateDirectory(Path.GetDirectoryName(generatedPath)!);
                File.WriteAllText(
                    generatedPath,
                    mergedText.ReplaceLineEndings("\r\n"),
                    new UTF8Encoding(false));

                if (!RitsuSteamInputInterop.SetInputActionManifestFilePath(Path.GetFullPath(generatedPath)))
                {
                    RitsuLibFramework.Logger.Warn(
                        "[SteamInput] Steam rejected the generated RitsuLib action manifest.");
                    return;
                }

                GeneratedManifestPath = generatedPath;
                IsManifestInstalled = true;
                RitsuLibFramework.Logger.Info(
                    $"[SteamInput] Installed optional RitsuLib action manifest: {generatedPath} ({actions.Count} action(s)).");
            }
            catch (Exception ex)
            {
                IsManifestInstalled = false;
                RitsuLibFramework.Logger.Warn(
                    $"[SteamInput] Optional manifest install failed; falling back to normal input. {ex.Message}");
            }
        }

        private static bool TryMerge(string originalText, IReadOnlyList<RitsuSteamInputActionDescriptor> actions,
            out string mergedText)
        {
            mergedText = originalText;
            if (!TryFindNamedBlock(mergedText, "actions", 0, mergedText.Length, out var actionsBlock) ||
                !TryFindNamedBlock(mergedText, "Controls", actionsBlock.OpenBrace, actionsBlock.CloseBrace,
                    out var controlsBlock) ||
                !TryFindNamedBlock(mergedText, "Button", controlsBlock.OpenBrace, controlsBlock.CloseBrace,
                    out var buttonBlock))
                return false;

            var actionLines = BuildActionLines(actions, mergedText);
            if (actionLines.Length > 0)
                mergedText = mergedText.Insert(buttonBlock.CloseBrace, actionLines);

            if (TryFindNamedBlock(mergedText, "localization", 0, mergedText.Length, out var localizationBlock))
            {
                var languageBlocks =
                    FindChildBlocks(mergedText, localizationBlock.OpenBrace, localizationBlock.CloseBrace);
                foreach (var languageBlock in languageBlocks.OrderByDescending(static block => block.CloseBrace))
                {
                    var localizationLines = BuildLocalizationLines(actions, mergedText, languageBlock.Name);
                    if (localizationLines.Length > 0)
                        mergedText = mergedText.Insert(languageBlock.CloseBrace, localizationLines);
                }
            }
            else
            {
                var insertion = BuildLocalizationBlock(actions);
                if (TryFindLastTopLevelCloseBrace(mergedText, out var closeBrace))
                    mergedText = mergedText.Insert(closeBrace, insertion);
            }

            return true;
        }

        private static string BuildActionLines(IReadOnlyList<RitsuSteamInputActionDescriptor> actions, string text)
        {
            var builder = new StringBuilder();
            foreach (var action in actions)
            {
                if (text.Contains($"\"{action.SteamActionId}\"", StringComparison.Ordinal))
                    continue;

                builder
                    .Append('\t').Append('\t').Append('\t')
                    .Append('"').Append(EscapeVdf(action.SteamActionId)).Append('"')
                    .Append('\t')
                    .Append('"').Append(EscapeVdf(LocalizationReference(action))).Append('"')
                    .AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildLocalizationLines(
            IReadOnlyList<RitsuSteamInputActionDescriptor> actions,
            string text,
            string language)
        {
            var builder = new StringBuilder();
            foreach (var action in actions)
            {
                var key = LocalizationKey(action);
                if (text.Contains($"\"{key}\"", StringComparison.Ordinal))
                    continue;

                builder
                    .Append('\t').Append('\t')
                    .Append('"').Append(EscapeVdf(key)).Append('"')
                    .Append('\t')
                    .Append('"').Append(EscapeVdf(ResolveDisplayName(action))).Append('"')
                    .AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildLocalizationBlock(IReadOnlyList<RitsuSteamInputActionDescriptor> actions)
        {
            var builder = new StringBuilder()
                .AppendLine()
                .Append('\t').AppendLine("\"localization\"")
                .Append('\t').AppendLine("{")
                .Append('\t').Append('\t').AppendLine("\"english\"")
                .Append('\t').Append('\t').AppendLine("{");
            foreach (var action in actions)
                builder
                    .Append('\t').Append('\t').Append('\t')
                    .Append('"').Append(EscapeVdf(LocalizationKey(action))).Append('"')
                    .Append('\t')
                    .Append('"').Append(EscapeVdf(ResolveDisplayName(action))).Append('"')
                    .AppendLine();

            return builder
                .Append('\t').Append('\t').AppendLine("}")
                .Append('\t').AppendLine("}")
                .ToString();
        }

        private static string LocalizationReference(RitsuSteamInputActionDescriptor action)
        {
            return "#" + LocalizationKey(action);
        }

        private static string LocalizationKey(RitsuSteamInputActionDescriptor action)
        {
            return "RitsuLib_" + action.SteamActionId;
        }

        private static string ResolveDisplayName(RitsuSteamInputActionDescriptor action)
        {
            try
            {
                var displayName = action.DisplayName.Resolve();
                return string.IsNullOrWhiteSpace(displayName) ? action.InputActionName : displayName;
            }
            catch
            {
                return action.InputActionName;
            }
        }

        private static bool TryFindOriginalManifest(out string path)
        {
            foreach (var root in EnumerateSearchRoots())
            foreach (var directory in WalkUp(root, 6))
            {
                var candidate = Path.Combine(directory, "controller_config", OriginalManifestFileName);
                if (!File.Exists(candidate)) continue;
                path = candidate;
                return true;
            }

            path = string.Empty;
            return false;
        }

        private static IEnumerable<string> EnumerateSearchRoots()
        {
            yield return AppContext.BaseDirectory;

            var gameAssemblyDirectory = Path.GetDirectoryName(typeof(SteamControllerInputStrategy).Assembly.Location);
            if (!string.IsNullOrWhiteSpace(gameAssemblyDirectory))
                yield return gameAssemblyDirectory;

            var currentDirectory = Directory.GetCurrentDirectory();
            if (!string.IsNullOrWhiteSpace(currentDirectory))
                yield return currentDirectory;
        }

        private static IEnumerable<string> WalkUp(string root, int maxDepth)
        {
            var directory = Directory.Exists(root)
                ? new DirectoryInfo(root)
                : new DirectoryInfo(Path.GetDirectoryName(root) ?? root);

            for (var i = 0; directory != null && i <= maxDepth; i++)
            {
                yield return directory.FullName;
                directory = directory.Parent;
            }
        }

        private static string ResolveGeneratedManifestPath()
        {
            var userDataDir = OS.GetUserDataDir();
            if (string.IsNullOrWhiteSpace(userDataDir))
                userDataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SlayTheSpire2");

            return Path.Combine(userDataDir, "mods", "RitsuLib", "steam_input", GeneratedManifestFileName);
        }

        private static bool TryFindNamedBlock(string text, string name, int start, int end, out VdfBlock block)
        {
            var token = $"\"{name}\"";
            var index = text.IndexOf(token, start, StringComparison.Ordinal);
            while (index >= 0 && index < end)
            {
                var openBrace = text.IndexOf('{', index + token.Length);
                if (openBrace < 0 || openBrace >= end)
                    break;

                var closeBrace = FindMatchingBrace(text, openBrace);
                if (closeBrace >= 0 && closeBrace <= end)
                {
                    block = new(name, index, openBrace, closeBrace);
                    return true;
                }

                index = text.IndexOf(token, index + token.Length, StringComparison.Ordinal);
            }

            block = default;
            return false;
        }

        private static List<VdfBlock> FindChildBlocks(string text, int openBrace, int closeBrace)
        {
            var result = new List<VdfBlock>();
            var index = openBrace + 1;
            while (index < closeBrace)
            {
                var nameStart = text.IndexOf('"', index);
                if (nameStart < 0 || nameStart >= closeBrace)
                    break;
                var nameEnd = text.IndexOf('"', nameStart + 1);
                if (nameEnd < 0 || nameEnd >= closeBrace)
                    break;
                var childOpen = text.IndexOf('{', nameEnd + 1);
                if (childOpen < 0 || childOpen >= closeBrace)
                    break;
                var childClose = FindMatchingBrace(text, childOpen);
                if (childClose < 0 || childClose > closeBrace)
                    break;

                result.Add(new(text[(nameStart + 1)..nameEnd], nameStart, childOpen, childClose));
                index = childClose + 1;
            }

            return result;
        }

        private static int FindMatchingBrace(string text, int openBrace)
        {
            var depth = 0;
            var inString = false;
            for (var i = openBrace; i < text.Length; i++)
            {
                var ch = text[i];
                if (ch == '"' && (i == 0 || text[i - 1] != '\\'))
                    inString = !inString;
                if (inString)
                    continue;
                switch (ch)
                {
                    case '{':
                        depth++;
                        break;
                    case '}' when --depth == 0:
                        return i;
                }
            }

            return -1;
        }

        private static bool TryFindLastTopLevelCloseBrace(string text, out int closeBrace)
        {
            closeBrace = text.LastIndexOf('}');
            return closeBrace >= 0;
        }

        private static string EscapeVdf(string value)
        {
            return value.Replace("\\", @"\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal);
        }

        private static void OnActionsChanged()
        {
            if (!IsManifestInstalled || Interlocked.Exchange(ref _actionsChangedAfterInstallLogged, 1) == 1)
                return;

            RitsuLibFramework.Logger.Warn(
                "[SteamInput] RitsuLib Steam Input actions changed after manifest install; restart the game for Steam Overlay to see new actions.");
        }

        private readonly record struct VdfBlock(string Name, int NameStart, int OpenBrace, int CloseBrace);
    }
}
