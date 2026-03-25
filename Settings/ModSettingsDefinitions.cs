using System.Text.Json;
using Godot;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    public interface IModSettingsBinding
    {
        string ModId { get; }
        string DataKey { get; }
        SaveScope Scope { get; }
        void Save();
    }

    public interface IModSettingsValueBinding<TValue> : IModSettingsBinding
    {
        TValue Read();
        void Write(TValue value);
    }

    public interface IDefaultModSettingsValueBinding<TValue> : IModSettingsValueBinding<TValue>
    {
        TValue CreateDefaultValue();
    }

    public interface ITransientModSettingsBinding : IModSettingsBinding
    {
    }

    public interface IStructuredModSettingsValueAdapter<TValue>
    {
        TValue Clone(TValue value);
        string Serialize(TValue value);
        bool TryDeserialize(string text, out TValue value);
    }

    public interface IStructuredModSettingsValueBinding<TValue> : IModSettingsValueBinding<TValue>
    {
        IStructuredModSettingsValueAdapter<TValue> Adapter { get; }
    }

    public static class ModSettingsBindings
    {
        public static ModSettingsValueBinding<TModel, TValue> Create<TModel, TValue>(
            string modId,
            string dataKey,
            SaveScope scope,
            Func<TModel, TValue> getter,
            Action<TModel, TValue> setter)
            where TModel : class, new()
        {
            return new(modId, dataKey, scope, getter, setter);
        }

        public static ModSettingsValueBinding<TModel, TValue> Global<TModel, TValue>(
            string modId,
            string dataKey,
            Func<TModel, TValue> getter,
            Action<TModel, TValue> setter)
            where TModel : class, new()
        {
            return Create(modId, dataKey, SaveScope.Global, getter, setter);
        }

        public static ModSettingsValueBinding<TModel, TValue> Profile<TModel, TValue>(
            string modId,
            string dataKey,
            Func<TModel, TValue> getter,
            Action<TModel, TValue> setter)
            where TModel : class, new()
        {
            return Create(modId, dataKey, SaveScope.Profile, getter, setter);
        }

        public static InMemoryModSettingsValueBinding<TValue> InMemory<TValue>(
            string modId,
            string dataKey,
            TValue initialValue)
        {
            return new(modId, dataKey, initialValue);
        }

        public static StructuredModSettingsValueBinding<TValue> WithAdapter<TValue>(
            IModSettingsValueBinding<TValue> inner,
            IStructuredModSettingsValueAdapter<TValue> adapter)
        {
            return new(inner, adapter);
        }

        public static DefaultModSettingsValueBinding<TValue> WithDefault<TValue>(
            IModSettingsValueBinding<TValue> inner,
            Func<TValue> defaultValueFactory,
            IStructuredModSettingsValueAdapter<TValue>? adapter = null)
        {
            return new(inner, defaultValueFactory, adapter);
        }

        public static ProjectedModSettingsValueBinding<TSource, TValue> Project<TSource, TValue>(
            IModSettingsValueBinding<TSource> parent,
            string dataKey,
            Func<TSource, TValue> getter,
            Func<TSource, TValue, TSource> setter,
            IStructuredModSettingsValueAdapter<TValue>? adapter = null)
        {
            return new(parent, dataKey, getter, setter, adapter);
        }
    }

    public static class ModSettingsStructuredData
    {
        public static IStructuredModSettingsValueAdapter<TValue> Json<TValue>(JsonSerializerOptions? options = null)
        {
            return new JsonStructuredValueAdapter<TValue>(options);
        }

        public static IStructuredModSettingsValueAdapter<List<TItem>> List<TItem>(
            IStructuredModSettingsValueAdapter<TItem>? itemAdapter = null,
            JsonSerializerOptions? options = null)
        {
            return new ListStructuredValueAdapter<TItem>(itemAdapter, options);
        }
    }

    public sealed class ModSettingsValueBinding<TModel, TValue>(
        string modId,
        string dataKey,
        SaveScope scope,
        Func<TModel, TValue> getter,
        Action<TModel, TValue> setter)
        : IModSettingsValueBinding<TValue>
        where TModel : class, new()
    {
        public string ModId { get; } = modId;
        public string DataKey { get; } = dataKey;
        public SaveScope Scope { get; } = scope;

        public TValue Read()
        {
            var store = RitsuLibFramework.GetDataStore(ModId);
            return getter(store.Get<TModel>(DataKey));
        }

        public void Write(TValue value)
        {
            var store = RitsuLibFramework.GetDataStore(ModId);
            store.Modify<TModel>(DataKey, model => setter(model, value));
        }

        public void Save()
        {
            RitsuLibFramework.GetDataStore(ModId).Save(DataKey);
        }
    }

    public sealed class InMemoryModSettingsValueBinding<TValue>(string modId, string dataKey, TValue initialValue)
        : IStructuredModSettingsValueBinding<TValue>, ITransientModSettingsBinding,
            IDefaultModSettingsValueBinding<TValue>
    {
        private readonly TValue _defaultValue = initialValue;
        private TValue _value = initialValue;

        public TValue CreateDefaultValue()
        {
            return Adapter.Clone(_defaultValue);
        }

        public string ModId { get; } = modId;
        public string DataKey { get; } = dataKey;
        public SaveScope Scope { get; } = SaveScope.Global;
        public IStructuredModSettingsValueAdapter<TValue> Adapter { get; } = ModSettingsStructuredData.Json<TValue>();

        public TValue Read()
        {
            return _value;
        }

        public void Write(TValue value)
        {
            _value = value;
        }

        public void Save()
        {
        }
    }

    public sealed class StructuredModSettingsValueBinding<TValue>(
        IModSettingsValueBinding<TValue> inner,
        IStructuredModSettingsValueAdapter<TValue> adapter)
        : IStructuredModSettingsValueBinding<TValue>
    {
        public string ModId => inner.ModId;
        public string DataKey => inner.DataKey;
        public SaveScope Scope => inner.Scope;
        public IStructuredModSettingsValueAdapter<TValue> Adapter { get; } = adapter;

        public TValue Read()
        {
            return inner.Read();
        }

        public void Write(TValue value)
        {
            inner.Write(value);
        }

        public void Save()
        {
            inner.Save();
        }
    }

    public sealed class ProjectedModSettingsValueBinding<TSource, TValue>(
        IModSettingsValueBinding<TSource> parent,
        string dataKey,
        Func<TSource, TValue> getter,
        Func<TSource, TValue, TSource> setter,
        IStructuredModSettingsValueAdapter<TValue>? adapter = null)
        : IStructuredModSettingsValueBinding<TValue>
    {
        public string ModId => parent.ModId;
        public string DataKey => string.IsNullOrWhiteSpace(dataKey) ? parent.DataKey : $"{parent.DataKey}.{dataKey}";
        public SaveScope Scope => parent.Scope;

        public IStructuredModSettingsValueAdapter<TValue> Adapter { get; } =
            adapter ?? ModSettingsStructuredData.Json<TValue>();

        public TValue Read()
        {
            return getter(parent.Read());
        }

        public void Write(TValue value)
        {
            var source = parent.Read();
            parent.Write(setter(source, value));
        }

        public void Save()
        {
            parent.Save();
        }
    }

    public sealed class DefaultModSettingsValueBinding<TValue>(
        IModSettingsValueBinding<TValue> inner,
        Func<TValue> defaultValueFactory,
        IStructuredModSettingsValueAdapter<TValue>? adapter = null)
        : IStructuredModSettingsValueBinding<TValue>, IDefaultModSettingsValueBinding<TValue>
    {
        public TValue CreateDefaultValue()
        {
            return defaultValueFactory();
        }

        public string ModId => inner.ModId;
        public string DataKey => inner.DataKey;
        public SaveScope Scope => inner.Scope;

        public IStructuredModSettingsValueAdapter<TValue> Adapter { get; } =
            inner is IStructuredModSettingsValueBinding<TValue> structured
                ? structured.Adapter
                : adapter ?? ModSettingsStructuredData.Json<TValue>();

        public TValue Read()
        {
            return inner.Read();
        }

        public void Write(TValue value)
        {
            inner.Write(value);
        }

        public void Save()
        {
            inner.Save();
        }
    }

    internal sealed class JsonStructuredValueAdapter<TValue>(JsonSerializerOptions? options)
        : IStructuredModSettingsValueAdapter<TValue>
    {
        public TValue Clone(TValue value)
        {
            var json = JsonSerializer.Serialize(value, options);
            return JsonSerializer.Deserialize<TValue>(json, options)!;
        }

        public string Serialize(TValue value)
        {
            return JsonSerializer.Serialize(value, options);
        }

        public bool TryDeserialize(string text, out TValue value)
        {
            try
            {
                value = JsonSerializer.Deserialize<TValue>(text, options)!;
                return true;
            }
            catch
            {
                value = default!;
                return false;
            }
        }
    }

    internal sealed class ListStructuredValueAdapter<TItem>(
        IStructuredModSettingsValueAdapter<TItem>? itemAdapter,
        JsonSerializerOptions? options)
        : IStructuredModSettingsValueAdapter<List<TItem>>
    {
        public List<TItem> Clone(List<TItem> value)
        {
            return itemAdapter == null ? value.ToList() : value.Select(itemAdapter.Clone).ToList();
        }

        public string Serialize(List<TItem> value)
        {
            return JsonSerializer.Serialize(value, options);
        }

        public bool TryDeserialize(string text, out List<TItem> value)
        {
            try
            {
                value = JsonSerializer.Deserialize<List<TItem>>(text, options) ?? [];
                return true;
            }
            catch
            {
                value = [];
                return false;
            }
        }
    }

    public readonly record struct ModSettingsChoiceOption<TValue>(TValue Value, ModSettingsText Label);

    public enum ModSettingsChoicePresentation
    {
        Stepper = 0,
        Dropdown = 1,
    }

    public enum ModSettingsButtonTone
    {
        Normal = 0,
        Accent = 1,
        Danger = 2,
    }

    public sealed class ModSettingsPage
    {
        internal ModSettingsPage(
            string modId,
            string id,
            string? parentPageId,
            ModSettingsText? title,
            ModSettingsText? description,
            int sortOrder,
            IReadOnlyList<ModSettingsSection> sections)
        {
            ModId = modId;
            Id = id;
            ParentPageId = parentPageId;
            Title = title;
            Description = description;
            SortOrder = sortOrder;
            Sections = sections;
        }

        public string ModId { get; }
        public string Id { get; }
        public string? ParentPageId { get; }
        public ModSettingsText? Title { get; }
        public ModSettingsText? Description { get; }
        public int SortOrder { get; }
        public IReadOnlyList<ModSettingsSection> Sections { get; }
    }

    public sealed class ModSettingsSection
    {
        internal ModSettingsSection(
            string id,
            ModSettingsText? title,
            ModSettingsText? description,
            bool isCollapsible,
            bool startCollapsed,
            IReadOnlyList<ModSettingsEntryDefinition> entries)
        {
            Id = id;
            Title = title;
            Description = description;
            IsCollapsible = isCollapsible;
            StartCollapsed = startCollapsed;
            Entries = entries;
        }

        public string Id { get; }
        public ModSettingsText? Title { get; }
        public ModSettingsText? Description { get; }
        public bool IsCollapsible { get; }
        public bool StartCollapsed { get; }
        public IReadOnlyList<ModSettingsEntryDefinition> Entries { get; }
    }

    public abstract class ModSettingsEntryDefinition
    {
        protected ModSettingsEntryDefinition(string id, ModSettingsText label, ModSettingsText? description)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(label);

            Id = id;
            Label = label;
            Description = description;
        }

        public string Id { get; }
        public ModSettingsText Label { get; }
        public ModSettingsText? Description { get; }

        internal abstract Control CreateControl(ModSettingsUiContext context);
    }

    public sealed class ToggleModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<bool> binding,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public IModSettingsValueBinding<bool> Binding { get; } = binding;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateToggleEntry(context, this);
        }
    }

    public sealed class SliderModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<float> binding,
        float minValue,
        float maxValue,
        float step,
        Func<float, string>? valueFormatter,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public IModSettingsValueBinding<float> Binding { get; } = binding;
        public float MinValue { get; } = minValue;
        public float MaxValue { get; } = maxValue;
        public float Step { get; } = step;
        public Func<float, string>? ValueFormatter { get; } = valueFormatter;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateSliderEntry(context, this);
        }
    }

    public sealed class ChoiceModSettingsEntryDefinition<TValue>(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<TValue> binding,
        IReadOnlyList<ModSettingsChoiceOption<TValue>> options,
        ModSettingsChoicePresentation presentation,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public IModSettingsValueBinding<TValue> Binding { get; } = binding;
        public IReadOnlyList<ModSettingsChoiceOption<TValue>> Options { get; } = options;
        public ModSettingsChoicePresentation Presentation { get; } = presentation;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateChoiceEntry(context, this);
        }
    }

    public sealed class ColorModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<string> binding,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public IModSettingsValueBinding<string> Binding { get; } = binding;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateColorEntry(context, this);
        }
    }

    public sealed class KeyBindingModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<string> binding,
        bool allowModifierCombos,
        bool allowModifierOnly,
        bool distinguishModifierSides,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public IModSettingsValueBinding<string> Binding { get; } = binding;
        public bool AllowModifierCombos { get; } = allowModifierCombos;
        public bool AllowModifierOnly { get; } = allowModifierOnly;
        public bool DistinguishModifierSides { get; } = distinguishModifierSides;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateKeyBindingEntry(context, this);
        }
    }

    public sealed class ButtonModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        ModSettingsText buttonText,
        Action action,
        ModSettingsButtonTone tone,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public ModSettingsText ButtonText { get; } = buttonText;
        public Action Action { get; } = action;
        public ModSettingsButtonTone Tone { get; } = tone;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateButtonEntry(context, this);
        }
    }

    public sealed class HeaderModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateHeaderEntry(context, this);
        }
    }

    public sealed class ParagraphModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateParagraphEntry(context, this);
        }
    }

    public sealed class ImageModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        Func<Texture2D?> textureProvider,
        float previewHeight,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public Func<Texture2D?> TextureProvider { get; } = textureProvider;
        public float PreviewHeight { get; } = previewHeight;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateImageEntry(context, this);
        }
    }

    public sealed class ModSettingsListItemContext<TItem>
    {
        private readonly Action? _duplicate;
        private readonly Action? _moveDown;
        private readonly Action? _moveUp;
        private readonly Action _remove;
        private readonly Action _requestRefresh;
        private readonly ModSettingsUiContext _uiContext;
        private readonly Action<TItem> _update;

        internal ModSettingsListItemContext(
            ModSettingsUiContext uiContext,
            IModSettingsValueBinding<TItem> binding,
            int index,
            int itemCount,
            TItem item,
            Action<TItem> update,
            Action? moveUp,
            Action? moveDown,
            Action? duplicate,
            Action remove,
            Action requestRefresh)
        {
            _uiContext = uiContext;
            Binding = binding;
            Index = index;
            ItemCount = itemCount;
            Item = item;
            _update = update;
            _moveUp = moveUp;
            _moveDown = moveDown;
            _duplicate = duplicate;
            _remove = remove;
            _requestRefresh = requestRefresh;
        }

        public int Index { get; }
        public int ItemCount { get; }
        public TItem Item { get; }
        public bool CanMoveUp => Index > 0;
        public bool CanMoveDown => Index < ItemCount - 1;
        public IModSettingsValueBinding<TItem> Binding { get; }

        public bool SupportsStructuredClipboard => Binding is IStructuredModSettingsValueBinding<TItem>;

        public void Update(TItem item)
        {
            _update(item);
        }

        public void Remove()
        {
            _remove();
        }

        public void MoveUp()
        {
            _moveUp?.Invoke();
        }

        public void MoveDown()
        {
            _moveDown?.Invoke();
        }

        public void Duplicate()
        {
            _duplicate?.Invoke();
        }

        public void RequestRefresh()
        {
            _requestRefresh();
        }

        public bool TryCopyToClipboard(ModSettingsClipboardScope scope = ModSettingsClipboardScope.Self)
        {
            if (Binding is not IStructuredModSettingsValueBinding<TItem> structured)
                return false;

            DisplayServer.ClipboardSet(JsonSerializer.Serialize(new ModSettingsClipboardEnvelope(
                "ritsulib.settings.value",
                typeof(TItem).FullName ?? typeof(TItem).Name,
                scope,
                structured.Adapter.Serialize(Item))));
            return true;
        }

        public bool CanPasteFromClipboard()
        {
            if (Binding is not IStructuredModSettingsValueBinding<TItem> structured)
                return false;

            var clipboard = DisplayServer.ClipboardGet();
            if (string.IsNullOrWhiteSpace(clipboard))
                return false;

            try
            {
                var envelope = JsonSerializer.Deserialize<ModSettingsClipboardEnvelope>(clipboard);
                if (envelope is { Kind: "ritsulib.settings.value" }
                    && string.Equals(envelope.TypeName, typeof(TItem).FullName ?? typeof(TItem).Name,
                        StringComparison.Ordinal))
                    return structured.Adapter.TryDeserialize(envelope.Payload, out _);
            }
            catch
            {
                // ignored
            }

            return structured.Adapter.TryDeserialize(clipboard, out _);
        }

        public bool TryPasteFromClipboard()
        {
            if (Binding is not IStructuredModSettingsValueBinding<TItem> structured)
                return false;

            var clipboard = DisplayServer.ClipboardGet();
            if (string.IsNullOrWhiteSpace(clipboard))
                return false;

            TItem value;
            try
            {
                var envelope = JsonSerializer.Deserialize<ModSettingsClipboardEnvelope>(clipboard);
                if (envelope is { Kind: "ritsulib.settings.value" }
                    && string.Equals(envelope.TypeName, typeof(TItem).FullName ?? typeof(TItem).Name,
                        StringComparison.Ordinal))
                {
                    if (!structured.Adapter.TryDeserialize(envelope.Payload, out value))
                        return false;
                }
                else if (!structured.Adapter.TryDeserialize(clipboard, out value))
                {
                    return false;
                }
            }
            catch
            {
                if (!structured.Adapter.TryDeserialize(clipboard, out value))
                    return false;
            }

            Update(value);
            return true;
        }

        public IModSettingsValueBinding<TValue> Project<TValue>(
            string dataKey,
            Func<TItem, TValue> getter,
            Func<TItem, TValue, TItem> setter,
            IStructuredModSettingsValueAdapter<TValue>? adapter = null)
        {
            return ModSettingsBindings.Project(Binding, dataKey, getter, setter, adapter);
        }

        public Control CreateEntry(ModSettingsEntryDefinition entry)
        {
            return entry.CreateControl(_uiContext);
        }

        public Control CreateListEditor<TChild>(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<List<TChild>> binding,
            Func<TChild> createItem,
            Func<TChild, ModSettingsText> itemLabel,
            Func<TChild, ModSettingsText?>? itemDescription = null,
            Func<ModSettingsListItemContext<TChild>, Control>? itemEditorFactory = null,
            ModSettingsText? addButtonText = null,
            ModSettingsText? description = null)
        {
            return CreateEntry(new ListModSettingsEntryDefinition<TChild>(
                id,
                label,
                binding,
                createItem,
                itemLabel,
                itemDescription,
                itemEditorFactory,
                null,
                addButtonText ?? ModSettingsText.I18N(ModSettingsLocalization.Instance, "button.add", "Add"),
                description));
        }
    }

    public sealed class ListModSettingsEntryDefinition<TItem>(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<List<TItem>> binding,
        Func<TItem> createItem,
        Func<TItem, ModSettingsText> itemLabel,
        Func<TItem, ModSettingsText?>? itemDescription,
        Func<ModSettingsListItemContext<TItem>, Control>? itemEditorFactory,
        IStructuredModSettingsValueAdapter<TItem>? itemDataAdapter,
        ModSettingsText addButtonText,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public IModSettingsValueBinding<List<TItem>> Binding { get; } =
            binding is IStructuredModSettingsValueBinding<List<TItem>>
                ? binding
                : ModSettingsBindings.WithAdapter(binding, ModSettingsStructuredData.List(itemDataAdapter));

        public Func<TItem> CreateItem { get; } = createItem;
        public Func<TItem, ModSettingsText> ItemLabel { get; } = itemLabel;
        public Func<TItem, ModSettingsText?>? ItemDescription { get; } = itemDescription;
        public Func<ModSettingsListItemContext<TItem>, Control>? ItemEditorFactory { get; } = itemEditorFactory;
        public IStructuredModSettingsValueAdapter<TItem>? ItemDataAdapter { get; } = itemDataAdapter;
        public ModSettingsText AddButtonText { get; } = addButtonText;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateListEntry(context, this);
        }
    }

    public sealed class IntSliderModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<int> binding,
        int minValue,
        int maxValue,
        int step,
        Func<int, string>? valueFormatter,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public IModSettingsValueBinding<int> Binding { get; } = binding;
        public int MinValue { get; } = minValue;
        public int MaxValue { get; } = maxValue;
        public int Step { get; } = step;
        public Func<int, string>? ValueFormatter { get; } = valueFormatter;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateIntSliderEntry(context, this);
        }
    }

    public sealed class SubpageModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        string targetPageId,
        ModSettingsText buttonText,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public string TargetPageId { get; } = targetPageId;
        public ModSettingsText ButtonText { get; } = buttonText;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateSubpageEntry(context, this);
        }
    }

    public sealed class ModSettingsPageBuilder
    {
        private readonly HashSet<string> _sectionIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ModSettingsSection> _sections = [];

        public ModSettingsPageBuilder(string modId, string? pageId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ModId = modId;
            PageId = string.IsNullOrWhiteSpace(pageId) ? modId : pageId;
        }

        public string ModId { get; }
        public string PageId { get; }
        public string? ParentPageId { get; private set; }
        public ModSettingsText? Title { get; private set; }
        public ModSettingsText? Description { get; private set; }
        public ModSettingsText? ModDisplayName { get; private set; }
        public int SortOrder { get; private set; }

        public ModSettingsPageBuilder AsChildOf(string parentPageId)
        {
            ParentPageId = parentPageId;
            return this;
        }

        public ModSettingsPageBuilder WithTitle(ModSettingsText title)
        {
            Title = title;
            return this;
        }

        public ModSettingsPageBuilder WithDescription(ModSettingsText description)
        {
            Description = description;
            return this;
        }

        public ModSettingsPageBuilder WithModDisplayName(ModSettingsText displayName)
        {
            ModDisplayName = displayName;
            return this;
        }

        public ModSettingsPageBuilder WithSortOrder(int sortOrder)
        {
            SortOrder = sortOrder;
            return this;
        }

        public ModSettingsPageBuilder AddSection(string id, Action<ModSettingsSectionBuilder> configure)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(configure);

            if (!_sectionIds.Add(id))
                throw new InvalidOperationException($"Duplicate settings section id '{id}' for mod '{ModId}'.");

            var builder = new ModSettingsSectionBuilder(id);
            configure(builder);
            _sections.Add(builder.Build());
            return this;
        }

        public ModSettingsPage Build()
        {
            if (_sections.Count == 0)
                throw new InvalidOperationException($"Settings page '{PageId}' for mod '{ModId}' has no sections.");

            if (ModDisplayName != null)
                ModSettingsRegistry.RegisterModDisplayName(ModId, ModDisplayName);

            return new(
                ModId,
                PageId,
                ParentPageId,
                Title,
                Description,
                SortOrder,
                _sections.ToArray()
            );
        }
    }

    public sealed class ModSettingsSectionBuilder
    {
        private readonly List<ModSettingsEntryDefinition> _entries = [];
        private readonly HashSet<string> _entryIds = new(StringComparer.OrdinalIgnoreCase);

        internal ModSettingsSectionBuilder(string id)
        {
            Id = id;
        }

        public string Id { get; }
        public ModSettingsText? Title { get; private set; }
        public ModSettingsText? Description { get; private set; }
        public bool IsCollapsible { get; private set; }
        public bool StartCollapsed { get; private set; }

        public ModSettingsSectionBuilder WithTitle(ModSettingsText title)
        {
            Title = title;
            return this;
        }

        public ModSettingsSectionBuilder WithDescription(ModSettingsText description)
        {
            Description = description;
            return this;
        }

        public ModSettingsSectionBuilder Collapsible(bool startCollapsed = false)
        {
            IsCollapsible = true;
            StartCollapsed = startCollapsed;
            return this;
        }

        public ModSettingsSectionBuilder AddHeader(
            string id,
            ModSettingsText label,
            ModSettingsText? description = null)
        {
            AddEntry(id, new HeaderModSettingsEntryDefinition(id, label, description));
            return this;
        }

        public ModSettingsSectionBuilder AddParagraph(
            string id,
            ModSettingsText text,
            ModSettingsText? description = null)
        {
            AddEntry(id, new ParagraphModSettingsEntryDefinition(id, text, description));
            return this;
        }

        public ModSettingsSectionBuilder AddImage(
            string id,
            ModSettingsText label,
            Func<Texture2D?> textureProvider,
            float previewHeight = 160f,
            ModSettingsText? description = null)
        {
            ArgumentNullException.ThrowIfNull(textureProvider);
            AddEntry(id, new ImageModSettingsEntryDefinition(id, label, textureProvider, previewHeight, description));
            return this;
        }

        public ModSettingsSectionBuilder AddList<TItem>(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<List<TItem>> binding,
            Func<TItem> createItem,
            Func<TItem, ModSettingsText> itemLabel,
            Func<TItem, ModSettingsText?>? itemDescription = null,
            Func<ModSettingsListItemContext<TItem>, Control>? itemEditorFactory = null,
            IStructuredModSettingsValueAdapter<TItem>? itemDataAdapter = null,
            ModSettingsText? addButtonText = null,
            ModSettingsText? description = null)
        {
            ArgumentNullException.ThrowIfNull(createItem);
            ArgumentNullException.ThrowIfNull(itemLabel);
            AddEntry(id, new ListModSettingsEntryDefinition<TItem>(
                id,
                label,
                binding,
                createItem,
                itemLabel,
                itemDescription,
                itemEditorFactory,
                itemDataAdapter,
                addButtonText ?? ModSettingsText.I18N(ModSettingsLocalization.Instance, "button.add", "Add"),
                description));
            return this;
        }

        public ModSettingsSectionBuilder AddToggle(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<bool> binding,
            ModSettingsText? description = null)
        {
            AddEntry(id, new ToggleModSettingsEntryDefinition(id, label, binding, description));
            return this;
        }

        public ModSettingsSectionBuilder AddIntSlider(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<int> binding,
            int minValue,
            int maxValue,
            int step = 1,
            Func<int, string>? valueFormatter = null,
            ModSettingsText? description = null)
        {
            if (maxValue < minValue)
                throw new ArgumentOutOfRangeException(nameof(maxValue), "Slider maxValue must be >= minValue.");

            if (step <= 0)
                throw new ArgumentOutOfRangeException(nameof(step), "Slider step must be > 0.");

            AddEntry(id, new IntSliderModSettingsEntryDefinition(
                id,
                label,
                binding,
                minValue,
                maxValue,
                step,
                valueFormatter,
                description));
            return this;
        }

        public ModSettingsSectionBuilder AddSlider(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<float> binding,
            float minValue,
            float maxValue,
            float step = 1f,
            Func<float, string>? valueFormatter = null,
            ModSettingsText? description = null)
        {
            if (maxValue < minValue)
                throw new ArgumentOutOfRangeException(nameof(maxValue), "Slider maxValue must be >= minValue.");

            if (step <= 0f)
                throw new ArgumentOutOfRangeException(nameof(step), "Slider step must be > 0.");

            AddEntry(id, new SliderModSettingsEntryDefinition(
                id,
                label,
                binding,
                minValue,
                maxValue,
                step,
                valueFormatter,
                description));
            return this;
        }

        public ModSettingsSectionBuilder AddChoice<TValue>(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<TValue> binding,
            IEnumerable<ModSettingsChoiceOption<TValue>> options,
            ModSettingsText? description = null,
            ModSettingsChoicePresentation presentation = ModSettingsChoicePresentation.Stepper)
        {
            ArgumentNullException.ThrowIfNull(options);
            var materializedOptions = options.ToArray();
            if (materializedOptions.Length == 0)
                throw new InvalidOperationException($"Choice setting '{id}' requires at least one option.");

            AddEntry(id, new ChoiceModSettingsEntryDefinition<TValue>(
                id,
                label,
                binding,
                materializedOptions,
                presentation,
                description));
            return this;
        }

        public ModSettingsSectionBuilder AddEnumChoice<TEnum>(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<TEnum> binding,
            Func<TEnum, ModSettingsText>? optionLabelFactory = null,
            ModSettingsText? description = null,
            ModSettingsChoicePresentation presentation = ModSettingsChoicePresentation.Stepper)
            where TEnum : struct, Enum
        {
            optionLabelFactory ??= value => ModSettingsText.Literal(value.ToString());

            return AddChoice(
                id,
                label,
                binding,
                Enum.GetValues<TEnum>()
                    .Select(value => new ModSettingsChoiceOption<TEnum>(value, optionLabelFactory(value))),
                description,
                presentation);
        }

        public ModSettingsSectionBuilder AddColor(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<string> binding,
            ModSettingsText? description = null)
        {
            AddEntry(id, new ColorModSettingsEntryDefinition(id, label, binding, description));
            return this;
        }

        public ModSettingsSectionBuilder AddKeyBinding(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<string> binding,
            bool allowModifierCombos = true,
            bool allowModifierOnly = true,
            bool distinguishModifierSides = false,
            ModSettingsText? description = null)
        {
            AddEntry(id,
                new KeyBindingModSettingsEntryDefinition(id, label, binding, allowModifierCombos, allowModifierOnly,
                    distinguishModifierSides, description));
            return this;
        }

        public ModSettingsSectionBuilder AddButton(
            string id,
            ModSettingsText label,
            ModSettingsText buttonText,
            Action action,
            ModSettingsButtonTone tone = ModSettingsButtonTone.Normal,
            ModSettingsText? description = null)
        {
            ArgumentNullException.ThrowIfNull(action);
            AddEntry(id, new ButtonModSettingsEntryDefinition(id, label, buttonText, action, tone, description));
            return this;
        }

        public ModSettingsSectionBuilder AddSubpage(
            string id,
            ModSettingsText label,
            string targetPageId,
            ModSettingsText? buttonText = null,
            ModSettingsText? description = null)
        {
            AddEntry(id,
                new SubpageModSettingsEntryDefinition(
                    id,
                    label,
                    targetPageId,
                    buttonText ?? ModSettingsText.Literal(">"),
                    description));
            return this;
        }

        internal ModSettingsSection Build()
        {
            return _entries.Count == 0
                ? throw new InvalidOperationException($"Settings section '{Id}' has no entries.")
                : new(Id, Title, Description, IsCollapsible, StartCollapsed, _entries.ToArray());
        }

        private void AddEntry(string id, ModSettingsEntryDefinition entry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            if (!_entryIds.Add(id))
                throw new InvalidOperationException($"Duplicate settings entry id '{id}' in section '{Id}'.");

            _entries.Add(entry);
        }
    }
}
