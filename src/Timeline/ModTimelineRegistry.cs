using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Diagnostics;

namespace STS2RitsuLib.Timeline
{
    /// <summary>
    ///     Per-mod registry for custom <c>EpochModel</c> and <c>StoryModel</c> types wired into the game's static
    ///     timeline dictionaries. Epochs are individual unlock slots; a <see cref="StoryModel" /> groups them into one
    ///     timeline column (story title + ordered epoch list in the progression UI).
    ///     按 mod 划分的注册表，用于将自定义 <c>EpochModel</c> 和 <c>StoryModel</c> 类型接入游戏的静态
    ///     时间线字典。纪元是单独的解锁槽；一个 <see cref="StoryModel" /> 会把它们归入一个
    ///     时间线列（进度界面中的故事标题 + 有序纪元列表）。
    /// </summary>
    public sealed class ModTimelineRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModTimelineRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<Type> RegisteredEpochTypes = [];

        private static readonly HashSet<Type> RegisteredStoryTypes = [];
        private readonly Logger _logger;

        private readonly string _modId;
        private string? _freezeReason;

        private ModTimelineRegistry(string modId)
        {
            _modId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
        }

        /// <summary>
        ///     True after the framework freezes further epoch/story registrations (e.g. at model init).
        ///     框架冻结后续 epoch/story 注册后为 true（例如模型初始化时）。
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     Returns the timeline registry singleton for <paramref name="modId" />.
        ///     返回 timeline registry 单例，用于 <paramref name="modId" />。
        /// </summary>
        public static ModTimelineRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var registry))
                    return registry;

                registry = new(modId);
                Registries[modId] = registry;
                return registry;
            }
        }

        /// <summary>
        ///     Registers a concrete epoch type so its id appears in vanilla epoch discovery.
        ///     注册具体 纪元类型 使其 id 出现在 原版纪元发现。
        /// </summary>
        public void RegisterEpoch<TEpoch>() where TEpoch : EpochModel, new()
        {
            RegisterEpoch(typeof(TEpoch));
        }

        /// <summary>
        ///     Registers <paramref name="epochType" /> so its id appears in vanilla epoch discovery.
        ///     注册 <paramref name="epochType" /> 使其 id 出现在 原版纪元发现。
        /// </summary>
        public void RegisterEpoch(Type epochType)
        {
            EnsureMutable($"register epoch '{epochType.Name}'");
            EnsureSubtype(epochType, typeof(EpochModel), nameof(epochType));

            var epochId = GetEpochId(epochType);

            lock (SyncRoot)
            {
                RegistrationConflictDetector.ThrowIfEpochIdConflicts(
                    epochId,
                    epochType,
                    GetKnownEpochTypes());

                if (!RegisteredEpochTypes.Add(epochType))
                {
                    _logger.Debug($"[Timeline] Skipping duplicate epoch registration: {epochType.Name} (id={epochId})");
                    return;
                }

                var epochTypeDictionary =
                    GetStaticField<Dictionary<string, Type>>(typeof(EpochModel), "_epochTypeDictionary");
                var typeToIdDictionary =
                    GetStaticField<Dictionary<Type, string>>(typeof(EpochModel), "_typeToIdDictionary");

                epochTypeDictionary[epochId] = epochType;
                typeToIdDictionary[epochType] = epochId;
#if STS2_AT_LEAST_0_108_0
                AddEpochTypeToAllEpochsLocked(epochType);
#endif
                RefreshAllEpochIdsSnapshotLocked();
            }

            _logger.Info($"[Timeline] Registered epoch: {epochType.Name} (id={epochId})");
        }

        /// <summary>
        ///     Registers a concrete story type in the game's story type dictionary.
        ///     注册具体 故事类型 在游戏的 story 类型字典。
        /// </summary>
        public void RegisterStory<TStory>() where TStory : StoryModel, new()
        {
            RegisterStory(typeof(TStory));
        }

        /// <summary>
        ///     Registers <paramref name="storyType" /> in the game's story type dictionary.
        ///     注册 <paramref name="storyType" /> 在游戏的 story 类型字典。
        /// </summary>
        public void RegisterStory(Type storyType)
        {
            EnsureMutable($"register story '{storyType.Name}'");
            EnsureSubtype(storyType, typeof(StoryModel), nameof(storyType));

            var storyId = GetStoryId(storyType);

            lock (SyncRoot)
            {
                RegistrationConflictDetector.ThrowIfStoryIdConflicts(
                    storyId,
                    storyType,
                    GetKnownStoryTypes());

                if (!RegisteredStoryTypes.Add(storyType))
                {
                    _logger.Debug($"[Timeline] Skipping duplicate story registration: {storyType.Name} (id={storyId})");
                    return;
                }

                var storyDictionary =
                    GetStaticField<Dictionary<string, Type>>(typeof(StoryModel), "_storyTypeDictionary");
                storyDictionary[storyId] = storyType;
            }

            _logger.Info($"[Timeline] Registered story: {storyType.Name} (id={storyId})");
        }

        /// <summary>
        ///     Registers <typeparamref name="TEpoch" /> with vanilla epoch discovery and appends it to
        ///     <typeparamref name="TStory" />'s ordered column via <see cref="ModStoryEpochBindings" /> (manifest / pack order).
        ///     注册 <typeparamref name="TEpoch" /> 到原版纪元发现中 并将其追加到
        ///     <typeparamref name="TStory" />'s 有序列 通过 <see cref="ModStoryEpochBindings" /> (manifest / pack 顺序)。
        /// </summary>
        public void RegisterStoryEpoch<TStory, TEpoch>()
            where TStory : StoryModel, new()
            where TEpoch : EpochModel, new()
        {
            RegisterStoryEpoch(typeof(TStory), typeof(TEpoch));
        }

        /// <summary>
        ///     Registers <paramref name="epochType" /> and binds it to <paramref name="storyType" />'s story column.
        ///     注册 <paramref name="epochType" /> 并将其绑定到 <paramref name="storyType" />'s 故事列。
        /// </summary>
        public void RegisterStoryEpoch(Type storyType, Type epochType)
        {
            ArgumentNullException.ThrowIfNull(storyType);
            ArgumentNullException.ThrowIfNull(epochType);
            EnsureMutable($"register story-epoch binding '{storyType.Name}' ← '{epochType.Name}'");
            EnsureSubtype(storyType, typeof(StoryModel), nameof(storyType));
            EnsureSubtype(epochType, typeof(EpochModel), nameof(epochType));

            RegisterEpoch(epochType);
            ModStoryEpochBindings.Append(storyType, epochType);
            _logger.Info($"[Timeline] Story-epoch binding: {storyType.Name} ← {epochType.Name}");
        }

        internal static void FreezeRegistrations(string reason)
        {
            lock (SyncRoot)
            {
                if (IsFrozen)
                    return;

                IsFrozen = true;
                ModStoryEpochBindings.Freeze();
                foreach (var registry in Registries.Values)
                    registry._freezeReason = reason;
            }

            ModTimelineLayoutRegistry.FreezeAndValidate();
        }

        internal static int RegisteredEpochCount()
        {
            lock (SyncRoot)
            {
                return RegisteredEpochTypes.Count;
            }
        }

        /// <summary>
        ///     Rewrites <see cref="EpochModel.AllEpochIds" /> from the live <c>_epochTypeDictionary</c> so
        ///     <see cref="EpochModel.IsValid" /> matches <see cref="EpochModel.Get" /> after third-party dictionary edits or
        ///     early vanilla lazy-init of <c>_allEpochIds</c>.
        ///     从实时 <c>_epochTypeDictionary</c> 重写 <see cref="EpochModel.AllEpochIds" />，使
        ///     <see cref="EpochModel.IsValid" /> 在第三方字典编辑或
        ///     原版过早惰性初始化 <c>_allEpochIds</c> 后仍与 <see cref="EpochModel.Get" /> 一致。
        /// </summary>
        internal static void EnsureAllEpochIdsSyncedWithDictionary()
        {
            lock (SyncRoot)
            {
                RefreshAllEpochIdsSnapshotLocked();
            }
        }

        internal static string GetEpochId(Type epochType)
        {
            EnsureSubtype(epochType, typeof(EpochModel), nameof(epochType));
            var epoch = Activator.CreateInstance(epochType) as EpochModel
                        ?? throw new InvalidOperationException(
                            $"Could not construct epoch type '{epochType.FullName}'.");
            return string.IsNullOrWhiteSpace(epoch.Id)
                ? throw new InvalidOperationException($"Epoch type '{epochType.FullName}' returned an empty Id.")
                : epoch.Id.Trim();
        }

        private void EnsureMutable(string operation)
        {
            if (!IsFrozen)
                return;

            throw new InvalidOperationException(
                $"Cannot {operation} after timeline registration has been frozen ({_freezeReason ?? "unknown"}). " +
                "Register custom stories and epochs from your mod initializer before model initialization.");
        }

        private static void EnsureSubtype(Type type, Type expectedBaseType, string paramName)
        {
            if (type.IsAbstract || type.IsInterface || !expectedBaseType.IsAssignableFrom(type))
                throw new ArgumentException(
                    $"Type '{type.FullName}' must be a concrete subtype of '{expectedBaseType.FullName}'.",
                    paramName);
        }

        private static Type[] GetKnownEpochTypes()
        {
            var typeToIdDictionary =
                GetStaticField<Dictionary<Type, string>>(typeof(EpochModel), "_typeToIdDictionary");
            return typeToIdDictionary.Keys.ToArray();
        }

        private static Type[] GetKnownStoryTypes()
        {
            var storyDictionary = GetStaticField<Dictionary<string, Type>>(typeof(StoryModel), "_storyTypeDictionary");
            return storyDictionary.Values.ToArray();
        }

        private static string GetStoryId(Type storyType)
        {
            var story = (StoryModel)Activator.CreateInstance(storyType)!;
            var property = storyType.GetProperty("Id",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return (string)(property?.GetValue(story) ??
                            throw new InvalidOperationException(
                                $"Story type '{storyType.FullName}' does not expose an Id property."));
        }

        private static TField GetStaticField<TField>(Type ownerType, string fieldName) where TField : class
        {
            var field = ownerType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)
                        ?? throw new MissingFieldException(ownerType.FullName, fieldName);

            return (TField)(field.GetValue(null)
                            ?? throw new InvalidOperationException(
                                $"Static field '{ownerType.FullName}.{fieldName}' is null."));
        }

        private static void SetStaticField(Type ownerType, string fieldName, object? value)
        {
            var field = ownerType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)
                        ?? throw new MissingFieldException(ownerType.FullName, fieldName);

            field.SetValue(null, value);
        }

#if STS2_AT_LEAST_0_108_0
        private static void AddEpochTypeToAllEpochsLocked(Type epochType)
        {
            var allEpochs = GetStaticField<List<Type>>(typeof(EpochModel), "_allEpochs");
            if (!allEpochs.Contains(epochType))
                allEpochs.Add(epochType);
        }
#endif

        private static void RefreshAllEpochIdsSnapshotLocked()
        {
            var epochTypeDictionary =
                GetStaticField<Dictionary<string, Type>>(typeof(EpochModel), "_epochTypeDictionary");
            var ids = epochTypeDictionary.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();

            var field = typeof(EpochModel).GetField("_allEpochIds", BindingFlags.Static | BindingFlags.NonPublic)
                        ?? throw new MissingFieldException(typeof(EpochModel).FullName, "_allEpochIds");
            field.SetValue(null, field.FieldType == typeof(List<string>) ? ids : ids.ToArray());
        }
    }
}
