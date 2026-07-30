using System.Text.Json;
using Godot;
using FileAccess = Godot.FileAccess;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     <para xml:lang="en">Provides Godot <see cref="FileAccess" /> operations with result objects, logging, and optional backup-and-replace writes.</para>
    ///     <para xml:lang="zh-CN">提供带结果对象和日志记录的 Godot <see cref="FileAccess" /> 操作，并支持可选的备份替换写入。</para>
    /// </summary>
    public static class FileOperations
    {
        private const string TempSuffix = ".tmp";
        private const string BackupSuffix = ".backup";

        /// <summary>
        ///     <para xml:lang="en">Reads text from a file and reports detailed failures.</para>
        ///     <para xml:lang="zh-CN">从文件读取文本并报告详细失败原因。</para>
        /// </summary>
        public static ReadResult ReadText(string filePath, string? logContext = null)
        {
            var context = logContext ?? "FileOperations";

            try
            {
                if (!FileAccess.FileExists(filePath))
                {
                    RitsuLibFramework.Logger.Debug($"[{context}] File not found at '{filePath}'");
                    return new()
                    {
                        Success = false,
                        ErrorMessage = "File not found",
                    };
                }

                using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    var error = FileAccess.GetOpenError();
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[{context}] Failed to open file '{filePath}' (Error: {error})");
                    return new()
                    {
                        Success = false,
                        ErrorCode = error,
                        ErrorMessage = $"Failed to open file (Error: {error})",
                    };
                }

                var content = file.GetAsText();

                if (string.IsNullOrWhiteSpace(content))
                {
                    RitsuLibFramework.Logger.Warn($"[{context}] File '{filePath}' is empty");
                    return new()
                    {
                        Success = false,
                        Content = content,
                        ErrorMessage = "File is empty",
                    };
                }

                RitsuLibFramework.Logger.Debug(
                    $"[{context}] Successfully read file '{filePath}' ({content.Length} characters)");
                return new()
                {
                    Success = true,
                    Content = content,
                };
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[{context}] Unexpected error reading file '{filePath}': {ex.Message}");
                return new()
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}",
                };
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Writes text, optionally by rotating a backup, writing a temporary file, then renaming it into place.</para>
        ///     <para xml:lang="zh-CN">写入文本；可选地轮换备份、写入临时文件，再将其重命名到目标位置。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">Despite the <paramref name="atomic" /> parameter name, this is a best-effort backup-and-replace sequence rather than a transactional atomic-write guarantee; each file-system step can fail independently.</para>
        ///     <para xml:lang="zh-CN">尽管参数名为 <paramref name="atomic" />，该流程只是尽力完成的备份替换序列，并不保证事务式原子写入；每个文件系统步骤都可能独立失败。</para>
        /// </remarks>
        public static WriteResult WriteText(string filePath, string content, string? logContext = null,
            bool atomic = true)
        {
            var context = logContext ?? "FileOperations";

            if (!atomic)
                return WriteTextDirect(filePath, content, context);

            try
            {
                EnsureDirectoryExists(filePath);

                var tempPath = filePath + TempSuffix;
                var backupPath = filePath + BackupSuffix;

                RotateBackup(filePath, backupPath, context);

                var writeResult = WriteTextDirect(tempPath, content, context);
                if (!writeResult.Success)
                {
                    RestoreFromBackup(filePath, backupPath, context);
                    return writeResult;
                }

                var renameResult = RenameFile(tempPath, filePath, context);
                if (!renameResult.Success)
                {
                    DeleteFileSilent(tempPath);
                    RestoreFromBackup(filePath, backupPath, context);
                    return new()
                    {
                        Success = false,
                        ErrorMessage = $"Failed to rename temp file: {renameResult.ErrorMessage}",
                    };
                }

                RitsuLibFramework.Logger.Debug($"[{context}] Atomic write completed for '{filePath}'");
                return new() { Success = true };
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[{context}] Unexpected error during atomic write to '{filePath}': {ex.Message}");
                return new()
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}",
                };
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Writes text directly without the backup-and-temporary-file sequence.</para>
        ///     <para xml:lang="zh-CN">直接写入文本，不使用备份与临时文件写入流程。</para>
        /// </summary>
        private static WriteResult WriteTextDirect(string filePath, string content, string context)
        {
            try
            {
                EnsureDirectoryExists(filePath);

                using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
                if (file == null)
                {
                    var error = FileAccess.GetOpenError();
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[{context}] Failed to open file '{filePath}' for writing (Error: {error})");
                    return new()
                    {
                        Success = false,
                        ErrorCode = error,
                        ErrorMessage = $"Failed to open file for writing (Error: {error})",
                    };
                }

                file.StoreString(content);
                RitsuLibFramework.Logger.Debug(
                    $"[{context}] Successfully wrote to file '{filePath}' ({content.Length} characters)");
                return new() { Success = true };
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[{context}] Unexpected error writing to file '{filePath}': {ex.Message}");
                return new()
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}",
                };
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Deletes the previous backup and renames the current file to the backup path.</para>
        ///     <para xml:lang="zh-CN">删除先前备份，并将当前文件重命名为备份路径。</para>
        /// </summary>
        private static void RotateBackup(string filePath, string backupPath, string context)
        {
            try
            {
                if (FileAccess.FileExists(backupPath))
                    DeleteFileSilent(backupPath);

                if (!FileAccess.FileExists(filePath)) return;
                var result = RenameFile(filePath, backupPath, context);
                if (result.Success)
                    RitsuLibFramework.Logger.Debug($"[{context}] Rotated '{filePath}' to backup");
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[{context}] Failed to rotate backup: {ex.Message}");
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Restores the target file from its backup.</para>
        ///     <para xml:lang="zh-CN">从备份还原目标文件。</para>
        /// </summary>
        private static void RestoreFromBackup(string filePath, string backupPath, string context)
        {
            try
            {
                if (!FileAccess.FileExists(backupPath)) return;

                var result = RenameFile(backupPath, filePath, context);
                if (result.Success)
                    RitsuLibFramework.Logger.Info($"[{context}] Restored '{filePath}' from backup");
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[{context}] Failed to restore from backup: {ex.Message}");
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Renames a file.</para>
        ///     <para xml:lang="zh-CN">重命名文件。</para>
        /// </summary>
        public static WriteResult RenameFile(string fromPath, string toPath, string? logContext = null)
        {
            try
            {
                var dir = GetDirectoryFromPath(fromPath);
                using var dirAccess = DirAccess.Open(dir);

                if (dirAccess == null)
                    return new()
                    {
                        Success = false,
                        ErrorMessage = $"Failed to access directory '{dir}'",
                    };

                var error = dirAccess.Rename(fromPath, toPath);
                if (error != Error.Ok)
                    return new()
                    {
                        Success = false,
                        ErrorCode = error,
                        ErrorMessage = $"Rename failed (Error: {error})",
                    };

                return new() { Success = true };
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                return new()
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}",
                };
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Reads the backup file when reading the primary file fails.</para>
        ///     <para xml:lang="zh-CN">读取主文件失败时读取备份文件。</para>
        /// </summary>
        public static ReadResult ReadTextWithBackupFallback(string filePath, string? logContext = null)
        {
            var context = logContext ?? "FileOperations";
            var result = ReadText(filePath, context);

            if (result.Success)
                return result;

            var backupPath = filePath + BackupSuffix;
            if (!FileAccess.FileExists(backupPath))
                return result;

            RitsuLibFramework.Logger.Info($"[{context}] Attempting to load from backup '{backupPath}'");
            var backupResult = ReadText(backupPath, context);

            if (!backupResult.Success) return backupResult;
            backupResult = backupResult with { LoadedFromBackup = true };
            RitsuLibFramework.Logger.Info($"[{context}] Successfully loaded from backup");

            return backupResult;
        }

        private static void DeleteFileSilent(string filePath)
        {
            try
            {
                if (!FileAccess.FileExists(filePath)) return;
                var dir = GetDirectoryFromPath(filePath);
                using var dirAccess = DirAccess.Open(dir);
                dirAccess?.Remove(filePath);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                // Ignore errors in silent delete
            }
        }

        private static string GetDirectoryFromPath(string filePath)
        {
            var lastSlash = filePath.LastIndexOf('/');
            if (lastSlash < 0)
                return "user://";

            var schemeSeparator = filePath.IndexOf("://", StringComparison.Ordinal);
            if (schemeSeparator >= 0)
            {
                var schemeRootLength = schemeSeparator + 3;
                return lastSlash < schemeRootLength ? filePath[..schemeRootLength] : filePath[..lastSlash];
            }

            if (lastSlash == 0)
                return "/";
            if (lastSlash == 2 && filePath.Length > 2 && filePath[1] == ':')
                return filePath[..3];

            return filePath[..lastSlash];
        }

        /// <summary>
        ///     <para xml:lang="en">Ensures that the directory containing a file path exists.</para>
        ///     <para xml:lang="zh-CN">确保包含文件路径的目录存在。</para>
        /// </summary>
        private static void EnsureDirectoryExists(string filePath)
        {
            var lastSlash = filePath.LastIndexOf('/');
            if (lastSlash <= 0) return;

            var directory = filePath[..lastSlash];
            if (string.IsNullOrEmpty(directory)) return;
            if (DirAccess.DirExistsAbsolute(directory)) return;

            var error = DirAccess.MakeDirRecursiveAbsolute(directory);
            if (error != Error.Ok)
                RitsuLibFramework.Logger.Warn($"Failed to create directory '{directory}' (Error: {error})");
        }

        /// <summary>
        ///     <para xml:lang="en">Reads and deserializes JSON from a file.</para>
        ///     <para xml:lang="zh-CN">从文件读取并反序列化 JSON。</para>
        /// </summary>
        public static JsonResult<T> ReadJson<T>(string filePath, JsonSerializerOptions? options = null,
            string? logContext = null)
        {
            var context = logContext ?? "FileOperations";
            var readResult = ReadText(filePath, context);

            if (!readResult.Success || readResult.Content == null)
                return new()
                {
                    Success = false,
                    ErrorMessage = readResult.ErrorMessage,
                };

            try
            {
                var data = JsonSerializer.Deserialize<T>(readResult.Content, options);

                if (data == null)
                {
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[{context}] Deserialization resulted in null object for file '{filePath}'");
                    return new()
                    {
                        Success = false,
                        ErrorMessage = "Deserialization resulted in null object",
                    };
                }

                RitsuLibFramework.Logger.Debug($"[{context}] Successfully deserialized JSON from '{filePath}'");
                return new()
                {
                    Success = true,
                    Data = data,
                };
            }
            catch (JsonException ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[{context}] JSON parsing error in file '{filePath}': {ex.Message}");
                return new()
                {
                    Success = false,
                    ErrorMessage = $"JSON parsing error: {ex.Message}",
                };
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[{context}] Unexpected error deserializing file '{filePath}': {ex.Message}");
                return new()
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}",
                };
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Serializes and writes JSON to a file.</para>
        ///     <para xml:lang="zh-CN">序列化 JSON 并写入文件。</para>
        /// </summary>
        public static WriteResult WriteJson<T>(string filePath, T data, JsonSerializerOptions? options = null,
            string? logContext = null)
        {
            var context = logContext ?? "FileOperations";

            try
            {
                var jsonContent = JsonSerializer.Serialize(data, options);
                return WriteText(filePath, jsonContent, context);
            }
            catch (JsonException ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[{context}] JSON serialization error: {ex.Message}");
                return new()
                {
                    Success = false,
                    ErrorMessage = $"JSON serialization error: {ex.Message}",
                };
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[{context}] Unexpected error serializing data: {ex.Message}");
                return new()
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}",
                };
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Determines whether a file exists.</para>
        ///     <para xml:lang="zh-CN">确定文件是否存在。</para>
        /// </summary>
        public static bool FileExists(string filePath)
        {
            return FileAccess.FileExists(filePath);
        }

        /// <summary>
        ///     <para xml:lang="en">Deletes a file and reports detailed failures.</para>
        ///     <para xml:lang="zh-CN">删除文件并报告详细失败原因。</para>
        /// </summary>
        public static WriteResult DeleteFile(string filePath, string? logContext = null)
        {
            var context = logContext ?? "FileOperations";

            try
            {
                if (!FileAccess.FileExists(filePath))
                {
                    RitsuLibFramework.Logger.Debug($"[{context}] File '{filePath}' does not exist, nothing to delete");
                    return new() { Success = true };
                }

                var directory = GetDirectoryFromPath(filePath);

                var dirAccess = DirAccess.Open(directory);
                if (dirAccess == null)
                {
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[{context}] Failed to access directory '{directory}' for file deletion");
                    return new()
                    {
                        Success = false,
                        ErrorMessage = $"Failed to access directory '{directory}'",
                    };
                }

                var error = dirAccess.Remove(filePath);
                if (error != Error.Ok)
                {
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[{context}] Failed to delete file '{filePath}' (Error: {error})");
                    return new()
                    {
                        Success = false,
                        ErrorCode = error,
                        ErrorMessage = $"Failed to delete file (Error: {error})",
                    };
                }

                RitsuLibFramework.Logger.Info($"[{context}] Successfully deleted file '{filePath}'");
                return new() { Success = true };
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[{context}] Unexpected error deleting file '{filePath}': {ex.Message}");
                return new()
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}",
                };
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to recursively delete a directory and its contents. Child cleanup continues after a
        ///         failure, and the first failure is returned.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试递归删除目录及其内容。子项清理失败后会继续处理，并返回首个失败。</para>
        /// </summary>
        public static WriteResult DeleteDirectoryRecursive(string directoryPath, string? logContext = null)
        {
            var context = logContext ?? "FileOperations";

            try
            {
                if (!DirAccess.DirExistsAbsolute(directoryPath))
                {
                    RitsuLibFramework.Logger.Debug(
                        $"[{context}] Directory '{directoryPath}' does not exist, nothing to delete");
                    return new() { Success = true };
                }

                using var dirAccess = DirAccess.Open(directoryPath);
                if (dirAccess == null)
                {
                    var error = DirAccess.GetOpenError();
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[{context}] Failed to open directory '{directoryPath}' (Error: {error})");
                    return new()
                    {
                        Success = false,
                        ErrorCode = error,
                        ErrorMessage = $"Failed to open directory '{directoryPath}' (Error: {error})",
                    };
                }

                WriteResult? firstChildFailure = null;
                foreach (var file in dirAccess.GetFiles())
                {
                    var filePath = $"{directoryPath}/{file}";
                    var result = DeleteFile(filePath, context);
                    if (result.Success)
                        continue;

                    RitsuLibFramework.Logger.Warn(
                        $"[{context}] Failed to delete file '{filePath}': {result.ErrorMessage}");
                    firstChildFailure ??= new()
                    {
                        Success = false,
                        ErrorCode = result.ErrorCode,
                        ErrorMessage = $"Failed to delete child file '{filePath}': {result.ErrorMessage}",
                    };
                }

                foreach (var subDir in dirAccess.GetDirectories())
                {
                    var subDirPath = $"{directoryPath}/{subDir}";
                    var result = DeleteDirectoryRecursive(subDirPath, context);
                    if (result.Success)
                        continue;

                    firstChildFailure ??= new()
                    {
                        Success = false,
                        ErrorCode = result.ErrorCode,
                        ErrorMessage = $"Failed to delete child directory '{subDirPath}': {result.ErrorMessage}",
                    };
                }

                if (firstChildFailure != null)
                    return firstChildFailure;

                var parentPath = GetDirectoryFromPath(directoryPath);
                using var parentAccess = DirAccess.Open(parentPath);
                if (parentAccess == null)
                {
                    var error = DirAccess.GetOpenError();
                    RitsuLibFramework.Logger.Warn(
                        $"[{context}] Failed to open parent directory '{parentPath}' while deleting "
                        + $"'{directoryPath}' (Error: {error})");
                    return new()
                    {
                        Success = false,
                        ErrorCode = error,
                        ErrorMessage = $"Failed to open parent directory '{parentPath}' (Error: {error})",
                    };
                }

                var removeError = parentAccess.Remove(directoryPath);
                if (removeError != Error.Ok)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[{context}] Failed to remove directory '{directoryPath}' (Error: {removeError})");
                    return new()
                    {
                        Success = false,
                        ErrorCode = removeError,
                        ErrorMessage = $"Failed to remove directory (Error: {removeError})",
                    };
                }

                RitsuLibFramework.Logger.Info($"[{context}] Successfully deleted directory '{directoryPath}'");
                return new() { Success = true };
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[{context}] Unexpected error deleting directory '{directoryPath}': {ex.Message}");
                return new()
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}",
                };
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Represents the result of a file read operation.</para>
        ///     <para xml:lang="zh-CN">表示文件读取操作的结果。</para>
        /// </summary>
        public record ReadResult
        {
            /// <summary>
            ///     <para xml:lang="en">Indicates whether non-empty file content was read successfully.</para>
            ///     <para xml:lang="zh-CN">指示是否成功读取到非空文件内容。</para>
            /// </summary>
            public bool Success { get; init; }

            /// <summary>
            ///     <para xml:lang="en">File text when <see cref="Success" /> is true.</para>
            ///     <para xml:lang="zh-CN"><see cref="Success" /> 为 true 时的文件文本。</para>
            /// </summary>
            public string? Content { get; init; }

            /// <summary>
            ///     <para xml:lang="en">Godot error code for a low-level open or read failure.</para>
            ///     <para xml:lang="zh-CN">底层打开或读取失败时的 Godot 错误码。</para>
            /// </summary>
            public Error? ErrorCode { get; init; }

            /// <summary>
            ///     <para xml:lang="en">Human-readable failure reason when <see cref="Success" /> is false.</para>
            ///     <para xml:lang="zh-CN"><see cref="Success" /> 为 false 时的可读失败原因。</para>
            /// </summary>
            public string? ErrorMessage { get; init; }

            /// <summary>
            ///     <para xml:lang="en">Indicates whether content was recovered from the sibling <c>.backup</c> file.</para>
            ///     <para xml:lang="zh-CN">指示内容是否从同级 <c>.backup</c> 文件恢复。</para>
            /// </summary>
            public bool LoadedFromBackup { get; init; }
        }

        /// <summary>
        ///     <para xml:lang="en">Represents the result of a mutating file-system operation.</para>
        ///     <para xml:lang="zh-CN">表示文件系统修改操作的结果。</para>
        /// </summary>
        public class WriteResult
        {
            /// <summary>
            ///     <para xml:lang="en">Indicates whether the requested operation, including a no-op deletion of a missing target, completed successfully.</para>
            ///     <para xml:lang="zh-CN">指示请求的操作是否成功完成；删除不存在的目标也视为空操作成功。</para>
            /// </summary>
            public bool Success { get; init; }

            /// <summary>
            ///     <para xml:lang="en">Godot error code for a low-level operation failure.</para>
            ///     <para xml:lang="zh-CN">底层操作失败时的 Godot 错误码。</para>
            /// </summary>
            public Error? ErrorCode { get; init; }

            /// <summary>
            ///     <para xml:lang="en">Human-readable failure reason when <see cref="Success" /> is false.</para>
            ///     <para xml:lang="zh-CN"><see cref="Success" /> 为 false 时的可读失败原因。</para>
            /// </summary>
            public string? ErrorMessage { get; init; }
        }

        /// <summary>
        ///     <para xml:lang="en">Represents the result of a JSON deserialization operation.</para>
        ///     <para xml:lang="zh-CN">表示 JSON 反序列化操作的结果。</para>
        /// </summary>
        public class JsonResult<T>
        {
            /// <summary>
            ///     <para xml:lang="en">Indicates whether JSON was parsed into a non-null instance.</para>
            ///     <para xml:lang="zh-CN">指示 JSON 是否被解析为非 null 实例。</para>
            /// </summary>
            public bool Success { get; init; }

            /// <summary>
            ///     <para xml:lang="en">Deserialized object when <see cref="Success" /> is true.</para>
            ///     <para xml:lang="zh-CN"><see cref="Success" /> 为 true 时的反序列化对象。</para>
            /// </summary>
            public T? Data { get; init; }

            /// <summary>
            ///     <para xml:lang="en">Human-readable failure reason when <see cref="Success" /> is false.</para>
            ///     <para xml:lang="zh-CN"><see cref="Success" /> 为 false 时的可读失败原因。</para>
            /// </summary>
            public string? ErrorMessage { get; init; }
        }
    }
}
