using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Secondary-resource payment details for a card play.
    ///     一次出牌的次级资源支付详情。
    /// </summary>
    public sealed record SecondaryResourcePlayLedger(
        CardModel Card,
        Player? Player,
        bool IsFree,
        IReadOnlyDictionary<string, SecondaryResourcePlayLedgerLine> Lines)
    {
        /// <summary>
        ///     Play-use lines keyed by stable use id.
        ///     按稳定条款 id 索引的出牌条款明细。
        /// </summary>
        public IReadOnlyDictionary<string, SecondaryResourcePlayLedgerLine> UseLines { get; init; } = Lines;

        /// <summary>
        ///     True when at least one resource line exists.
        ///     至少存在一个资源行时为 true。
        /// </summary>
        public bool HasLines => Lines.Count > 0 || UseLines.Count > 0;

        /// <summary>
        ///     Empty ledger with no lines.
        ///     没有资源行的空 ledger。
        /// </summary>
        public static SecondaryResourcePlayLedger Empty(CardModel card, Player? player, bool isFree = false)
        {
            return new(card, player, isFree,
                new Dictionary<string, SecondaryResourcePlayLedgerLine>(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Returns the amount spent for a resource.
        ///     返回某个资源的消耗数量。
        /// </summary>
        public int Spent(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            return Lines.TryGetValue(resourceId.Trim(), out var line) ? line.AmountSpent : 0;
        }

        /// <summary>
        ///     Returns the amount spent for a play-use id.
        ///     返回某个出牌条款 id 的消耗数量。
        /// </summary>
        public int SpentByUse(string useId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            return UseLines.TryGetValue(useId.Trim(), out var line) ? line.AmountSpent : 0;
        }

        /// <summary>
        ///     Returns the value captured for a resource.
        ///     返回某个资源捕获到的数值。
        /// </summary>
        public int Value(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            return Lines.TryGetValue(resourceId.Trim(), out var line) ? line.Value : 0;
        }

        /// <summary>
        ///     Returns the value captured for a play-use id.
        ///     返回某个出牌条款 id 捕获到的数值。
        /// </summary>
        public int ValueByUse(string useId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            return UseLines.TryGetValue(useId.Trim(), out var line) ? line.Value : 0;
        }

        /// <summary>
        ///     Returns whether a resource was captured as X.
        ///     返回某个资源是否按 X 值捕获。
        /// </summary>
        public bool CostsX(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            return Lines.TryGetValue(resourceId.Trim(), out var line) && line.CostsX;
        }

        /// <summary>
        ///     Returns whether a play-use line was activated for this play.
        ///     返回某个出牌条款行是否在本次出牌中激活。
        /// </summary>
        public bool Activated(string useId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            return UseLines.TryGetValue(useId.Trim(), out var line) && line.Activated;
        }

        /// <summary>
        ///     Attempts to get a play-use line by use id.
        ///     尝试按条款 id 获取出牌条款行。
        /// </summary>
        public bool TryGetUseLine(string useId, out SecondaryResourcePlayLedgerLine line)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            return UseLines.TryGetValue(useId.Trim(), out line!);
        }
    }

    /// <summary>
    ///     Secondary-resource ledger line.
    ///     次级资源 ledger 行。
    /// </summary>
    public sealed record SecondaryResourcePlayLedgerLine(
        string ResourceId,
        int AmountSpent,
        int Value,
        bool CostsX,
        bool IsFree)
    {
        /// <summary>
        ///     Stable play-use id for this line.
        ///     该行的稳定出牌条款 id。
        /// </summary>
        public string UseId { get; init; } = ResourceId;

        /// <summary>
        ///     Semantic role for this line.
        ///     该行的语义角色。
        /// </summary>
        public SecondaryResourceUseKind Kind { get; init; } = SecondaryResourceUseKind.RequiredCost;

        /// <summary>
        ///     True when this line was active for the play.
        ///     该行已在本次出牌中激活。
        /// </summary>
        public bool Activated { get; init; } = IsFree || AmountSpent > 0 || Value > 0;

        /// <summary>
        ///     True when this line came from an optional spend.
        ///     该行来自可选支付时为 true。
        /// </summary>
        public bool IsOptional => Kind == SecondaryResourceUseKind.OptionalSpend;
    }

    internal sealed class SecondaryResourcePlayLedgerBuilder(
        CardModel card,
        Player? player,
        bool isFree)
    {
        private readonly Dictionary<string, SecondaryResourcePlayLedgerLine> _useLines =
            new(StringComparer.OrdinalIgnoreCase);

        public void Add(SecondaryResourcePaymentLine line)
        {
            _useLines[line.UseId] = new(
                line.ResourceId,
                line.IsFree ? 0 : line.AmountToSpend,
                line.Value,
                line.CostsX,
                line.IsFree)
            {
                UseId = line.UseId,
                Kind = line.Kind,
                Activated = line.Activated,
            };
        }

        public SecondaryResourcePlayLedger Build()
        {
            var useLines = _useLines
                .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(static pair => pair.Key, static pair => pair.Value,
                    StringComparer.OrdinalIgnoreCase);

            var resourceLines = useLines.Values
                .GroupBy(static line => line.ResourceId, StringComparer.OrdinalIgnoreCase)
                .OrderBy(static group => group.Key, StringComparer.Ordinal)
                .ToDictionary(
                    static group => group.Key,
                    static group =>
                    {
                        var lines = group.ToArray();
                        return new SecondaryResourcePlayLedgerLine(
                            group.Key,
                            lines.Sum(static line => line.AmountSpent),
                            lines.Sum(static line => line.Value),
                            lines.Any(static line => line.CostsX),
                            lines.All(static line => line.IsFree))
                        {
                            UseId = group.Key,
                            Kind = lines.Any(static line => line.Kind == SecondaryResourceUseKind.RequiredCost)
                                ? SecondaryResourceUseKind.RequiredCost
                                : SecondaryResourceUseKind.OptionalSpend,
                            Activated = lines.Any(static line => line.Activated),
                        };
                    },
                    StringComparer.OrdinalIgnoreCase);

            return new(card, player, isFree,
                resourceLines)
            {
                UseLines = useLines,
            };
        }
    }

    /// <summary>
    ///     Extension helpers for CardPlay secondary-resource ledgers.
    ///     CardPlay 次级资源 ledger 的扩展辅助工具。
    /// </summary>
    public static class SecondaryResourcePlayExtensions
    {
        /// <summary>
        ///     Returns the ledger attached to this play, or an empty ledger.
        ///     返回附加在本次出牌上的 ledger；没有时返回空 ledger。
        /// </summary>
        public static SecondaryResourcePlayLedger SecondaryResources(this CardPlay play)
        {
            ArgumentNullException.ThrowIfNull(play);
            return SecondaryResourcePlayLedgerRuntime.Get(play);
        }

        /// <summary>
        ///     Attempts to get a material attached ledger.
        ///     尝试获取一个实际附加的 ledger。
        /// </summary>
        public static bool TryGetSecondaryResources(
            this CardPlay play,
            out SecondaryResourcePlayLedger ledger)
        {
            ArgumentNullException.ThrowIfNull(play);
            return SecondaryResourcePlayLedgerRuntime.TryGet(play, out ledger);
        }
    }

    /// <summary>
    ///     Runtime storage for pending and attached play ledgers.
    ///     pending 与已附加出牌 ledger 的运行时存储。
    /// </summary>
    public static class SecondaryResourcePlayLedgerRuntime
    {
        private static readonly AttachedState<CardPlay, SecondaryResourcePlayLedger> PlayLedgers = new();

        private static readonly AttachedState<CardModel, Queue<SecondaryResourcePlayLedger>> PendingLedgers =
            new(() => new());

        /// <summary>
        ///     Gets a ledger attached to a play, or an empty ledger.
        ///     获取附加在一次出牌上的 ledger；没有时返回空 ledger。
        /// </summary>
        public static SecondaryResourcePlayLedger Get(CardPlay play)
        {
            ArgumentNullException.ThrowIfNull(play);

            return PlayLedgers.TryGetValue(play, out var ledger)
                ? ledger
                : SecondaryResourcePlayLedger.Empty(play.Card, play.Card.Owner, play.IsAutoPlay);
        }

        /// <summary>
        ///     Attempts to get a material ledger attached to a play.
        ///     尝试获取附加在一次出牌上的实际 ledger。
        /// </summary>
        public static bool TryGet(CardPlay play, out SecondaryResourcePlayLedger ledger)
        {
            ArgumentNullException.ThrowIfNull(play);
            if (PlayLedgers.TryGetValue(play, out ledger!) && ledger.HasLines)
                return true;

            ledger = null!;
            return false;
        }

        /// <summary>
        ///     Attaches a ledger directly to a play.
        ///     将 ledger 直接附加到一次出牌。
        /// </summary>
        public static void Attach(CardPlay play, SecondaryResourcePlayLedger ledger)
        {
            ArgumentNullException.ThrowIfNull(play);
            ArgumentNullException.ThrowIfNull(ledger);
            PlayLedgers.Set(play, ledger);
        }

        /// <summary>
        ///     Queues a ledger to be attached to the next CardPlay created for a card.
        ///     将 ledger 排队，等待附加到该卡牌创建的下一次 CardPlay。
        /// </summary>
        public static void SetPending(CardModel card, SecondaryResourcePlayLedger ledger)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentNullException.ThrowIfNull(ledger);

            PendingLedgers.GetOrCreate(card).Enqueue(ledger);
        }

        /// <summary>
        ///     Binds a pending ledger to a newly created play, if present.
        ///     如果存在 pending ledger，则绑定到新创建的出牌。
        /// </summary>
        public static bool TryBindPending(CardPlay play)
        {
            ArgumentNullException.ThrowIfNull(play);

            if (!PendingLedgers.TryGetValue(play.Card, out var queue) || queue.Count == 0)
                return false;

            Attach(play, queue.Dequeue());
            return true;
        }
    }
}
