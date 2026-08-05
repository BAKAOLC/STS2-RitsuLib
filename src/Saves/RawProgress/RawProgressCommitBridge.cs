using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Migrations;
using MegaCrit.Sts2.Core.Saves.Validation;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Saves.RawProgress
{
    internal sealed class RawProgressCommitBridge : IRawProgressCommitBridge
    {
        internal const int ProtocolVersion = 1;
        internal const long MaxDocumentUtf8Bytes = 16 * 1024 * 1024;
        private const int MaxRetainedRecoveryJournals = 8;
        private const string RecoveryLogContext = "RawProgressRecovery";
        private const string RecoveryDirectoryName = "recovery/raw-progress";

#if STS2_AT_LEAST_0_110_0
        internal const int SupportedSchema = 24;
#elif STS2_AT_LEAST_0_108_0
        internal const int SupportedSchema = 22;
#else
        internal const int SupportedSchema = 21;
#endif

        private static readonly object SaveWindow = new();
        private static readonly UTF8Encoding StrictUtf8 = new(false, true);
        private static readonly FrozenSet<int> SupportedSchemas = new[] { SupportedSchema }.ToFrozenSet();
        private static readonly Dictionary<Guid, CompletedTransaction> CompletedTransactions = [];
        private static readonly Queue<Guid> CompletedTransactionOrder = [];

        private static readonly JsonSerializerOptions JournalJsonOptions = new()
        {
            WriteIndented = true,
        };

        [ThreadStatic] private static bool _isPreparingCommitProjection;

        private readonly RawProgressBridgeDescriptor _descriptor;

        private RawProgressCommitBridge()
        {
            var features = RawProgressBridgeFeature.UnknownJsonPassThrough |
                           RawProgressBridgeFeature.LiveGameStoreCommit |
                           RawProgressBridgeFeature.DurableLocalReplacement |
                           RawProgressBridgeFeature.CloudSaveBatch |
                           RawProgressBridgeFeature.ConditionalGenerationCheck |
                           RawProgressBridgeFeature.ExclusiveSaveWindow |
                           RawProgressBridgeFeature.LocalOnlyRecoveryJournal |
                           RawProgressBridgeFeature.StructuredRecoveryOutcome |
                           RawProgressBridgeFeature.StablePublicContract |
                           RawProgressBridgeFeature.CloudReadBackVerification |
                           RawProgressBridgeFeature.LiveProgressStateSynchronization |
                           RawProgressBridgeFeature.SubsequentSaveUnknownJsonPreservation |
                           RawProgressBridgeFeature.ActiveProgressSnapshot;

#if !STS2_AT_LEAST_0_108_0
            features |= RawProgressBridgeFeature.RawSchema21Document;
#endif

            _descriptor = new()
            {
                ProviderId = Const.ModId,
                ProviderVersion = Version.Parse(Const.Version),
                ProtocolVersion = ProtocolVersion,
                SupportedSchemas = SupportedSchemas,
                Features = features,
                MaxDocumentUtf8Bytes = MaxDocumentUtf8Bytes,
            };
        }

        internal static RawProgressCommitBridge Instance { get; } = new();

        internal static bool IsPreparingCommitProjection => _isPreparingCommitProjection;

        public RawProgressBridgeDescriptor Describe()
        {
            return _descriptor;
        }

        public ValueTask<RawProgressReadResult> CaptureAsync(CancellationToken cancellationToken = default)
        {
            return new(RitsuMainThread.InvokeAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                lock (SaveWindow)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return CaptureCore().Result;
                }
            }, cancellationToken));
        }

        public ValueTask<RawProgressCommitResult> CommitAsync(
            RawProgressCommitRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new(RitsuMainThread.InvokeAsync(() => CommitOnMainThread(request, cancellationToken)));
        }

        internal static void SaveOrdinaryProgress(ProgressSaveManager manager)
        {
            ArgumentNullException.ThrowIfNull(manager);

            lock (SaveWindow)
            {
                try
                {
                    var progress = manager.Progress;
                    var serializable = progress.ToSerializable();
                    serializable.SchemaVersion = MigrationManager(manager).GetLatestVersion<SerializableProgress>();
                    var knownJson = JsonSerializationUtility.ToJson(serializable);
                    var content = RawProgressJsonPreservation.PreserveAndAdvance(progress, knownJson);
                    SaveStore(manager).WriteFile(
                        ProgressSaveManager.GetProgressPathForProfile(ProfileIdProvider(manager).CurrentProfileId),
                        content);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to save progress: {ex}");
                    SentryService.CaptureException(ex);
                }
            }
        }

        internal static LoadCapture? BeginProgressLoad(ProgressSaveManager manager)
        {
            Monitor.Enter(SaveWindow);
            try
            {
                var profileId = ProfileIdProvider(manager).CurrentProfileId;
                var path = ProgressSaveManager.GetProgressPathForProfile(profileId);
                var rawJson = SaveStore(manager).ReadFile(path);
                return string.IsNullOrWhiteSpace(rawJson) ? null : new(rawJson);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RawProgress] Could not capture the raw document before load: {ex.Message}");
                return null;
            }
        }

        internal static void CompleteProgressLoad(
            ProgressSaveManager manager,
            ReadSaveResult<SerializableProgress> result,
            LoadCapture? capture)
        {
            try
            {
                if (capture == null || result is not { Success: true, SaveData: not null })
                    return;

                if (!TryReadSchema(capture.RawJson, out var rawSchema) || rawSchema != SupportedSchema)
                    return;

                var rawReadResult = JsonSerializationUtility.FromJson<SerializableProgress>(capture.RawJson);
                if (rawReadResult is not { Success: true, SaveData: not null })
                    return;

                rawReadResult.SaveData.SchemaVersion = SupportedSchema;
                var knownJson = JsonSerializationUtility.ToJson(rawReadResult.SaveData);
                if (!RawProgressJsonPreservation.TryAttach(manager.Progress, capture.RawJson, knownJson))
                    RitsuLibFramework.Logger.Warn(
                        "[RawProgress] The loaded document contains unknown properties that could not be matched safely; " +
                        "ordinary saves will use the known game projection.");
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RawProgress] Failed to install preservation state after progress load: {ex.Message}");
            }
        }

        internal static void EndProgressLoad()
        {
            Monitor.Exit(SaveWindow);
        }

        internal static void EnterProfileMutation()
        {
            Monitor.Enter(SaveWindow);
        }

        internal static void ExitProfileMutation()
        {
            Monitor.Exit(SaveWindow);
        }

        private static RawProgressCommitResult CommitOnMainThread(
            RawProgressCommitRequest request,
            CancellationToken cancellationToken)
        {
            if (request.ProtocolVersion != ProtocolVersion)
                return CreateResult(RawProgressCommitOutcome.ProviderIncompatible);
            if (request.SchemaVersion != SupportedSchema)
                return CreateResult(RawProgressCommitOutcome.SchemaUnsupported);
            if (cancellationToken.IsCancellationRequested)
                return CreateResult(RawProgressCommitOutcome.CancelledBeforeCommit);

            if (!TryValidateProposedDocument(request, out var proposed))
                return CreateResult(RawProgressCommitOutcome.ValidationFailed);
            if (cancellationToken.IsCancellationRequested)
                return CreateResult(RawProgressCommitOutcome.CancelledBeforeCommit);

            lock (SaveWindow)
            {
                if (TryGetCompletedTransaction(request, out var completed))
                    return completed;
                if (cancellationToken.IsCancellationRequested)
                    return Remember(request, CreateResult(RawProgressCommitOutcome.CancelledBeforeCommit));

                var current = CaptureCore();
                if (current.Result.Outcome != RawProgressReadOutcome.Succeeded || current.Result.Snapshot == null)
                    return Remember(request, MapCaptureFailure(current.Result.Outcome));

                var snapshot = current.Result.Snapshot;
                if (snapshot.Generation.ProfileId != request.ExpectedGeneration.ProfileId ||
                    snapshot.Generation.IsModded != request.ExpectedGeneration.IsModded)
                    return Remember(request, CreateResult(RawProgressCommitOutcome.ActiveProfileChanged));
                if (!GenerationMatches(snapshot.Generation, request.ExpectedGeneration))
                    return Remember(request, CreateResult(RawProgressCommitOutcome.GenerationConflict));
                if (!string.Equals(proposed.ProgressUniqueId, snapshot.Generation.ProgressUniqueId,
                        StringComparison.Ordinal))
                    return Remember(request, CreateResult(RawProgressCommitOutcome.ValidationFailed));

                var journal = RecoveryJournal.Create(request, snapshot.RawJson);
                if (!journal.TryPrepare())
                    return Remember(request, CreateResult(
                        RawProgressCommitOutcome.RecoveryRequired,
                        recoveryJournalRetained: journal.Exists));
                if (cancellationToken.IsCancellationRequested)
                {
                    var removed = journal.TryDelete();
                    return Remember(request, CreateResult(
                        RawProgressCommitOutcome.CancelledBeforeCommit,
                        verifiedBackupAvailable: !removed,
                        recoveryJournalRetained: !removed));
                }

                return Remember(request, CommitPrepared(request, proposed, current, journal));
            }
        }

        private static RawProgressCommitResult CommitPrepared(
            RawProgressCommitRequest request,
            ValidatedDocument proposed,
            CapturedProgress current,
            RecoveryJournal journal)
        {
            var destinationMayHaveChanged = false;
            var localVerified = false;
            var batchFailure = false;
            var localReadBackHash = (string?)null;
            var cloudStatus = CloudReadBackStatus.NotRequired;
            var saveManager = current.SaveManager!;
            var saveStore = current.Store!;
            var localStore = GetLocalStore(saveStore);
            var path = current.Path!;
            SaveBatchScope? batch = null;

            try
            {
                batch = saveManager.BeginSaveBatch();
                destinationMayHaveChanged = true;
                saveStore.WriteFile(path, request.ProposedRawJson);
                var localReadBack = localStore.ReadFile(path);
                if (localReadBack != null)
                {
                    localReadBackHash = ComputeSha256(localReadBack);
                    localVerified = HashEquals(localReadBackHash, request.ProposedSha256);
                }
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[RawProgress] Commit write failed: {ex.Message}");
            }
            finally
            {
                try
                {
                    batch?.Dispose();
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    batchFailure = true;
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[RawProgress] Failed to end cloud save batch: {ex.Message}");
                }
            }

            var gameBackupVerified = VerifyGameBackup(localStore, path, current.Result.Snapshot!.RawJson);
            if (!localVerified)
            {
                journal.TryUpdate("local_unverified", localReadBackHash, null, null);
                return CreateResult(
                    RawProgressCommitOutcome.LocalReplacementUnverified,
                    localReadBackSha256: localReadBackHash,
                    cloudStatus: batchFailure ? CloudReadBackStatus.FailureObserved : cloudStatus,
                    destinationMayHaveChanged: destinationMayHaveChanged,
                    verifiedBackupAvailable: gameBackupVerified || journal.Exists,
                    recoveryJournalRetained: journal.Exists);
            }

            journal.TryUpdate("local_verified", localReadBackHash, null, null);
            var cloudReadBack = VerifyCloudReadBack(saveStore, path, request.ProposedSha256, batchFailure);
            cloudStatus = cloudReadBack.Status;
            var cloudReadBackHash = cloudReadBack.Hash;

            var liveProjectionHash = (string?)null;
            var continuationInstalled = false;
            try
            {
                saveManager.Progress = proposed.Progress;
                continuationInstalled = RawProgressJsonPreservation.TryAttach(
                    proposed.Progress,
                    request.ProposedRawJson,
                    proposed.KnownProjectionJson);

                var liveSerializable = saveManager.Progress.ToSerializable();
                liveSerializable.SchemaVersion = SupportedSchema;
                liveProjectionHash = ComputeSha256(JsonSerializationUtility.ToJson(liveSerializable));
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[RawProgress] Failed to synchronize the live progress projection: {ex.Message}");
            }

            RawProgressCommitOutcome outcome;
            if (!HashEquals(liveProjectionHash, proposed.KnownProjectionSha256))
                outcome = RawProgressCommitOutcome.LiveProgressStateUnverified;
            else if (!continuationInstalled)
                outcome = RawProgressCommitOutcome.UnknownJsonContinuationUnverified;
            else if (cloudStatus is CloudReadBackStatus.Unavailable or CloudReadBackStatus.FailureObserved)
                outcome = RawProgressCommitOutcome.CloudReadBackUnverifiedLocalPreserved;
            else if (cloudStatus == CloudReadBackStatus.Mismatch)
                outcome = RawProgressCommitOutcome.CloudReadBackMismatchLocalPreserved;
            else
                outcome = RawProgressCommitOutcome.CommittedVerified;

            if (outcome == RawProgressCommitOutcome.CommittedVerified)
            {
                if (!journal.TryDelete())
                    outcome = RawProgressCommitOutcome.RecoveryRequired;
            }
            else
            {
                journal.TryUpdate("verification_incomplete", localReadBackHash, cloudReadBackHash,
                    liveProjectionHash);
            }

            return CreateResult(
                outcome,
                localReadBackHash,
                cloudStatus,
                cloudReadBackHash,
                liveProjectionHash,
                continuationInstalled,
                continuationInstalled ? request.ProposedSha256 : null,
                destinationMayHaveChanged,
                gameBackupVerified || journal.Exists,
                journal.Exists);
        }

        private static CapturedProgress CaptureCore()
        {
            SaveManager saveManager;
            int profileId;
            try
            {
                saveManager = SaveManager.Instance;
                profileId = saveManager.CurrentProfileId;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Debug($"[RawProgress] Active profile is unavailable: {ex.Message}");
                return new(new() { Outcome = RawProgressReadOutcome.ActiveProfileUnavailable });
            }

            var saveStore = SaveStore(saveManager);
            var localStore = GetLocalStore(saveStore);
            var isModded = UserDataPathProvider.IsRunningModded;
            var path = GetProgressPath(profileId, isModded);
            string? rawJson;
            long localModified;
            int localStoredLength;
            try
            {
                if (!localStore.FileExists(path))
                    return new(new() { Outcome = RawProgressReadOutcome.LocalReadUnavailable });
                var modifiedBeforeRead = localStore.GetLastModifiedTime(path).UtcTicks;
                rawJson = localStore.ReadFile(path);
                localStoredLength = localStore.GetFileSize(path);
                localModified = localStore.GetLastModifiedTime(path).UtcTicks;
                if (modifiedBeforeRead != localModified)
                    return new(new() { Outcome = RawProgressReadOutcome.LocalReadUnavailable });
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[RawProgress] Failed to read active local progress: {ex.Message}");
                return new(new() { Outcome = RawProgressReadOutcome.LocalReadUnavailable });
            }

            if (!TryInspectExistingDocument(rawJson, out var schema, out var uniqueId, out var localBytes))
                return new(new() { Outcome = RawProgressReadOutcome.ValidationFailed });
            if (schema != SupportedSchema)
                return new(new() { Outcome = RawProgressReadOutcome.SchemaUnsupported });

            try
            {
                var localHash = ComputeSha256(localBytes);
                if (localStoredLength != localBytes.Length)
                    return new(new() { Outcome = RawProgressReadOutcome.LocalReadUnavailable });
                var cloudAvailable = saveStore is CloudSaveStore;
                var cloudSyncEnabled = false;
                var cloudPersisted = false;
                string? cloudHash = null;
                long? cloudLength = null;
                long? cloudModified = null;

                if (saveStore is CloudSaveStore cloudSaveStore)
                {
                    var cloudStore = cloudSaveStore.CloudStore;
                    cloudSyncEnabled = cloudSaveStore.HasUserEnabledCloudSync();
                    cloudPersisted = cloudStore.IsFilePersisted(path);
                    if (cloudPersisted)
                    {
                        var cloudModifiedBeforeRead = cloudStore.GetLastModifiedTime(path).UtcTicks;
                        var cloudJson = cloudStore.ReadFile(path);
                        if (cloudJson == null || !TryGetUtf8Bytes(cloudJson, out var cloudBytes))
                            return new(new() { Outcome = RawProgressReadOutcome.CloudReadUnavailable });

                        var cloudStoredLength = cloudStore.GetFileSize(path);
                        var cloudModifiedAfterRead = cloudStore.GetLastModifiedTime(path).UtcTicks;
                        if (cloudModifiedBeforeRead != cloudModifiedAfterRead || cloudStoredLength != cloudBytes.Length)
                            return new(new() { Outcome = RawProgressReadOutcome.CloudReadUnavailable });

                        cloudHash = ComputeSha256(cloudBytes);
                        cloudLength = cloudBytes.LongLength;
                        cloudModified = cloudModifiedAfterRead;
                    }
                }

                var generation = new ProgressGeneration
                {
                    ProfileId = profileId,
                    IsModded = isModded,
                    ProgressUniqueId = uniqueId,
                    LocalSha256 = localHash,
                    LocalLength = localBytes.LongLength,
                    LocalLastModifiedUtcTicks = localModified,
                    CloudAvailable = cloudAvailable,
                    CloudSyncEnabled = cloudSyncEnabled,
                    CloudPersisted = cloudPersisted,
                    CloudSha256 = cloudHash,
                    CloudLength = cloudLength,
                    CloudLastModifiedUtcTicks = cloudModified,
                };
                var snapshot = new RawProgressSnapshot
                {
                    SchemaVersion = schema,
                    RawJson = rawJson!,
                    Generation = generation,
                };
                return new(
                    new() { Outcome = RawProgressReadOutcome.Succeeded, Snapshot = snapshot },
                    saveManager,
                    saveStore,
                    path);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[RawProgress] Failed to capture destination generation: {ex.Message}");
                return new(new() { Outcome = RawProgressReadOutcome.CloudReadUnavailable });
            }
        }

        private static bool TryValidateProposedDocument(
            RawProgressCommitRequest request,
            out ValidatedDocument proposed)
        {
            proposed = null!;
            if (request.TransactionId == Guid.Empty ||
                request.ExpectedGeneration == null ||
                request.ProposedRawJson == null ||
                request.ProposedSha256 == null ||
                request.ExpectedGeneration.ProfileId is < 1 or > 3 ||
                string.IsNullOrWhiteSpace(request.ExpectedGeneration.ProgressUniqueId) ||
                !IsSha256(request.ProposedSha256) ||
                !IsSha256(request.ExpectedGeneration.LocalSha256) ||
                request.ProposedRawJson.Length > MaxDocumentUtf8Bytes ||
                !TryGetUtf8Bytes(request.ProposedRawJson, out var proposedBytes) ||
                proposedBytes.LongLength == 0 ||
                proposedBytes.LongLength > MaxDocumentUtf8Bytes ||
                proposedBytes.LongLength != request.ProposedUtf8Length ||
                !HashEquals(ComputeSha256(proposedBytes), request.ProposedSha256) ||
                !TryValidateJsonShape(request.ProposedRawJson, out var schema) ||
                schema != request.SchemaVersion)
                return false;

            var readResult = JsonSerializationUtility.FromJson<SerializableProgress>(request.ProposedRawJson);
            if (readResult is not { Success: true, SaveData: not null } ||
                readResult.SaveData.SchemaVersion != request.SchemaVersion ||
                string.IsNullOrWhiteSpace(readResult.SaveData.UniqueId) ||
                !string.Equals(readResult.SaveData.UniqueId, request.ExpectedGeneration.ProgressUniqueId,
                    StringComparison.Ordinal))
                return false;

            try
            {
                _isPreparingCommitProjection = true;
                var context = new DeserializationContext();
                var progress = ProgressState.FromSerializable(readResult.SaveData, context);
                if (context.Errors.Any(static error => error.IsFatal))
                    return false;

                var knownProjection = progress.ToSerializable();
                knownProjection.SchemaVersion = request.SchemaVersion;
                var knownProjectionJson = JsonSerializationUtility.ToJson(knownProjection);
                proposed = new(
                    readResult.SaveData.UniqueId,
                    progress,
                    knownProjectionJson,
                    ComputeSha256(knownProjectionJson));
                return true;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[RawProgress] Proposed document validation failed: {ex.Message}");
                return false;
            }
            finally
            {
                _isPreparingCommitProjection = false;
            }
        }

        private static bool TryInspectExistingDocument(
            string? rawJson,
            out int schema,
            out string uniqueId,
            out byte[] utf8Bytes)
        {
            schema = 0;
            uniqueId = string.Empty;
            utf8Bytes = [];
            if (string.IsNullOrWhiteSpace(rawJson) ||
                rawJson.Length > MaxDocumentUtf8Bytes ||
                !TryGetUtf8Bytes(rawJson, out utf8Bytes) ||
                utf8Bytes.LongLength > MaxDocumentUtf8Bytes ||
                !TryValidateJsonShape(rawJson, out schema))
                return false;

            var readResult = JsonSerializationUtility.FromJson<SerializableProgress>(rawJson);
            if (readResult is not { Success: true, SaveData: not null } ||
                readResult.SaveData.SchemaVersion != schema ||
                string.IsNullOrWhiteSpace(readResult.SaveData.UniqueId))
                return false;

            uniqueId = readResult.SaveData.UniqueId;
            return true;
        }

        private static bool TryValidateJsonShape(string json, out int schema)
        {
            schema = 0;
            try
            {
                using var document = JsonDocument.Parse(json, new()
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 128,
                });
                if (document.RootElement.ValueKind != JsonValueKind.Object ||
                    !document.RootElement.TryGetProperty("schema_version", out var schemaElement) ||
                    !schemaElement.TryGetInt32(out schema) ||
                    schema < 1)
                    return false;

                return HasUniquePropertyNames(document.RootElement);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool HasUniquePropertyNames(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                {
                    var propertyNames = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var property in element.EnumerateObject())
                        if (!propertyNames.Add(property.Name) || !HasUniquePropertyNames(property.Value))
                            return false;
                    break;
                }
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                        if (!HasUniquePropertyNames(item))
                            return false;
                    break;
            }

            return true;
        }

        private static bool TryReadSchema(string json, out int schema)
        {
            return TryValidateJsonShape(json, out schema);
        }

        private static bool GenerationMatches(ProgressGeneration actual, ProgressGeneration expected)
        {
            return actual.ProfileId == expected.ProfileId &&
                   actual.IsModded == expected.IsModded &&
                   string.Equals(actual.ProgressUniqueId, expected.ProgressUniqueId, StringComparison.Ordinal) &&
                   HashEquals(actual.LocalSha256, expected.LocalSha256) &&
                   actual.LocalLength == expected.LocalLength &&
                   actual.LocalLastModifiedUtcTicks == expected.LocalLastModifiedUtcTicks &&
                   actual.CloudAvailable == expected.CloudAvailable &&
                   actual.CloudSyncEnabled == expected.CloudSyncEnabled &&
                   actual.CloudPersisted == expected.CloudPersisted &&
                   HashEquals(actual.CloudSha256, expected.CloudSha256) &&
                   actual.CloudLength == expected.CloudLength &&
                   actual.CloudLastModifiedUtcTicks == expected.CloudLastModifiedUtcTicks;
        }

        private static (CloudReadBackStatus Status, string? Hash) VerifyCloudReadBack(
            ISaveStore saveStore,
            string path,
            string expectedHash,
            bool batchFailure)
        {
            if (saveStore is not CloudSaveStore cloudSaveStore)
                return (CloudReadBackStatus.NotRequired, null);
            if (batchFailure)
                return (CloudReadBackStatus.FailureObserved, null);

            try
            {
                var cloudStore = cloudSaveStore.CloudStore;
                if (!cloudStore.IsFilePersisted(path))
                    return (CloudReadBackStatus.Unavailable, null);

                var cloudJson = cloudStore.ReadFile(path);
                if (cloudJson == null || !TryGetUtf8Bytes(cloudJson, out var cloudBytes))
                    return (CloudReadBackStatus.Unavailable, null);

                var cloudHash = ComputeSha256(cloudBytes);
                return HashEquals(cloudHash, expectedHash)
                    ? (CloudReadBackStatus.Succeeded, cloudHash)
                    : (CloudReadBackStatus.Mismatch, cloudHash);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[RawProgress] Cloud read-back failed: {ex.Message}");
                return (CloudReadBackStatus.FailureObserved, null);
            }
        }

        private static bool VerifyGameBackup(ISaveStore localStore, string path, string originalRawJson)
        {
            try
            {
                var backup = localStore.ReadFile(path + ".backup");
                return backup != null && HashEquals(ComputeSha256(backup), ComputeSha256(originalRawJson));
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[RawProgress] Could not verify the game backup: {ex.Message}");
                return false;
            }
        }

        private static RawProgressCommitResult MapCaptureFailure(RawProgressReadOutcome outcome)
        {
            return outcome switch
            {
                RawProgressReadOutcome.ActiveProfileUnavailable =>
                    CreateResult(RawProgressCommitOutcome.ActiveProfileChanged),
                RawProgressReadOutcome.SchemaUnsupported =>
                    CreateResult(RawProgressCommitOutcome.SchemaUnsupported),
                _ => CreateResult(RawProgressCommitOutcome.GenerationConflict),
            };
        }

        private static RawProgressCommitResult CreateResult(
            RawProgressCommitOutcome outcome,
            string? localReadBackSha256 = null,
            CloudReadBackStatus cloudStatus = CloudReadBackStatus.NotRequired,
            string? cloudReadBackSha256 = null,
            string? liveKnownProjectionSha256 = null,
            bool unknownJsonContinuationInstalled = false,
            string? preservedRawSha256 = null,
            bool destinationMayHaveChanged = false,
            bool verifiedBackupAvailable = false,
            bool recoveryJournalRetained = false)
        {
            return new()
            {
                Outcome = outcome,
                LocalReadBackSha256 = localReadBackSha256,
                CloudStatus = cloudStatus,
                CloudReadBackSha256 = cloudReadBackSha256,
                LiveKnownProjectionSha256 = liveKnownProjectionSha256,
                UnknownJsonContinuationInstalled = unknownJsonContinuationInstalled,
                PreservedRawSha256 = preservedRawSha256,
                DestinationMayHaveChanged = destinationMayHaveChanged,
                VerifiedBackupAvailable = verifiedBackupAvailable,
                RecoveryJournalRetained = recoveryJournalRetained,
            };
        }

        private static bool TryGetCompletedTransaction(
            RawProgressCommitRequest request,
            out RawProgressCommitResult result)
        {
            if (!CompletedTransactions.TryGetValue(request.TransactionId, out var completed))
            {
                result = null!;
                return false;
            }

            result = HashEquals(completed.ProposedSha256, request.ProposedSha256)
                ? completed.Result
                : CreateResult(RawProgressCommitOutcome.ValidationFailed);
            return true;
        }

        private static RawProgressCommitResult Remember(
            RawProgressCommitRequest request,
            RawProgressCommitResult result)
        {
            if (!CompletedTransactions.ContainsKey(request.TransactionId))
            {
                CompletedTransactions.Add(request.TransactionId, new(request.ProposedSha256, result));
                CompletedTransactionOrder.Enqueue(request.TransactionId);
                while (CompletedTransactionOrder.Count > 256)
                    CompletedTransactions.Remove(CompletedTransactionOrder.Dequeue());
            }

            return result;
        }

        private static ISaveStore GetLocalStore(ISaveStore saveStore)
        {
            return saveStore is CloudSaveStore cloudSaveStore ? cloudSaveStore.LocalStore : saveStore;
        }

        private static string GetProgressPath(int profileId, bool isModded)
        {
#if STS2_AT_LEAST_0_110_0
            return ProgressSaveManager.GetProgressPathForProfile(profileId, isModded);
#else
            return ProgressSaveManager.GetProgressPathForProfile(profileId);
#endif
        }

        private static bool TryGetUtf8Bytes(string value, out byte[] bytes)
        {
            try
            {
                bytes = StrictUtf8.GetBytes(value);
                return true;
            }
            catch (EncoderFallbackException)
            {
                bytes = [];
                return false;
            }
        }

        private static string ComputeSha256(string value)
        {
            return ComputeSha256(StrictUtf8.GetBytes(value));
        }

        private static string ComputeSha256(byte[] bytes)
        {
            return Convert.ToHexStringLower(SHA256.HashData(bytes));
        }

        private static bool IsSha256(string value)
        {
            return value.Length == 64 && value.All(static character =>
                character is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');
        }

        private static bool HashEquals(string? left, string? right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetRecoveryDirectory()
        {
            return $"{ProfileManager.GetAccountBasePath()}/{RecoveryDirectoryName}";
        }

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_saveStore")]
        private static extern ref readonly ISaveStore SaveStore(SaveManager manager);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_saveStore")]
        private static extern ref readonly ISaveStore SaveStore(ProgressSaveManager manager);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_migrationManager")]
        private static extern ref readonly MigrationManager MigrationManager(ProgressSaveManager manager);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_profileIdProvider")]
        private static extern ref readonly IProfileIdProvider ProfileIdProvider(ProgressSaveManager manager);

        internal sealed record LoadCapture(string RawJson);

        private sealed record CapturedProgress(
            RawProgressReadResult Result,
            SaveManager? SaveManager = null,
            ISaveStore? Store = null,
            string? Path = null);

        private sealed record ValidatedDocument(
            string ProgressUniqueId,
            ProgressState Progress,
            string KnownProjectionJson,
            string KnownProjectionSha256);

        private sealed record CompletedTransaction(string ProposedSha256, RawProgressCommitResult Result);

        private sealed record RecoveryJournalData
        {
            public required int JournalProtocolVersion { get; init; }
            public required Guid TransactionId { get; init; }
            public required int ProfileId { get; init; }
            public required bool IsModded { get; init; }
            public required string ProgressUniqueId { get; init; }
            public required string Stage { get; init; }
            public required string OriginalRawJson { get; init; }
            public required string OriginalSha256 { get; init; }
            public required string ProposedSha256 { get; init; }
            public string? LocalReadBackSha256 { get; init; }
            public string? CloudReadBackSha256 { get; init; }
            public string? LiveKnownProjectionSha256 { get; init; }
        }

        private sealed class RecoveryJournal
        {
            private RecoveryJournal(string path, RecoveryJournalData data)
            {
                Path = path;
                Data = data;
            }

            private string Path { get; }
            private RecoveryJournalData Data { get; set; }
            internal bool Exists => FileOperations.FileExists(Path) || FileOperations.FileExists(Path + ".backup");

            internal static RecoveryJournal Create(RawProgressCommitRequest request, string originalRawJson)
            {
                var path = $"{GetRecoveryDirectory()}/{request.TransactionId:N}.json";
                return new(path, new()
                {
                    JournalProtocolVersion = ProtocolVersion,
                    TransactionId = request.TransactionId,
                    ProfileId = request.ExpectedGeneration.ProfileId,
                    IsModded = request.ExpectedGeneration.IsModded,
                    ProgressUniqueId = request.ExpectedGeneration.ProgressUniqueId,
                    Stage = "prepared",
                    OriginalRawJson = originalRawJson,
                    OriginalSha256 = ComputeSha256(originalRawJson),
                    ProposedSha256 = request.ProposedSha256,
                });
            }

            internal bool TryPrepare()
            {
                if (Exists || CountRetainedJournals() >= MaxRetainedRecoveryJournals)
                    return false;

                return TryWrite();
            }

            internal void TryUpdate(
                string stage,
                string? localReadBackSha256,
                string? cloudReadBackSha256,
                string? liveKnownProjectionSha256)
            {
                Data = Data with
                {
                    Stage = stage,
                    LocalReadBackSha256 = localReadBackSha256,
                    CloudReadBackSha256 = cloudReadBackSha256,
                    LiveKnownProjectionSha256 = liveKnownProjectionSha256,
                };
                TryWrite();
            }

            internal bool TryDelete()
            {
                var primary = FileOperations.DeleteFile(Path, RecoveryLogContext);
                var backup = FileOperations.DeleteFile(Path + ".backup", RecoveryLogContext);
                var temporary = FileOperations.DeleteFile(Path + ".tmp", RecoveryLogContext);
                return primary.Success && backup.Success && temporary.Success && !Exists;
            }

            private bool TryWrite()
            {
                try
                {
                    var json = JsonSerializer.Serialize(Data, JournalJsonOptions);
                    return FileOperations.WriteText(Path, json, RecoveryLogContext).Success;
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[RawProgress] Failed to serialize recovery metadata: {ex.Message}");
                    return false;
                }
            }

            private static int CountRetainedJournals()
            {
                try
                {
                    using var directory = DirAccess.Open(GetRecoveryDirectory());
                    return directory?.GetFiles().Count(static file =>
                        file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) ?? 0;
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RawProgress] Failed to count retained recovery journals: {ex.Message}");
                    return MaxRetainedRecoveryJournals;
                }
            }
        }
    }
}
