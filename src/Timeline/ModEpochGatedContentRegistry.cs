using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Diagnostics;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Timeline
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Maps epoch IDs to card and relic CLR types gated by those epochs. Content-pack flows such as
    ///         <see cref="TimelineColumnPackEntry{TStory}" /> populate the registry, and pack-declared unlock epoch templates
    ///         consume it. Potion gating is registered directly with
    ///         <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将纪元 ID 映射到受其限制的卡牌和遗物 CLR 类型。<see cref="TimelineColumnPackEntry{TStory}" /> 等内容包流程
    ///         会填充此注册表，再由内容包声明的解锁纪元模板使用。药水的纪元限制则直接通过
    ///         <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" /> 注册。
    ///     </para>
    /// </summary>
    public static class ModEpochGatedContentRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, EpochGatedContentEntry> ByEpochId =
            new(StringComparer.Ordinal);

        private static bool _isFrozen;

        /// <summary>
        ///     <para xml:lang="en">Gets whether registration has been frozen.</para>
        ///     <para xml:lang="zh-CN">获取注册是否已被冻结。</para>
        /// </summary>
        public static bool IsFrozen
        {
            get
            {
                lock (SyncRoot)
                {
                    return _isFrozen;
                }
            }
        }

        internal static void FreezeRegistrations(string reason)
        {
            lock (SyncRoot)
            {
                if (_isFrozen)
                    return;

                _isFrozen = true;
            }
        }

        internal static void ValidateFrozenModelReferences()
        {
            EpochGatedContentEntry[] entries;
            lock (SyncRoot)
            {
                entries = [.. ByEpochId.Values];
            }

            foreach (var entry in entries)
            {
                foreach (var type in entry.CardTypes)
                    RegistrationFreezeDiagnostics.WarnMissingModelType(
                        "EpochGatedContent",
                        entry.ModId,
                        "epoch-gated card",
                        type,
                        typeof(CardModel));

                foreach (var type in entry.RelicTypes)
                    RegistrationFreezeDiagnostics.WarnMissingModelType(
                        "EpochGatedContent",
                        entry.ModId,
                        "epoch-gated relic",
                        type,
                        typeof(RelicModel));
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers the model types gated by the unique <paramref name="epochId" />. At least one card or relic type is
        ///         required.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册受唯一 <paramref name="epochId" /> 限制的模型类型。必须至少提供一个卡牌或遗物类型。
        ///     </para>
        /// </summary>
        public static void Register(string modId, string epochId, IReadOnlyList<Type>? cardTypes,
            IReadOnlyList<Type>? relicTypes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);

            var cards = SnapshotTypes<CardModel>(cardTypes, nameof(cardTypes));
            var relics = SnapshotTypes<RelicModel>(relicTypes, nameof(relicTypes));
            if (cards.Count == 0 && relics.Count == 0)
                throw new ArgumentException(
                    $"Gated content for epoch '{epochId}' must include at least one card or relic type.",
                    nameof(cardTypes));

            lock (SyncRoot)
            {
                EnsureMutable($"register gated content for epoch '{epochId}'");
                if (ByEpochId.ContainsKey(epochId))
                    throw new InvalidOperationException(
                        $"Epoch gated content was already registered for id '{epochId}'.");

                ByEpochId[epochId] = new(modId, cards, relics);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether a content pack registered gated types for <paramref name="epochId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回内容包是否为 <paramref name="epochId" /> 注册了受限类型。</para>
        /// </summary>
        public static bool TryGet(string epochId, out EpochGatedContentEntry entry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);

            lock (SyncRoot)
            {
                return ByEpochId.TryGetValue(epochId, out entry!);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves the <see cref="CardModel" /> instances gated by an epoch ID.</para>
        ///     <para xml:lang="zh-CN">解析受指定纪元 ID 限制的 <see cref="CardModel" /> 实例。</para>
        /// </summary>
        public static IReadOnlyList<CardModel> ResolveCards(string epochId)
        {
            if (!TryGet(epochId, out var entry))
                return [];

            return
            [
                .. entry.CardTypes
                    .Select(type => ModelDb.GetById<CardModel>(ModelDb.GetId(type))),
            ];
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves the <see cref="RelicModel" /> instances gated by an epoch ID.</para>
        ///     <para xml:lang="zh-CN">解析受指定纪元 ID 限制的 <see cref="RelicModel" /> 实例。</para>
        /// </summary>
        public static IReadOnlyList<RelicModel> ResolveRelics(string epochId)
        {
            if (!TryGet(epochId, out var entry))
                return [];

            return
            [
                .. entry.RelicTypes
                    .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type))),
            ];
        }

        private static void EnsureMutable(string operation)
        {
            if (!_isFrozen)
                return;

            throw new InvalidOperationException(
                $"Cannot {operation} after epoch gated content registration has been frozen.");
        }

        private static IReadOnlyList<Type> SnapshotTypes<TModel>(
            IReadOnlyList<Type>? types,
            string paramName)
            where TModel : AbstractModel
        {
            if (types == null || types.Count == 0)
                return Array.AsReadOnly(Array.Empty<Type>());

            var snapshot = types.ToArray();
            var seen = new HashSet<Type>();
            foreach (var type in snapshot)
            {
                if (type == null || type.IsAbstract || type.IsInterface || type.ContainsGenericParameters ||
                    !typeof(TModel).IsAssignableFrom(type))
                    throw new ArgumentException(
                        $"Type '{type?.FullName ?? "<null>"}' must be a closed concrete " +
                        $"{typeof(TModel).Name} subtype.",
                        paramName);
                if (!seen.Add(type))
                    throw new ArgumentException(
                        $"Type '{type.FullName}' is listed more than once.",
                        paramName);
            }

            return Array.AsReadOnly(snapshot);
        }

        /// <summary>
        ///     <para xml:lang="en">Contains a snapshot of the types registered for one epoch by its owning mod.</para>
        ///     <para xml:lang="zh-CN">包含所属模组为一个纪元注册的类型快照。</para>
        /// </summary>
        public sealed record EpochGatedContentEntry(
            string ModId,
            IReadOnlyList<Type> CardTypes,
            IReadOnlyList<Type> RelicTypes);
    }
}
