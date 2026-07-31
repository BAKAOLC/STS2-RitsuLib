using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">Records the secondary-resource payments resolved for one card play.</para>
    ///     <para xml:lang="zh-CN">记录一次出牌所解析的次级资源支付情况。</para>
    /// </summary>
    public sealed record SecondaryResourcePlayLedger(
        CardModel Card,
        Player? Player,
        bool IsFree,
        IReadOnlyDictionary<string, SecondaryResourcePlayLedgerLine> Lines)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the individual payment entries keyed by stable use identifier.</para>
        ///     <para xml:lang="zh-CN">获取按稳定支付条款标识符索引的各项支付记录。</para>
        /// </summary>
        public IReadOnlyDictionary<string, SecondaryResourcePlayLedgerLine> UseLines { get; init; } = Lines;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the ledger contains any resource aggregate or individual use entry.</para>
        ///     <para xml:lang="zh-CN">获取该支付记录是否包含任何资源汇总或独立支付条目。</para>
        /// </summary>
        public bool HasLines => Lines.Count > 0 || UseLines.Count > 0;

        /// <summary>
        ///     <para xml:lang="en">Creates a ledger with no secondary-resource payment entries.</para>
        ///     <para xml:lang="zh-CN">创建一份不包含次级资源支付条目的记录。</para>
        /// </summary>
        public static SecondaryResourcePlayLedger Empty(CardModel card, Player? player, bool isFree = false)
        {
            return new(card, player, isFree,
                new Dictionary<string, SecondaryResourcePlayLedgerLine>(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the total amount spent from the specified resource.</para>
        ///     <para xml:lang="zh-CN">返回从指定资源中消耗的总量。</para>
        /// </summary>
        public int Spent(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            return Lines.TryGetValue(resourceId.Trim(), out var line) ? line.AmountSpent : 0;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the amount spent by the specified payment use.</para>
        ///     <para xml:lang="zh-CN">返回指定支付条款消耗的资源数量。</para>
        /// </summary>
        public int SpentByUse(string useId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            return UseLines.TryGetValue(useId.Trim(), out var line) ? line.AmountSpent : 0;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the total amount spent by the specified payment use; equivalent to
        ///         <see cref="SpentByUse" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回指定支付条款消耗的总量；等同于 <see cref="SpentByUse" />。</para>
        /// </summary>
        public int TotalSpentByUse(string useId)
        {
            return SpentByUse(useId);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the amount spent on repeatable extra payments by the specified use.</para>
        ///     <para xml:lang="zh-CN">返回指定条款用于可重复额外支付的资源数量。</para>
        /// </summary>
        public int ExtraSpentByUse(string useId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            return UseLines.TryGetValue(useId.Trim(), out var line) ? line.ExtraAmountSpent : 0;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the number of complete extra-payment units bought by the specified use.</para>
        ///     <para xml:lang="zh-CN">返回指定条款购买的完整额外支付单位数。</para>
        /// </summary>
        public int ExtraStacksByUse(string useId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            return UseLines.TryGetValue(useId.Trim(), out var line) ? line.ExtraStacks : 0;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the total effect value captured for the specified resource.</para>
        ///     <para xml:lang="zh-CN">返回为指定资源记录的效果数值总量。</para>
        /// </summary>
        public int Value(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            return Lines.TryGetValue(resourceId.Trim(), out var line) ? line.Value : 0;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the total remaining payment shortfall for the specified resource.</para>
        ///     <para xml:lang="zh-CN">返回指定资源仍未补足的支付缺口总量。</para>
        /// </summary>
        public int Shortfall(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            return Lines.TryGetValue(resourceId.Trim(), out var line) ? line.Shortfall : 0;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the total repeatable extra payment made with the specified resource.</para>
        ///     <para xml:lang="zh-CN">返回使用指定资源完成的可重复额外支付总量。</para>
        /// </summary>
        public int ExtraSpent(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            return Lines.TryGetValue(resourceId.Trim(), out var line) ? line.ExtraAmountSpent : 0;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the total number of complete extra-payment units bought with the specified resource.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回使用指定资源购买的完整额外支付单位总数。</para>
        /// </summary>
        public int ExtraStacks(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            return Lines.TryGetValue(resourceId.Trim(), out var line) ? line.ExtraStacks : 0;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the specified resource's total shortfall before replacement payments.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回应用替代支付前指定资源的原始费用缺口总量。</para>
        /// </summary>
        public int OriginalShortfall(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            return Lines.TryGetValue(resourceId.Trim(), out var line) ? line.OriginalShortfall : 0;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the amount of the specified resource's shortfall covered by replacement payments.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回替代支付为指定资源补足的费用缺口总量。</para>
        /// </summary>
        public int CoveredShortfall(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            return Lines.TryGetValue(resourceId.Trim(), out var line) ? line.CoveredShortfall : 0;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the effect value captured for the specified payment use.</para>
        ///     <para xml:lang="zh-CN">返回为指定支付条款记录的效果数值。</para>
        /// </summary>
        public int ValueByUse(string useId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            return UseLines.TryGetValue(useId.Trim(), out var line) ? line.Value : 0;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns whether any payment entry for the specified resource used an X cost.</para>
        ///     <para xml:lang="zh-CN">返回指定资源是否有任何支付条目使用了 X 费用。</para>
        /// </summary>
        public bool CostsX(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            return Lines.TryGetValue(resourceId.Trim(), out var line) && line.CostsX;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns whether the specified payment use was active for this card play.</para>
        ///     <para xml:lang="zh-CN">返回指定支付条款是否在本次出牌中激活。</para>
        /// </summary>
        public bool Activated(string useId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            return UseLines.TryGetValue(useId.Trim(), out var line) && line.Activated;
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get an individual payment entry by its use identifier.</para>
        ///     <para xml:lang="zh-CN">尝试按支付条款标识符获取一项独立支付记录。</para>
        /// </summary>
        public bool TryGetUseLine(string useId, out SecondaryResourcePlayLedgerLine line)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            return UseLines.TryGetValue(useId.Trim(), out line!);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the remaining payment shortfall for the specified use.</para>
        ///     <para xml:lang="zh-CN">返回指定支付条款仍未补足的费用缺口。</para>
        /// </summary>
        public int ShortfallByUse(string useId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            return UseLines.TryGetValue(useId.Trim(), out var line) ? line.Shortfall : 0;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the specified use's shortfall before replacement payments.</para>
        ///     <para xml:lang="zh-CN">返回应用替代支付前指定支付条款的原始费用缺口。</para>
        /// </summary>
        public int OriginalShortfallByUse(string useId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            return UseLines.TryGetValue(useId.Trim(), out var line) ? line.OriginalShortfall : 0;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the amount of the specified use's shortfall covered by replacement payments.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回替代支付为指定支付条款补足的费用缺口数量。</para>
        /// </summary>
        public int CoveredShortfallByUse(string useId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            return UseLines.TryGetValue(useId.Trim(), out var line) ? line.CoveredShortfall : 0;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Records one payment use or one resource aggregate in a card-play ledger.</para>
    ///     <para xml:lang="zh-CN">记录出牌支付记录中的一项支付条款或一项资源汇总。</para>
    /// </summary>
    public sealed record SecondaryResourcePlayLedgerLine(
        string ResourceId,
        int AmountSpent,
        int Value,
        bool CostsX,
        bool IsFree)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable payment-use identifier represented by this entry.</para>
        ///     <para xml:lang="zh-CN">获取该条目所表示的稳定支付条款标识符。</para>
        /// </summary>
        public string UseId { get; init; } = ResourceId;

        /// <summary>
        ///     <para xml:lang="en">Gets the payment role represented by this entry.</para>
        ///     <para xml:lang="zh-CN">获取该条目所表示的支付用途。</para>
        /// </summary>
        public SecondaryResourceUseKind Kind { get; init; } = SecondaryResourceUseKind.RequiredCost;

        /// <summary>
        ///     <para xml:lang="en">Gets whether this entry was active for the card play.</para>
        ///     <para xml:lang="zh-CN">获取该条目是否在本次出牌中激活。</para>
        /// </summary>
        public bool Activated { get; init; } = IsFree || AmountSpent > 0 || Value > 0;

        /// <summary>
        ///     <para xml:lang="en">Gets the payment shortfall that remained after replacement payments.</para>
        ///     <para xml:lang="zh-CN">获取替代支付完成后仍未补足的费用缺口。</para>
        /// </summary>
        public int Shortfall { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the original shortfall before replacement payments.</para>
        ///     <para xml:lang="zh-CN">获取应用替代支付前的原始费用缺口。</para>
        /// </summary>
        public int OriginalShortfall { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the amount of the original shortfall covered by replacement payments.</para>
        ///     <para xml:lang="zh-CN">获取替代支付在原始费用缺口中补足的数量。</para>
        /// </summary>
        public int CoveredShortfall { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the amount spent on the base required or optional payment.</para>
        ///     <para xml:lang="zh-CN">获取用于基础必需支付或可选支付的资源数量。</para>
        /// </summary>
        public int BaseAmountSpent { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the amount spent on repeatable extra payments.</para>
        ///     <para xml:lang="zh-CN">获取用于可重复额外支付的资源数量。</para>
        /// </summary>
        public int ExtraAmountSpent { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the number of complete extra-payment units bought.</para>
        ///     <para xml:lang="zh-CN">获取购买的完整额外支付单位数。</para>
        /// </summary>
        public int ExtraStacks { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether this entry represents an optional payment.</para>
        ///     <para xml:lang="zh-CN">获取该条目是否表示一项可选支付。</para>
        /// </summary>
        public bool IsOptional => Kind == SecondaryResourceUseKind.OptionalSpend;

        /// <summary>
        ///     <para xml:lang="en">Gets whether this entry represents a repeatable extra payment.</para>
        ///     <para xml:lang="zh-CN">获取该条目是否表示一项可重复额外支付。</para>
        /// </summary>
        public bool IsExtraSpend => Kind == SecondaryResourceUseKind.ExtraSpend;

        /// <summary>
        ///     <para xml:lang="en">Gets whether any payment shortfall remains.</para>
        ///     <para xml:lang="zh-CN">获取是否仍有未补足的费用缺口。</para>
        /// </summary>
        public bool HasShortfall => Shortfall > 0;
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
            var ledgerLine = new SecondaryResourcePlayLedgerLine(
                line.ResourceId,
                line.IsFree ? 0 : line.AmountToSpend,
                line.Value,
                line.CostsX,
                line.IsFree)
            {
                UseId = line.UseId,
                Kind = line.Kind,
                Activated = line.Activated,
                OriginalShortfall = line.OriginalShortfall,
                CoveredShortfall = line.CoveredShortfall,
                Shortfall = line.Shortfall,
                BaseAmountSpent = line.Kind == SecondaryResourceUseKind.ExtraSpend
                    ? 0
                    : line.IsFree
                        ? 0
                        : line.AmountToSpend,
                ExtraAmountSpent = line.ExtraAmountToSpend,
                ExtraStacks = line.ExtraStacks,
            };

            if (!_useLines.TryAdd(line.UseId, ledgerLine))
                throw new InvalidOperationException(
                    $"Duplicate secondary-resource use id '{line.UseId}' in the card-play ledger.");
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
                            SumSaturating(lines, static line => line.AmountSpent),
                            SumSaturating(lines, static line => line.Value),
                            lines.Any(static line => line.CostsX),
                            lines.All(static line => line.IsFree))
                        {
                            UseId = group.Key,
                            Kind = lines.Any(static line => line.Kind == SecondaryResourceUseKind.RequiredCost)
                                ? SecondaryResourceUseKind.RequiredCost
                                : lines.Any(static line => line.Kind == SecondaryResourceUseKind.OptionalSpend)
                                    ? SecondaryResourceUseKind.OptionalSpend
                                    : SecondaryResourceUseKind.ExtraSpend,
                            Activated = lines.Any(static line => line.Activated),
                            OriginalShortfall = SumSaturating(lines, static line => line.OriginalShortfall),
                            CoveredShortfall = SumSaturating(lines, static line => line.CoveredShortfall),
                            Shortfall = SumSaturating(lines, static line => line.Shortfall),
                            BaseAmountSpent = SumSaturating(lines, static line => line.BaseAmountSpent),
                            ExtraAmountSpent = SumSaturating(lines, static line => line.ExtraAmountSpent),
                            ExtraStacks = SumSaturating(lines, static line => line.ExtraStacks),
                        };
                    },
                    StringComparer.OrdinalIgnoreCase);

            return new(card, player, isFree,
                resourceLines)
            {
                UseLines = useLines,
            };
        }

        private static int SumSaturating(
            IEnumerable<SecondaryResourcePlayLedgerLine> lines,
            Func<SecondaryResourcePlayLedgerLine, int> selector)
        {
            return lines.Aggregate(
                0,
                (sum, line) => SecondaryResourceAmountMath.AddSaturating(sum, selector(line)));
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides access to secondary-resource payment ledgers attached to <see cref="CardPlay" /> instances.
    ///     </para>
    ///     <para xml:lang="zh-CN">提供对附加到 <see cref="CardPlay" /> 实例的次级资源支付记录的访问。</para>
    /// </summary>
    public static class SecondaryResourcePlayExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">Returns this card play's attached payment ledger, or an empty ledger.</para>
        ///     <para xml:lang="zh-CN">返回附加到本次出牌的支付记录；没有时返回空记录。</para>
        /// </summary>
        public static SecondaryResourcePlayLedger SecondaryResources(this CardPlay play)
        {
            ArgumentNullException.ThrowIfNull(play);
            return SecondaryResourcePlayLedgerRuntime.Get(play);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get a nonempty payment ledger attached to this card play.</para>
        ///     <para xml:lang="zh-CN">尝试获取附加到本次出牌的非空支付记录。</para>
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
    ///     <para xml:lang="en">Stores queued and attached card-play payment ledgers at runtime.</para>
    ///     <para xml:lang="zh-CN">在运行时存储排队等待绑定及已附加的出牌支付记录。</para>
    /// </summary>
    public static class SecondaryResourcePlayLedgerRuntime
    {
        private static readonly AttachedState<CardPlay, SecondaryResourcePlayLedger> PlayLedgers = new();

        private static readonly AttachedState<CardModel, Queue<SecondaryResourcePlayLedger>> PendingLedgers =
            new(() => new());

        private static readonly AttachedState<CardModel, List<PendingLedgerBindingScope>> ActiveBindingScopes =
            new(() => []);

        /// <summary>
        ///     <para xml:lang="en">Gets the ledger attached to a card play, or an empty ledger.</para>
        ///     <para xml:lang="zh-CN">获取附加到一次出牌的支付记录；没有时返回空记录。</para>
        /// </summary>
        public static SecondaryResourcePlayLedger Get(CardPlay play)
        {
            ArgumentNullException.ThrowIfNull(play);

            return PlayLedgers.TryGetValue(play, out var ledger)
                ? ledger
                : SecondaryResourcePlayLedger.Empty(play.Card, play.Card.Owner, play.IsAutoPlay);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get a nonempty ledger attached to a card play.</para>
        ///     <para xml:lang="zh-CN">尝试获取附加到一次出牌的非空支付记录。</para>
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
        ///     <para xml:lang="en">Attaches a payment ledger directly to a card play.</para>
        ///     <para xml:lang="zh-CN">将支付记录直接附加到一次出牌。</para>
        /// </summary>
        public static void Attach(CardPlay play, SecondaryResourcePlayLedger ledger)
        {
            ArgumentNullException.ThrowIfNull(play);
            ArgumentNullException.ThrowIfNull(ledger);
            PlayLedgers.Set(play, ledger);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues a ledger for the next <see cref="CardPlay" /> created for the specified card.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将支付记录加入队列，等待附加到指定卡牌创建的下一个 <see cref="CardPlay" />。
        ///     </para>
        /// </summary>
        public static void SetPending(CardModel card, SecondaryResourcePlayLedger ledger)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentNullException.ThrowIfNull(ledger);

            PendingLedgers.GetOrCreate(card).Enqueue(ledger);
        }

        internal static bool TryRemovePending(CardModel card, SecondaryResourcePlayLedger ledger)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentNullException.ThrowIfNull(ledger);

            if (!PendingLedgers.TryGetValue(card, out var queue) || queue.Count == 0)
                return false;

            var removed = false;
            var ledgers = queue.ToArray();
            queue.Clear();
            foreach (var candidate in ledgers)
            {
                if (!removed && ReferenceEquals(candidate, ledger))
                {
                    removed = true;
                    continue;
                }

                queue.Enqueue(candidate);
            }

            if (queue.Count == 0)
                PendingLedgers.Remove(card);

            return removed;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether the card has a queued ledger or an active binding scope.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回该卡牌是否有排队等待绑定的支付记录或活动绑定作用域。</para>
        /// </summary>
        public static bool HasPending(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);

            if (ActiveBindingScopes.TryGetValue(card, out var scopes) && scopes.Count > 0)
                return true;

            return PendingLedgers.TryGetValue(card, out var queue) && queue.Count > 0;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Begins an <c>OnPlayWrapper</c> binding scope that reuses one queued ledger for every
        ///         <see cref="CardPlay" /> created within the scope.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         开始一个 <c>OnPlayWrapper</c> 绑定作用域，使一份排队的支付记录可供作用域内创建的每个
        ///         <see cref="CardPlay" /> 复用。
        ///     </para>
        /// </summary>
        public static IDisposable? BeginPendingScope(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);

            if (!PendingLedgers.TryGetValue(card, out var queue) || queue.Count == 0)
                return null;

            var scope = new PendingLedgerBindingScope(card, queue.Dequeue());
            ActiveBindingScopes.GetOrCreate(card).Add(scope);
            if (queue.Count == 0)
                PendingLedgers.Remove(card);

            return scope;
        }

        /// <summary>
        ///     <para xml:lang="en">Binds an available queued or scoped ledger to a newly created card play.</para>
        ///     <para xml:lang="zh-CN">将可用的排队或作用域内支付记录绑定到新创建的出牌。</para>
        /// </summary>
        public static bool TryBindPending(CardPlay play)
        {
            ArgumentNullException.ThrowIfNull(play);

            if (ActiveBindingScopes.TryGetValue(play.Card, out var scopes) && scopes.Count > 0)
            {
                Attach(play, scopes[^1].Ledger);
                return true;
            }

            if (!PendingLedgers.TryGetValue(play.Card, out var queue) || queue.Count == 0)
                return false;

            Attach(play, queue.Dequeue());
            if (queue.Count == 0)
                PendingLedgers.Remove(play.Card);
            return true;
        }

        private sealed class PendingLedgerBindingScope(
            CardModel card,
            SecondaryResourcePlayLedger ledger) : IDisposable
        {
            private bool _disposed;

            public SecondaryResourcePlayLedger Ledger { get; } = ledger;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                if (!ActiveBindingScopes.TryGetValue(card, out var scopes))
                    return;

                scopes.Remove(this);
                if (scopes.Count == 0)
                    ActiveBindingScopes.Remove(card);
            }
        }
    }
}
