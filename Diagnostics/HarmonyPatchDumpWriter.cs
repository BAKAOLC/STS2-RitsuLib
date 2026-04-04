using System.Reflection;
using System.Text;
using Godot;
using HarmonyLib;
using FileAccess = System.IO.FileAccess;

namespace STS2RitsuLib.Diagnostics
{
    /// <summary>
    ///     Writes a UTF-8 text report of all Harmony-patched methods (similar to standalone dump mods).
    /// </summary>
    internal static class HarmonyPatchDumpWriter
    {
        /// <summary>
        ///     Resolves <c>user://</c> / <c>res://</c> via Godot and returns an absolute filesystem path.
        /// </summary>
        internal static string? TryResolveFilesystemPath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                return null;

            var trimmed = rawPath.Trim();
            if (trimmed.StartsWith("user://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
                return ProjectSettings.GlobalizePath(trimmed);

            return Path.GetFullPath(trimmed);
        }

        internal static bool TryWrite(string filesystemPath, out string? errorMessage)
        {
            errorMessage = null;
            try
            {
                var dir = Path.GetDirectoryName(filesystemPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                using var fileStream =
                    new FileStream(filesystemPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using var streamWriter = new StreamWriter(fileStream, Encoding.UTF8);
                WriteReport(streamWriter);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private static void WriteReport(StreamWriter streamWriter)
        {
            streamWriter.WriteLine("=======================================================");
            streamWriter.WriteLine("===          Harmony Patch Dump Report             ===");
            streamWriter.WriteLine("=======================================================");
            streamWriter.WriteLine($"Generated at: {DateTime.Now:O}");
            streamWriter.WriteLine($"User data dir: {OS.GetUserDataDir()}");
            streamWriter.WriteLine("=======================================================");
            streamWriter.WriteLine();

            var allPatchedMethods = Harmony.GetAllPatchedMethods()
                .OrderBy(m => m.DeclaringType?.FullName ?? "Unknown")
                .ThenBy(m => m.Name)
                .ToList();

            var methodCount = 0;
            var totalPrefixes = 0;
            var totalPostfixes = 0;
            var totalTranspilers = 0;
            var totalFinalizers = 0;

            foreach (var patchedMethod in allPatchedMethods)
            {
                methodCount++;
                var counts = LogPatchedMethodInfo(patchedMethod, streamWriter);
                totalPrefixes += counts.prefixes;
                totalPostfixes += counts.postfixes;
                totalTranspilers += counts.transpilers;
                totalFinalizers += counts.finalizers;
                streamWriter.WriteLine();
            }

            streamWriter.WriteLine("=======================================================");
            streamWriter.WriteLine("===                   Summary                      ===");
            streamWriter.WriteLine("=======================================================");
            streamWriter.WriteLine($"Total Patched Methods:  {methodCount}");
            streamWriter.WriteLine($"  - Prefix patches:     {totalPrefixes}");
            streamWriter.WriteLine($"  - Postfix patches:    {totalPostfixes}");
            streamWriter.WriteLine($"  - Transpiler patches: {totalTranspilers}");
            streamWriter.WriteLine($"  - Finalizer patches:  {totalFinalizers}");
            streamWriter.WriteLine(
                $"  - Total patches:      {totalPrefixes + totalPostfixes + totalTranspilers + totalFinalizers}");
            streamWriter.WriteLine("=======================================================");
        }

        private static (int prefixes, int postfixes, int transpilers, int finalizers) LogPatchedMethodInfo(
            MethodBase methodBase, StreamWriter streamWriter)
        {
            var patchInfo = Harmony.GetPatchInfo(methodBase);
            if (patchInfo == null) return (0, 0, 0, 0);

            var declaringType = methodBase.DeclaringType?.FullName ?? "Unknown";
            var methodSignature = GetMethodSignature(methodBase);
            var returnType = methodBase is MethodInfo mi ? mi.ReturnType.Name : "void";

            streamWriter.WriteLine($"┌─ [{declaringType}]");
            streamWriter.WriteLine($"│  Method: {returnType} {methodSignature}");
            streamWriter.WriteLine("│");

            var prefixCount = 0;
            var postfixCount = 0;
            var transpilerCount = 0;
            var finalizerCount = 0;

            if (patchInfo.Prefixes.Count > 0)
            {
                streamWriter.WriteLine($"│  ├─ Prefixes ({patchInfo.Prefixes.Count}):");
                foreach (var patch in patchInfo.Prefixes.OrderBy(p => p.priority).ThenBy(p => p.owner))
                {
                    streamWriter.WriteLine($"│  │  {FormatPatchInfo(patch)}");
                    prefixCount++;
                }
            }

            if (patchInfo.Postfixes.Count > 0)
            {
                streamWriter.WriteLine($"│  ├─ Postfixes ({patchInfo.Postfixes.Count}):");
                foreach (var patch in patchInfo.Postfixes.OrderBy(p => p.priority).ThenBy(p => p.owner))
                {
                    streamWriter.WriteLine($"│  │  {FormatPatchInfo(patch)}");
                    postfixCount++;
                }
            }

            if (patchInfo.Transpilers.Count > 0)
            {
                streamWriter.WriteLine($"│  ├─ Transpilers ({patchInfo.Transpilers.Count}):");
                foreach (var patch in patchInfo.Transpilers.OrderBy(p => p.priority).ThenBy(p => p.owner))
                {
                    streamWriter.WriteLine($"│  │  {FormatPatchInfo(patch)}");
                    transpilerCount++;
                }
            }

            if (patchInfo.Finalizers.Count > 0)
            {
                streamWriter.WriteLine($"│  └─ Finalizers ({patchInfo.Finalizers.Count}):");
                foreach (var patch in patchInfo.Finalizers.OrderBy(p => p.priority).ThenBy(p => p.owner))
                {
                    streamWriter.WriteLine($"│     {FormatPatchInfo(patch)}");
                    finalizerCount++;
                }
            }

            streamWriter.WriteLine("└─────────────────────────────────────────────────────────────────");

            return (prefixCount, postfixCount, transpilerCount, finalizerCount);
        }

        private static string GetMethodSignature(MethodBase methodBase)
        {
            var parameters = methodBase.GetParameters();
            var paramString = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
            return $"{methodBase.Name}({paramString})";
        }

        private static string FormatPatchInfo(Patch patch)
        {
            var sb = new StringBuilder();
            sb.Append($"├─ [Priority: {patch.priority}] ");
            sb.Append($"[{patch.owner}] ");
            var patchClass = patch.PatchMethod.DeclaringType?.FullName ?? "Unknown";
            var patchMethodName = patch.PatchMethod.Name;
            sb.Append($"{patchClass}.{patchMethodName}");

            try
            {
                var moduleName = Path.GetFileName(patch.PatchMethod.Module.FullyQualifiedName);
                if (!string.IsNullOrEmpty(moduleName) && moduleName != "<Unknown>")
                    sb.Append($" (from {moduleName})");
            }
            catch
            {
                // ignored
            }

            return sb.ToString();
        }
    }
}
