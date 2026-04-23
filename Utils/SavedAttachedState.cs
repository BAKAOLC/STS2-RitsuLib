using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Stores mod-attached state on arbitrary reference objects and bridges it through <see cref="SavedProperties" />
    ///     for models that participate in vanilla save serialization.
    /// </summary>
    public sealed class SavedAttachedState<TKey, TValue> where TKey : class
    {
        private readonly SavedAttachedStateRegistration<TKey, TValue> _registration;
        private readonly ConditionalWeakTable<TKey, Box> _table = [];
        private readonly Func<TKey, TValue> _valueFactory;

        /// <summary>
        ///     Creates persisted attached state using an optional parameterless factory for default values.
        /// </summary>
        public SavedAttachedState(string name, Func<TValue>? defaultValueFactory = null, int order = 0)
            : this(name, _ => defaultValueFactory != null ? defaultValueFactory() : default!, order)
        {
        }

        /// <summary>
        ///     Creates persisted attached state using an optional per-key factory for default values.
        /// </summary>
        public SavedAttachedState(string name, Func<TKey, TValue>? valueFactory, int order = 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            SavedAttachedStateRegistry.ValidateSupportedType(typeof(TValue));

            _valueFactory = valueFactory ?? (_ => default!);
            _registration = new(typeof(TKey).Name + "_" + name, order,
                this);
            SavedAttachedStateRegistry.Register(_registration);
        }

        /// <summary>
        ///     Gets or sets the attached value for <paramref name="key" />.
        /// </summary>
        public TValue this[TKey key]
        {
            get => GetOrCreate(key);
            set => Set(key, value);
        }

        /// <summary>
        ///     Returns the existing value for <paramref name="key" /> or creates and stores one.
        /// </summary>
        public TValue GetOrCreate(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _table.GetValue(key, k => new(_valueFactory(k))).Value;
        }

        /// <summary>
        ///     Attempts to read the attached value without creating it.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (_table.TryGetValue(key, out var box))
            {
                value = box.Value;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        ///     Replaces the stored value for <paramref name="key" /> and returns <paramref name="value" />.
        /// </summary>
        public TValue Set(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            _table.Remove(key);
            _table.Add(key, new(value));
            return value;
        }

        /// <summary>
        ///     Mutates the stored value in place using <paramref name="updater" />.
        /// </summary>
        public TValue Update(TKey key, Func<TValue, TValue> updater)
        {
            ArgumentNullException.ThrowIfNull(updater);
            var updated = updater(GetOrCreate(key));
            return Set(key, updated);
        }

        /// <summary>
        ///     Removes any value attached to <paramref name="key" />.
        /// </summary>
        /// <returns>True if an entry was removed.</returns>
        public bool Remove(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _table.Remove(key);
        }

        private sealed class Box(TValue value)
        {
            public TValue Value { get; } = value;
        }

        private sealed class SavedAttachedStateRegistration<TSavedKey, TSavedValue>(
            string name,
            int order,
            SavedAttachedState<TSavedKey, TSavedValue> owner) : ISavedAttachedState where TSavedKey : class
        {
            public string Name { get; } = name;

            public int Order { get; } = order;

            public Type TargetType { get; } = typeof(TSavedKey);

            public bool Export(object model, SavedProperties props)
            {
                return owner.TryGetValue((TSavedKey)model, out var value) &&
                       SavedAttachedStateRegistry.AddToProperties(props, Name, value);
            }

            public void Import(object model, SavedProperties props)
            {
                if (SavedAttachedStateRegistry.TryGetFromProperties<TSavedValue>(props, Name, out var value))
                    owner.Set((TSavedKey)model, value!);
            }
        }
    }

    internal interface ISavedAttachedState
    {
        string Name { get; }
        int Order { get; }
        Type TargetType { get; }
        bool Export(object model, SavedProperties props);
        void Import(object model, SavedProperties props);
    }

    internal static class SavedAttachedStateRegistry
    {
        private static readonly Lock SyncRoot = new();
        private static readonly List<ISavedAttachedState> RegisteredStates = [];
        private static readonly HashSet<string> RegisteredNames = [];

        private static readonly HashSet<Type> SupportedTypes =
        [
            typeof(int),
            typeof(bool),
            typeof(string),
            typeof(ModelId),
            typeof(int[]),
            typeof(SerializableCard),
            typeof(SerializableCard[]),
            typeof(List<SerializableCard>),
        ];

        internal static void Register(ISavedAttachedState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            lock (SyncRoot)
            {
                if (!RegisteredNames.Add(state.Name))
                    throw new InvalidOperationException($"SavedAttachedState name is not unique: {state.Name}");

                RegisteredStates.Add(state);
                RegisteredStates.Sort(static (a, b) =>
                {
                    var orderCompare = a.Order.CompareTo(b.Order);
                    return orderCompare != 0 ? orderCompare : string.CompareOrdinal(a.Name, b.Name);
                });
                InjectNameIntoBaseGameCache(state.Name);
            }
        }

        internal static IReadOnlyList<ISavedAttachedState> GetStatesForModel(object model)
        {
            ArgumentNullException.ThrowIfNull(model);

            lock (SyncRoot)
            {
                return RegisteredStates.Where(state => state.TargetType.IsInstanceOfType(model)).ToArray();
            }
        }

        internal static void ValidateSupportedType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            if (SupportedTypes.Contains(type) || type.IsEnum || (type.IsArray && type.GetElementType()?.IsEnum == true))
                return;

            throw new NotSupportedException(
                $"SavedAttachedState uses unsupported type {type.Name}. Only SavedProperties-compatible value types are supported.");
        }

        internal static bool AddToProperties(SavedProperties props, string name, object? value)
        {
            switch (value)
            {
                case null:
                    return false;
                case int i:
                    (props.ints ??= []).Add(new(name, i));
                    return true;
                case bool b:
                    (props.bools ??= []).Add(new(name, b));
                    return true;
                case string s:
                    (props.strings ??= []).Add(new(name, s));
                    return true;
                case Enum e:
                    (props.ints ??= []).Add(new(name, Convert.ToInt32(e)));
                    return true;
                case ModelId modelId:
                    (props.modelIds ??= []).Add(new(name, modelId));
                    return true;
                case SerializableCard card:
                    (props.cards ??= []).Add(new(name, card));
                    return true;
                case int[] ints:
                    (props.intArrays ??= []).Add(new(name, ints));
                    return true;
                case Enum[] enums:
                    (props.intArrays ??= []).Add(new(name, enums.Select(Convert.ToInt32).ToArray()));
                    return true;
                case SerializableCard[] cardArray:
                    (props.cardArrays ??= []).Add(new(name, cardArray));
                    return true;
                case List<SerializableCard> cardList:
                    (props.cardArrays ??= []).Add(new(name, cardList.ToArray()));
                    return true;
                default:
                    return false;
            }
        }

        internal static bool TryGetFromProperties<T>(SavedProperties props, string name, out T? value)
        {
            value = default;

            if (typeof(T) == typeof(int) || typeof(T).IsEnum)
            {
                var found = props.ints?.FirstOrDefault(p => p.name == name);
                if (found == null) return false;

                value = typeof(T).IsEnum
                    ? (T)Enum.ToObject(typeof(T), found.Value.value)
                    : (T)(object)found.Value.value;
                return true;
            }

            if (typeof(T) == typeof(bool))
            {
                var found = props.bools?.FirstOrDefault(p => p.name == name);
                if (found == null) return false;

                value = (T)(object)found.Value.value;
                return true;
            }

            if (typeof(T) == typeof(string))
            {
                var found = props.strings?.FirstOrDefault(p => p.name == name);
                if (found == null) return false;

                value = (T)(object)found.Value.value;
                return true;
            }

            if (typeof(T) == typeof(ModelId))
            {
                var found = props.modelIds?.FirstOrDefault(p => p.name == name);
                if (found == null) return false;

                value = (T)(object)found.Value.value;
                return true;
            }

            if (typeof(T) == typeof(int[]) || (typeof(T).IsArray && typeof(T).GetElementType()?.IsEnum == true))
            {
                var found = props.intArrays?.FirstOrDefault(p => p.name == name);
                if (found == null) return false;

                if (typeof(T).IsArray && typeof(T).GetElementType()?.IsEnum == true)
                {
                    var enumType = typeof(T).GetElementType()!;
                    var enumArray = Array.CreateInstance(enumType, found.Value.value.Length);
                    for (var i = 0; i < found.Value.value.Length; i++)
                        enumArray.SetValue(Enum.ToObject(enumType, found.Value.value[i]), i);
                    value = (T)(object)enumArray;
                }
                else
                {
                    value = (T)(object)found.Value.value;
                }

                return true;
            }

            if (typeof(T) == typeof(SerializableCard))
            {
                var found = props.cards?.FirstOrDefault(p => p.name == name);
                if (found == null) return false;

                value = (T)(object)found.Value.value;
                return true;
            }

            if (typeof(T) != typeof(SerializableCard[]) && typeof(T) != typeof(List<SerializableCard>)) return false;
            {
                var found = props.cardArrays?.FirstOrDefault(p => p.name == name);
                if (found == null) return false;

                value = typeof(T) == typeof(List<SerializableCard>)
                    ? (T)(object)found.Value.value.ToList()
                    : (T)(object)found.Value.value;
                return true;
            }
        }

        private static void InjectNameIntoBaseGameCache(string name)
        {
            var propertyToId = AccessTools.StaticFieldRefAccess<Dictionary<string, int>>(
                typeof(SavedPropertiesTypeCache),
                "_propertyNameToNetIdMap");
            var idToProperty = AccessTools.StaticFieldRefAccess<List<string>>(
                typeof(SavedPropertiesTypeCache),
                "_netIdToPropertyNameMap");

            if (propertyToId.ContainsKey(name))
                throw new InvalidOperationException(
                    $"SavedAttachedState name is not unique in SavedPropertiesTypeCache: {name}");

            propertyToId[name] = idToProperty.Count;
            idToProperty.Add(name);

            var newBitSize = (int)Math.Ceiling(Math.Log2(idToProperty.Count));
            AccessTools.Property(typeof(SavedPropertiesTypeCache), nameof(SavedPropertiesTypeCache.NetIdBitSize))
                ?.SetValue(null, newBitSize);
        }
    }
}
