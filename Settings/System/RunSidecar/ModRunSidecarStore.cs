using System.Text.Json;
using Godot;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;
using STS2RitsuLib.Utils.Persistence.Context;
using FileAccess = Godot.FileAccess;

namespace STS2RitsuLib.Settings.RunSidecar
{
    /// <summary>
    ///     Reads and writes per-mod run sidecar JSON under the framework mod’s profile-scoped storage tree (see
    ///     <see cref="ProfileManager" /> base paths for <see cref="SaveScope.Profile" /> and <see cref="Const.ModId" />;
    ///     Godot <c>user://</c>, resolve with <see cref="ProjectSettings.GlobalizePath(string)" />). Client-local
    ///     only; not part of vanilla save sync.
    ///     在框架 mod 按档案限定的存储树下读写每个 mod 的跑局 sidecar JSON（参见 <see cref="ProfileManager" /> 中 <see cref="SaveScope.Profile" /> 和
    ///     <see cref="Const.ModId" /> 的基础路径；Godot <c>user://</c>，用 <see cref="ProjectSettings.GlobalizePath(string)" />
    ///     解析）。仅限客户端本地；不属于原版存档同步。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Layout under the framework profile base (<see cref="Const.ModId" />):
    ///         <c>{ProfileBase}/{RunSidecarSegment}/{fingerprintStem}/{sanitizedModId}.json</c>.
    ///         <c>RunSidecarSegment</c> is <c>run_sidecar/v1</c>; <c>fingerprintStem</c> is the lowercase hex SHA-256 from
    ///         <see cref="ModRunSidecarFingerprint" /> (one folder per run). Each consumer mod owns one JSON file in
    ///         that folder; the folder is removed when the run ends or when vanilla deletes the current run save file
    ///         (see <see cref="ModRunSidecarSession" />). Writes
    ///         (see <c>ModRunSidecarSession</c>). Writes
    ///         use atomic replace via <see cref="FileOperations.WriteText" />;
    ///         if a write fails before any durable <c>*.json</c> exists, the empty run folder is removed best-effort.
    ///     </para>
    ///     <para>
    ///         框架档案基础目录（<see cref="Const.ModId" />）下的布局：
    ///         <c>RunSidecarSegment</c> 为 <c>run_sidecar/v1</c>；<c>fingerprintStem</c> 是来自
    ///         <see cref="ModRunSidecarFingerprint" /> 的小写十六进制 SHA-256（每个跑局一个文件夹）。每个消费方 mod 在
    ///         该文件夹中拥有一个 JSON 文件；跑局结束或原版删除当前跑局存档文件时会移除该文件夹
    ///         （见 <see cref="ModRunSidecarSession" />）。写入
    ///         （见 <c>ModRunSidecarSession</c>）。写入
    ///         通过 <see cref="FileOperations.WriteText" /> 使用原子替换；
    ///         如果在任何持久 <c>*.json</c> 存在之前写入失败，会尽力移除空跑局文件夹。
    ///     </para>
    /// </remarks>
    public static class ModRunSidecarStore
    {
        private const int EnvelopeVersion = 1;
        private const string RunSidecarSegment = "run_sidecar/v1";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        private static readonly Lock FileLock = new();

        /// <summary>
        ///     When a run is in progress, sets <paramref name="runDirectoryUserPath" /> to the per-run sidecar folder
        ///     (<c>…/run_sidecar/v1/{fingerprintStem}/</c>) so callers can zip or back up every mod file for that run in
        ///     one step. That directory is deleted when the run ends; copy it first if you need to keep it.
        ///     当有进行中的跑局时，将 <paramref name="runDirectoryUserPath" /> 设置为该跑局的 sidecar 文件夹（<c>…/run_sidecar/v1/{fingerprintStem}/</c>
        ///     ），让调用方可以一步压缩或备份该跑局的每个 mod 文件。该目录会在跑局结束时删除；如果需要保留，请先复制。
        /// </summary>
        /// <param name="runDirectoryUserPath">
        ///     Godot <c>user://</c> path ending at the run directory, or empty when false.
        ///     以跑局目录结尾的 Godot <c>user://</c> 路径；返回 false 时为空。
        /// </param>
        /// <returns>
        ///     False when no active run fingerprint is available.
        ///     没有可用的活动跑局指纹时为 false。
        /// </returns>
        public static bool TryGetRunSidecarPackDirectoryUserPath(out string runDirectoryUserPath)
        {
            runDirectoryUserPath = string.Empty;
            if (!ModRunSidecarFingerprint.TryGetLive(out var live))
                return false;

            runDirectoryUserPath = ResolveRunDirectoryUserPath(live);
            return true;
        }

