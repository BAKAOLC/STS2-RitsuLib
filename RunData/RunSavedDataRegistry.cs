using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.RunRngs;

namespace STS2RitsuLib.RunData
{
    internal static class RunSavedDataRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, IRunSavedDataSlot> Slots =
            new(StringComparer.OrdinalIgnoreCase);

        public static bool HasSlots
        {
            get
            {
                lock (SyncRoot)
                {
                    return Slots.Count > 0;
                }
            }
        }

        public static void Register(IRunSavedDataSlot slot)
        {
            ArgumentNullException.ThrowIfNull(slot);
            var id = GetSlotId(slot.ModId, slot.Key);
            lock (SyncRoot)
            {
                if (!Slots.TryAdd(id, slot))
                    throw new InvalidOperationException(
                        $"RunSavedData slot is already registered: {slot.ModId}::{slot.Key}");
            }
        }

        public static void Import(SerializableRun save, RunState runState)
        {
            if (!RunSavedDataRuntime.TryGetDocument(save, out var document))
                return;

            var bag = RunSavedDataRuntime.GetBag(runState);
            bag.PreservedDocument = document.Clone();
            foreach (var slot in GetSlotsSnapshot())
                slot.Import(runState, document);
        }

        public static RunSavedDataDocument? Export(RunState runState)
        {
            ModRunRngRegistry.SyncToSavedData(runState);

            var document = RunSavedDataRuntime.TryGetBag(runState, out var bag) && bag.PreservedDocument != null
                ? bag.PreservedDocument.Clone()
                : new();

            foreach (var slot in GetSlotsSnapshot())
                slot.Export(runState, document);

            return document.IsEmpty ? null : document;
        }

        public static void AttachDocumentFromJson(SerializableRun? save, string? json)
        {
            if (save == null)
                return;

            var document = RunSavedDataDocument.FromJson(json);
            RunSavedDataRuntime.AttachDocument(save, document);
        }

        public static void AttachDocument(SerializableRun? save, RunSavedDataDocument? document)
        {
            if (save != null)
                RunSavedDataRuntime.AttachDocument(save, document);
        }

        public static RunSavedDataDocument? BuildDocumentFromRun(RunState? runState)
        {
            return runState == null ? null : Export(runState);
        }

        public static string InjectIntoJson(string json, SerializableRun save)
        {
            return RunSavedDataRuntime.TryGetDocument(save, out var document)
                ? RunSavedDataDocument.InjectIntoJson(json, document)
                : json;
        }

        public static bool HasDocument(SerializableRun save)
        {
            return RunSavedDataRuntime.TryGetDocument(save, out var document) && !document.IsEmpty;
        }

        public static void MergeDocuments(SerializableRun target, SerializableRun source)
        {
            if (RunSavedDataRuntime.TryGetDocument(source, out var document))
                RunSavedDataRuntime.AttachDocument(target, document.Clone());
        }

        public static void ImportPayloadIntoRun(RunState runState, string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return;

            var document = RunSavedDataDocument.FromJson(payload);
            if (document == null)
                return;

            var bag = RunSavedDataRuntime.GetBag(runState);
            bag.PreservedDocument = document.Clone();
            foreach (var slot in GetSlotsSnapshot())
                slot.Import(runState, document);
        }

        public static string? BuildPayload(RunState? runState)
        {
            var document = BuildDocumentFromRun(runState);
            if (document == null || document.IsEmpty)
                return null;

            return document.ToRootObject().ToJsonString(new() { WriteIndented = false });
        }

        public static string? BuildPayloadFromSerializable(SerializableRun save)
        {
            if (!RunSavedDataRuntime.TryGetDocument(save, out var source))
                return null;

            var payload = new RunSavedDataDocument();
            foreach (var (modId, key, entry) in source.Entries())
                payload.SetRaw(modId, key, entry.DeepClone().AsObject());

            return payload.IsEmpty ? null : payload.ToRootObject().ToJsonString(new() { WriteIndented = false });
        }

        public static string? BuildLobbyContributionPayload(StartRunLobby lobby, ulong contributorNetId)
        {
            if (!RunSavedDataLobbyRuntime.TryGetSession(lobby, out var session))
                return null;

            var isHostNetId = contributorNetId == lobby.NetService.NetId;
            var document = new RunSavedDataDocument();
            foreach (var slot in GetSlotsSnapshot())
                slot.TryExportLobbyContribution(session, contributorNetId, isHostNetId, document);

            return document.IsEmpty ? null : document.ToRootObject().ToJsonString(new() { WriteIndented = false });
        }

        public static void MergeLobbyContribution(StartRunLobby lobby, ulong contributorNetId, string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return;

            var document = RunSavedDataDocument.FromJson(payload);
            if (document == null)
                return;

            var session = RunSavedDataLobbyRuntime.GetSession(lobby);
            var isHostNetId = contributorNetId == lobby.NetService.NetId;
            foreach (var (modId, key, entry) in document.Entries())
            {
                var slot = TryGetSlot(modId, key);
                slot?.ImportLobbyContribution(session, contributorNetId, isHostNetId, entry);
            }

            RunSavedDataLobby.PublishStagingEvent(lobby, RunSavedDataLobbyStagingReason.ContributionMerged);
        }

        private static IRunSavedDataSlot? TryGetSlot(string modId, string key)
        {
            lock (SyncRoot)
            {
                return Slots.GetValueOrDefault(GetSlotId(modId, key));
            }
        }

        internal static IRunSavedDataSlot[] GetRegisteredSlots()
        {
            return GetSlotsSnapshot();
        }

        private static IRunSavedDataSlot[] GetSlotsSnapshot()
        {
            lock (SyncRoot)
            {
                return [.. Slots.Values];
            }
        }

        private static string GetSlotId(string modId, string key)
        {
            return $"{modId}\u001f{key}";
        }
    }
}
