using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Models.Capabilities
{
    internal static class ModelSavedDataRegistry
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<ModelSavedDataSlotKey, IModelSavedDataSlot> Slots = [];
        private static bool _propertyNameRegistered;

        public static void EnsureInitialized()
        {
            lock (SyncRoot)
            {
                if (_propertyNameRegistered)
                    return;

                SavedAttachedStateRegistry.RegisterPropertyName(ModelSavedDataRuntime.SavedPropertiesName);
                _propertyNameRegistered = true;
            }
        }

        public static void Register(IModelSavedDataSlot slot)
        {
            ArgumentNullException.ThrowIfNull(slot);
            EnsureInitialized();

            lock (SyncRoot)
            {
                if (!Slots.TryAdd(slot.SlotKey, slot))
                    throw new InvalidOperationException(
                        $"ModelSavedData slot is already registered: {slot.ModId}::{slot.Key}");
            }
        }

        public static void EnsureImported(AbstractModel model, ModelSavedDataBag bag)
        {
            if (bag.IsInitialized)
                return;

            bag.IsInitialized = true;
            var document = bag.PreservedDocument;
            if (document == null)
                return;

            foreach (var slot in GetSlotsSnapshot(model))
                if (document.TryGetRaw(slot.ModId, slot.Key, out var entry))
                    slot.Import(model, entry, bag);
        }

        public static string? Export(AbstractModel model)
        {
            if (!ModelSavedDataRuntime.TryGetBag(model, out var bag))
                return null;

            var document = bag.PreservedDocument?.Clone() ?? new();

            foreach (var slot in GetSlotsSnapshot(model))
                slot.Export(model, bag, document);

            return document.IsEmpty ? null : document.ToJsonString();
        }

        public static void Import(AbstractModel model, string? json)
        {
            ModelSavedDataRuntime.AttachDocument(model, ModelSavedDataDocument.FromJson(json));
        }

        public static void NotifyCloned(AbstractModel prototype, AbstractModel clone)
        {
            if (!ModelSavedDataRuntime.TryGetBag(prototype, out var sourceBag))
                return;

            var targetBag = ModelSavedDataRuntime.GetBag(clone);
            targetBag.PreservedDocument = sourceBag.PreservedDocument?.Clone();

            foreach (var slot in GetSlotsSnapshot(prototype))
                slot.Clone(prototype, clone, sourceBag, targetBag);
        }

        private static IModelSavedDataSlot[] GetSlotsSnapshot(AbstractModel model)
        {
            lock (SyncRoot)
            {
                return Slots.Values
                    .Where(slot => slot.TargetType.IsInstanceOfType(model))
                    .ToArray();
            }
        }
    }
}
