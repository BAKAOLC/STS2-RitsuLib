using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
#if STS2_AT_LEAST_0_109_0
using SavedPropertyCache = MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache;

#else
using SavedPropertyCache = MegaCrit.Sts2.Core.Saves.Runs.SavedPropertiesTypeCache;
#endif

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Stores mod-attached state on arbitrary reference objects and bridges it through <see cref="SavedProperties" />
    ///     for models that participate in vanilla save serialization.
    ///     在任意引用对象上存储 mod 附加状态，并通过 <see cref="SavedProperties" /> 桥接它
    ///     以支持参与原版保存序列化的模型。
    /// </summary>
    public sealed class SavedAttachedState<TKey, TValue> where TKey : class
    {
        private readonly SavedAttachedStateRegistration<TKey, TValue> _registration;
        private readonly ConditionalWeakTable<TKey, Box> _table = [];
        private readonly Func<TKey, TValue> _valueFactory;

        /// <summary>
        ///     Creates persisted attached state using an optional parameterless factory for default values.
        ///     使用可选的无参工厂为默认值创建持久化附加状态。
        /// </summary>
        public SavedAttachedState(string name, Func<TValue>? defaultValueFactory = null, int order = 0)
            : this(name, _ => defaultValueFactory != null ? defaultValueFactory() : default!, order)
        {
        }

        /// <summary>
        ///     Creates persisted attached state using an optional per-key factory for default values.
        ///     使用可选的按键工厂为默认值创建持久化附加状态。
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
        ///     获取或设置 <paramref name="key" /> 的附加值。
        /// </summary>
        public TValue this[TKey key]
        {
            get => GetOrCreate(key);
            set => Set(key, value);
        }

        /// <summary>
        ///     Determines whether an entry exists for <paramref name="key" /> (without creating one).
        ///     确定 <paramref name="key" /> 是否存在条目（不会创建条目）。
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _table.TryGetValue(key, out _);
        }

        /// <summary>
        ///     Adds an entry for <paramref name="key" /> if absent.
        ///     如果缺少 <paramref name="key" />，则添加条目。
        /// </summary>
        /// <returns>
        ///     True if the entry was added; false if <paramref name="key" /> already had a value.
        ///     如果已添加条目，则为 true；如果 <paramref name="key" /> 已有值，则为 false。
        /// </returns>
        public bool TryAdd(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _table.TryAdd(key, new(value));
        }

        /// <summary>
        ///     Adds an entry for <paramref name="key" />.
        ///     为 <paramref name="key" /> 添加条目。
        /// </summary>
        /// <exception cref="ArgumentException">An entry for <paramref name="key" /> already exists.</exception>
        public void Add(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            if (!_table.TryAdd(key, new(value)))
                throw new ArgumentException("An item with the same key has already been added.", nameof(key));
        }

        /// <summary>
        ///     Returns the existing value for <paramref name="key" /> or adds <paramref name="value" /> and returns it.
        ///     返回 <paramref name="key" /> 的现有值；如果不存在，则添加并返回 <paramref name="value" />。
        /// </summary>
        public TValue GetOrAdd(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _table.GetValue(key, _ => new(value)).Value;
        }

        /// <summary>
        ///     Returns the existing value for <paramref name="key" /> or creates one with <paramref name="valueFactory" />.
        ///     返回 <paramref name="key" /> 的现有值；如果不存在，则用 <paramref name="valueFactory" /> 创建一个。
        /// </summary>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(valueFactory);
            return _table.GetValue(key, k => new(valueFactory(k))).Value;
        }

        /// <summary>
        ///     Returns the existing value for <paramref name="key" /> or creates and stores one.
        ///     返回 <paramref name="key" /> 的现有值；如果不存在，则创建并存储一个。
        /// </summary>
        public TValue GetOrCreate(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _table.GetValue(key, k => new(_valueFactory(k))).Value;
        }

        /// <summary>
        ///     Returns the value for <paramref name="key" /> if present; otherwise <c>default(TValue)</c>.
        ///     如果存在，则返回 <paramref name="key" /> 的值；否则返回 <c>default(TValue)</c>。
        /// </summary>
        public TValue? GetValueOrDefault(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return TryGetValue(key, out var value) ? value : default;
        }

        /// <summary>
        ///     Returns the value for <paramref name="key" /> if present; otherwise <paramref name="defaultValue" />.
        ///     如果存在，则返回 <paramref name="key" /> 的值；否则返回 <paramref name="defaultValue" />。
        /// </summary>
        public TValue GetValueOrDefault(TKey key, TValue defaultValue)
        {
            ArgumentNullException.ThrowIfNull(key);
            return TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        ///     Attempts to read the attached value without creating it.
        ///     尝试读取附加值，但不创建它。
        /// </summary>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
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
        ///     替换 <paramref name="key" /> 的已存储值，并返回 <paramref name="value" />。
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
        ///     使用 <paramref name="updater" /> 原地修改已存储值。
        ///     使用 <c>updater</c> 原地修改已存储值。
        /// </summary>
        public TValue Update(TKey key, Func<TValue, TValue> updater)
        {
            ArgumentNullException.ThrowIfNull(updater);
            var updated = updater(GetOrCreate(key));
            return Set(key, updated);
        }

        /// <summary>
        ///     Removes any value attached to <paramref name="key" />.
        ///     移除附加到 <paramref name="key" /> 的任何值。
        /// </summary>
        /// <returns>
        ///     True if an entry was removed.
        ///     如果已移除条目，则为 true。
        /// </returns>
        public bool Remove(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return TryRemove(key, out _);
        }

        /// <summary>
        ///     Removes the value attached to <paramref name="key" /> if present.
        ///     如果存在，则移除附加到 <paramref name="key" /> 的值。
        /// </summary>
        /// <returns>
        ///     True if an entry was removed.
        ///     如果已移除条目，则为 true。
        /// </returns>
        public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (!_table.TryGetValue(key, out var box))
            {
                value = default!;
                return false;
            }

            var extracted = box.Value;
            if (!_table.Remove(key))
            {
                value = default!;
                return false;
            }

            value = extracted;
            return true;
        }

        /// <summary>
        ///     Removes all in-memory entries from the table (does not unregister the saved field or alter disk data).
        ///     移除表中的所有内存条目（不会注销已保存字段，也不会更改磁盘数据）。
        /// </summary>
        public void Clear()
        {
            _table.Clear();
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
                return SavedAttachedStateRegistry.AddToProperties(
                    props,
                    Name,
                    owner.GetOrCreate((TSavedKey)model));
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
        private static bool _propertyNamesFinalized;

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
                ThrowIfPropertyNamesFinalized(state.Name);
                if (!RegisteredNames.Add(state.Name))
                    throw new InvalidOperationException($"SavedAttachedState name is not unique: {state.Name}");

                RegisteredStates.Add(state);
                RegisteredStates.Sort(static (a, b) =>
                {
                    var orderCompare = a.Order.CompareTo(b.Order);
                    return orderCompare != 0 ? orderCompare : string.CompareOrdinal(a.Name, b.Name);
                });
            }
        }

        internal static void RegisterPropertyName(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            lock (SyncRoot)
            {
                ThrowIfPropertyNamesFinalized(name);
                RegisteredNames.Add(name);
            }
        }

        internal static IReadOnlyList<string> FinalizePropertyNameRegistration(bool injectIntoBaseGameCache = true)
        {
            string[] names;
            lock (SyncRoot)
            {
                if (_propertyNamesFinalized)
                    return [];

                _propertyNamesFinalized = true;
                names =
                [
                    .. RegisteredNames
                        .OrderBy(static name => name, StringComparer.Ordinal),
                ];
            }

            // ReSharper disable once InvertIf
            if (injectIntoBaseGameCache)
                foreach (var name in names)
                    InjectNameIntoBaseGameCache(name);

            return names;
        }

        internal static IReadOnlyList<ISavedAttachedState> GetStatesForModel(object model)
        {
            ArgumentNullException.ThrowIfNull(model);

            lock (SyncRoot)
            {
                return [.. RegisteredStates.Where(state => state.TargetType.IsInstanceOfType(model))];
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
                    (props.intArrays ??= []).Add(new(name, [.. enums.Select(Convert.ToInt32)]));
                    return true;
                case SerializableCard[] cardArray:
                    (props.cardArrays ??= []).Add(new(name, cardArray));
                    return true;
                case List<SerializableCard> cardList:
                    (props.cardArrays ??= []).Add(new(name, [.. cardList]));
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
                typeof(SavedPropertyCache),
                "_propertyNameToNetIdMap");
            var idToProperty = AccessTools.StaticFieldRefAccess<List<string>>(
                typeof(SavedPropertyCache),
                "_netIdToPropertyNameMap");

            if (propertyToId.ContainsKey(name))
                throw new InvalidOperationException(
                    $"SavedAttachedState name is not unique in the saved-property net-id cache: {name}");

            propertyToId[name] = idToProperty.Count;
            idToProperty.Add(name);

            var newBitSize = (int)Math.Ceiling(Math.Log2(idToProperty.Count));
#if STS2_AT_LEAST_0_109_0
            AccessTools.Property(typeof(SavedPropertyCache), nameof(SavedPropertyCache.PropertyIdBitSize))
#else
            AccessTools.Property(typeof(SavedPropertyCache), nameof(SavedPropertyCache.NetIdBitSize))
#endif
                ?.SetValue(null, newBitSize);
        }

        private static void ThrowIfPropertyNamesFinalized(string name)
        {
            if (_propertyNamesFinalized)
                throw new InvalidOperationException(
                    $"SavedProperties extension property name '{name}' was registered after saved-property cache finalization. " +
                    "Register SavedAttachedState and ModelSavedData during mod type discovery or mod initialization.");
        }
    }
}