        internal static void AppendActiveRunSidecarSyncRelativePaths(int profileId, HashSet<string> sink)
        {
            if (!ModRunSidecarFingerprint.TryGetLive(out var fp) || fp.ProfileId != profileId)
                return;
            if (!TryGetRunSidecarPackDirectoryUserPath(out var runDirUser))
                return;

            var abs = ProjectSettings.GlobalizePath(runDirUser);
            if (!DirAccess.DirExistsAbsolute(abs))
                return;

            string[] names;
            using (var dir = DirAccess.Open(abs))
            {
                if (dir == null)
                    return;
                names = dir.GetFiles();
            }

            foreach (var name in names)
            {
                if (name is "." or "..")
                    continue;
                if (!name.EndsWith(".json", StringComparison.Ordinal))
                    continue;

                var fullUser = $"{runDirUser.TrimEnd('/')}/{name}";
                if (ModAccountRelativePath.TryGetRelativeAccountPath(fullUser, out var rel))
                    sink.Add(rel);
            }
        }

        internal static bool IsActiveRunSidecarRelativeAccountPath(string relativeAccountPath, int activeProfileId)
        {
            if (!ModRunSidecarFingerprint.TryGetLive(out var fp) || fp.ProfileId != activeProfileId)
                return false;
            if (!TryGetRunSidecarPackDirectoryUserPath(out var runDirUser))
                return false;
            if (!ModAccountRelativePath.TryGetRelativeAccountPath(runDirUser.TrimEnd('/'), out var runDirRel))
                return false;

            var prefix = runDirRel + "/";
            if (!relativeAccountPath.StartsWith(prefix, StringComparison.Ordinal) ||
                relativeAccountPath.Length <= prefix.Length)
                return false;

            var remainder = relativeAccountPath[prefix.Length..];
            return remainder.Length > 0 && !remainder.Contains('/') &&
                   remainder.EndsWith(".json", StringComparison.Ordinal);
        }

        /// <summary>
        ///     Attempts to read the sidecar for <paramref name="modId" /> for the active run.
        ///     尝试读取活动跑局中 <paramref name="modId" /> 的 sidecar。
        /// </summary>
        /// <typeparam name="TModel">
        ///     Settings DTO type; must deserialize from JSON with a parameterless constructor.
        ///     设置 DTO 类型；必须能通过无参构造函数从 JSON 反序列化。
        /// </typeparam>
        /// <param name="modId">
        ///     Owning mod id; used in the file path after sanitization.
        ///     所属 mod id；清理后用于文件路径。
        /// </param>
        /// <param name="model">
        ///     On success, the deserialized settings; otherwise a new instance or default.
        ///     成功时为反序列化后的设置；否则为新实例或默认值。
        /// </param>
        /// <returns>
        ///     Status describing why read succeeded or failed.
        /// </returns>
        public static ModRunSidecarReadStatus TryReadModel<TModel>(string modId, out TModel model)
            where TModel : class, new()
        {
            model = new();
            if (!ModRunSidecarFingerprint.TryGetLive(out var live))
                return ModRunSidecarReadStatus.NoActiveRun;

            lock (FileLock)
            {
                var path = ResolvePath(modId, live);
                if (!FileAccess.FileExists(path))
                    return ModRunSidecarReadStatus.MissingFile;

                try
                {
                    var read = FileOperations.ReadText(path, "RunSidecar");
                    if (!read.Success || read.Content == null)
                        return ModRunSidecarReadStatus.InvalidJson;

                    var envelope = JsonSerializer.Deserialize<ModRunSidecarEnvelope<TModel>>(read.Content, JsonOptions);
                    if (envelope?.Fingerprint == null || envelope.EnvelopeVersion != EnvelopeVersion)
                        return ModRunSidecarReadStatus.InvalidJson;

                    var stored = ModRunSidecarFingerprint.FromDto(envelope.Fingerprint);
                    if (!stored.EqualsFully(live))
                        return ModRunSidecarReadStatus.FingerprintMismatch;

                    model = envelope.Settings ?? new TModel();
                    return ModRunSidecarReadStatus.Ok;
                }
                catch
                {
                    return ModRunSidecarReadStatus.InvalidJson;
                }
            }
        }

        /// <summary>
        ///     Writes <paramref name="model" /> for the active run after validating the live fingerprint.
        ///     校验实时指纹后，为活动跑局写入 <paramref name="model" />。
        /// </summary>
        /// <typeparam name="TModel">
        ///     Settings DTO type serialized into the envelope.
        ///     序列化到信封中的设置 DTO 类型。
        /// </typeparam>
        /// <param name="modId">
        ///     Owning mod id; used in the file path after sanitization.
        ///     所属 mod id；清理后用于文件路径。
        /// </param>
        /// <param name="model">
        ///     Settings payload stored under the current run fingerprint.
        ///     存储在当前跑局指纹下的设置载荷。
        /// </param>
        /// <returns>
        ///     True when the file was written; false when no active run or I/O failed.
        ///     文件已写入时为 true；没有活动跑局或 I/O 失败时为 false。
        /// </returns>
        public static bool TryWriteModel<TModel>(string modId, TModel model) where TModel : class, new()
        {
            if (!ModRunSidecarFingerprint.TryGetLive(out var live))
                return false;

            lock (FileLock)
            {
                var path = ResolvePath(modId, live);
                var runDir = ResolveRunDirectoryUserPath(live);

                var envelope = new ModRunSidecarEnvelope<TModel>
                {
                    EnvelopeVersion = EnvelopeVersion,
                    Fingerprint = live.ToDto(),
                    Settings = model,
                };

                var json = JsonSerializer.Serialize(envelope, JsonOptions);
                var write = FileOperations.WriteText(path, json, "RunSidecar");
                if (write.Success)
                {
                    ModDataCloudMirror.MirrorLocalFileAfterWriteIfEnabled(path);
                    return true;
                }

                TryRemoveRunDirectoryIfWithoutSidecarJson(runDir);
                return false;
            }
        }

