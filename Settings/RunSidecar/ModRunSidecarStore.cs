using System.Text.Json;
using Godot;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;
using FileAccess = Godot.FileAccess;

namespace STS2RitsuLib.Settings.RunSidecar
{
    /// <summary>
    ///     Reads and writes per-mod run sidecar JSON under the framework mod’s profile-scoped storage tree (see
    ///     <see cref="ProfileManager" /> base paths for <see cref="SaveScope.Profile" /> and <see cref="Const.ModId" />;
    ///     Godot <c>user://</c>, resolve with <see cref="ProjectSettings.GlobalizePath(string)" />). Client-local
    ///     only; not part of vanilla save sync.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Layout under the framework profile base (<see cref="Const.ModId" />):
    ///         <c>{ProfileBase}/{RunSidecarSegment}/{fingerprintStem}/{sanitizedModId}.json</c>.
    ///         <c>RunSidecarSegment</c> is <c>run_sidecar/v1</c>; <c>fingerprintStem</c> is the lowercase hex SHA-256 from
    ///         <see cref="ModRunSidecarFingerprint" /> (one folder per run). Each consumer mod owns one JSON file in
    ///         that folder; the folder is removed when the run ends or when vanilla deletes the current run save file
    ///         (see <see cref="ModRunSidecarSession" />). Writes
    ///         use atomic replace via <see cref="FileOperations.WriteText" />;
    ///         if a write fails before any durable <c>*.json</c> exists, the empty run folder is removed best-effort.
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
        /// </summary>
        /// <param name="runDirectoryUserPath">Godot <c>user://</c> path ending at the run directory, or empty when false.</param>
        /// <returns>False when no active run fingerprint is available.</returns>
        public static bool TryGetRunSidecarPackDirectoryUserPath(out string runDirectoryUserPath)
        {
            runDirectoryUserPath = string.Empty;
            if (!ModRunSidecarFingerprint.TryGetLive(out var live))
                return false;

            runDirectoryUserPath = ResolveRunDirectoryUserPath(live);
            return true;
        }

        /// <summary>
        ///     Attempts to read the sidecar for <paramref name="modId" /> for the active run.
        /// </summary>
        /// <typeparam name="TModel">Settings DTO type; must deserialize from JSON with a parameterless constructor.</typeparam>
        /// <param name="modId">Owning mod id; used in the file path after sanitization.</param>
        /// <param name="model">On success, the deserialized settings; otherwise a new instance or default.</param>
        /// <returns>Status describing why read succeeded or failed.</returns>
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
        /// </summary>
        /// <typeparam name="TModel">Settings DTO type serialized into the envelope.</typeparam>
        /// <param name="modId">Owning mod id; used in the file path after sanitization.</param>
        /// <param name="model">Settings payload stored under the current run fingerprint.</param>
        /// <returns>True when the file was written; false when no active run or I/O failed.</returns>
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
                    return true;

                TryRemoveRunDirectoryIfWithoutSidecarJson(runDir);
                return false;
            }
        }

        /// <summary>
        ///     Deletes the on-disk per-run sidecar directory for <paramref name="fingerprint" /> (best-effort).
        /// </summary>
        /// <remarks>
        ///     Called when a run ends so local sidecar data does not outlive the run instance. Uses the same path
        ///     layout as reads/writes for that fingerprint.
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

            var profileBase = ProfileManager.GetBasePath(SaveScope.Profile, live.ProfileId);
            return $"{profileBase}/{RunSidecarSegment}/{stem}";
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
