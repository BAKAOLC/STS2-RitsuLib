namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">Specifies how an underfunded required secondary-resource cost behaves.</para>
    ///     <para xml:lang="zh-CN">指定必需的次级资源费用无法足额支付时的行为。</para>
    /// </summary>
    public enum SecondaryResourceInsufficientPaymentMode
    {
        /// <summary>
        ///     <para xml:lang="en">Prevents the card from being played.</para>
        ///     <para xml:lang="zh-CN">阻止打出卡牌。</para>
        /// </summary>
        BlockPlay = 0,

        /// <summary>
        ///     <para xml:lang="en">Allows play and reports the unpaid amount as a shortfall.</para>
        ///     <para xml:lang="zh-CN">允许出牌，并将未支付数量报告为缺口。</para>
        /// </summary>
        AllowPlay = 1,
    }

    /// <summary>
    ///     <para xml:lang="en">Handles a committed card payment with a remaining shortfall.</para>
    ///     <para xml:lang="zh-CN">处理已提交且仍有缺口的卡牌支付。</para>
    /// </summary>
    public delegate Task SecondaryResourceShortfallPaymentHandler(SecondaryResourceShortfallContext context);

    /// <summary>
    ///     <para xml:lang="en">Plans how much of a shortfall another payment source can cover.</para>
    ///     <para xml:lang="zh-CN">规划其他支付来源可补足多少缺口。</para>
    /// </summary>
    public delegate SecondaryResourceShortfallResolution SecondaryResourceShortfallResolver(
        SecondaryResourceShortfallResolutionContext context);

    /// <summary>
    ///     <para xml:lang="en">Describes a side-effect-free replacement-payment plan for a shortfall.</para>
    ///     <para xml:lang="zh-CN">描述用于补足缺口的无副作用替代支付方案。</para>
    /// </summary>
    public sealed record SecondaryResourceShortfallResolution
    {
        /// <summary>
        ///     <para xml:lang="en">Gets a plan that covers none of the shortfall.</para>
        ///     <para xml:lang="zh-CN">获取不补足任何缺口的方案。</para>
        /// </summary>
        public static SecondaryResourceShortfallResolution None { get; } = new();

        /// <summary>
        ///     <para xml:lang="en">Gets the amount covered by the replacement payment.</para>
        ///     <para xml:lang="zh-CN">获取替代支付补足的数量。</para>
        /// </summary>
        public int CoveredAmount { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional callback that commits the replacement payment.</para>
        ///     <para xml:lang="zh-CN">获取提交替代支付的可选回调。</para>
        /// </summary>
        public SecondaryResourceShortfallPaymentHandler? OnCommit { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Creates a plan that covers a nonnegative amount of the shortfall.</para>
        ///     <para xml:lang="zh-CN">创建补足非负缺口数量的方案。</para>
        /// </summary>
        public static SecondaryResourceShortfallResolution Cover(
            int amount,
            SecondaryResourceShortfallPaymentHandler? onCommit = null)
        {
            return new()
            {
                CoveredAmount = Math.Max(0, amount),
                OnCommit = onCommit,
            };
        }

        internal Task Commit(SecondaryResourceShortfallContext context)
        {
            if (OnCommit == null)
                return Task.CompletedTask;

            return OnCommit(context) ??
                   throw new InvalidOperationException("A shortfall replacement-payment handler returned null.");
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines behavior for an underfunded required secondary-resource payment.</para>
    ///     <para xml:lang="zh-CN">定义必需的次级资源支付无法足额完成时的行为。</para>
    /// </summary>
    public sealed record SecondaryResourceInsufficientPayment
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the shared policy that blocks play on a shortfall.</para>
        ///     <para xml:lang="zh-CN">获取存在缺口时阻止出牌的共享策略。</para>
        /// </summary>
        public static SecondaryResourceInsufficientPayment BlockPlay { get; } = new();

        /// <summary>
        ///     <para xml:lang="en">Gets the selected behavior for an unpaid amount.</para>
        ///     <para xml:lang="zh-CN">获取未支付数量所采用的行为。</para>
        /// </summary>
        public SecondaryResourceInsufficientPaymentMode Mode { get; init; } =
            SecondaryResourceInsufficientPaymentMode.BlockPlay;

        /// <summary>
        ///     <para xml:lang="en">Gets whether available resource is spent before reporting the remaining shortfall.</para>
        ///     <para xml:lang="zh-CN">获取是否先支付可用资源，再报告剩余缺口。</para>
        /// </summary>
        public bool SpendAvailable { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets the optional callback invoked after committing a remaining shortfall.</para>
        ///     <para xml:lang="zh-CN">获取提交剩余缺口后调用的可选回调。</para>
        /// </summary>
        public SecondaryResourceShortfallPaymentHandler? OnShortfall { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional side-effect-free replacement-payment planner.</para>
        ///     <para xml:lang="zh-CN">获取可选的无副作用替代支付规划器。</para>
        /// </summary>
        public SecondaryResourceShortfallResolver? ResolveShortfall { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether card-play checks may pass despite a shortfall.</para>
        ///     <para xml:lang="zh-CN">获取存在缺口时是否仍可通过出牌检查。</para>
        /// </summary>
        public bool AllowsPlay => Mode == SecondaryResourceInsufficientPaymentMode.AllowPlay;

        /// <summary>
        ///     <para xml:lang="en">Creates a policy that permits play with an underfunded required cost.</para>
        ///     <para xml:lang="zh-CN">创建允许在必需费用不足时出牌的策略。</para>
        /// </summary>
        public static SecondaryResourceInsufficientPayment AllowPlay(
            SecondaryResourceShortfallPaymentHandler? onShortfall = null,
            bool spendAvailable = true,
            SecondaryResourceShortfallResolver? resolveShortfall = null)
        {
            return new()
            {
                Mode = SecondaryResourceInsufficientPaymentMode.AllowPlay,
                SpendAvailable = spendAvailable,
                OnShortfall = onShortfall,
                ResolveShortfall = resolveShortfall,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an allow-play policy with a synchronous shortfall callback.</para>
        ///     <para xml:lang="zh-CN">创建带同步缺口回调的允许出牌策略。</para>
        /// </summary>
        public static SecondaryResourceInsufficientPayment AllowPlay(
            Action<SecondaryResourceShortfallContext> onShortfall,
            bool spendAvailable = true,
            SecondaryResourceShortfallResolver? resolveShortfall = null)
        {
            ArgumentNullException.ThrowIfNull(onShortfall);
            return AllowPlay(
                context =>
                {
                    onShortfall(context);
                    return Task.CompletedTask;
                },
                spendAvailable,
                resolveShortfall);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an allow-play policy that first plans a replacement payment.</para>
        ///     <para xml:lang="zh-CN">创建优先规划替代支付的允许出牌策略。</para>
        /// </summary>
        public static SecondaryResourceInsufficientPayment AllowPlayWithReplacement(
            SecondaryResourceShortfallResolver resolveShortfall,
            SecondaryResourceShortfallPaymentHandler? onRemainingShortfall = null,
            bool spendAvailable = true)
        {
            ArgumentNullException.ThrowIfNull(resolveShortfall);
            return AllowPlay(onRemainingShortfall, spendAvailable, resolveShortfall);
        }

        internal Task InvokeShortfall(SecondaryResourceShortfallContext context)
        {
            if (OnShortfall == null)
                return Task.CompletedTask;

            return OnShortfall(context) ??
                   throw new InvalidOperationException("A remaining-shortfall payment handler returned null.");
        }

        internal SecondaryResourceShortfallResolution Resolve(SecondaryResourceShortfallResolutionContext context)
        {
            if (ResolveShortfall == null)
                return SecondaryResourceShortfallResolution.None;

            return ResolveShortfall(context) ??
                   throw new InvalidOperationException("A shortfall replacement-payment resolver returned null.");
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Specifies the persistence scope of a secondary combat resource.</para>
    ///     <para xml:lang="zh-CN">指定次级战斗资源的持久化范围。</para>
    /// </summary>
    public enum SecondaryResourcePersistencePolicy
    {
        /// <summary>
        ///     <para xml:lang="en">Does not write the resource to run saves.</para>
        ///     <para xml:lang="zh-CN">不将资源写入一局游戏的存档。</para>
        /// </summary>
        None = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Includes the resource only in explicitly requested combat-scoped snapshots. Normal run-save
        ///         synchronization excludes it.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         仅在显式请求的战斗范围快照中包含资源；常规的一局游戏存档同步会排除该资源。
        ///     </para>
        /// </summary>
        Combat = 1,

        /// <summary>
        ///     <para xml:lang="en">Persists the resource across combats in the current run.</para>
        ///     <para xml:lang="zh-CN">使资源在当前一局游戏中跨战斗持久化。</para>
        /// </summary>
        Run = 2,
    }

    /// <summary>
    ///     <para xml:lang="en">Specifies built-in turn-start behavior for a secondary resource.</para>
    ///     <para xml:lang="zh-CN">指定次级资源的内置回合开始行为。</para>
    /// </summary>
    public enum SecondaryResourceTurnStartPolicy
    {
        /// <summary>
        ///     <para xml:lang="en">Leaves the current amount unchanged.</para>
        ///     <para xml:lang="zh-CN">保持当前数量不变。</para>
        /// </summary>
        None = 0,

        /// <summary>
        ///     <para xml:lang="en">Sets the current amount to the hook-modified maximum.</para>
        ///     <para xml:lang="zh-CN">将当前数量设为经钩子修正后的最大数量。</para>
        /// </summary>
        ResetToMax = 1,

        /// <summary>
        ///     <para xml:lang="en">Adds the hook-modified maximum to the current amount.</para>
        ///     <para xml:lang="zh-CN">将经钩子修正后的最大数量加到当前数量。</para>
        /// </summary>
        AddMaxToCurrent = 2,

        /// <summary>
        ///     <para xml:lang="en">Sets the current amount to the hard lower bound.</para>
        ///     <para xml:lang="zh-CN">将当前数量设为硬下限。</para>
        /// </summary>
        Clear = 3,
    }

    /// <summary>
    ///     <para xml:lang="en">Specifies the reason for a secondary-resource amount mutation.</para>
    ///     <para xml:lang="zh-CN">指定次级资源数量变化的原因。</para>
    /// </summary>
    public enum SecondaryResourceChangeReason
    {
        /// <summary>
        ///     <para xml:lang="en">Indicates an unspecified or custom reason.</para>
        ///     <para xml:lang="zh-CN">表示未指定或自定义原因。</para>
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     <para xml:lang="en">Indicates an amount increase.</para>
        ///     <para xml:lang="zh-CN">表示数量增加。</para>
        /// </summary>
        Gain = 1,

        /// <summary>
        ///     <para xml:lang="en">Indicates a decrease without payment semantics.</para>
        ///     <para xml:lang="zh-CN">表示不带支付语义的数量减少。</para>
        /// </summary>
        Lose = 2,

        /// <summary>
        ///     <para xml:lang="en">Indicates direct amount assignment.</para>
        ///     <para xml:lang="zh-CN">表示直接设置数量。</para>
        /// </summary>
        Set = 3,

        /// <summary>
        ///     <para xml:lang="en">Indicates an amount spent as payment.</para>
        ///     <para xml:lang="zh-CN">表示作为支付消耗数量。</para>
        /// </summary>
        Spend = 4,

        /// <summary>
        ///     <para xml:lang="en">Indicates an explicit reset.</para>
        ///     <para xml:lang="zh-CN">表示显式重置。</para>
        /// </summary>
        Reset = 5,

        /// <summary>
        ///     <para xml:lang="en">Indicates a change made by a turn-start policy.</para>
        ///     <para xml:lang="zh-CN">表示由回合开始策略造成的变化。</para>
        /// </summary>
        TurnStart = 6,

        /// <summary>
        ///     <para xml:lang="en">Indicates an explicit clamp to the current maximum.</para>
        ///     <para xml:lang="zh-CN">表示显式限制到当前最大数量。</para>
        /// </summary>
        ClampToMax = 7,
    }
}
