using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Timeline
{
    /// <summary>
    ///     Maps epoch ids to CLR types gated by that epoch (cards and/or relics only — not potions), populated from pack
    ///     flow such as <see cref="TimelineColumnPackEntry{TStory}" /> slot <c>Cards</c>/<c>Relics</c>/<c>RelicsFromPool</c>/
    ///     <c>CardsFromPool</c>.
    ///     Potions use <c>RequireAllPotionsInPool</c> / <c>Potions</c> on <c>EpochSlotBuilder&lt;TEpoch&gt;</c>
    ///     (RequireEpoch only).
    ///     Used by pack-declared unlock epoch templates and stays in sync with
    ///     <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" />.
    ///     将 epoch id 映射到受该纪元门控的 CLR 类型（仅卡牌和/或遗物，不包括药水），由 pack 流程填充，例如 <see cref="TimelineColumnPackEntry{TStory}" /> 槽位
    ///     <c>Cards</c>/<c>Relics</c>/<c>RelicsFromPool</c>/<c>CardsFromPool</c>。
    ///     药水使用 <c>EpochSlotBuilder&lt;TEpoch&gt;</c> 上的 <c>RequireAllPotionsInPool</c> / <c>Potions</c>（仅 RequireEpoch）。
    ///     供 pack 声明的解锁纪元模板使用，并与 <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" /> 保持同步。
    /// </summary>
    public static class ModEpochGatedContentRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, EpochGatedContentEntry> ByEpochId =
            new(StringComparer.Ordinal);

        private static bool _isFrozen;

        /// <summary>
        ///     True after <see cref="FreezeRegistrations" />.
        ///     在 <see cref="FreezeRegistrations" /> 之后为 true。
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

        /// <summary>
        ///     Registers gated model types for <paramref name="epochId" /> (must be unique). At least one card or relic
        ///     type is required.
        ///     为 <paramref name="epochId" /> 注册受门控的模型类型（必须唯一）。至少需要一个卡牌或遗物类型。
        /// </summary>
        public static void Register(string modId, string epochId, IReadOnlyList<Type>? cardTypes,
            IReadOnlyList<Type>? relicTypes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);

            var cards = cardTypes ?? [];
            var relics = relicTypes ?? [];
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
        ///     Returns whether <paramref name="epochId" /> has pack-registered gated types.
        ///     返回 <paramref name="epochId" /> 是否有由 pack 注册的门控类型。
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
        ///     Resolves <see cref="CardModel" /> instances for a gated epoch id.
        ///     为受门控的纪元 id 解析 <see cref="CardModel" /> 实例。
        /// </summary>
        public static IReadOnlyList<CardModel> ResolveCards(string epochId)
        {
            if (!TryGet(epochId, out var entry))
                return [];

            return entry.CardTypes
                .Select(type => ModelDb.GetById<CardModel>(ModelDb.GetId(type)))
                .ToArray();
        }

        /// <summary>
        ///     Resolves <see cref="RelicModel" /> instances for a gated epoch id.
        ///     为受门控的纪元 id 解析 <see cref="RelicModel" /> 实例。
        /// </summary>
        public static IReadOnlyList<RelicModel> ResolveRelics(string epochId)
        {
            if (!TryGet(epochId, out var entry))
                return [];

            return entry.RelicTypes
                .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
                .ToArray();
        }

        private static void EnsureMutable(string operation)
        {
            if (!_isFrozen)
                return;

            throw new InvalidOperationException(
                $"Cannot {operation} after epoch gated content registration has been frozen.");
        }

        /// <summary>
        ///     Snapshot of types registered for one epoch (from the owning mod’s pack).
        ///     为一个纪元注册的类型快照（来自所属 mod 的 pack）。
        /// </summary>
        public sealed record EpochGatedContentEntry(
            string ModId,
            IReadOnlyList<Type> CardTypes,
            IReadOnlyList<Type> RelicTypes);
    }
}
