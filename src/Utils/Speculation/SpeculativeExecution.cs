using System.Reflection;
using System.Runtime.CompilerServices;

namespace STS2RitsuLib.Utils.Speculation
{
    /// <summary>
    ///     <para xml:lang="en">Severity of a speculative-execution diagnostic.</para>
    ///     <para xml:lang="zh-CN">推演执行诊断的严重程度。</para>
    /// </summary>
    public enum SpeculativeDiagnosticSeverity
    {
        /// <summary>
        ///     <para xml:lang="en">Informational execution detail.</para>
        ///     <para xml:lang="zh-CN">信息性执行详情。</para>
        /// </summary>
        Information,

        /// <summary>
        ///     <para xml:lang="en">Recoverable condition that may reduce precision.</para>
        ///     <para xml:lang="zh-CN">可能降低精度的可恢复状况。</para>
        /// </summary>
        Warning,

        /// <summary>
        ///     <para xml:lang="en">Condition that makes the result incomplete.</para>
        ///     <para xml:lang="zh-CN">导致结果不完整的错误状况。</para>
        /// </summary>
        Error,
    }

    /// <summary>
    ///     <para xml:lang="en">Limits applied to one speculative execution.</para>
    ///     <para xml:lang="zh-CN">单次推演执行使用的限制。</para>
    /// </summary>
    public sealed record SpeculativeExecutionBudget
    {
        /// <summary>
        ///     <para xml:lang="en">Maximum number of accounted operations.</para>
        ///     <para xml:lang="zh-CN">允许计入的最大操作数量。</para>
        /// </summary>
        public int MaxOperations { get; init; } = 100_000;

        /// <summary>
        ///     <para xml:lang="en">Maximum nested execution-frame depth.</para>
        ///     <para xml:lang="zh-CN">允许嵌套的最大执行帧深度。</para>
        /// </summary>
        public int MaxDepth { get; init; } = 128;
    }

    /// <summary>
    ///     <para xml:lang="en">One diagnostic emitted during speculative execution.</para>
    ///     <para xml:lang="zh-CN">推演执行期间产生的一条诊断。</para>
    /// </summary>
    public sealed record SpeculativeDiagnostic(
        SpeculativeDiagnosticSeverity Severity,
        string Code,
        string Message,
        MethodBase? Method = null,
        int? InstructionIndex = null);

    /// <summary>
    ///     <para xml:lang="en">One ordered terminal effect recorded by a speculative execution.</para>
    ///     <para xml:lang="zh-CN">推演执行记录的一条有序终端效果。</para>
    /// </summary>
    public sealed record SpeculativeEffect(
        int Sequence,
        string Kind,
        object? Source,
        object? Payload);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Async-flowing execution context with a shadow-state overlay, ordered effect journal, and
    ///         explicit operation and frame-depth budgets.
    ///     </para>
    ///     <para xml:lang="zh-CN">随异步流传播的执行上下文，包含影子状态覆盖、有序效果日志，以及显式的操作和执行帧深度预算。</para>
    /// </summary>
    public sealed class SpeculativeExecutionSession
    {
        private static readonly AsyncLocal<AmbientNode?> Ambient = new();

        private readonly List<SpeculativeDiagnostic> _diagnostics = [];
        private readonly List<SpeculativeEffect> _effects = [];
        private readonly Stack<object?> _sources = [];
        private readonly Dictionary<StateKey, object?> _state = new(StateKeyComparer.Instance);
        private int _depth;
        private bool _depthBudgetReported;
        private bool _operationBudgetReported;

