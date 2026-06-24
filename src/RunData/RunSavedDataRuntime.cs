using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.RunData
{
    internal sealed class RunSavedDataBag
    {
        private readonly HashSet<RunSavedDataSlotKey> _dirty = [];
        private readonly Dictionary<RunSavedDataSlotKey, object> _values = [];

        public RunSavedDataDocument? PreservedDocument { get; set; }

        public bool TryGet(RunSavedDataSlotKey key, out object value)
        {
            return _values.TryGetValue(key, out value!);
        }

        public void Set(RunSavedDataSlotKey key, object value, bool dirty = true)
        {
            _values[key] = value;
            if (dirty)
                _dirty.Add(key);
        }

        public bool Remove(RunSavedDataSlotKey key)
        {
            _dirty.Add(key);
            return _values.Remove(key);
        }

        public bool IsDirty(RunSavedDataSlotKey key)
        {
            return _dirty.Contains(key);
        }
    }

    internal readonly record struct RunSavedDataSlotKey(string ModId, string Key, RunSavedDataKind Kind);

    internal enum RunSavedDataKind
    {
        Run,
        Player,
    }

    internal static class RunSavedDataRuntime
    {
        private static readonly ConditionalWeakTable<RunState, RunSavedDataBag> RunBags = [];

        private static readonly ConditionalWeakTable<SerializableRun, RunSavedDataDocumentBox> SerializableDocuments =
            [];

        public static RunSavedDataBag GetBag(RunState runState)
        {
            ArgumentNullException.ThrowIfNull(runState);
            return RunBags.GetValue(runState, _ => new());
        }

        public static bool TryGetBag(RunState runState, out RunSavedDataBag bag)
        {
            ArgumentNullException.ThrowIfNull(runState);
            return RunBags.TryGetValue(runState, out bag!);
        }

        public static void AttachDocument(SerializableRun save, RunSavedDataDocument? document)
        {
            ArgumentNullException.ThrowIfNull(save);
            SerializableDocuments.Remove(save);
            if (document is { IsEmpty: false })
                SerializableDocuments.Add(save, new(document));
        }

        public static bool TryGetDocument(SerializableRun save, out RunSavedDataDocument document)
        {
            ArgumentNullException.ThrowIfNull(save);
            if (SerializableDocuments.TryGetValue(save, out var box))
            {
                document = box.Document;
                return true;
            }

            document = null!;
            return false;
        }

        private sealed class RunSavedDataDocumentBox(RunSavedDataDocument document)
        {
            public RunSavedDataDocument Document { get; } = document;
        }
    }
}
