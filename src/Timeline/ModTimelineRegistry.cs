using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Diagnostics;

namespace STS2RitsuLib.Timeline
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Adds a mod's custom <see cref="EpochModel" /> and <see cref="StoryModel" /> types to the game's timeline
    ///         dictionaries. An epoch represents one unlock slot; a story groups ordered epochs under one progression column.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将模组自定义的 <see cref="EpochModel" /> 和 <see cref="StoryModel" /> 类型加入游戏的时间线字典。
    ///         一个纪元表示一个解锁槽位；一个故事会将有序纪元归入同一进度列。
    ///     </para>
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
        ///     <para xml:lang="en">Gets whether the framework has frozen further epoch and story registration.</para>
        ///     <para xml:lang="zh-CN">获取框架是否已冻结后续纪元与故事注册。</para>
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Returns the timeline registry for <paramref name="modId" />.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="modId" /> 对应的时间线注册表。</para>
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
        ///     <para xml:lang="en">Registers a concrete epoch type with the game's epoch discovery.</para>
        ///     <para xml:lang="zh-CN">向游戏的纪元发现机制注册具体纪元类型。</para>
        /// </summary>
        public void RegisterEpoch<TEpoch>() where TEpoch : EpochModel, new()
        {
            RegisterEpoch(typeof(TEpoch));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="epochType" /> with the game's epoch discovery.</para>
        ///     <para xml:lang="zh-CN">向游戏的纪元发现机制注册 <paramref name="epochType" />。</para>
        /// </summary>
        public void RegisterEpoch(Type epochType)
        {
            ArgumentNullException.ThrowIfNull(epochType);
            EnsureMutable($"register epoch '{epochType.Name}'");
            EnsureSubtype(epochType, typeof(EpochModel), nameof(epochType));

            var epochId = GetEpochId(epochType);

            lock (SyncRoot)
            {
                RegistrationConflictDetector.ThrowIfEpochIdConflicts(
                    epochId,
                    epochType,
                    GetKnownEpochTypes());

                if (RegisteredEpochTypes.Contains(epochType))
                {
                    _logger.Debug($"[Timeline] Skipping duplicate epoch registration: {epochType.Name} (id={epochId})");
                    return;
                }

                var epochTypeDictionary =
                    GetStaticField<Dictionary<string, Type>>(typeof(EpochModel), "_epochTypeDictionary");
                var typeToIdDictionary =
                    GetStaticField<Dictionary<Type, string>>(typeof(EpochModel), "_typeToIdDictionary");

                epochTypeDictionary.Add(epochId, epochType);
#if STS2_AT_LEAST_0_108_0
                var addedToAllEpochs = false;
#endif
                try
                {
                    typeToIdDictionary.Add(epochType, epochId);
#if STS2_AT_LEAST_0_108_0
                    addedToAllEpochs = AddEpochTypeToAllEpochsLocked(epochType);
#endif
                    RefreshAllEpochIdsSnapshotLocked();
                    RegisteredEpochTypes.Add(epochType);
                }
                catch
                {
                    epochTypeDictionary.Remove(epochId);
                    typeToIdDictionary.Remove(epochType);
#if STS2_AT_LEAST_0_108_0
                    if (addedToAllEpochs)
                        RemoveEpochTypeFromAllEpochsLocked(epochType);
#endif
                    RefreshAllEpochIdsSnapshotLocked();
                    throw;
                }
            }

            _logger.Info($"[Timeline] Registered epoch: {epochType.Name} (id={epochId})");
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a concrete story type in the game's story-type dictionary.</para>
        ///     <para xml:lang="zh-CN">向游戏的故事类型字典注册具体故事类型。</para>
        /// </summary>
        public void RegisterStory<TStory>() where TStory : StoryModel, new()
        {
            RegisterStory(typeof(TStory));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="storyType" /> in the game's story-type dictionary.</para>
        ///     <para xml:lang="zh-CN">向游戏的故事类型字典注册 <paramref name="storyType" />。</para>
        /// </summary>
        public void RegisterStory(Type storyType)
        {
            ArgumentNullException.ThrowIfNull(storyType);
            EnsureMutable($"register story '{storyType.Name}'");
            EnsureSubtype(storyType, typeof(StoryModel), nameof(storyType));

            var storyId = GetStoryId(storyType);

            lock (SyncRoot)
            {
                RegistrationConflictDetector.ThrowIfStoryIdConflicts(
                    storyId,
                    storyType,
                    GetKnownStoryTypes());

                if (RegisteredStoryTypes.Contains(storyType))
                {
                    _logger.Debug($"[Timeline] Skipping duplicate story registration: {storyType.Name} (id={storyId})");
                    return;
                }

                var storyDictionary =
                    GetStaticField<Dictionary<string, Type>>(typeof(StoryModel), "_storyTypeDictionary");
                storyDictionary.Add(storyId, storyType);
                RegisteredStoryTypes.Add(storyType);
            }

            _logger.Info($"[Timeline] Registered story: {storyType.Name} (id={storyId})");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <typeparamref name="TEpoch" /> with the game's epoch discovery and appends it to
        ///         <typeparamref name="TStory" />'s ordered column through <see cref="ModStoryEpochBindings" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         向游戏的纪元发现机制注册 <typeparamref name="TEpoch" />，并通过 <see cref="ModStoryEpochBindings" />
        ///         将其追加到 <typeparamref name="TStory" /> 的有序列。
        ///     </para>
        /// </summary>
        public void RegisterStoryEpoch<TStory, TEpoch>()
            where TStory : StoryModel, new()
            where TEpoch : EpochModel, new()
        {
            RegisterStoryEpoch(typeof(TStory), typeof(TEpoch));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="epochType" /> and binds it to <paramref name="storyType" />'s story column.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册 <paramref name="epochType" />，并将其绑定到 <paramref name="storyType" /> 的故事列。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Rebuilds <see cref="EpochModel.AllEpochIds" /> from the live <c>_epochTypeDictionary</c>, keeping
        ///         <see cref="EpochModel.IsValid" /> consistent with <see cref="EpochModel.Get" /> after third-party dictionary
        ///         edits or early initialization of the base-game ID cache.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         根据实时 <c>_epochTypeDictionary</c> 重建 <see cref="EpochModel.AllEpochIds" />，使第三方修改字典或
        ///         游戏本体过早初始化 ID 缓存后，<see cref="EpochModel.IsValid" /> 仍与 <see cref="EpochModel.Get" /> 保持一致。
        ///     </para>
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
            ArgumentNullException.ThrowIfNull(type, paramName);
            if (type.IsAbstract || type.IsInterface || !expectedBaseType.IsAssignableFrom(type))
                throw new ArgumentException(
                    $"Type '{type.FullName}' must be a concrete subtype of '{expectedBaseType.FullName}'.",
                    paramName);
        }

        private static Type[] GetKnownEpochTypes()
        {
            var typeToIdDictionary =
                GetStaticField<Dictionary<Type, string>>(typeof(EpochModel), "_typeToIdDictionary");
            return [.. typeToIdDictionary.Keys];
        }

        private static Type[] GetKnownStoryTypes()
        {
            var storyDictionary = GetStaticField<Dictionary<string, Type>>(typeof(StoryModel), "_storyTypeDictionary");
            return [.. storyDictionary.Values];
        }

        private static string GetStoryId(Type storyType)
        {
            var story = (StoryModel)Activator.CreateInstance(storyType)!;
            var property = storyType.GetProperty("Id",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var storyId = property?.GetValue(story) as string
                          ?? throw new InvalidOperationException(
                              $"Story type '{storyType.FullName}' does not expose a string Id property.");
            return string.IsNullOrWhiteSpace(storyId)
                ? throw new InvalidOperationException($"Story type '{storyType.FullName}' returned an empty Id.")
                : storyId.Trim();
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
        private static bool AddEpochTypeToAllEpochsLocked(Type epochType)
        {
            var allEpochs = GetStaticField<List<Type>>(typeof(EpochModel), "_allEpochs");
            if (allEpochs.Contains(epochType))
                return false;

            allEpochs.Add(epochType);
            return true;
        }

        private static void RemoveEpochTypeFromAllEpochsLocked(Type epochType)
        {
            var allEpochs = GetStaticField<List<Type>>(typeof(EpochModel), "_allEpochs");
            allEpochs.Remove(epochType);
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

            typeof(EpochModel)
                .GetField("_epochIdsHashSet", BindingFlags.Static | BindingFlags.NonPublic)
                ?.SetValue(null, null);
        }
    }
}