        /// <summary>
        ///     Deletes the on-disk per-run sidecar directory for <paramref name="fingerprint" /> (best-effort).
        ///     删除 <paramref name="fingerprint" /> 对应的磁盘上每跑局 sidecar 目录（尽力而为）。
        /// </summary>
        /// <remarks>
        ///     Called when a run ends so local sidecar data does not outlive the run instance. Uses the same path
        ///     layout as reads/writes for that fingerprint.
        ///     跑局结束时调用，确保本地 sidecar 数据不会比跑局实例存活更久。使用与该指纹读写相同的路径布局。
        /// </remarks>
        internal static void TryDeleteRunDirectoryForFingerprint(ModRunSidecarFingerprint fingerprint)
        {
            lock (FileLock)
            {
                var runDir = ResolveRunDirectoryUserPath(fingerprint);
                var abs = ProjectSettings.GlobalizePath(runDir);
                if (DirAccess.DirExistsAbsolute(abs))
                    _ = FileOperations.DeleteDirectoryRecursive(abs, "RunSidecarRunEnded");
            }
        }

        /// <summary>
        ///     Deletes all sidecar files for a profile directory (used when a profile is deleted).
        ///     删除档案目录中的所有 sidecar 文件（删除档案时使用）。
        /// </summary>
        internal static void TryDeleteAllForProfile(int profileId)
        {
            if (profileId < 0)
                return;

            lock (FileLock)
            {
                var dir = $"{ProfileManager.GetBasePath(SaveScope.Profile, profileId)}/{RunSidecarSegment}";
                var abs = ProjectSettings.GlobalizePath(dir);
                if (DirAccess.DirExistsAbsolute(abs))
                    FileOperations.DeleteDirectoryRecursive(abs, "RunSidecar");
            }
        }

        private static string ResolveRunDirectoryUserPath(ModRunSidecarFingerprint live)
        {
            var stem = live.ComputeFileStem();
            if (string.IsNullOrEmpty(stem))
                throw new InvalidOperationException("Run sidecar fingerprint stem must not be empty.");

            return StoragePathResolver.ResolveBasePathUser(Const.ModId, SaveScope.RunSidecar,
                StorageContext.Empty
                    .With(StorageContextKeys.ProfileId, live.ProfileId)
                    .With(StorageContextKeys.RunFingerprintStem, stem));
        }

        private static string ResolvePath(string modId, ModRunSidecarFingerprint live)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            var safeMod = SanitizePathSegment(modId);
            var runDir = ResolveRunDirectoryUserPath(live);
            return $"{runDir}/{safeMod}.json";
        }

        /// <summary>
        ///     Removes the per-run folder when it contains no committed <c>*.json</c> sidecar (e.g. only <c>.tmp</c> /
        ///     <c>.backup</c> left after a failed atomic write).
        ///     当每跑局文件夹不包含已提交的 <c>*.json</c> sidecar 时将其移除（例如原子写入失败后只剩 <c>.tmp</c> / <c>.backup</c>）。
        /// </summary>
        private static void TryRemoveRunDirectoryIfWithoutSidecarJson(string runDirectoryUserPath)
        {
            try
            {
                var abs = ProjectSettings.GlobalizePath(runDirectoryUserPath);
                if (!DirAccess.DirExistsAbsolute(abs))
                    return;

                string[] names;
                using (var dir = DirAccess.Open(abs))
                {
                    if (dir == null)
                        return;
                    names = dir.GetFiles();
                }

                if (names.Where(name => name is not ("." or "..")).Any(name =>
                        name.EndsWith(".json", StringComparison.Ordinal) &&
                        !name.EndsWith(".tmp", StringComparison.Ordinal)))
                    return;

                _ = FileOperations.DeleteDirectoryRecursive(abs, "RunSidecarPrune");
            }
            catch
            {
                // best-effort: avoid leaving empty or junk-only run folders
            }
        }

        private static string SanitizePathSegment(string modId)
        {
            var chars = modId
                .Select(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-' ? ch : '_')
                .ToArray();
            var s = new string(chars).Trim('_');
            return string.IsNullOrEmpty(s) ? "mod" : s;
        }
    }
}
