using System.Text.Json;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    internal interface IModelSavedDataSlot
    {
        string ModId { get; }
        string Key { get; }
        Type TargetType { get; }
        ModelSavedDataOptions Options { get; }
        ModelSavedDataSlotKey SlotKey { get; }
        void Import(AbstractModel model, JsonObject entry, ModelSavedDataBag bag);
        void Export(AbstractModel model, ModelSavedDataBag bag, ModelSavedDataDocument document);

        void Clone(AbstractModel prototype, AbstractModel clone, ModelSavedDataBag sourceBag,
            ModelSavedDataBag targetBag);
    }

    internal abstract class ModelSavedDataSlot<TTarget, TPayload>(
        string modId,
        string key,
        Func<TPayload>? defaultFactory,
        ModelSavedDataOptions? options)
        : IModelSavedDataSlot
        where TTarget : AbstractModel
        where TPayload : class, new()
    {
        private const string SchemaPropertyName = "schema";
        private const string TargetPropertyName = "target";
        private const string DataPropertyName = "data";

        private readonly Func<TPayload> _defaultFactory = defaultFactory ?? (() => new());

        public string ModId { get; } = modId;
        public string Key { get; } = key;
        public Type TargetType { get; } = typeof(TTarget);
        public ModelSavedDataOptions Options { get; } = options ?? new();
        public ModelSavedDataSlotKey SlotKey { get; } = new(modId, key);

        public void Import(AbstractModel model, JsonObject entry, ModelSavedDataBag bag)
        {
            if (model is not TTarget target)
                return;

            try
            {
                ImportCore(target, entry, bag);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModelSavedData] Failed to import '{ModId}'::{Key} for {model.Id}: {ex.Message}");
            }
        }

        public void Export(AbstractModel model, ModelSavedDataBag bag, ModelSavedDataDocument document)
        {
            if (model is not TTarget target)
                return;

            if (!TryBuildEntry(target, bag, out var entry))
            {
                if (bag.IsDirty(SlotKey))
                    document.Remove(ModId, Key);
                return;
            }

            document.SetRaw(ModId, Key, entry);
        }

        public void Clone(AbstractModel prototype, AbstractModel clone, ModelSavedDataBag sourceBag,
            ModelSavedDataBag targetBag)
        {
            if (prototype is not TTarget || clone is not TTarget)
                return;

            switch (Options.ClonePolicy)
            {
                case ModelSavedDataClonePolicy.Drop:
                    targetBag.Remove(SlotKey);
                    break;
                case ModelSavedDataClonePolicy.Share:
                    if (sourceBag.TryGet(SlotKey, out var shared))
                        targetBag.Set(SlotKey, shared, sourceBag.IsDirty(SlotKey));
                    break;
                default:
                    if (sourceBag.TryGet(SlotKey, out var raw) && raw is TPayload typed)
                        targetBag.Set(SlotKey, DeepClone(typed), sourceBag.IsDirty(SlotKey));
                    break;
            }
        }

        public TPayload GetOrCreate(TTarget model)
        {
            var bag = ModelSavedDataRuntime.GetBag(model);
            if (bag.TryGet(SlotKey, out var value) && value is TPayload typed)
                return typed;

            var created = _defaultFactory();
            bag.Set(SlotKey, created, false);
            return created;
        }

        public bool TryGet(TTarget model, out TPayload value)
        {
            if (ModelSavedDataRuntime.TryGetBag(model, out var bag))
                if (bag.TryGet(SlotKey, out var raw) && raw is TPayload typed)
                {
                    value = typed;
                    return true;
                }

            value = null!;
            return false;
        }

        public void Set(TTarget model, TPayload value)
        {
            ArgumentNullException.ThrowIfNull(value);
            ModelSavedDataRuntime.GetBag(model).Set(SlotKey, value);
        }

        public void MarkDirty(TTarget model)
        {
            var bag = ModelSavedDataRuntime.GetBag(model);
            if (bag.TryGet(SlotKey, out var value) && value is TPayload typed)
            {
                bag.Set(SlotKey, typed);
                return;
            }

            bag.Set(SlotKey, _defaultFactory());
        }

        public bool Remove(TTarget model)
        {
            return ModelSavedDataRuntime.GetBag(model).Remove(SlotKey);
        }

        protected JsonObject CreateEntry(TPayload value)
        {
            return new()
            {
                [SchemaPropertyName] = Options.SchemaVersion,
                [TargetPropertyName] = typeof(TTarget).FullName ?? typeof(TTarget).Name,
                [DataPropertyName] = JsonSerializer.SerializeToNode(value, ModelSavedDataJson.Options),
            };
        }

        protected bool TryReadData(JsonObject entry, out TPayload value)
        {
            value = null!;
            var schema = entry[SchemaPropertyName]?.GetValue<int>() ?? 1;
            if (!TryMigrate(entry, schema, out var migrated))
                return false;

            var dataNode = migrated[DataPropertyName];
            var deserialized = dataNode?.Deserialize<TPayload>(ModelSavedDataJson.Options);
            if (deserialized == null)
                return false;

            value = deserialized;
            return true;
        }

        protected bool ShouldWrite(ModelSavedDataBag bag, object? value)
        {
            return Options.WritePolicy switch
            {
                ModelSavedDataWritePolicy.AlwaysWhenPresent => value != null,
                ModelSavedDataWritePolicy.WhenNonDefault => value != null && !IsDefaultValue((TPayload)value),
                _ => bag.IsDirty(SlotKey) && value != null,
            };
        }

        protected TPayload DeepClone(TPayload value)
        {
            var node = JsonSerializer.SerializeToNode(value, ModelSavedDataJson.Options);
            return node?.Deserialize<TPayload>(ModelSavedDataJson.Options) ?? _defaultFactory();
        }

        protected abstract bool TryBuildEntry(TTarget model, ModelSavedDataBag bag, out JsonObject entry);
        protected abstract void ImportCore(TTarget model, JsonObject entry, ModelSavedDataBag bag);

        private bool TryMigrate(JsonObject entry, int schema, out JsonObject migrated)
        {
            migrated = entry;
            if (schema == Options.SchemaVersion)
                return true;

            if (schema > Options.SchemaVersion)
                return false;

            if (Options.Migrations == null || Options.Migrations.Count == 0)
                return false;

            migrated = entry.DeepClone().AsObject();
            var current = schema;
            while (current != Options.SchemaVersion)
            {
                var migration = Options.Migrations.FirstOrDefault(m => m.FromVersion == current);
                if (migration == null || !migration.Migrate(migrated))
                    return false;

                current = migration.ToVersion;
                migrated[SchemaPropertyName] = current;
            }

            return true;
        }

        private bool IsDefaultValue(TPayload value)
        {
            try
            {
                var left = JsonSerializer.SerializeToNode(value, ModelSavedDataJson.Options)?.ToJsonString();
                var right = JsonSerializer.SerializeToNode(_defaultFactory(), ModelSavedDataJson.Options)
                    ?.ToJsonString();
                return string.Equals(left, right, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }
    }

    internal sealed class StoredModelSavedDataSlot<TTarget, TPayload>(
        string modId,
        string key,
        Func<TPayload>? defaultFactory,
        ModelSavedDataOptions? options)
        : ModelSavedDataSlot<TTarget, TPayload>(modId, key, defaultFactory, options)
        where TTarget : AbstractModel
        where TPayload : class, new()
    {
        protected override bool TryBuildEntry(TTarget model, ModelSavedDataBag bag, out JsonObject entry)
        {
            entry = null!;
            if (!bag.TryGet(SlotKey, out var raw) || raw is not TPayload value || !ShouldWrite(bag, value))
                return false;

            entry = CreateEntry(value);
            return true;
        }

        protected override void ImportCore(TTarget model, JsonObject entry, ModelSavedDataBag bag)
        {
            if (TryReadData(entry, out var value))
            {
                bag.Set(SlotKey, value, false);
                return;
            }

            RitsuLibFramework.Logger.Warn($"[ModelSavedData] Failed to read model data '{ModId}'::{Key}.");
        }
    }

    internal sealed class ComputedModelSavedDataSlot<TTarget, TPayload>(
        string modId,
        string key,
        Func<TTarget, TPayload?> exporter,
        Action<TTarget, TPayload?> importer,
        Func<TPayload>? defaultFactory,
        ModelSavedDataOptions? options)
        : ModelSavedDataSlot<TTarget, TPayload>(modId, key, defaultFactory, options)
        where TTarget : AbstractModel
        where TPayload : class, new()
    {
        protected override bool TryBuildEntry(TTarget model, ModelSavedDataBag bag, out JsonObject entry)
        {
            entry = null!;
            var value = exporter(model);
            if (value == null || !ShouldWriteComputed(value))
                return false;

            entry = CreateEntry(value);
            return true;
        }

        protected override void ImportCore(TTarget model, JsonObject entry, ModelSavedDataBag bag)
        {
            importer(model, TryReadData(entry, out var value) ? value : null);
        }

        private bool ShouldWriteComputed(TPayload value)
        {
            return Options.WritePolicy switch
            {
                ModelSavedDataWritePolicy.WhenNonDefault => !string.Equals(
                    JsonSerializer.SerializeToNode(value, ModelSavedDataJson.Options)?.ToJsonString(),
                    JsonSerializer.SerializeToNode(new TPayload(), ModelSavedDataJson.Options)
                        ?.ToJsonString(),
                    StringComparison.Ordinal),
                _ => true,
            };
        }
    }
}