        /// <summary>
        ///     <para xml:lang="en">Creates an isolated speculative-execution session.</para>
        ///     <para xml:lang="zh-CN">创建一个独立的推演执行会话。</para>
        /// </summary>
        public SpeculativeExecutionSession(SpeculativeExecutionBudget? budget = null)
        {
            Budget = budget ?? new();
            ArgumentOutOfRangeException.ThrowIfLessThan(Budget.MaxOperations, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(Budget.MaxDepth, 1);
        }

        /// <summary>
        ///     <para xml:lang="en">Session currently active in this async control flow, if any.</para>
        ///     <para xml:lang="zh-CN">当前异步控制流中处于活动状态的会话；没有时为 null。</para>
        /// </summary>
        public static SpeculativeExecutionSession? Current => Ambient.Value?.Session;

        /// <summary>
        ///     <para xml:lang="en">Limits applied to this session.</para>
        ///     <para xml:lang="zh-CN">此会话使用的限制。</para>
        /// </summary>
        public SpeculativeExecutionBudget Budget { get; }

        /// <summary>
        ///     <para xml:lang="en">Ordered effects recorded by this session.</para>
        ///     <para xml:lang="zh-CN">此会话记录的有序效果。</para>
        /// </summary>
        public IReadOnlyList<SpeculativeEffect> Effects => _effects;

        /// <summary>
        ///     <para xml:lang="en">Diagnostics recorded by this session.</para>
        ///     <para xml:lang="zh-CN">此会话记录的诊断。</para>
        /// </summary>
        public IReadOnlyList<SpeculativeDiagnostic> Diagnostics => _diagnostics;

        /// <summary>
        ///     <para xml:lang="en">Number of operations consumed so far.</para>
        ///     <para xml:lang="zh-CN">当前已消耗的操作数量。</para>
        /// </summary>
        public int Operations { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">True when no error diagnostic was recorded.</para>
        ///     <para xml:lang="zh-CN">未记录错误诊断时为 true。</para>
        /// </summary>
        public bool IsComplete =>
            _diagnostics.All(static diagnostic =>
                diagnostic.Severity != SpeculativeDiagnosticSeverity.Error);

        /// <summary>
        ///     <para xml:lang="en">Makes this session current until the returned scope is disposed.</para>
        ///     <para xml:lang="zh-CN">在返回的作用域释放前将此会话设为当前会话。</para>
        /// </summary>
        public IDisposable Enter()
        {
            var previous = Ambient.Value;
            var node = new AmbientNode(this, previous);
            Ambient.Value = node;
            return new ScopeLease(() =>
            {
                if (!ReferenceEquals(Ambient.Value, node))
                    throw new InvalidOperationException(
                        "Speculative execution scopes must be disposed in reverse order.");
                Ambient.Value = previous;
            });
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to enter one nested execution frame.</para>
        ///     <para xml:lang="zh-CN">尝试进入一个嵌套执行帧。</para>
        /// </summary>
        /// <param name="method">
        ///     <para xml:lang="en">Method represented by the frame, when known.</para>
        ///     <para xml:lang="zh-CN">此执行帧所表示的方法（如已知）。</para>
        /// </param>
        /// <param name="scope">
        ///     <para xml:lang="en">Scope that leaves the frame when disposed.</para>
        ///     <para xml:lang="zh-CN">释放时离开执行帧的作用域。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">True when both operation and depth budgets permit the frame.</para>
        ///     <para xml:lang="zh-CN">操作预算与深度预算均允许此执行帧时为 true。</para>
        /// </returns>
        public bool TryEnterFrame(MethodBase? method, out IDisposable scope)
        {
            if (!TryConsumeOperation(method))
            {
                scope = ScopeLease.Empty;
                return false;
            }

            if (_depth >= Budget.MaxDepth)
            {
                if (!_depthBudgetReported)
                {
                    _depthBudgetReported = true;
                    AddDiagnostic(new(
                        SpeculativeDiagnosticSeverity.Error,
                        "depth_budget_exceeded",
                        $"Execution depth budget {Budget.MaxDepth} was exceeded.",
                        method));
                }

                scope = ScopeLease.Empty;
                return false;
            }

            _depth++;
            scope = new ScopeLease(() => _depth--);
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Accounts one operation and reports whether execution may continue.</para>
        ///     <para xml:lang="zh-CN">计入一个操作并报告执行是否可以继续。</para>
        /// </summary>
        public bool TryConsumeOperation(MethodBase? method = null, int? instructionIndex = null)
        {
            if (Operations >= Budget.MaxOperations)
            {
                if (_operationBudgetReported) return false;
                _operationBudgetReported = true;
                AddDiagnostic(new(
                    SpeculativeDiagnosticSeverity.Error,
                    "operation_budget_exceeded",
                    $"Operation budget {Budget.MaxOperations} was exceeded.",
                    method,
                    instructionIndex));

                return false;
            }

            Operations++;
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Pushes a causal source used by effects that do not supply an explicit source.</para>
        ///     <para xml:lang="zh-CN">压入因果来源，供未显式指定来源的效果使用。</para>
        /// </summary>
        public IDisposable PushSource(object? source)
        {
            _sources.Push(source);
            return new ScopeLease(() => _sources.Pop());
        }

        /// <summary>
        ///     <para xml:lang="en">Appends one terminal effect to the ordered journal.</para>
        ///     <para xml:lang="zh-CN">向有序日志追加一条终端效果。</para>
        /// </summary>
        public SpeculativeEffect RecordEffect(
            string kind,
            object? payload = null,
            object? source = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(kind);
            var effect = new SpeculativeEffect(
                _effects.Count,
                kind,
                source ?? (_sources.TryPeek(out var ambientSource) ? ambientSource : null),
                payload);
            _effects.Add(effect);
            return effect;
        }

        /// <summary>
        ///     <para xml:lang="en">Appends a diagnostic.</para>
        ///     <para xml:lang="zh-CN">追加一条诊断。</para>
        /// </summary>
        public void AddDiagnostic(SpeculativeDiagnostic diagnostic)
        {
            ArgumentNullException.ThrowIfNull(diagnostic);
            _diagnostics.Add(diagnostic);
        }

        /// <summary>
        ///     <para xml:lang="en">Reads an overlaid value or obtains and caches its original value.</para>
        ///     <para xml:lang="zh-CN">读取覆盖值；不存在时读取并缓存原始值。</para>
        /// </summary>
        public T GetState<T>(
            object target,
            object slot,
            Func<T> readOriginal)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(slot);
            ArgumentNullException.ThrowIfNull(readOriginal);

            var key = new StateKey(target, slot);
            if (_state.TryGetValue(key, out var value))
                return CastState<T>(value, slot);

            var original = readOriginal();
            _state.Add(key, original);
            return original;
        }

        /// <summary>
        ///     <para xml:lang="en">Writes a value to the session overlay without modifying the target object.</para>
        ///     <para xml:lang="zh-CN">将值写入会话覆盖层而不修改目标对象。</para>
        /// </summary>
        public void SetState<T>(object target, object slot, T value)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(slot);
            _state[new(target, slot)] = value;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns whether this session contains an overlaid value.</para>
        ///     <para xml:lang="zh-CN">返回此会话是否包含指定覆盖值。</para>
        /// </summary>
        public bool HasState(object target, object slot)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(slot);
            return _state.ContainsKey(new(target, slot));
        }

        private static T CastState<T>(object? value, object slot)
        {
            return value switch
            {
                T typed => typed,
                null when default(T) == null => default!,
                _ => throw new InvalidCastException(
                    $"Shadow slot '{slot}' contains {value?.GetType().FullName ?? "null"}, not {typeof(T).FullName}."),
            };
        }

        private sealed record AmbientNode(
            SpeculativeExecutionSession Session,
            AmbientNode? Previous);

        private readonly record struct StateKey(object Target, object Slot);

        private sealed class StateKeyComparer : IEqualityComparer<StateKey>
        {
            public static StateKeyComparer Instance { get; } = new();

            public bool Equals(StateKey x, StateKey y)
            {
                return ReferenceEquals(x.Target, y.Target) &&
                       Equals(x.Slot, y.Slot);
            }

            public int GetHashCode(StateKey obj)
            {
                return HashCode.Combine(
                    RuntimeHelpers.GetHashCode(obj.Target),
                    obj.Slot);
            }
        }

        private sealed class ScopeLease(Action? dispose) : IDisposable
        {
            private Action? _dispose = dispose;

            public static ScopeLease Empty { get; } = new(null);

            public void Dispose()
            {
                Interlocked.Exchange(ref _dispose, null)?.Invoke();
            }
        }
    }
}
