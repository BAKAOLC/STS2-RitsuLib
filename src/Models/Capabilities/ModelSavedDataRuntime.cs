using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    internal sealed class ModelSavedDataBag
    {
        private readonly HashSet<ModelSavedDataSlotKey> _dirty = [];
        private readonly Dictionary<ModelSavedDataSlotKey, object> _values = [];

        public ModelSavedDataDocument? PreservedDocument { get; set; }

        public bool IsInitialized { get; set; }

        internal void ResetForDocument(ModelSavedDataDocument? document)
        {
            _dirty.Clear();
            _values.Clear();
            PreservedDocument = document;
            IsInitialized = false;
        }

        public bool TryGet(ModelSavedDataSlotKey key, out object value)
        {
            return _values.TryGetValue(key, out value!);
        }

        public void Set(ModelSavedDataSlotKey key, object value, bool dirty = true)
        {
            _values[key] = value;
            if (dirty)
                _dirty.Add(key);
        }

        public bool Remove(ModelSavedDataSlotKey key)
        {
            _dirty.Add(key);
            return _values.Remove(key);
        }

        public bool IsDirty(ModelSavedDataSlotKey key)
        {
            return _dirty.Contains(key);
        }
    }

    internal static class ModelSavedDataRuntime
    {
        internal const string SavedPropertiesName = "RitsuLib_ModelSavedData";

        private static readonly ConditionalWeakTable<AbstractModel, ModelSavedDataBag> ModelBags = [];

        public static ModelSavedDataBag GetBag(AbstractModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            var bag = ModelBags.GetValue(model, _ => new());
            ModelSavedDataRegistry.EnsureImported(model, bag);
            return bag;
        }

        public static bool TryGetBag(AbstractModel model, out ModelSavedDataBag bag)
        {
            ArgumentNullException.ThrowIfNull(model);
            if (!ModelBags.TryGetValue(model, out bag!))
                return false;

            ModelSavedDataRegistry.EnsureImported(model, bag);
            return true;
        }

        public static void AttachDocument(AbstractModel model, ModelSavedDataDocument? document)
        {
            ArgumentNullException.ThrowIfNull(model);
            if (ModelCapabilityUpgradeReplayContext.TryDeferCardModelSavedDataImport(model, document))
                return;

            AttachDocumentImmediate(model, document);
        }

        internal static void AttachDocumentImmediate(AbstractModel model, ModelSavedDataDocument? document)
        {
            ArgumentNullException.ThrowIfNull(model);
            var bag = ModelBags.GetValue(model, _ => new());
            bag.ResetForDocument(document);
            ModelSavedDataRegistry.EnsureImported(model, bag);
        }
    }
}
