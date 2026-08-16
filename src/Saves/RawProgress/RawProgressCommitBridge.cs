using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        internal const int MaxRecoveryOwnerIdUtf8Bytes = 256;
        private const int MaxRetainedRecoveryJournals = 8;
        private const int MaxRecoveryDirectoryFiles = 128;
        private const int MaxQuarantinedRecoveryFiles = 64;
        private const long MaxQuarantinedRecoveryBytes = 64 * 1024 * 1024;
        private const string RecoveryLogContext = "RawProgressRecovery";
        private const string RecoveryDirectoryName = "recovery/raw-progress";
        private const string RecoveryQuarantineDirectoryName = "recovery/raw-progress-quarantine";

        private static readonly object SaveWindow = new();
        private static readonly UTF8Encoding StrictUtf8 = new(false, true);
        private static readonly Dictionary<RecoveryTransactionKey, CompletedTransaction> CompletedTransactions = [];
        private static readonly Queue<RecoveryTransactionKey> CompletedTransactionOrder = [];
        private static int _observedRuntimeSchema;

        private static readonly JsonSerializerOptions JournalJsonOptions = new()
        {
            WriteIndented = true,
            MaxDepth = 32,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };

        [field: ThreadStatic] internal static bool IsPreparingCommitProjection { get; private set; }
        [field: ThreadStatic] internal static bool IsSavingOrdinaryProgress { get; private set; }

        private readonly RawProgressBridgeFeature _features;

        private RawProgressCommitBridge()
        {
#if !STS2_AT_LEAST_0_108_0
            const RawProgressBridgeFeature schemaFeatures = RawProgressBridgeFeature.RawSchema21Document;
#else
            const RawProgressBridgeFeature schemaFeatures = 0;
#endif
            const RawProgressBridgeFeature features = RawProgressBridgeFeature.UnknownJsonPassThrough |
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
                                                      RawProgressBridgeFeature.ActiveProgressSnapshot |
                                                      RawProgressBridgeFeature.RecoveryJournalManagement |
                                                      RawProgressBridgeFeature.RecoveryJournalOwnership |
                                                      RawProgressBridgeFeature.RecoveryJournalDisposition |
                                                      RawProgressBridgeFeature.InvalidRecoveryJournalQuarantine |
                                                      schemaFeatures;

            _features = features;
        }

        internal static RawProgressCommitBridge Instance { get; } = new();

        public RawProgressBridgeDescriptor Describe()
        {
            var supportedSchema = Volatile.Read(ref _observedRuntimeSchema);
            return new()
            {
                ProviderId = Const.ModId,
                ProviderVersion = Version.Parse(Const.Version),
                ProtocolVersion = ProtocolVersion,
                SupportedSchemas = supportedSchema > 0
                    ? new[] { supportedSchema }.ToFrozenSet()
                    : FrozenSet<int>.Empty,
                Features = _features,
                MaxDocumentUtf8Bytes = MaxDocumentUtf8Bytes,
                MaxRetainedRecoveryJournals = MaxRetainedRecoveryJournals,
                MaxRecoveryOwnerIdUtf8Bytes = MaxRecoveryOwnerIdUtf8Bytes,
            };
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

            return new(RitsuMainThread.InvokeAsync(
                () => CommitOnMainThread(request, cancellationToken),
                cancellationToken));
        }

        public ValueTask<RawProgressRecoveryReadResult> GetPendingRecoveriesAsync(
            string ownerId,
            CancellationToken cancellationToken = default)
        {
            if (!IsValidOwnerId(ownerId))
                throw new ArgumentException("The recovery owner identifier is invalid.", nameof(ownerId));

            return new(RitsuMainThread.InvokeAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                lock (SaveWindow)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return RecoveryJournal.ReadAll(ownerId);
                }
            }, cancellationToken));
        }

        public ValueTask<RawProgressCommitResult> RestoreRecoveryAsync(
            RawProgressRecoveryRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new(RitsuMainThread.InvokeAsync(
                () => RestoreRecoveryOnMainThread(request, cancellationToken),
                cancellationToken));
        }

        public ValueTask<RawProgressRecoveryDiscardResult> DiscardRecoveryAsync(
            RawProgressRecoveryRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new(RitsuMainThread.InvokeAsync(
                () => DiscardRecoveryOnMainThread(request, cancellationToken),
                cancellationToken));
        }

        internal static void SaveOrdinaryProgress(ProgressSaveManager manager)
        {
            ArgumentNullException.ThrowIfNull(manager);

            lock (SaveWindow)
            {
                try
                {
                    var progress = manager.Progress;
                    SerializableProgress serializable;
                    var wasSavingOrdinaryProgress = IsSavingOrdinaryProgress;
                    try
                    {
                        IsSavingOrdinaryProgress = true;
                        serializable = progress.ToSerializable();
                    }
                    finally
                    {
                        IsSavingOrdinaryProgress = wasSavingOrdinaryProgress;
                    }

                    serializable.SchemaVersion = GetSupportedSchema(manager);
                    var knownJson = JsonSerializationUtility.ToJson(serializable);
                    ProgressMirrorStore.SaveMirror(serializable, knownJson);
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

                var supportedSchema = GetSupportedSchema(manager);
                if (!TryReadSchema(capture.RawJson, out var rawSchema) || rawSchema != supportedSchema)
                    return;

                var rawReadResult = JsonSerializationUtility.FromJson<SerializableProgress>(capture.RawJson);
                if (rawReadResult is not { Success: true, SaveData: not null })
                    return;

                rawReadResult.SaveData.SchemaVersion = supportedSchema;
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
            if (request.ProtocolVersion != ProtocolVersion || !TryGetSupportedSchema(out var supportedSchema))
                return CreateResult(RawProgressCommitOutcome.ProviderIncompatible);
            if (request.SchemaVersion != supportedSchema)
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

                return Remember(request, CommitPrepared(
                    request.ProposedRawJson,
                    request.ProposedSha256,
                    proposed,
                    current,
                    journal));
            }
        }

        private static RawProgressCommitResult RestoreRecoveryOnMainThread(
            RawProgressRecoveryRequest request,
            CancellationToken cancellationToken)
        {
            if (!IsValidRecoveryRequest(request))
                return CreateResult(RawProgressCommitOutcome.ValidationFailed);
            if (cancellationToken.IsCancellationRequested)
                return CreateResult(RawProgressCommitOutcome.CancelledBeforeCommit);

            lock (SaveWindow)
            {
                if (cancellationToken.IsCancellationRequested)
                    return CreateResult(RawProgressCommitOutcome.CancelledBeforeCommit);

                var loadOutcome = RecoveryJournal.TryLoad(request.OwnerId, request.TransactionId, out var journal);
                if (loadOutcome == RecoveryJournalLoadOutcome.NotFound)
                    return CreateResult(RawProgressCommitOutcome.RecoveryJournalNotFound);
                if (loadOutcome != RecoveryJournalLoadOutcome.Succeeded || journal == null)
                    return CreateResult(RawProgressCommitOutcome.RecoveryJournalInvalid);
                if (!HashEquals(journal.RecoveryToken, request.RecoveryToken))
                    return CreateResult(
                        RawProgressCommitOutcome.RecoveryJournalChanged,
                        verifiedBackupAvailable: true,
                        recoveryJournalRetained: journal.Exists);

                var data = journal.Data;
                if (!TryGetSupportedSchema(out var supportedSchema))
                    return CreateResult(
                        RawProgressCommitOutcome.ProviderIncompatible,
                        verifiedBackupAvailable: true,
                        recoveryJournalRetained: journal.Exists);
                if (data.SchemaVersion != supportedSchema)
                    return CreateResult(
                        RawProgressCommitOutcome.SchemaUnsupported,
                        verifiedBackupAvailable: true,
                        recoveryJournalRetained: journal.Exists);

                var expected = request.ExpectedGeneration;
                if (data.ProfileId != expected.ProfileId || data.IsModded != expected.IsModded ||
                    !string.Equals(data.ProgressUniqueId, expected.ProgressUniqueId, StringComparison.Ordinal))
                    return CreateResult(
                        RawProgressCommitOutcome.ActiveProfileChanged,
                        verifiedBackupAvailable: true,
                        recoveryJournalRetained: journal.Exists);

                var current = CaptureCore();
                if (current.Result.Outcome != RawProgressReadOutcome.Succeeded || current.Result.Snapshot == null)
                    return CreateResult(
                        MapCaptureFailure(current.Result.Outcome).Outcome,
                        verifiedBackupAvailable: true,
                        recoveryJournalRetained: journal.Exists);

                var currentGeneration = current.Result.Snapshot.Generation;
                if (currentGeneration.ProfileId != data.ProfileId || currentGeneration.IsModded != data.IsModded ||
                    !string.Equals(currentGeneration.ProgressUniqueId, data.ProgressUniqueId, StringComparison.Ordinal))
                    return CreateResult(
                        RawProgressCommitOutcome.ActiveProfileChanged,
                        verifiedBackupAvailable: true,
                        recoveryJournalRetained: journal.Exists);
                if (!GenerationMatches(currentGeneration, expected))
                    return CreateResult(
                        RawProgressCommitOutcome.GenerationConflict,
                        verifiedBackupAvailable: true,
                        recoveryJournalRetained: journal.Exists);
                if (cancellationToken.IsCancellationRequested)
                    return CreateResult(
                        RawProgressCommitOutcome.CancelledBeforeCommit,
                        verifiedBackupAvailable: true,
                        recoveryJournalRetained: journal.Exists);

                if (!TryGetUtf8Bytes(data.OriginalRawJson, out var originalBytes))
                    return CreateResult(
                        RawProgressCommitOutcome.RecoveryJournalInvalid,
                        verifiedBackupAvailable: true,
                        recoveryJournalRetained: journal.Exists);

                var validationRequest = new RawProgressCommitRequest
                {
                    ProtocolVersion = ProtocolVersion,
                    SchemaVersion = data.SchemaVersion,
                    OwnerId = data.OwnerId,
                    TransactionId = data.TransactionId,
                    ExpectedGeneration = expected,
                    ProposedRawJson = data.OriginalRawJson,
                    ProposedSha256 = data.OriginalSha256,
                    ProposedUtf8Length = originalBytes.LongLength,
                };
                if (!TryValidateProposedDocument(validationRequest, out var original))
                    return CreateResult(
                        RawProgressCommitOutcome.RecoveryJournalInvalid,
                        verifiedBackupAvailable: true,
                        recoveryJournalRetained: journal.Exists);

                return CommitPrepared(
                    data.OriginalRawJson,
                    data.OriginalSha256,
                    original,
                    current,
                    journal);
            }
        }

        private static RawProgressRecoveryDiscardResult DiscardRecoveryOnMainThread(
            RawProgressRecoveryRequest request,
            CancellationToken cancellationToken)
        {
            if (!IsValidRecoveryRequest(request))
                return CreateDiscardResult(RawProgressRecoveryDiscardOutcome.ValidationFailed);
            if (cancellationToken.IsCancellationRequested)
                return CreateDiscardResult(RawProgressRecoveryDiscardOutcome.Cancelled);

            lock (SaveWindow)
            {
                if (cancellationToken.IsCancellationRequested)
                    return CreateDiscardResult(RawProgressRecoveryDiscardOutcome.Cancelled);

                var loadOutcome = RecoveryJournal.TryLoad(request.OwnerId, request.TransactionId, out var journal);
                if (loadOutcome == RecoveryJournalLoadOutcome.NotFound)
                    return CreateDiscardResult(RawProgressRecoveryDiscardOutcome.RecoveryJournalNotFound);
                if (loadOutcome != RecoveryJournalLoadOutcome.Succeeded || journal == null)
                    return CreateDiscardResult(RawProgressRecoveryDiscardOutcome.RecoveryJournalInvalid);
                if (!HashEquals(journal.RecoveryToken, request.RecoveryToken))
                    return CreateDiscardResult(
                        RawProgressRecoveryDiscardOutcome.RecoveryJournalChanged,
                        journal.Exists);

                var data = journal.Data;
                var expected = request.ExpectedGeneration;
                if (data.ProfileId != expected.ProfileId || data.IsModded != expected.IsModded ||
                    !string.Equals(data.ProgressUniqueId, expected.ProgressUniqueId, StringComparison.Ordinal))
                    return CreateDiscardResult(
                        RawProgressRecoveryDiscardOutcome.ActiveProfileChanged,
                        journal.Exists);

                var current = CaptureCore();
                if (current.Result.Outcome != RawProgressReadOutcome.Succeeded || current.Result.Snapshot == null)
                    return CreateDiscardResult(
                        current.Result.Outcome == RawProgressReadOutcome.ActiveProfileUnavailable
                            ? RawProgressRecoveryDiscardOutcome.ActiveProfileChanged
                            : RawProgressRecoveryDiscardOutcome.DestinationUnavailable,
                        journal.Exists);

                var currentGeneration = current.Result.Snapshot.Generation;
                if (currentGeneration.ProfileId != data.ProfileId || currentGeneration.IsModded != data.IsModded ||
                    !string.Equals(currentGeneration.ProgressUniqueId, data.ProgressUniqueId, StringComparison.Ordinal))
                    return CreateDiscardResult(
                        RawProgressRecoveryDiscardOutcome.ActiveProfileChanged,
                        journal.Exists);
                if (!GenerationMatches(currentGeneration, expected))
                    return CreateDiscardResult(
                        RawProgressRecoveryDiscardOutcome.GenerationConflict,
                        journal.Exists);
                if (cancellationToken.IsCancellationRequested)
                    return CreateDiscardResult(
                        RawProgressRecoveryDiscardOutcome.Cancelled,
                        journal.Exists);

                var discarded = journal.TryDelete();
                return CreateDiscardResult(
                    discarded
                        ? RawProgressRecoveryDiscardOutcome.Discarded
                        : RawProgressRecoveryDiscardOutcome.StorageFailure,
                    journal.Exists);
            }
        }

        private static RawProgressCommitResult CommitPrepared(
            string proposedRawJson,
            string proposedSha256,
            ValidatedDocument proposed,
            CapturedProgress current,
            RecoveryJournal journal)
        {
            var destinationMayHaveChanged = false;
            var localVerified = false;
            var batchFailure = false;
            string? localReadBackHash = null;
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
                saveStore.WriteFile(path, proposedRawJson);
                var localReadBack = localStore.ReadFile(path);
                if (localReadBack != null)
                {
                    localReadBackHash = ComputeSha256(localReadBack);
                    localVerified = HashEquals(localReadBackHash, proposedSha256);
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
            (cloudStatus, var cloudReadBackHash) = VerifyCloudReadBack(
                saveStore,
                path,
                proposedSha256,
                batchFailure);

            string? liveProjectionHash = null;
            var continuationInstalled = false;
            try
            {
                saveManager.Progress = proposed.Progress;
                continuationInstalled = RawProgressJsonPreservation.TryAttach(
                    proposed.Progress,
                    proposedRawJson,
                    proposed.KnownProjectionJson);

                var liveSerializable = saveManager.Progress.ToSerializable();
                liveSerializable.SchemaVersion = current.Result.Snapshot!.SchemaVersion;
                liveProjectionHash = ComputeSha256(JsonSerializationUtility.ToJson(liveSerializable));
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[RawProgress] Failed to synchronize the live progress projection: {ex.Message}");
            }

            var outcome = (
                    LiveProjectionMatches: HashEquals(liveProjectionHash, proposed.KnownProjectionSha256),
                    ContinuationInstalled: continuationInstalled,
                    CloudStatus: cloudStatus) switch
                {
                    { LiveProjectionMatches: false } => RawProgressCommitOutcome.LiveProgressStateUnverified,
                    { ContinuationInstalled: false } => RawProgressCommitOutcome.UnknownJsonContinuationUnverified,
                    { CloudStatus: CloudReadBackStatus.Unavailable or CloudReadBackStatus.FailureObserved } =>
                        RawProgressCommitOutcome.CloudReadBackUnverifiedLocalPreserved,
                    { CloudStatus: CloudReadBackStatus.Mismatch } =>
                        RawProgressCommitOutcome.CloudReadBackMismatchLocalPreserved,
                    _ => RawProgressCommitOutcome.CommittedVerified,
                };

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
                continuationInstalled ? proposedSha256 : null,
                destinationMayHaveChanged,
                gameBackupVerified || journal.Exists,
                journal.Exists);
        }

        private static CapturedProgress CaptureCore()
        {
            SaveManager saveManager;
            int profileId;
            int supportedSchema;
            try
            {
                saveManager = SaveManager.Instance;
                profileId = saveManager.CurrentProfileId;
                supportedSchema = GetSupportedSchema(saveManager);
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
            if (schema != supportedSchema)
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
            if (!IsValidOwnerId(request.OwnerId) ||
                request.TransactionId == Guid.Empty ||
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
                IsPreparingCommitProjection = true;
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
                IsPreparingCommitProjection = false;
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
                    // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                    foreach (var property in element.EnumerateObject())
                        if (!propertyNames.Add(property.Name) || !HasUniquePropertyNames(property.Value))
                            return false;
                    break;
                }
                case JsonValueKind.Array:
                    // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
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

        private static RawProgressRecoveryDiscardResult CreateDiscardResult(
            RawProgressRecoveryDiscardOutcome outcome,
            bool recoveryJournalRetained = false)
        {
            return new()
            {
                Outcome = outcome,
                RecoveryJournalRetained = recoveryJournalRetained,
            };
        }

        private static bool TryGetCompletedTransaction(
            RawProgressCommitRequest request,
            out RawProgressCommitResult result)
        {
            var key = new RecoveryTransactionKey(request.OwnerId, request.TransactionId);
            if (!CompletedTransactions.TryGetValue(key, out var completed))
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
            var key = new RecoveryTransactionKey(request.OwnerId, request.TransactionId);
            if (!CompletedTransactions.ContainsKey(key))
            {
                CompletedTransactions.Add(key, new(request.ProposedSha256, result));
                CompletedTransactionOrder.Enqueue(key);
                while (CompletedTransactionOrder.Count > 256)
                    CompletedTransactions.Remove(CompletedTransactionOrder.Dequeue());
            }

            return result;
        }

        private static ISaveStore GetLocalStore(ISaveStore saveStore)
        {
            return saveStore is CloudSaveStore cloudSaveStore ? cloudSaveStore.LocalStore : saveStore;
        }

        private static int GetSupportedSchema(SaveManager saveManager)
        {
            return ObserveSupportedSchema(saveManager.GetLatestSchemaVersion<SerializableProgress>());
        }

        private static int GetSupportedSchema(ProgressSaveManager saveManager)
        {
            return ObserveSupportedSchema(MigrationManager(saveManager).GetLatestVersion<SerializableProgress>());
        }

        private static int ObserveSupportedSchema(int supportedSchema)
        {
            if (supportedSchema > 0)
                Volatile.Write(ref _observedRuntimeSchema, supportedSchema);

            return supportedSchema;
        }

        private static bool TryGetSupportedSchema(out int supportedSchema)
        {
            try
            {
                supportedSchema = GetSupportedSchema(SaveManager.Instance);
                return supportedSchema > 0;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RawProgress] Could not resolve the active progress schema: {ex.Message}");
                supportedSchema = 0;
                return false;
            }
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

        private static bool IsValidOwnerId(string? ownerId)
        {
            return !string.IsNullOrWhiteSpace(ownerId) &&
                   ownerId.Length <= MaxRecoveryOwnerIdUtf8Bytes &&
                   string.Equals(ownerId, ownerId.Trim(), StringComparison.Ordinal) &&
                   TryGetUtf8Bytes(ownerId, out var bytes) &&
                   bytes.Length is > 0 and <= MaxRecoveryOwnerIdUtf8Bytes;
        }

        private static bool IsValidRecoveryRequest(RawProgressRecoveryRequest request)
        {
            return IsValidOwnerId(request.OwnerId) &&
                   request.TransactionId != Guid.Empty &&
                   request.RecoveryToken != null &&
                   IsSha256(request.RecoveryToken) &&
                   request.ExpectedGeneration is
                   {
                       ProfileId: >= 1 and <= 3,
                   } expected &&
                   !string.IsNullOrWhiteSpace(expected.ProgressUniqueId) &&
                   expected.ProgressUniqueId.Length <= 256 &&
                   IsSha256(expected.LocalSha256);
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

        private static string ComputeOwnerStorageId(string ownerId)
        {
            return ComputeSha256(ownerId);
        }

        private static string GetRecoveryPath(string ownerId, Guid transactionId)
        {
            return GetRecoveryPath(new(ComputeOwnerStorageId(ownerId), transactionId));
        }

        private static string GetRecoveryPath(RecoveryJournalKey key)
        {
            return $"{GetRecoveryDirectory()}/{key.OwnerStorageId}-{key.TransactionId:N}.json";
        }

        private static string GetRecoveryQuarantineDirectory()
        {
            return $"{ProfileManager.GetAccountBasePath()}/{RecoveryQuarantineDirectoryName}";
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

        private readonly record struct RecoveryTransactionKey(string OwnerId, Guid TransactionId);

        private readonly record struct RecoveryJournalKey(string OwnerStorageId, Guid TransactionId);

        private enum RecoveryJournalLoadOutcome
        {
            Succeeded,
            NotFound,
            Invalid,
        }

        private sealed record RecoveryJournalData
        {
            public required int JournalProtocolVersion { get; init; }
            public required string OwnerId { get; init; }
            public required int SchemaVersion { get; init; }
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
            private RecoveryJournal(
                string basePath,
                string writePath,
                RecoveryJournalData data,
                bool canUpdate = true)
            {
                BasePath = basePath;
                WritePath = writePath;
                Data = data;
                CanUpdate = canUpdate;
            }

            private string BasePath { get; }
            private string WritePath { get; }
            private bool CanUpdate { get; }
            internal RecoveryJournalData Data { get; private set; }

            internal string RecoveryToken => ComputeSha256(JsonSerializer.Serialize(Data, JournalJsonOptions));

            internal bool Exists => FileOperations.FileExists(BasePath) ||
                                    FileOperations.FileExists(BasePath + ".backup") ||
                                    FileOperations.FileExists(BasePath + ".backup.backup");

            internal static RecoveryJournal Create(RawProgressCommitRequest request, string originalRawJson)
            {
                var path = GetRecoveryPath(request.OwnerId, request.TransactionId);
                return new(path, path, new()
                {
                    JournalProtocolVersion = ProtocolVersion,
                    OwnerId = request.OwnerId,
                    SchemaVersion = request.SchemaVersion,
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

            internal static RawProgressRecoveryReadResult ReadAll(string ownerId)
            {
                var ownerStorageId = ComputeOwnerStorageId(ownerId);
                if (!TryEnumerateJournalKeys(out var journalKeys, out var invalidFiles))
                    return new()
                    {
                        OwnerId = ownerId,
                        Outcome = RawProgressRecoveryReadOutcome.StorageUnavailable,
                        Records = [],
                        InvalidEntryCount = CountInvalidOwnerFiles(invalidFiles, ownerStorageId),
                    };

                var ownerKeys = journalKeys
                    .Where(key => string.Equals(key.OwnerStorageId, ownerStorageId, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var invalidEntryCount = CountInvalidOwnerFiles(invalidFiles, ownerStorageId);
                var records = new List<RawProgressRecoveryRecord>(ownerKeys.Length);
                foreach (var key in ownerKeys)
                {
                    var outcome = TryLoadKey(key, ownerId, out var journal);
                    if (outcome != RecoveryJournalLoadOutcome.Succeeded || journal == null)
                    {
                        invalidEntryCount++;
                        continue;
                    }

                    records.Add(journal.ToRecord());
                }

                records.Sort(static (left, right) => left.TransactionId.CompareTo(right.TransactionId));
                return new()
                {
                    OwnerId = ownerId,
                    Outcome = invalidEntryCount == 0
                        ? RawProgressRecoveryReadOutcome.Succeeded
                        : RawProgressRecoveryReadOutcome.InvalidEntriesIgnored,
                    Records = records.AsReadOnly(),
                    InvalidEntryCount = invalidEntryCount,
                };
            }

            internal static RecoveryJournalLoadOutcome TryLoad(
                string ownerId,
                Guid transactionId,
                out RecoveryJournal? journal)
            {
                if (!IsValidOwnerId(ownerId) || transactionId == Guid.Empty)
                {
                    journal = null;
                    return RecoveryJournalLoadOutcome.Invalid;
                }

                var key = new RecoveryJournalKey(ComputeOwnerStorageId(ownerId), transactionId);
                return TryLoadKey(key, ownerId, out journal);
            }

            private static RecoveryJournalLoadOutcome TryLoadKey(
                RecoveryJournalKey key,
                string? expectedOwnerId,
                out RecoveryJournal? journal)
            {
                journal = null;
                var basePath = GetRecoveryPath(key);
                var primaryExists = FileOperations.FileExists(basePath);
                var backupPath = basePath + ".backup";
                var backupExists = FileOperations.FileExists(backupPath);
                var secondBackupPath = backupPath + ".backup";
                var secondBackupExists = FileOperations.FileExists(secondBackupPath);
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (!primaryExists && !backupExists && !secondBackupExists)
                    return RecoveryJournalLoadOutcome.NotFound;

                if (primaryExists && TryReadData(basePath, key, expectedOwnerId, out var primaryData))
                {
                    journal = new(basePath, basePath, primaryData);
                    return RecoveryJournalLoadOutcome.Succeeded;
                }

                if (backupExists && TryReadData(backupPath, key, expectedOwnerId, out var backupData))
                {
                    journal = new(basePath, backupPath, backupData);
                    return RecoveryJournalLoadOutcome.Succeeded;
                }

                if (secondBackupExists && TryReadData(secondBackupPath, key, expectedOwnerId,
                        out var secondBackupData))
                {
                    journal = new(basePath, secondBackupPath, secondBackupData, false);
                    return RecoveryJournalLoadOutcome.Succeeded;
                }

                return RecoveryJournalLoadOutcome.Invalid;
            }

            internal bool TryPrepare()
            {
                if (!TryMaintainAndCountRetainedJournals(out var retainedJournalCount) ||
                    Exists ||
                    retainedJournalCount >= MaxRetainedRecoveryJournals)
                    return false;

                return TryWrite();
            }

            internal void TryUpdate(
                string stage,
                string? localReadBackSha256,
                string? cloudReadBackSha256,
                string? liveKnownProjectionSha256)
            {
                if (!CanUpdate)
                    return;

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
                var paths = new[]
                {
                    BasePath,
                    BasePath + ".backup",
                    BasePath + ".tmp",
                    BasePath + ".backup.backup",
                    BasePath + ".backup.tmp",
                    BasePath + ".backup.backup.backup",
                    BasePath + ".backup.backup.tmp",
                };
                var success = true;
                foreach (var path in paths)
                    success &= FileOperations.DeleteFile(path, RecoveryLogContext).Success;
                return success && !Exists;
            }

            private bool TryWrite()
            {
                try
                {
                    var json = JsonSerializer.Serialize(Data, JournalJsonOptions);
                    return FileOperations.WriteText(WritePath, json, RecoveryLogContext).Success;
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[RawProgress] Failed to serialize recovery metadata: {ex.Message}");
                    return false;
                }
            }

            private RawProgressRecoveryRecord ToRecord()
            {
                _ = TryParseStage(Data.Stage, out var stage);
                return new()
                {
                    OwnerId = Data.OwnerId,
                    SchemaVersion = Data.SchemaVersion,
                    TransactionId = Data.TransactionId,
                    ProfileId = Data.ProfileId,
                    IsModded = Data.IsModded,
                    ProgressUniqueId = Data.ProgressUniqueId,
                    Stage = stage,
                    OriginalSha256 = Data.OriginalSha256,
                    ProposedSha256 = Data.ProposedSha256,
                    RecoveryToken = RecoveryToken,
                };
            }

            private static bool TryMaintainAndCountRetainedJournals(out int retainedJournalCount)
            {
                retainedJournalCount = 0;
                if (!TryEnumerateJournalKeys(out var journalKeys, out var invalidFiles))
                    return false;

                var recoveryDirectory = GetRecoveryDirectory();
                foreach (var invalidFile in invalidFiles)
                    _ = TryQuarantineFile($"{recoveryDirectory}/{invalidFile}");

                foreach (var key in journalKeys)
                {
                    var outcome = TryLoadKey(key, null, out _);
                    switch (outcome)
                    {
                        case RecoveryJournalLoadOutcome.Succeeded:
                            retainedJournalCount++;
                            break;
                        case RecoveryJournalLoadOutcome.Invalid:
                            TryQuarantineJournal(key);
                            break;
                    }
                }

                return true;
            }

            private static void TryQuarantineJournal(RecoveryJournalKey key)
            {
                var basePath = GetRecoveryPath(key);
                var paths = new[]
                {
                    basePath,
                    basePath + ".backup",
                    basePath + ".tmp",
                    basePath + ".backup.backup",
                    basePath + ".backup.tmp",
                    basePath + ".backup.backup.backup",
                    basePath + ".backup.backup.tmp",
                };
                foreach (var path in paths)
                    _ = TryQuarantineFile(path);
            }

            private static bool TryQuarantineFile(string sourcePath)
            {
                if (!FileOperations.FileExists(sourcePath))
                    return true;
                if (!TryGetFileLength(sourcePath, out var sourceLength) ||
                    !TryGetQuarantineUsage(out var quarantineFileCount, out var quarantineBytes) ||
                    quarantineFileCount >= MaxQuarantinedRecoveryFiles ||
                    sourceLength > MaxQuarantinedRecoveryBytes - quarantineBytes)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RawProgress] Could not quarantine invalid recovery data at '{sourcePath}' within limits.");
                    return false;
                }

                var quarantineDirectory = GetRecoveryQuarantineDirectory();
                if (!DirAccess.DirExistsAbsolute(quarantineDirectory))
                {
                    var createError = DirAccess.MakeDirRecursiveAbsolute(quarantineDirectory);
                    if (createError != Error.Ok)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[RawProgress] Could not create the recovery quarantine (Error: {createError}).");
                        return false;
                    }
                }

                var destinationPath =
                    $"{quarantineDirectory}/{DateTime.UtcNow.Ticks}-{Guid.NewGuid():N}.invalid";
                var renameError = DirAccess.RenameAbsolute(sourcePath, destinationPath);
                if (renameError != Error.Ok)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RawProgress] Could not quarantine invalid recovery data at '{sourcePath}' " +
                        $"(Error: {renameError}).");
                    return false;
                }

                RitsuLibFramework.Logger.Warn(
                    $"[RawProgress] Quarantined invalid recovery data from '{sourcePath}'.");
                return true;
            }

            private static bool TryGetQuarantineUsage(out int fileCount, out long totalBytes)
            {
                fileCount = 0;
                totalBytes = 0;
                var quarantineDirectory = GetRecoveryQuarantineDirectory();
                if (!DirAccess.DirExistsAbsolute(quarantineDirectory))
                    return true;

                try
                {
                    using var directory = DirAccess.Open(quarantineDirectory);
                    if (directory == null)
                        return false;

                    foreach (var file in directory.GetFiles())
                    {
                        fileCount++;
                        if (fileCount > MaxQuarantinedRecoveryFiles ||
                            !TryGetFileLength($"{quarantineDirectory}/{file}", out var length) ||
                            length > MaxQuarantinedRecoveryBytes - totalBytes)
                            return false;

                        totalBytes += length;
                    }

                    return true;
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RawProgress] Could not inspect the recovery quarantine: {ex.Message}");
                    return false;
                }
            }

            private static bool TryGetFileLength(string path, out long length)
            {
                length = 0;
                try
                {
                    using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
                    if (file == null)
                        return false;

                    var rawLength = file.GetLength();
                    if (rawLength > long.MaxValue)
                        return false;

                    length = (long)rawLength;
                    return true;
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RawProgress] Could not inspect recovery data at '{path}': {ex.Message}");
                    return false;
                }
            }

            private static int CountInvalidOwnerFiles(IEnumerable<string> invalidFiles, string ownerStorageId)
            {
                var ownerPrefix = ownerStorageId + "-";
                return invalidFiles.Count(file =>
                    TryStripRecoverySuffix(file, out var stem) &&
                    stem.StartsWith(ownerPrefix, StringComparison.OrdinalIgnoreCase));
            }

            private static bool TryReadData(
                string path,
                RecoveryJournalKey key,
                string? expectedOwnerId,
                out RecoveryJournalData data)
            {
                data = null!;
                var read = FileOperations.ReadText(path, RecoveryLogContext);
                if (!read.Success || read.Content == null)
                    return false;

                try
                {
                    var candidate = JsonSerializer.Deserialize<RecoveryJournalData>(read.Content, JournalJsonOptions);
                    if (!IsValid(candidate, key, expectedOwnerId))
                        return false;

                    data = candidate!;
                    return true;
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RawProgress] Failed to read recovery metadata at '{path}': {ex.Message}");
                    return false;
                }
            }

            private static bool IsValid(
                RecoveryJournalData? data,
                RecoveryJournalKey key,
                string? expectedOwnerId)
            {
                if (data is not { JournalProtocolVersion: ProtocolVersion } ||
                    !IsValidOwnerId(data.OwnerId) ||
                    !string.Equals(ComputeOwnerStorageId(data.OwnerId), key.OwnerStorageId,
                        StringComparison.OrdinalIgnoreCase) ||
                    expectedOwnerId != null &&
                    !string.Equals(data.OwnerId, expectedOwnerId, StringComparison.Ordinal) ||
                    data.SchemaVersion < 1 ||
                    data.TransactionId != key.TransactionId ||
                    data.ProfileId is < 1 or > 3 ||
                    string.IsNullOrWhiteSpace(data.ProgressUniqueId) ||
                    data.ProgressUniqueId.Length > 256 ||
                    !TryParseStage(data.Stage, out _) ||
                    string.IsNullOrWhiteSpace(data.OriginalRawJson) ||
                    data.OriginalRawJson.Length > MaxDocumentUtf8Bytes ||
                    !IsSha256(data.OriginalSha256) ||
                    !IsSha256(data.ProposedSha256) ||
                    data.LocalReadBackSha256 != null && !IsSha256(data.LocalReadBackSha256) ||
                    data.CloudReadBackSha256 != null && !IsSha256(data.CloudReadBackSha256) ||
                    data.LiveKnownProjectionSha256 != null && !IsSha256(data.LiveKnownProjectionSha256) ||
                    !TryInspectExistingDocument(
                        data.OriginalRawJson,
                        out var schema,
                        out var uniqueId,
                        out var originalBytes) ||
                    schema != data.SchemaVersion ||
                    originalBytes.LongLength > MaxDocumentUtf8Bytes ||
                    !HashEquals(ComputeSha256(originalBytes), data.OriginalSha256) ||
                    !string.Equals(uniqueId, data.ProgressUniqueId, StringComparison.Ordinal))
                    return false;

                return true;
            }

            private static bool TryParseStage(string? value, out RawProgressRecoveryStage stage)
            {
                stage = value switch
                {
                    "prepared" => RawProgressRecoveryStage.Prepared,
                    "local_unverified" => RawProgressRecoveryStage.LocalUnverified,
                    "local_verified" => RawProgressRecoveryStage.LocalVerified,
                    "verification_incomplete" => RawProgressRecoveryStage.VerificationIncomplete,
                    _ => default,
                };
                return value is "prepared" or "local_unverified" or "local_verified" or
                    "verification_incomplete";
            }

            private static bool TryStripRecoverySuffix(string fileName, out string stem)
            {
                foreach (var suffix in new[]
                         {
                             ".json.backup.backup.backup",
                             ".json.backup.backup.tmp",
                             ".json.backup.backup",
                             ".json.backup.tmp",
                             ".json.backup",
                             ".json.tmp",
                             ".json",
                         })
                {
                    if (!fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                        continue;

                    stem = fileName[..^suffix.Length];
                    return true;
                }

                stem = string.Empty;
                return false;
            }

            private static bool TryParseJournalKey(string stem, out RecoveryJournalKey key)
            {
                key = default;
                if (stem.Length != 97 || stem[64] != '-')
                    return false;

                var ownerStorageId = stem[..64];
                if (!IsSha256(ownerStorageId) ||
                    !Guid.TryParseExact(stem[65..], "N", out var transactionId))
                    return false;

                key = new(ownerStorageId.ToLowerInvariant(), transactionId);
                return true;
            }

            private static bool TryEnumerateJournalKeys(
                out HashSet<RecoveryJournalKey> journalKeys,
                out HashSet<string> invalidFiles)
            {
                journalKeys = [];
                invalidFiles = new(StringComparer.OrdinalIgnoreCase);
                var recoveryDirectory = GetRecoveryDirectory();
                try
                {
                    if (!DirAccess.DirExistsAbsolute(recoveryDirectory))
                        return true;

                    using var directory = DirAccess.Open(recoveryDirectory);
                    if (directory == null)
                        return false;

                    var fileCount = 0;
                    foreach (var file in directory.GetFiles())
                    {
                        fileCount++;
                        if (fileCount > MaxRecoveryDirectoryFiles)
                            return false;

                        if (!TryStripRecoverySuffix(file, out var stem))
                        {
                            invalidFiles.Add(file);
                            continue;
                        }

                        if (TryParseJournalKey(stem, out var key))
                            journalKeys.Add(key);
                        else
                            invalidFiles.Add(file);
                    }

                    return true;
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RawProgress] Failed to enumerate retained recovery journals: {ex.Message}");
                    return false;
                }
            }
        }
    }
}
