namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Behavior for required secondary-resource costs when the player does not have enough resource.
    ///     玩家资源不足以支付必需次级资源费用时的行为。
    /// </summary>
    public enum SecondaryResourceInsufficientPaymentMode
    {
        /// <summary>
        ///     Keep the current hard-cost behavior: the card cannot be played.
        ///     保持当前硬费用行为：卡牌不能被打出。
        /// </summary>
        BlockPlay = 0,

        /// <summary>
        ///     Allow the card to be played and report the unpaid amount as a shortfall.
        ///     允许卡牌被打出，并将未支付部分记录为短缺额。
        /// </summary>
        AllowPlay = 1,
    }

    /// <summary>
    ///     Handles a committed secondary-resource shortfall payment.
    ///     处理已提交的次级资源短缺支付。
    /// </summary>
    public delegate Task SecondaryResourceShortfallPaymentHandler(SecondaryResourceShortfallContext context);

    /// <summary>
    ///     Resolves whether another payment source can cover a secondary-resource shortfall.
    ///     解析是否可用其他支付来源覆盖次级资源短缺。
    /// </summary>
    public delegate SecondaryResourceShortfallResolution SecondaryResourceShortfallResolver(
        SecondaryResourceShortfallResolutionContext context);

    /// <summary>
    ///     Pure planning result for replacing a secondary-resource shortfall with another payment source.
    ///     用其他支付来源替代次级资源短缺的纯规划结果。
    /// </summary>
    public sealed record SecondaryResourceShortfallResolution
    {
        /// <summary>
        ///     No shortfall amount is covered.
        ///     不覆盖任何短缺数量。
        /// </summary>
        public static SecondaryResourceShortfallResolution None { get; } = new();

        /// <summary>
        ///     Amount of the shortfall covered by the replacement payment.
        ///     替代支付覆盖的短缺数量。
        /// </summary>
        public int CoveredAmount { get; init; }

        /// <summary>
        ///     Optional callback that commits the replacement payment.
        ///     提交替代支付的可选回调。
        /// </summary>
        public SecondaryResourceShortfallPaymentHandler? OnCommit { get; init; }

        /// <summary>
        ///     Creates a resolution that covers part or all of the shortfall.
        ///     创建覆盖部分或全部短缺的解析结果。
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
            return OnCommit?.Invoke(context) ?? Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Policy for required secondary-resource payments that are short on resource.
    ///     必需次级资源支付在资源不足时使用的策略。
    /// </summary>
    public sealed record SecondaryResourceInsufficientPayment
    {
        /// <summary>
        ///     Shared policy that preserves the default hard-cost behavior.
        ///     保持默认硬费用行为的共享策略。
        /// </summary>
        public static SecondaryResourceInsufficientPayment BlockPlay { get; } = new();

        /// <summary>
        ///     Selected shortfall behavior.
        ///     选中的短缺行为。
        /// </summary>
        public SecondaryResourceInsufficientPaymentMode Mode { get; init; } =
            SecondaryResourceInsufficientPaymentMode.BlockPlay;

        /// <summary>
        ///     True to spend as much of the available resource as possible before reporting the remaining shortfall.
        ///     为 true 时先消耗可用资源，再把剩余部分报告为短缺额。
        /// </summary>
        public bool SpendAvailable { get; init; } = true;

        /// <summary>
        ///     Optional callback invoked once when the shortfall payment is committed.
        ///     短缺支付提交时调用一次的可选回调。
        /// </summary>
        public SecondaryResourceShortfallPaymentHandler? OnShortfall { get; init; }

        /// <summary>
        ///     Optional pure resolver that can cover the shortfall with another payment source before commit.
        ///     可选的纯解析器：在提交前判定能否用其他支付来源覆盖短缺。
        /// </summary>
        public SecondaryResourceShortfallResolver? ResolveShortfall { get; init; }

        /// <summary>
        ///     True when this policy allows a shortfall to pass card-play checks.
        ///     此策略允许短缺通过出牌检查时为 true。
        /// </summary>
        public bool AllowsPlay => Mode == SecondaryResourceInsufficientPaymentMode.AllowPlay;

        /// <summary>
        ///     Creates a policy that allows play when the required resource is short.
        ///     创建允许资源不足时打出卡牌的策略。
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
        ///     Creates a policy that allows play and runs a synchronous shortfall callback.
        ///     创建允许打出并运行同步短缺回调的策略。
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
        ///     Creates a policy that first tries to replace a shortfall with another payment source.
        ///     创建优先尝试用其他支付来源替代短缺的策略。
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
            return OnShortfall?.Invoke(context) ?? Task.CompletedTask;
        }

        internal SecondaryResourceShortfallResolution Resolve(SecondaryResourceShortfallResolutionContext context)
        {
            return ResolveShortfall?.Invoke(context) ?? SecondaryResourceShortfallResolution.None;
        }
    }

    /// <summary>
    ///     Persistence scope for a secondary combat resource.
    ///     次级战斗资源的持久化范围。
    /// </summary>
    public enum SecondaryResourcePersistencePolicy
    {
        /// <summary>
        ///     The resource is runtime-only and is not written to run saves.
        ///     该资源仅存在于运行时，不写入跑局存档。
        /// </summary>
        None = 0,

        /// <summary>
        ///     The resource should be restored while the current combat is restored.
        ///     Currently, normal run saves do not restore an in-progress combat, so this behaves mostly like
        ///     <see cref="None" /> unless a separate combat-state persistence path captures it.
        ///     该资源应随当前战斗恢复。
        ///     目前普通跑局存档不会恢复进行中的战斗；除非另有战斗状态持久化路径捕获它，否则它基本等同于
        ///     <see cref="None" />。
        /// </summary>
        Combat = 1,

        /// <summary>
        ///     The resource persists across combats for the current run.
        ///     该资源在当前跑局中跨战斗持久化。
        /// </summary>
        Run = 2,
    }

    /// <summary>
    ///     Built-in turn-start behavior for a secondary resource.
    ///     次级资源的内建回合开始行为。
    /// </summary>
    public enum SecondaryResourceTurnStartPolicy
    {
        /// <summary>
        ///     Leave the current amount unchanged.
        ///     保持当前数量不变。
        /// </summary>
        None = 0,

        /// <summary>
        ///     Set the current amount to the hook-modified max amount.
        ///     将当前数量设为经过 hook 修正后的最大数量。
        /// </summary>
        ResetToMax = 1,

        /// <summary>
        ///     Add the hook-modified max amount to the current amount.
        ///     将经过 hook 修正后的最大数量加到当前数量上。
        /// </summary>
        AddMaxToCurrent = 2,

        /// <summary>
        ///     Set the current amount to zero.
        ///     将当前数量设为零。
        /// </summary>
        Clear = 3,
    }

    /// <summary>
    ///     Reason attached to a secondary resource amount mutation.
    ///     附加在次级资源数量变更上的原因。
    /// </summary>
    public enum SecondaryResourceChangeReason
    {
        /// <summary>
        ///     Unspecified or custom reason.
        ///     未指定或自定义原因。
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     Resource amount increased.
        ///     资源数量增加。
        /// </summary>
        Gain = 1,

        /// <summary>
        ///     Resource amount decreased without payment semantics.
        ///     资源数量减少，但不带支付语义。
        /// </summary>
        Lose = 2,

        /// <summary>
        ///     Resource amount was assigned directly.
        ///     资源数量被直接赋值。
        /// </summary>
        Set = 3,

        /// <summary>
        ///     Resource amount was spent as payment.
        ///     资源数量作为支付被消耗。
        /// </summary>
        Spend = 4,

        /// <summary>
        ///     Resource amount was reset.
        ///     资源数量被重置。
        /// </summary>
        Reset = 5,

        /// <summary>
        ///     Resource amount changed from a turn-start policy.
        ///     资源数量因回合开始策略而改变。
        /// </summary>
        TurnStart = 6,
    }
}
